## Context

Immersive Chefs has a loadable RimWorld 1.6/Harmony implementation baseline. The requested simulation crosses recipes, reservations, Thing stacking, ingestion, rot, temperatures, work givers, linked buildings, caravans, Royalty expectations, and several independently optional mods. Harmony is the only required third-party mod. Local Workshop inspection found usable Expanded Materials metals and adobe, Dubs Bad Hygiene, Hospitality Continued, Gastronomy, Common Sense, Variety Matters, Vanilla Food Variety Expanded, Vanilla Expanded Framework, Vanilla Nutrient Paste Expanded, ABS polymer, Processor Framework, and a downloaded but inactive Vanilla Cooking Expanded; it did not find a suitable RimWorld 1.6 ceramic material mod or brass Def.

This document defines the implementation architecture and acceptance contract; completed behavior is tracked in `tasks.md` and is accepted only after its in-game observation task is complete.

## Goals / Non-Goals

**Goals:**

- Make culinary tools, preparation, teamwork, serving ware, sanitation, temperature, and dining standards form one understandable simulation.
- Preserve meal and ware state across save/load, stacking, splitting, hauling, ingestion, rot, and optional-mod workflows.
- Keep optional integrations absent-safe and compatible by using package IDs, Def discovery, narrow patches, and adapters.
- Keep the colony playable under shortages, starvation, job interruption, and unsupported third-party recipes.
- Expose conservative settings with stable defaults and explicit restart behavior.

**Non-Goals:**

- Food waste, canning, fermentation, and other preservation systems. The data model will leave extension points, but no inactive feature will alter play.
- Drinks, drugs, baby food, animal feeding, knife washing, detergent, dish breakage, utensil wear, or general cross-contamination simulation beyond the explicit wild-water wash provenance.
- Player-visible caravan dishwashing jobs or caravan washing buildings; travel washing is an automatic journey abstraction.
- Replacing Variety Matters, food-variety content mods, or nutrient-paste overhaul logic.
- Compile-time references to optional mod assemblies.
- Ceramic/porcelain content or research, Vanilla Cooking Expanded recipe classification, and compatibility migrations between unreleased development schemas.

## Decisions

### 1. Persistent state belongs to small composable ThingComps

Use separate, versioned components for sanitation, embedded serving ware, preparation provenance, and culinary meal state. Components serialize stable Def/package identifiers and numeric snapshots rather than runtime object references. Each embedded plate binding includes Def, Stuff, quality, hit points, and sanitation so terminal recovery can return the exact surviving item. Stack compatibility SHALL include component state; split and merge operations SHALL preserve per-serving plate counts and reject lossy merges.

This is preferred to one global map registry because Things can leave maps, enter containers, split, rot, or be transformed. A registry is easier initially but becomes fragile across save/load and third-party holders.

### 2. Classification is explicit and extensible

Use DefModExtensions and configured Def-name/category lists for meal coverage, complexity, hand-eaten exclusions, fabrication tiers, plastics, ceramic-like materials, water sources, service ware, and compatible appliances. Built-in fallbacks cover vanilla materials. Optional definitions are added through package-ID/Def-presence-gated XML patches. Broad Stuff tags alone are insufficient because the locally installed ABS polymer is also tagged Metallic, Woody, and Stony.

The initial complexity table is deliberately exact: vanilla Simple recipe Defs map to `Simple`, Fine recipe Defs map to the internal `Advanced` tier, and Lavish recipe Defs map to `Elaborate`. Unclassified mod recipes keep their original work amount until compatibility XML explicitly classifies them; there is no label, preferability, ingredient-count, or translated-text heuristic. Complexity classification is separate from meal coverage, so a compatible modded meal may still preserve ware and provenance without receiving a guessed work multiplier. Pemmican, packaged/travel survival meals, raw food, drinks, drugs, and baby food are excluded. Vanilla Cooking Expanded classification is deferred to a later compatibility change.

### 3. Culinary work uses reservations and emergency degradation

Cooking jobs reserve the required cookware and clean plates before ingredients, then reserve the lead stove. Clean ware is preferred. Under an urgent recipe or when a pawn's food need crosses the configured emergency threshold, the system can use dirty ware or produce an unplated meal, with explicit risk and thought consequences; it MUST never create an infinite job loop or allow avoidable starvation.

Cookware becomes dirty only after at least one actual cooking tick. Plates remain embedded per serving until ingestion, rot-to-nonmeal transformation, or explicit recovery. Cutlery is acquired by the eater or Gastronomy server and becomes dirty after eating.

### 4. Washing is one state machine with source adapters

A sanitation job selects, in order: an available powered dishwasher, a Dubs Bad Hygiene kitchen sink, another recognized connected appliance, a hauled-water source, then a reachable water terrain tile if enabled. A powered appliance is connected when its required power/fuel and water contract are satisfied; a Dubs appliance additionally requires its plumbing contract. Support stations are connected by RimWorld's linked-facility mechanic to the lead cooking building.

The domestic and industrial dishwasher expose 16 and 64 plate-equivalent units of capacity before the capacity setting. When the supported Processor Framework is active, an Immersive Chefs adapter SHALL use its process timing and presentation while intercepting the destructive completion path and returning each original Thing with Stuff, quality, hit points, ownership, stack metadata, and sanitation intact. A PF-absent or shape-incompatible installation uses the behaviorally equivalent local cycle rather than failing the base mod. A newly admitted item opens a Def-configured, save-persistent loading phase so the remaining pieces of a place setting can join the same batch; each further admission resets that phase, and no cleaning progress occurs during it. The closed batch then captures its contents, duration, and progress; loss of power or a temporary breakdown pauses it, and restoration or repair resumes the same cycle. With a validated Dubs Bad Hygiene adapter active, a dishwasher also requires a supplied plumbing connection and atomically debits one positive, load-scaled, Def-configured water charge when the loading phase closes and the cycle starts; insufficient water retains the admitted dirty batch without starting it, later supply loss pauses the cycle, and resume never charges again. Cancellation/removal does not refund already admitted water. If Dubs is active but its shape is incompatible, dishwashers fail closed while non-Dubs hand-washing fallbacks remain available. Explicit cancellation, deconstruction, or terminal destruction ejects every recoverable exact input still dirty and never fabricates replacement output.

### 5. Assistant effects accrue, not merely attach

When the lead cooking job starts its ingredient-hauling phase, linked sauce, meat, vegetable, and pastry stations advertise claims to available cooks. A claimed assistant contributes only on ticks when the lead pawn is actively cooking and the assistant is actively manning an applicable station. Contribution snapshots use the assistant's Cooking skill, are accumulated over time, and are capped by the configured assistant count/effect scale. Jobs release claims immediately on cancellation, loss of linkage, danger, higher-priority needs, or lead-job completion.

This avoids granting a bonus for walking to or idling at a station and keeps interruption behavior compatible with RimWorld's normal job priorities.

### 6. Meal outcomes use bounded snapshots

Culinary quality is a 0–100 snapshot calculated at completion from lead skill (30%), ingredient diversity (15%), ingredient quality (10%), preparation quality (15%), cookware (10%), knife (5%), and accrued assistants (15%). Inputs absent in vanilla, such as ordinary ingredient craftsmanship, default to the Normal midpoint instead of failing. Every component clamps its value; optional modifiers operate through documented hooks.

Meal temperature starts at 70 °C and moves toward ambient using a configurable two-hour baseline half-life. Refrigeration accelerates cooling by 2× and freezing by 4×. Temperature bands are steaming hot (>=55), warm (35–54.9), room temperature (15–34.9), cold (>0–14.9), and frozen (<=0). The microwave restores a serving to 60 °C, removes five culinary-quality points, and adds 0.5 percentage points of poisoning risk. The final custom poisoning contribution is capped independently of vanilla risk.

### 7. Expectations are satisfiable tiers, not item-label checks

Dining standards compare material-category scores, plate/cutlery comfort, meal complexity, and culinary quality against deterministic expectation and title tables. Royalty requirements combine component-wise with colony expectations and progress from Refined service at low titles to silver and then gold. Immersive Chefs does not invent ceramic when no compatible ceramic path exists: registered ceramic and stainless qualify as Refined when available, Good-or-better vanilla steel is the no-optional-mod Refined substitute, and base-game silver/gold keep later tiers attainable. Prisoners receive ware when available but no wealth expectation; colonists, slaves, and guests use their applicable expectation/title rules; animals are excluded. One combined thought reports the most important unmet dining standard to avoid mood stacking.

### 8. Optional integrations are adapters with safe no-op fallbacks

Integration activation requires both a known package ID and the expected Def/type/member shape. Reflection is isolated behind adapters and cached after validation. Failure disables only that adapter and logs one actionable warning. Harmony patches target the narrowest stable public behavior, use prepare guards, preserve original return values/state, and avoid transpilers where a prefix/postfix or Def patch can express the behavior.

The base implementation preserves `CompIngredients` and other unknown ThingComps. This is the main compatibility boundary for Variety Matters and food-variety mods. Hospitality support recognizes an arrived guest only through the active `Orion.Hospitality` adapter and its validated `Hospitality.Utilities.GuestUtility.IsArrivedGuest` shape; colony service ware remains the first source and the guest's inventory is the fallback.

Common Sense support is a map-only post-dining adapter. Local Workshop inspection identifies package `avilmask.CommonSense`, assembly `CommonSense`, public `CommonSense.Settings.adv_cleaning_ingest : bool`, and public static `CommonSense.Utility.IncapableOfCleaning(Verse.Pawn) : bool` as the compatibility shape; the implementation MUST revalidate that exact public shape without a hard assembly reference before enabling the adapter. After a completed self-eating job, the diner owns opportunistic cleanup of the exact dirty plate and cutlery from that serving. After `FeedPatient`, the nurse owns it instead. An accepting dishwasher outranks hand washing; an unavailable cleaning path leaves ware for ordinary cleaners without delaying dining, retaining reservations, or starting a retry loop. Gastronomy-owned clearing remains authoritative when both integrations are active, and caravan washing remains governed by the travel abstraction.

### 9. Travel and assisted dining reuse the same physical ware lifecycle

Eligible meals keep cooling while held by a caravan, using the caravan tile's outdoor temperature as ambient. A caravan diner uses the serving's exact embedded plate or, for an imported unplated serving, attaches an eligible loose caravan plate at dining selection; cutlery is selected from caravan inventory. After ingestion the same plate and cutlery return to caravan inventory. The journey then abstracts routine washing by marking those items clean with wild-water provenance. Wild-water washing is not equivalent to safe fixture or dishwasher cleaning: it leaves the item usable and clean while adding a small deterministic poisoning-risk input until a later safe wash replaces that provenance.

Map guests use available colony service ware first and may fall back to their own inventory. A child enters the ordinary dining workflow when vanilla allows independent eating; baby food, milk, and other toddler-feeding exclusions remain untouched. For patient feeding, the feeder acquires and carries the cutlery but the dining result belongs to the patient. A missing-cutlery thought applies only when that patient is conscious, and eating or feeding without cutlery creates one bounded vanilla dirt event at the actual eating location when a map exists.

Dirty ware remains ordinarily haulable and is not automatically forbidden. Player forbiddance remains authoritative. Two mutually exclusive special storage filters expose clean and dirty kitchenware so players can create dedicated wash-input and clean-service stockpiles; sanitation changes notify normal storage hauling so cleaned ware can leave dirty-only storage.

### 10. Settings separate restart-required classification from live tuning

| Setting | Default | Allowed values | Application |
|---|---:|---:|---|
| Ware requirement mode | Strict | Strict / Prefer / Off | Restart |
| Dirty-ware fallback | Urgent only | Never / Urgent only / Always | Live |
| Emergency hunger threshold | 15% | 5–35% | Live |
| Simple recipe time multiplier | 0.75 | 0.25–2 | Restart |
| Advanced recipe time multiplier | 2 | 1–5 | Restart |
| Elaborate recipe time multiplier | 3 | 1–8 | Restart |
| Prepared work reduction | 40% | 0–75% | Live |
| Prepared rot multiplier | 4 | 1–10 | Restart |
| Paste preparation quality | 20 | 0–50 | Live |
| Auto-call assistants | On | On / Off | Live |
| Maximum assistants | 4 | 0–4 | Live |
| Assistant effect scale | 1 | 0–3 | Live |
| Prefer dishwashers | On | On / Off | Live |
| Allow terrain handwashing | On | On / Off | Live |
| Dishwashing work scale | 1 | 0.25–4 | Live |
| Dishwasher capacity scale | 1 | 0.5–4 | Restart |
| Culinary quality | On | On / Off | Live |
| Quality mood scale | 1 | 0–2 | Live |
| Food-poisoning effect scale | 1 | 0–3 | Live |
| Maximum custom poison chance | 50% | 5–100% | Live |
| Meal temperature | On | On / Off | Live |
| Thermal half-life hours | 2 | 0.25–12 | Live |
| Auto-microwave below | 10 °C | -10–30 °C | Live |
| Microwave quality loss | 5 | 0–20 | Live |
| Microwave extra poison chance | 0.5 pp | 0–5 pp | Live |
| Colony dining standards | On | On / Off | Live |
| Royalty dining standards | On | On / Off | Live |
| Each optional integration | Auto | Auto / Off | Restart |

Restart-required settings alter generated Defs, recipe users, or classification caches. Live settings are read at job selection or outcome calculation. Save/load within the same development schema SHALL preserve state, but backward migration between unreleased schemas and uninstall cleanup are deliberately deferred until release readiness.

### 11. Progression comes from workstations and two kitchen researches

Kitchenware recipes have no Immersive Chefs research prerequisite. The actual vanilla workstation gates provide progression: a crafting spot supports primitive stone cookware and soft service ware; fueled/electric smithies support medieval cookware and intermediate metals; and the machining table supports modern cookware, chef's knives, and a late universal service-ware route. A data-driven fabrication tier distinguishes soft, intermediate, and modern materials; unknown compatible metals default to the smithy tier unless explicitly registered otherwise.

`ImmersiveChefs_Dishwashing` requires vanilla `Electricity` and unlocks the domestic dishwasher. `ImmersiveChefs_ProfessionalKitchens` requires `ImmersiveChefs_Dishwashing` and vanilla `Machining`, and unlocks the industrial dishwasher plus the prep, sauce, meat, vegetable, and pastry stations. The microwave uses vanilla `Electricity` directly. Glitterworld cookware has no recipe or research and appears only through trade or quest rewards. Ceramic/porcelain progression remains absent until a compatible provider is selected.

## Risks / Trade-offs

- **[Stack metadata causes fragmentation]** → Quantize culinary quality and temperature for stack compatibility, preserve exact plate counts, and prefer correctness over forced merging.
- **[Patches collide with food overhauls]** → Preserve unknown components, patch narrow lifecycle seams, add prepare guards, and test supported combinations in-game.
- **[Assistant jobs create priority churn]** → Use short-lived station claims, bounded searches, cancellation checks, and cooldowns after failed assignment.
- **[Dirty ware makes food unavailable]** → Provide explicit urgent/starvation fallback and clear alerts rather than hard job failure.
- **[Ingredient hiding bypasses dietary rules]** → Paste preparation hides exact ingredient identities from the player-facing meal but retains broad meat/plant, ideology, allergy, and dietary flags for validation.
- **[Temperature updates cost too much]** → Store the last temperature/time/ambient sample and calculate lazily on inspection, job choice, ingestion, or holder transition.
- **[Water connectivity differs by mod]** → Put source recognition behind adapters and never assume a building is usable from Def name alone.
- **[Processor Framework loses dish identity]** → Intercept its stock output path behind a version/shape guard, retain the original items, and fall back locally only when PF is absent or incompatible.
- **[Royalty demands become impossible]** → Validate every tier against base-game or fixed-recipe materials and degrade missing optional categories to documented equivalents.

## Implementation Rollout

1. Introduce serialized components and settings behind feature toggles, initializing newly covered or imported Things to clean, Normal quality, and ambient temperature where no evidence exists.
2. Add kitchenware and buildings before enabling strict recipe requirements; verify Def generation and save round trips.
3. Enable ware lifecycle, then preparation/assistants, then quality/temperature/expectations in independently reversible slices.
4. Add optional adapters one at a time with absent-mod and supported-combination smoke checks.
5. Before every real-game verification, write an isolated `ModsConfig.xml` under a dedicated `-savedatafolder`. If any workflow ever touches the normal configuration, hash and back it up first, restore it in `finally`, and verify the restored hash even after failure. Only recorded process IDs may be terminated.
6. Rollback disables the relevant setting/adapter and removes its Harmony owner patches. A public save-migration and uninstall-cleanup plan is release work, not a pre-release implementation requirement.

## Open Questions

- Food preservation and food waste need separate future OpenSpec changes; waste hooks must distinguish edible leftovers, spoilage, discarded prep, and sanitation residues before implementation.
- Vanilla Cooking Expanded needs a future compatibility change that explicitly classifies its recipes after its live behavior and Defs are verified; the current change does not guess those tiers.

## Affected Mods

- **Immersive Chefs** — package ID `fumblesneeze.immersivechefs`; repository path `mods/ImmersiveChefs`.
