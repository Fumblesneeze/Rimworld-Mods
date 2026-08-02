## ADDED Requirements
**Owning mod:** RimWorld Dev Gateway (`fumblesneeze.rimworlddevgateway`) at `mods/RimWorldDevGateway`.

### Requirement: Finalized Def database export
`POST /api/v1/defs/export` SHALL inspect RimWorld's real post-load `DefDatabase<T>` instances on the Unity main thread and return deterministic JSON snapshots. The request SHALL accept exact Def-type names, exact Def names, and exact source package IDs as optional intersection filters, an optional bounded list of exact top-level `fieldNames`, a versioned exclusive cursor, and a bounded page size. `fieldNames` SHALL select only matching projectable root fields and SHALL report requested missing/non-projectable fields as bounded warnings, subject to the per-Def warning cap and its explicit `warning_limit` marker. Filter counts, filter-value lengths, selector counts/lengths, cursor length, database-type candidates examined, per-database candidate scans, and the aggregate candidate scan across the request SHALL have endpoint-specific fixed bounds; database-type discovery SHALL stop after 1,024 candidates regardless of whether each candidate is accepted, and the aggregate Def scan SHALL stop at 100,000 candidates. Results SHALL sort by fully qualified database type and ordinal Def name so the same active mod list produces stable pages. The source SHALL use direct named lookup when exact Def names are supplied, poll request cancellation during bounded scans, and surface a bounded source warning or explicit failure rather than silently presenting a failed database as an empty successful result.

Def-export request JSON SHALL use the Gateway's general 32 MiB transport request-body ceiling and SHALL NOT impose a lower endpoint aggregate byte cap. Requests remain bounded by the endpoint's independent schema cardinality, value-length, cursor, and page-size limits before game work is dispatched.

Paging identity SHALL use a strict canonical `v1.<base64(UTF-8 database type)>.<base64(UTF-8 defName)>` cursor with two independently canonical segments. It SHALL round-trip newlines, CJK text, and valid surrogate pairs without separator ambiguity; non-canonical Base64, malformed/overlong UTF-8, unpaired surrogates, unknown versions, and legacy combined cursors SHALL be rejected. The cursor bound SHALL be derived from the two accepted 512-character/2,048-byte identity segments. A live Def whose database type or `defName` cannot be represented reversibly under those bounds SHALL be omitted with `def_identity_not_reversible` rather than yielding an unusable continuation cursor.

Each result SHALL identify the database type, concrete runtime type, `defName`, label, source package ID/name when retained by RimWorld, source file when retained, and a bounded field projection. The response SHALL identify its serializer as a diagnostic finalized-object snapshot and SHALL state that it is not canonical source XML.

#### Scenario: Export one patched Def
- **WHEN** an authenticated caller filters by a concrete Def type and Def name after RimWorld has applied XML inheritance and PatchOperations
- **THEN** the result is read from the live finalized Def database and contains the resulting field values rather than reparsing an individual mod's source XML

#### Scenario: Export Defs from one optional mod
- **WHEN** the caller filters by an active source package ID
- **THEN** the response contains only matching finalized Defs whose retained `modContentPack` has that exact package ID and includes enough type/name metadata to author a guarded patch

#### Scenario: Continue a truncated export
- **WHEN** more matching Defs exist than the requested page size
- **THEN** the response returns the stable ascending prefix, reports truncation and an exclusive continuation cursor, and performs at most one bounded source scan needed to establish that prefix; page-count candidates beyond the prefix are not projected, while exact byte sizing may project at most the next boundary candidate before omitting it

#### Scenario: Select patch-relevant fields
- **WHEN** a caller requests exact top-level fields such as `label`, `statBases`, and `modExtensions`
- **THEN** those projectable fields are returned without unrelated root fields and any requested missing field produces a `requested_field_not_found` warning

#### Scenario: A schema-valid request exceeds the legacy endpoint cap
- **WHEN** a Def-export request is larger than 65,536 UTF-8 bytes but remains within the 32 MiB transport body ceiling and every schema cardinality and value-length bound
- **THEN** the request is deserialized and dispatched normally rather than rejected by a lower endpoint aggregate byte cap

#### Scenario: Serialized page reaches its byte budget
- **WHEN** adding the next bounded Def would make the complete success envelope exceed 32 MiB of serialized UTF-8
- **THEN** that Def is omitted, the prior stable item remains the exclusive continuation position, `Truncated` is true, and the next request resumes with the omitted Def rather than failing or collapsing the page

#### Scenario: Serialized page reaches its node budget
- **WHEN** adding the next bounded Def would make the complete success envelope exceed 500,000 serialized JSON nodes while remaining below its byte budget
- **THEN** that Def is omitted under the same stable exclusive-cursor contract rather than allowing final envelope serialization to fail

### Requirement: Bounded graph-safe field projection
The exporter SHALL read instance fields rather than invoking arbitrary property getters. It SHALL skip static fields and RimWorld `[Unsaved]` fields, treat every nested `Def` as a typed `defName` reference, detect repeated object references, and enforce fixed field, total reflected-member work, reflected-hierarchy, depth, collection, string/path/type, per-Def node, per-field byte, per-Def byte, page-count, source-scan, response-node, and serialized-page bounds. The policy limits SHALL be 512 projectable fields, 4,096 reflected members examined before any static/`[Unsaved]`/duplicate exclusion, 1,024 collection entries, 65,536 characters per projected runtime string, 1 MiB for each complete projected top-level field key/value entry, 8 MiB for each complete Def item including metadata and warnings, 500,000 serialized JSON nodes for the complete success envelope, and 32 MiB for that envelope's serialized UTF-8. These are Gateway safety/payload policy values, not EmbedIO, Zlepper, Unity, or RimWorld serialization limits.

An individually oversized field SHALL remain named with a limit marker; an aggregate overflow SHALL retain a deterministic sorted field prefix plus a limit marker rather than replacing all useful fields. Byte checks SHALL use the actual JSON writer so escaped control characters, CJK, valid surrogate pairs, and unpaired surrogate code units are charged by their serialized UTF-8 representation. The exporter SHALL enumerate only arrays and explicitly recognized materialized BCL collection shapes; an arbitrary mod-defined `IEnumerable`/`IDictionary` SHALL become a bounded unsupported-container marker because exception handling or cancellation cannot preempt a blocking custom iterator on Unity's thread. Dictionary entry work SHALL count examined entries independently of normalized-key uniqueness and SHALL preserve or mark key collisions. A broken field, supported collection element, or modded object SHALL produce a bounded warning or placeholder for that Def without aborting healthy results in the page. Request cancellation SHALL be polled and propagated throughout field and supported collection projection. An `OperationCanceledException` SHALL propagate only when the active request token is cancelled; otherwise it SHALL be isolated like another mod failure. Diagnostic error formatting SHALL not invoke an arbitrary exception's virtual `Message`/`ToString`, and type formatting SHALL invoke `FullName`/`Name` only for the known CLR/Mono runtime `Type` implementation. Network threads SHALL receive only detached values.

`RuntimeType.GetFields` returns one declared-member array in a synchronous reflection call that cannot be preempted partway by managed cancellation. Total member accounting and cancellation SHALL apply before exclusions and to every returned member immediately after that call; this residual single-call non-preemptibility SHALL be documented rather than hidden.

#### Scenario: A Def graph contains cycles and cross-Def references
- **WHEN** a finalized Def points to another Def and contains a cycle through nested runtime objects
- **THEN** the export emits a compact typed reference for the other Def and a cycle marker for the repeated object rather than recursively walking the live graph

#### Scenario: One modded field throws while enumerated
- **WHEN** one matching Def contains a collection or reflected field that cannot be read safely
- **THEN** that failure is isolated and reported for the affected Def while later matching Defs remain exportable

#### Scenario: Excluded reflected members consume work
- **WHEN** a modded type exposes more than 4,096 reflected members that are static, `[Unsaved]`, duplicate, or otherwise excluded
- **THEN** discovery stops with `reflected_member_work_limit` before inspecting later members rather than allowing exclusions to bypass the Unity-thread work bound

#### Scenario: Mod exception mimics cancellation
- **WHEN** a mod member throws `OperationCanceledException` while the request token remains active
- **THEN** the exporter emits a safe bounded failure marker without reading virtual exception text and continues with healthy results

### Requirement: Honest XML serialization boundary
Version one SHALL support JSON only. It SHALL NOT expose `Verse.DirectXmlSaver` output as canonical patched XML: RimWorld discards the unified patched XML document after loading, while `DirectXmlSaver` reconstructs public and non-public runtime fields, omits provenance fields marked `[Unsaved]`, expands unsafe object graphs without cycle or size bounds, and does not reproduce the original inherited or patched document. An unsupported XML format request SHALL fail explicitly without dispatching an unbounded native serialization.

#### Scenario: Caller asks for native XML
- **WHEN** an export request selects an XML format
- **THEN** the gateway returns a stable `unsupported_def_export_format` error explaining that the bounded JSON snapshot is available and does not claim a lossy reconstruction is RimWorld's canonical patched XML
