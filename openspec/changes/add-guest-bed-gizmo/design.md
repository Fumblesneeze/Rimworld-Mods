## Context

**Owning mod:** Guest Bed Gizmo (`fumblesneeze.guestbedgizmo`) at `mods/GuestBedGizmo`.

With Ideology active, RimWorld 1.6 exposes one `Verse.Command_SetBedOwnerType` on humanlike beds. Its float menu contains colonist, prisoner, and slave ownership. The locally installed Hospitality Continued 1.6 build (`Orion.Hospitality`, Workshop item `3509486825`, `Hospitality.dll` SHA-256 `484BFDC9A4896EFC92E590B146A903B3C9FEA384C6BD41FB55A90FC15D582297`, MVID `2960a88a-6247-4553-ae68-04319c05246e`) predates that command. It post-processes `Building_Bed.GetGizmos` to prepend a standalone `Command_Toggle` whose action calls `Hospitality.Building_GuestBed.Swap`, and its guest-bed subclass disables the vanilla owner command while retaining a separate rental/attractiveness gizmo.

Hospitality and Ideology are activation prerequisites and remain optional package/load-order inputs; Hospitality is read-only. The product must not reference or package Hospitality's assembly. Harmony is the only hard runtime dependency. Actual loaded types, generated guest-bed Defs, Harmony ordering, Unity UI, selection replacement, and building conversion require fresh isolated RimWorld processes to verify honestly.

## Goals / Non-Goals

**Goals:**

- Present one native-style bed-owner command whose menu contains exactly colonist, prisoner, slave, and guest choices while Hospitality and Ideology are active.
- Preserve Hospitality's existing guest-bed representation and conversion behavior, including Stuff, quality, hit points, art, paint, style, fees, claiming, and room/visit logic.
- Preserve vanilla `SetBedOwnerTypeByInterface` behavior, including multi-selection, room propagation, owner warnings, and native sound, for non-guest choices.
- Install no product patch when Hospitality or Ideology is absent, or when Hospitality's supported runtime shape is incompatible.
- Ship one small reproducible Workshop preview card based on the reviewed in-game owner menu.

**Non-Goals:**

- Reimplementing Hospitality guest beds, rental pricing, guest AI, or save data.
- Adding `Guest` to RimWorld's closed `BedOwnerType` enum or modifying Core/Hospitality binaries.
- Supporting RimWorld versions other than 1.6 or claiming compatibility with uninspected Hospitality forks/shapes.
- Adding settings, persistent product state, custom bed textures, or a gameplay showcase scene.

## Decisions

### Guard one optional adapter by package and complete runtime shape

The mod constructor first checks the canonical package IDs `ludeon.rimworld.ideology` and `orion.hospitality`. Only then does it resolve the loaded `Hospitality` assembly and validate the guest-bed type, its public static `void Swap(Building_Bed)` method, the legacy gizmo closure/action identity, and the installed `GetGizmos` patch surface. A failed probe emits one bounded log warning and installs nothing.

This keeps optional type names out of fields, signatures, attributes, and eager JIT paths. A compile-time Hospitality reference was rejected because an inactive or updated optional assembly could then prevent the product from loading.

### Replace the command object, not vanilla IL or the owner enum

One manually installed Harmony postfix runs after Hospitality on `Building_Bed.GetGizmos`. Its lazy projection materializes one upstream enumeration so it can confirm that a vanilla owner command exists before removing Hospitality's old control. It then:

1. removes only the exact Hospitality legacy guest-toggle command by the action method's validated assembly/type/name identity, and only when the same enumeration contains a vanilla owner command to replace;
2. replaces each `Command_SetBedOwnerType` with a product `Command_GuestAwareBedOwnerType` that copies vanilla label/description/icon metadata for colonist, prisoner, and slave states;
3. enables and labels that same replacement command `For guests` on `Building_GuestBed`; and
4. passes every unrelated gizmo through unchanged, including Hospitality's rental/attractiveness gizmo.

The replacement derives from `Verse.Command_Action`, uses its native invocation path, and opens an ordinary `FloatMenu` with the three vanilla translations/icons plus Hospitality's existing translated guest label and icon. Transpiling the menu builder or extending the owner enum were rejected as more brittle and unable to represent Hospitality's separate guest-bed class honestly.

### Delegate conversion to Hospitality at vanilla's deferred commit

Choosing guests preflights and invokes the validated Hospitality `Swap` action for each selected bed that satisfies vanilla's player-faction, humanlike, non-baby owner-command predicate. Choosing colonist, prisoner, or slave first runs vanilla's prisoner-room preflight and then calls `SetBedOwnerTypeByInterface` on the original bed without swapping it.

A narrowly guarded transpiler prepares RimWorld's private deferred owner-assignment closure immediately before vanilla constructs its commit callback. It removes unsupported directly selected beds and includes selected guest beds that vanilla's closed `BedOwnerType` comparison would otherwise mistake for colonist beds. A prefix on the exact deferred commit preflights every guest replacement in the complete vanilla affected-bed list, including unselected room siblings, swaps those guest beds only after any native confirmation has been accepted, replaces the closure's list entries, and lets vanilla's original assignment/room-notification callback continue. Cancelling the dialog never reaches this prefix, so it performs no mutation. This preserves vanilla's warning text, accept/cancel lifecycle, owner-type propagation, sounds, and room notifications without assigning private ownership fields directly.

Patch publication is transactional. If any guarded compiler-generated seam fails while the three patches are being installed, the mod removes every patch under its owner ID, clears the adapter, reports one bounded incompatible status, and never exposes a partial unified UI.

Replacement beds are resolved on the same map, root position, and rotation immediately after the synchronous upstream action. All required Def/type mappings are checked before the first swap. A missing or ambiguous replacement fails the action with a bounded error instead of guessing; selection restoration runs in guaranteed cleanup.

### Keep verification tiers truthful

- Host tests cover the four-choice catalog, package-ID activation, exact optional-shape probe, legacy-command discrimination, conversion/preflight planning, and transactional rollback without pretending to load a mod.
- Main-menu integration tests cover the real generated guest-bed Defs, exact active package order, complete patch ownership, and selected strategy.
- A focused E2E/player run selects a real bed, opens the native command, observes all four menu choices, chooses guests, observes the replacement guest bed and unified label with no standalone toggle, then chooses a vanilla owner type and observes the reverse conversion.
- A separate product-only no-Gateway run proves clean absence behavior. All runs use unique saved data, exact PID/start identity, normal configuration hashes, reviewed build hashes, and controlled cleanup.

### Build the preview from accepted behavior

The Workshop card uses the repository's proven 1164×655 layout and stays below 1 MiB. Its subject is the exact reviewed live frame with the four-choice menu open, paired with a short headline and one concise supporting line. The source frame, crop, font, colors, copy, output hash, and alt text are pinned in a mod-owned manifest and rendered deterministically after gameplay review. It does not invent a colony scene or claim behavior absent from the screenshot.

## Risks / Trade-offs

- **[Hospitality compiler-generated closure names change]** → Exact validation disables the whole adapter and emits one warning; no fuzzy toggle removal or partial conversion remains active.
- **[Harmony postfix order changes]** → Declare explicit `after` owners for Hospitality's observed package-ID casing and assert the live patch order before player acceptance.
- **[An upstream swap does not yield one replacement at the old root cell]** → Reject the action, retain a diagnostic with source identity/cell, and do not apply a vanilla owner type to an uncertain Thing.
- **[Multi-selection contains mixed or destroyed beds]** → Filter directly selected beds by vanilla's owner-command predicate, preflight the complete supported set, replace exact source entries, and restore selection in guaranteed cleanup.
- **[Hospitality translations or icon path disappear]** → Treat those assets as part of the supported shape and fail closed during initialization rather than expose raw English or a missing texture.
- **[A screenshot-driven card becomes stale after runtime edits]** → Pin the reviewed package and screenshot hashes; any runtime-affecting edit invalidates the card source and requires a fresh live run/render/review.

## Migration Plan

The new package is additive and owns no save data. Enable Harmony, Core, Ideology, Hospitality, then Guest Bed Gizmo. Removing Guest Bed Gizmo restores Hospitality's original standalone toggle on the next launch; existing guest beds remain Hospitality Things and require no migration.

Every automated run stages only the exact product package into a disposable isolated configuration. It hashes the user's normal `ModsConfig.xml` before and after, requests graceful exact-PID shutdown, removes only run-owned staging/evidence credentials, and restores any temporarily changed camera, selection, developer mode, speed, and window state. No workflow edits the user's normal `ModsConfig.xml`; rollback is deletion of the exact `fumblesneeze.guestbedgizmo` staging folder if an intentional deploy was requested.

## Open Questions

None. The inspected local Hospitality shape and the requested user workflow are sufficient to implement and verify this bounded compatibility patch.
