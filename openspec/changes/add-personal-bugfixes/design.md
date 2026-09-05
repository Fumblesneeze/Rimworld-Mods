## Context

Owner: Personal Bugfixes (`fumblesneeze.personalbugfixes`), `mods/PersonalBugfixes`. This is a local-only mod with no Workshop identity or release/publication profile. The earlier hot patch is diagnostic evidence only. No current game is touched during implementation.

## Goals / Non-Goals

Each fix must prove a relevant method shape and reproduce its bug on disposable inputs before activation, verify the installed change, and explain every startup decision. The first fix preserves existing exploration flags and native childcare behavior. No new gameplay content, balance change, generic patch marketplace, full-method fingerprint, or permanent modification of downloaded assemblies is introduced.

## Decisions

Use a small common fix lifecycle with explicit outcomes: absent, incompatible, not required, applied, failed. Each implementation owns its symbols, local IL matcher, behavioral probe and author report. Startup runs after other mod initialization; unexpected patches or unknown probe behavior fail closed. Activation is transactional: postconditions must pass or only this fix's Harmony owner is removed. Startup logging uses developer diagnostics in English, not a player-facing UI or translated content catalog.

The Exploration fix matches one `learnedFeatures` field load, index local load and `List<bool>.get_Item(int)` call in UpdateGraphics, retaining branch metadata. Replace only that call with a helper that appends false through the requested index and returns the original flag. This also protects the following existing set_Item. It does not replace the method or suppress exceptions. Negative indexes retain their error behavior; null lists remain outside this initialized-list repair.

The probe copies the actual target using Harmony's snapshot reverse-patch facility, then substitutes fixture-only world, component, feature-tile and rendering access. It uses no live world swap and never invokes the original target on a real world. Both before and after activation it tests a disposable short list with an extra feature. The post-patch copy includes the installed transpiler. A local structural matcher is independent from the probe's safety checks: no full method byte/hash/IL equality gates compatibility. The probe must reject unknown external calls, unsupported side effects and foreign target patches rather than execute uncertain code. Control flow is budgeted; failures other than the expected bounds error are inconclusive, never evidence to patch. Existing flags and native update completion are asserted.

Ordinary tests cover lifecycle policy and list preservation without Harmony loaded. A separate Harmony process owns synthetic target fixtures, local IL drift and unrelated-edit tests, upstream fixed fixtures, probe isolation, postcondition rollback and exact patch cleanup. Workshop assemblies remain inspection-only and are never loaded into a host test as active mods. Focused live verification later uses Harmony, Core, Biotech, VEF, Exploration, Toddlers and Personal Bugfixes, plus a separate external-absent run.

## Risks / Trade-offs

The inspected update enum is backed by int, with `None=0`, `Fog=1`, `Full=2`, `Planet=3`. Probe state 3 exercises all update branches; guards check Full/Planet values. The first isolated run rejected an incorrect Full=3 assumption before patching; the corrected guard and fixture have focused RED/GREEN coverage. Probe safety allows only the fixture feature-list enumerator's constrained Dispose, bounded exact BCL sequences for ToList, and finally cleanup. Catch/filter/fault handlers are rejected so safety exceptions cannot be swallowed.

- Unknown upstream code or other patches may be safe but cannot be established by the probe → skip with actionable warning. Support can be widened deliberately after inspection.
- Synthetic probes prove the targeted rule, not the whole mod combination → native childcare acceptance remains unchecked until a fresh minimized game run is possible.
- The list remains positional → deletion/reordering of features is outside this fix; never truncate or reorder data.
- A later mod can patch the target after startup → startup verification describes its observed snapshot; report interference and do not claim continuous surveillance.
- The mod is intentionally local-only → no Workshop ID, previews, publication permission or distributable release profile. Repository-local builds remain available while playing; local installation and live validation wait.

## Migration Plan

The user stopped playing and authorized isolated verification on 2026-09-05. A first checkout uses typed `game_run_start` with the Personal Bugfixes project/package to build and deploy into the canonical local package folder. E2E discovery needs that initial package; subsequent exact E2E runs rebuild and stage their reviewed package and test bundle. No publication profile or permission is created. Verify in a fresh isolated process before accepting. The earlier session guard disappears when the user's game exits. Removing Personal Bugfixes leaves the preserved discovery data readable by Exploration Mode.
