# S068 — All Memories Fade research

Owner: **Make Rimworld Great Again**, `fumblesneeze.megamod`, `mods/megamod`; **1.7 beta**.
Read-only inspection **2026-11-04**. Origin: replacement with documented corrections and extensions.
[Change](../../../../openspec/changes/specify-megamod-memory-fading/proposal.md). No implementation.

## Primary documentation and identity

Mlie's [All Memories Fade](https://steamcommunity.com/sharedfiles/filedetails/?id=2800155563),
package `Mlie.AllMemoriesFade`, installed at `F:/Steam/steamapps/workshop/content/294100/2800155563`.
Steam description was read before decompilation: it describes gradual good/bad memory mood changes,
strength and duration multipliers, and duration-based inclusion. The advertised performance benefit
is an author claim, not megamod evidence. The public page also contains a counselling complaint;
that report is not treated as a reproduced bug or proof of endless duration.

[Author repository](https://github.com/emipa606/AllMemoriesFade) and installed README, About,
Manifest, changelog, MIT license and English settings text were read. Public Source-tree access
failed; installed 1.7 binary is authoritative for this audit. About/Manifest/version metadata say
**1.7.0**, supported 1.3–1.7; assembly 1.7.0.0, informational revision
`47c17a8e5a467e4fa771f1d560224eeb072d9c00`. No LoadFolders.xml; separate version folders exist.
The changelog records the 1.7 update and earlier additions of strength, range and duration settings.

License is MIT, copyright Mlie; a substantial source reuse would need its notice. This phase copies
no code, assemblies or art into product files. AI_POLICY.md is third-party material, not repository
instructions. Installed translations: English, ChineseSimplified, French, German, Russian; megamod
still requires all seven baseline languages. VersionFromManifest is a source UI dependency, not
selected for our package. Source About declares no required external mod or load order.

| Input | SHA-256 |
| --- | --- |
| 1.7/Assemblies/AllMemoriesFade.dll | `5BC1D7BA4E223B529BC06DF5AC315F4313A95540D4692A493DCA75F47CE139F3` |
| About/About.xml | `0CBBA8CC439FBCE870DFCE1A5A846B68D2F6158534537D7A445815D3B081D8A7` |
| About/Manifest.xml | `FA3CC52EFCF2B53A49378F0DE62F9E92C0395FF595A96612D25F405A11FB4470` |
| Current 1.7.4998 rev1299 Assembly-CSharp.dll | `FE9B615CF5918955AB750F9D627FB0082FD23C556BDFDEE410F88857C67A081B` |

Typed installation inspection confirmed the current game; operation registry has live decompilation
but no offline equivalent. ILSpy inspected files without loading/executing source assemblies.
Complete decompilation, including assembly metadata, is under
`artifacts/Refinement/megamod-spec-20261104/all-memories-fade/upstream/`; selected native types under
the sibling `native/` directory. Every non-generated source class was read in full.

## Complete source behavior

| Source surface | Inspected behavior |
| --- | --- |
| AllMemoriesFade static startup | Collect every ThoughtDef with durationDays strictly greater than 0.1; capture stage mood, duration and existing fade flag; apply fade, duration, then strength |
| Inclusion | Inclusive min/max comparison against each definition's **current** duration, sorted by that value; not against the saved original duration |
| Fading | Set lerpMoodToZero for included definitions; restore captured flag for excluded definitions; no own Harmony patches/ticker |
| Strength | Multiply captured stage baseMoodEffect for included definitions; restore excluded values; source uses −1 as a missing-stage sentinel |
| Duration | Multiply captured duration for included definitions, restore excluded values; refresh maximum UI duration to 110% of current longest definition |
| Settings | Global MemoryMoodOffset=1, MemoryLengthOffset=1, DurationRange=[0,float.MaxValue]; each multiplier slider 0–2 |
| UI timing | Length slider immediately updates lengths; ordinary WriteSettings updates strength and length but **not** fade flags; startup/Reset refresh fades |
| Reset | Reset range/strength/length, recompute lengths twice, set upper range to the current maximum and refresh all three behaviors |
| UI previews | Fixed numerical examples; affected-definition count and shortest/longest internal names; installed version footer |

Public methods are UpdateLerpValues, UpdateLengthValues, UpdateEffectValues and
GetCurrentAffectedThoughts; MaxDuration is public. Settings/mod classes are internal, with the three
saved keys above. No per-save data or own thought classes. No gameplay DLL beyond these three classes
and compiler-generated helpers; no source XML behavior patches, extra menu or bottom tab.

### Source-derived corrections, not live reproductions

- A 0.1-day memory is excluded despite the initial range suggesting all durations. Propose including
  all positive supported timed memories; the range can explicitly exclude short ones.
- Duration changes can move definitions across their own inclusion bounds. Later update passes can
  undo a duration change or apply strength/fading to a different set. Use unmodified baseline length.
- A real stage mood of −1 is skipped as if it were a missing stage. Do not use a gameplay value as a
  missing-data marker. Null stage/list handling also needs per-entry safety, not whole-mod failure.
- Range edits do not refresh fading on ordinary settings write. Commit effective controls together,
  with existing memories reevaluated on their next ordinary update and no age reset.
- Zero duration can reach native division by zero. Define zero lifetime as zero current contribution
  and native expiry at the next maintenance step; no reversed-sign after-expiry effect or NaN.
- Reusing source global Def mutations would leak save-specific settings and overwrite later provider
  changes. Use contextual calculations instead of restoring a once-captured whole-Def snapshot.
- Hardcoded example values are not reliable current-content previews. In inspected Core,
  KilledMyMother is an opinion-only social memory (−80 opinion, no base mood); do not advertise it
  as a −80 mood change. Preview current actual definitions and distinguish opinion from mood.

## Native memory semantics and comparators

Read Thought_Memory, selected ThoughtDef and Thought mood/duration sections, Thought_MemorySocial,
MemoryThoughtHandler lifecycle/acquisition, Thought_Counselled and CompAbilityEffect_Counsel.
Core ThoughtDefs and Ideology counselling definitions were inspected as XML, not loaded databases.

Thought_Memory derives duration from an explicit per-instance override when present, otherwise from
the definition. Age increases through ordinary 150-tick thought intervals; renewal resets age.
Normal discard is age greater than duration unless permanent. Native fade is
`1 - age / duration`, without clamping. It follows stage/stat/worker mood calculation, instance
power multiplication and additive mood offset. Nullification returns zero. Therefore apply one
fade to the resulting memory contribution; base-strength scaling does not multiply independent
instance additive adjustments. Do not double-fade a memory already using native fading.

MemoryThoughtHandler preserves grouping, stack limits, replacement, expiry and nextThought chains.
Expiry happens through the normal maintenance path. A configurable shorter lifetime can expire an
existing memory; increasing length later must not resurrect removed memories or replay successors.
Thought_MemorySocial has its own opinion interpolation and discard rules. The source does not change
opinion magnitude/curve, but its duration mutation changes the clock for social memories too. Retain
that distinction visibly; do not reinterpret mood-strength percentage as opinion-strength percentage.

| Inspected Core comparator | Base duration | Base effect | Implication |
| --- | --- | --- | --- |
| AteWithoutTable | 1 day | −3 mood | Simple ordinary negative example; nullifying traits/precepts remain |
| SoakingWet | 0.1 day | −3 mood | Concrete short memory omitted by source's strict cutoff |
| GotMarried | 30 days | +40 mood | Positive memories fade too; no assumption that this is always easier |
| BrokeUpWithMe | 60 days | −50 opinion, no base mood | Social lifetime versus mood must remain distinct |
| DivorcedMe | 180 days | −70 opinion, no base mood | Do not confuse relationship and mood definitions with similar labels |
| KilledMyMother | 120 days | −80 opinion, no base mood | Source hardcoded mood preview is unsuitable |

Values are inspected unpatched comparators, not hardcoded final gameplay balance or a complete
active-content audit. No game was launched and no native mood outcome was accepted.

### Counselling: explicit lifetime and captured effect

Successful Ideology counselling captures the target negative memory's **current** negative mood as
a positive amount and its remaining lifetime as an explicit duration. Thought_Counselled returns
that captured amount directly; it bypasses ordinary mood/fade calculation. Its Def has no positive
duration and is excluded by the source's collector. This supports a specific static finding: while
the negative fades, the compensating amount can stay constant. It does **not** prove the reported
bonus never expires; native age/discard still uses its finite override.

Proposed extension: treat supported timed counselling as an included positive timed effect,
selected by its own captured lifetime. Fade its captured amount over that lifetime without applying
strength/duration multipliers again. With unchanged settings and both memories included, this
maintains their initial compensation as they fade. Inclusion changes, source-memory renewal/removal
and later settings changes do not dynamically recalculate that native captured award; no linked
memory tracking, retroactive recounselling or new entitlement is introduced. Normal fallback
Counselled_MoodBoost and failed-counsel memories follow the ordinary policy. Permanent/nonpositive
and unsupported custom effects retain provider behavior. Ideology remains optional.

Independent native review additionally inspected CompProperties_AbilityCounsel: its current default
minMoodOffset is **−10**, and selection uses current mood at or below that threshold. A remaining
−9 memory therefore takes the ordinary fallback boost despite still being negative. This is a
native comparator, not a new fixed megamod eligibility rule.

The review found a P2 presentation gap in the initial draft: the native fallback success message
reads the raw stage baseMoodEffect. Contextual 200% scaling of its +8 base would grant +16 while
the unchanged message says +8. The source's global mutation does not have that particular discrepancy.
The corrected contract covers actual award/message agreement, a guarded action-local presentation
bridge, and native fallback for the affected boost content if that bridge cannot be admitted. The
review correction does not authorize global translation interception or shared Def mutation.

## UI, settings, contracts and composition

No source bottom tab. Configure in **Mood / Memories**, ordered base mood strength, ordinary duration,
then included baseline-duration range. Defaults: **100%, 100%, [0 days, Unlimited]**; percentages
**0–200%**, range inclusive, nonnegative and ordered. These are configuration, not per-pawn choices.
Use common global/save/Use global rows, dormant override retention and disabled main-menu save
callbacks. No new on/off toggle is invented; excluded memories retain their normal behavior.
A finite empty selection is valid and clearly says none; unlimited upper bound is explicit rather
than a slider maximum which shrinks as a duration changes. Invalid input leaves the last valid state.

Apply committed policy to existing and future supported memories without changing elapsed age.
No restart required for ordinary controls; shorten-to-expired takes effect through native removal.
Preview current named memory types, actual base/effective duration/mood and inclusion; counts are
types, not pawns/memory instances. Instance-timed effects are identified separately, not assigned
fictional fixed Def durations. Missing content, no matches and long labels must be safe/readable.

Scoped installed XML/CS/JSON search found the source About, RimPy package catalog and YART's seven
LayoutHarness example mod lists. These are discovery/test-fixture identities, not runtime consumers
of the public API. No actual runtime API consumer found in that scope; no all-binary audit claimed.
Do not impersonate source identity or import source settings. Native memory/save identity stays.

When the original is active, select it as provider and leave owned fading/scaling inactive. Explain
that its global behavior cannot satisfy megamod per-save controls; keep owned values dormant and
uneditable while unavailable. Never run both multipliers, mutate its settings or revert its Defs.
Other compatible providers compose via actual supported semantics, not owner-presence rejection.

S001 mood bar/S003 need indicators and ordinary Needs displays consume actual current values;
S037 social fights and other thought-producing features retain acquisition rules. S038 Cherry
Picker blocks new acquisition only under its applied policy; existing memories remain and continue
fading. Their types remain in the fading catalog/count with a distinct acquisition status. Requested
content changes stay pending until S038's save/reload boundary; genuinely missing Defs are different.
This corrects the initial draft's overly broad absence-from-preview claim. No runtime dependency.
S124 Drug Response
is queued: no clinical/drug/withdrawal-rule changes are inferred from the author's broad example.
No per-map Thing index, private pawn scan, copied memory store or new periodic ticker. Use native
thought collections and common configuration/catalog data; measure combined costs before claiming
an optimization. Seven-language settings and explanations remain future implementation.

## Open questions and semantic pass

| ID | Remaining work / selected draft direction |
| --- | --- |
| MF-Q1 | Proposed short-memory inclusion and counselling extension; retain 100%/100%/all durations and native captured awards, no doubled opinion/mood transformations |
| MF-Q2 | Exact effective runtime admission for baseline mood, one fade, duration, zero expiry and counselling; individual target documents and coherent atomic groups before code |
| MF-Q3 | Custom dynamic memory classes, explicit overrides, original/provider coexistence and final active-content preview coverage; do not claim every mod's arbitrary subclass is supported |
| MF-Q4 | Common save/global transitions, native renewal/expiry/successor workflows, languages, cost measurements and personally observed native acceptance |

At default strength/lifetime, a constant raw mood effect fading linearly has half the total signed
impact over its life compared with an unfaded constant effect; both positive and negative effects
change. Existing native fades are not applied twice. Duration scaling also changes social timing;
zero strength does not erase independent additive effects/opinions, and zero duration can remove
memories sooner. These are explicit source-derived settings, not an unqualified stability/easier-game
claim. Counselling is an acknowledged new correction, with no free extra captured strength/lifetime.
Finalized active-content balance/playability and real outcomes remain unverified.

## Review checkpoint

The draft contains **8 requirements, 30 observable scenarios and 25 unchecked tasks**. Independent
native review found one P2 presentation gap, resolved with actual counselling award/message coverage;
it also clarified the native qualifying threshold. Independent composition review found the two
P2 gaps below; a focused re-review confirmed both resolved and found no remaining P1/P2 issues.

Typed strict OpenSpec validation passed **90 items, zero failures**, exit code 0. Its separate
PostHog telemetry transport failure did not affect schema success. All **12 scoped Markdown
documents** passed local-link and whitespace checks. Queue: **69 reviewed drafts / 136 entries /
294 unique question IDs**. MF-Q1–4 remain open; no runtime compatibility, performance or native
acceptance is claimed. The S038 clarification preserves all unchecked implementation/acceptance tasks.

No runtime code/Defs/assets, tests, build/install, game launch, upstream execution, Workshop edits or
normal save/configuration mutation occurred. **Next: S069 Auto-Cut Blight - 1.6.**

The composition review found two P2 gaps, now specified: retained memories/catalogs must distinguish
applied versus pending acquisition exclusions, and native counselling can announce a bonus even when
TryGainMemory refused it. Combined owned behavior now reports the actual refusal for both captured
and fallback awards. If truthful reporting is unsupported, the affected action is refused before
cost/cooldown through an admitted boundary; if that refusal cannot be admitted either, the coupled
exclusion is unavailable at policy application rather than falsely advertised as applied. Requested
choices remain. S038's spec/design/tasks/coverage are updated with the same composed contract.
