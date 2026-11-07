## Context

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), 1.7 beta.
The user explicitly selected linked crop, sowing and cutting choices. Native `Zone_Growing` stores
`plantDefToGrow`, `allowSow`, `allowCut` separately for each zone. Defaults enable both work flags;
an unset crop lazily becomes potato or toxipotato depending on pollution. The crop command already
supports temporary multi-selection but does not persist a link.

[Research report](../../../mods/megamod/docs/replacements/linked-growing-zones.md) records identity,
source methods and coverage. No runtime changes exist.

## Goals / Non-Goals

**Goals:** durable same-map grouping, all three choices synchronized, contextual control and safe
zone lifecycle, while preserving actual plant/job rules.

**Non-goals:** faster growth, new crop availability, guaranteed work completion, changing every
plant-cutting job, synchronizing zone geometry, or including buildings in zone membership.
S076 now specifies separate building crop groups.

## Decisions

1. **Keep this a zone policy, not settings.** A map-owned membership record can propagate one owned
   policy transaction to native zone fields at mutation boundaries. This keeps native direct readers
   coherent without implementing a parallel job scheduler. Do not create an abstract linking framework
   merely because storage has superficially similar controls; reuse proven small operations only.
2. **Use native command eligibility.** `Verse.Command_SetPlantToGrow` composes selected growers,
   `PlantUtility.ValidPlantTypesForGrowers` requires all to accept the crop, and native commands handle
   research/skill/light/roof warnings. Extend that interaction to the group's members rather than
   replacing it with an unrestricted Def list. The proposed refusal of a link with an incompatible
   source crop is an explicit conservative policy, not observed original-mod behavior.
3. **Commit once per group.** Native toggle lambdas write public fields directly and selection can
   combine gizmos. Route owned actions through one deduplicated group edit; retain native fields for
   actual work consumers. Audit external writers before promising those writes propagate.
4. **Preserve cutting semantics.** `WorkGiver_GrowerSow.ExtraRequirements` checks `allowSow`; its
   blocker-clearing branches consult `allowCut`. `WorkGiver_GrowerHarvest.HasJobOnCell` with cutting
   disabled rejects plants different from the selected crop, not the selected crop itself. The
   feature must not turn either flag into an invented all-harvest prohibition.
5. **Persist group identity and missing-crop intent.** Store member references and enough identity
   to distinguish an absent saved crop from a newly created native default. The proposal pauses sowing
   for a missing choice while retaining the player's Allow sowing flag. Avoid silently invoking the
   native null-to-potato fallback for an established linked group. Exact recovery format is LG-Q2.
6. **Keep source composition direct.** [S076 building crop groups](../specify-megamod-linkable-hydroponics/design.md)
   share proven membership/command operations while remaining crop-only and separate from zones.
   Combined native crop actions resolve affected owned groups once without changing zone permissions.
   Original-provider authority must not accidentally route zone actions through foreign building
   groups. Mixed membership/new basin work flags remain outside both drafts. Grazing/growth-speed/
   blight features affect actual conditions or work, not a second group state or internal self-patches.

### Target specifications

Before code, document each actual hook for contextual command creation, selected-group mutation,
crop setting, lifecycle and persistence. A method-existence check suffices only for passive callbacks;
redirected reads/writes need semantic field/use checks and actual-stream validation for transpilers.
Admit atomic groups, roll back only newly owned patches, and warn once per skipped enabled patch.
Hashes identify research artifacts, not compatibility. Declarative content changes, if later needed,
use guarded merge-preserving XML Extensions operations and declare that dependency on first use.

## Risks / Trade-offs

- **Fields can be changed externally** → LG-Q1 audits actual writers; do not poll every zone each tick
  or claim every foreign write is synchronized without an admitted contract.
- **Group crop becomes unsuitable after linking** → retain the choice and native per-cell work
  restrictions; do not split membership or mutate plants automatically.
- **One selection toggles several group members** → deduplicate group action identities; cover native
  mixed selections, not just direct policy calls.
- **Native save fallback loses missing-crop information** → preserve a stable choice identity and
  clear player-visible recovery state; prototype save/removal behavior before accepting it.

## Migration Plan

Existing zones remain independent until linked. No imported third-party groups or rewritten normal
saves. Any future migration requires backup, atomicity, failure restoration and verified postconditions.

## Open Questions

- **LG-Q1 (research):** installed crop-command/zone-field consumers, exact multi-select toggle
  composition, and explicit ordered-job behavior after a permission changes mid-job.
- **LG-Q2 (design research):** group save representation, missing-crop recovery and fallback for
  previously linked saves when admission fails or the feature is disabled/removed.
- **LG-Q3 (composition):** S076 now drafts separate building groups with shared direct crop-command
  operations; zone membership/flags remain unchanged. Exact combined selection, original-provider
  authority and optional grower restrictions remain implementation research under LH-Q1–3.
