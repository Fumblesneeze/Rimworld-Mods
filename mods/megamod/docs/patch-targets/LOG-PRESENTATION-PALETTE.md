# LOG-PRESENTATION-PALETTE: native severity palette

Owner `fumblesneeze.megamod`, S035.
**Candidate target only; no patch, validator, setting registration or native acceptance delivered.**
Owning [observable contract](../../../../openspec/changes/specify-megamod-debug-log/specs/megamod-debug-log/spec.md):
severity colors alter presentation, keep original text/trace and copy intact, and apply on redraw.

## Target and minimal conditions

Native `Assembly-CSharp`, public instance non-generic getter
`UnityEngine.Color Verse.LogMessage.get_Color()`, plus the exact instance field
`Verse.LogMessageType Verse.LogMessage.type` required to choose the effective configured color.
Current native getter selects white/yellow/LogError. A postfix result projection can use severity
and C02's current effective palette; it need not execute the original body to derive its result.
There is no native per-category user palette setting or Def composition seam.

Executable checks must resolve the complete consumed shape and a safe current settings snapshot.
This result override has no original-body/data-flow dependency: do not add full-body IL, assembly
hash/version or foreign-owner absence checks. Compatible foreign observers and unrelated body edits
are allowed. An unsupported record severity preserves the native result rather than misclassifying it.
The adapter must respect its declared order and supported later repatch lifecycle without unpatching
another owner or claiming authority over a later incompatible result replacement.

Raw `LogMessage.text`, StackTrace, ToString, capture, native filtering/copy, automatic opening and
pause settings remain untouched. Embedded-color handling is a separate display-string target; a
color getter alone does not establish that embedded tags obey the configured severity color.

## Admission and fallback

Admit only when the shared settings and native presentation shapes are available. Active canonical
`bs.betterlog` suppresses owned overlapping log enhancement groups. Losing this target disables the
owned palette preference while retaining native inspection and raw records; do not suppress S094
source context or other independently admitted diagnostic capabilities.

Startup registration is idempotent. On enabled target/validator/install failure, roll back only the
newly added owned member and issue one bounded package/patch/spec/target/reason warning describing
unavailable custom palette and native-color fallback. Use the independent protected recorder.
No explicitly disabled feature failure warning and no redraw logging are permitted.

| Case | Required evidence | State |
| --- | --- | --- |
| Severity/default/custom/unknown decisions | Pure palette tests through current C02 value contract | Pending |
| Exact getter, benign body changes and compatible observers | Scoped Harmony fixture; no body-hash guard | Pending |
| Missing/wrong field/getter shape, install failure and repeat | Admission/owned Harmony fixtures | Pending |
| Existing retained row changes after setting edit | Native common-settings action → log redraw screenshots | Pending |
| Embedded tags on/off and unchanged copy/details | Separate display target + native inspection/copy | Pending |
| Save inheritance/override/switch and original provider | Exact settings/provider native workflows | Pending |

Add exact implementation, validator and test links before actual registration. Original-method
inspection is research, not effective-chain or player-workflow evidence.
