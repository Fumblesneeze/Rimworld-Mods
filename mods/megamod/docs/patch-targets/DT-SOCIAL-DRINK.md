# DT-SOCIAL-DRINK: filter native social drink candidates

Owner `fumblesneeze.megamod`, S138. Owning contract: social drinks obey the actual recipient's
tolerance choice without direct-joy exceptions, while ordinary drinkless social activity remains.
Native target: private static bool `JoyGiver_SocialRelax.TryFindIngestibleToNurse(IntVec3, Pawn,
out Thing)`, with its private static readonly `List<ThingDef> nurseableDrugs` working collection.
Observed 1.7.5005 native caller first creates the social job and only assigns its optional drink
when this selector succeeds; no owned patch changes that social-job caller or final ingestion.

Replace the unique consumed append to this working collection with an owned forwarding append.
It adds the actual drug only when the current actual recipient passes the owned rule. Other
native candidates continue through native shuffle/reachability/reservation selection, so a rejected
first drug does not hide an eligible alternative. With no candidates the ordinary native selector
returns no drink and the already eligible social job remains. No manual, medical or refill path
uses this hook, and no joy exception is introduced here.

Admission resolves the exact static bool/value-center/reference-pawn/out-reference callable and
readonly generic List field. Incoming IL must have one append and a dominating clear of this
specific list. Every access to the consumed list must flow directly to its known Clear/Add/Count/
indexer or native shuffle consumer; an unproved alias, address escape, replacement or alternate
append path is unavailable. A recipient-address read consumed immediately by ldind.ref/ldobj
is supported; storing or passing that address remains an unproved escape. Branches may enter the start of an intact receiver chain but cannot
enter its middle, including through labeled no-ops. The pawn argument cannot be reassigned or
address-escaped before the forwarding append. Other lists and unrelated edits remain permitted.
Original code/hash/version is not the predicate. The actual incoming stream is validated every
recomposition and returned unchanged if unproved. Protected operations are checked against final
Harmony inner effects with the shared publication reader; foreign transpilers are never replayed.

The forwarding callback looks up the exact initialized native working-list object; arbitrary
replacement makes the route unavailable. Publication identity and immutable admitted receipts
gate ordinary calls without rebuilding metadata. Compatible repatches re-admit; missing owned
callbacks, changed relevant semantics, output-replacing outer callbacks (including by-reference
bool results and non-void completion handlers) and skipped-original
selection are unavailable. Only the owned patch slots are rolled back. One bounded reason belongs
to the atomic enforcement/editor group, with native selection fallback and retained policy choices.

Output effects use the inspected provider's effective argument binding, not just source parameter
names. Read CustomAttributeData for default HarmonyArgument parameter, method and inherited-type
aliases and original-argument indexes; no foreign attribute constructors execute during admission.
Aliased result/out-drink/args-array mutations remain unavailable; an aliased value-only result
observer remains compatible. Unknown attribute identities, shapes, binding modes or excessive
metadata are unavailable instead of being interpreted as an observer. This is a feature-local
proof for this selector's completion effects, not a general provider-binding API.
Numeric argument names are parsed after alias resolution with the provider's ordinal double-
underscore prefix, integer parse and native argument bounds. Thus direct or aliased `__02`
cannot bypass the same out-drink protection as `__2`.

This candidate is never registered alone: DT-SCHEDULED, DT-DIRECT-JOY, native contextual controls,
persistence CanUseChoices and common effective enablement form the full group. Tests must execute
the patched candidate selector, including multiple drugs, all-blocked drinkless result, native
rejection, bypass/alias/branch edits, compatible observers and cleanup. Root must observe real
social activity with and without a drink on the reviewed native build before acceptance.
