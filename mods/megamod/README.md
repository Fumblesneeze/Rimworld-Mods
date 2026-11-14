# Make Rimworld Great Again

One RimWorld mod for independently engineered quality-of-life features, with shared infrastructure
and guarded game patches. Package ID: `fumblesneeze.megamod`. Author: Fumblesneeze.
Initial target: **RimWorld 1.7 beta**.

**Current state: implementation active; no whole feature accepted yet.** The resumed batch adds
scheduled potential-mood checking and its shared tolerance decision; live acceptance is pending.
Initial shared settings,
patch admission and content-policy rules have focused host tests. Drug-preset controls and unchanged-
catalog save/reload have narrow reviewed native evidence. Tolerance editing and selected scheduled,
recreation and manual drinking routes also have scoped native evidence; wider feature workflows
remain unfinished.
Allow Tool, Cherry Picker, Ctrl+F-style search and hauling are specified scope, not delivered
compatibility or parity claims. See the [coverage index](docs/replacements/README.md).

The [handwritten feature intake](docs/refinement/2026-11-01-handwritten-notes.md) records the initial
candidate list, custom ideas, installed-source matches, annotated photographs and unresolved questions.
It is a refinement document, separate from the feature specifications and replacement coverage.
The [sequential specification ledger](docs/refinement/specification-progress.md) tracks researched
sources, individual OpenSpec changes and questions retained for later. Each source is investigated
from documentation through source/decompilation before its observable behavior is specified.

## Development

Read [AGENTS.md](AGENTS.md) and the [foundation proposal](../../openspec/changes/add-megamod-foundation/proposal.md).
The [shared design](../../openspec/changes/plan-megamod-parallel-implementation/design.md),
[implementation roadmap](../../openspec/changes/plan-megamod-parallel-implementation/roadmap.md) and
[governing tasks](../../openspec/changes/plan-megamod-parallel-implementation/tasks.md) coordinate
152 feature entries (140 drafts and twelve intake sources) in 27 chunks across nine integration parts.
The [2026-11-14 additions](docs/refinement/2026-11-14-source-additions.md) include a Needle-inspired
all-mod option search draft, Assign-based gear comparison with Simple Sidearms support, and resource
substitution alongside content selection. New intake needs full per-source specifications before code.
Listed research/design prerequisites still apply; source intake is not delivered gameplay.
Implementers inspect referenced mods, independently implement our observable contracts through
focused tests and review, and author native verification scenarios. After all chunks in a part are
ready, the orchestrator runs and personally inspects the integrated workflows in isolated minimized
games. It may revise the plan on evidence while preserving feature obligations and acceptance gates.
When selected source mods modify one another, specify their composed result and implement it directly.

Every patch needs an executable, documented [target spec](docs/patch-targets/README.md).
Unrelated code changes or compatible Harmony owners do not disqualify a target. Failed prerequisites
must skip the affected patch and warn on startup. XML changes use XML Extensions content guards and
preserve existing list entries unless replacement is explicitly intended. Shared Thing data follows
[the data ownership contract](docs/SharedThingData.md).

[UI placement rules](docs/UIPlacement.md) supersede upstream tabs: debug/cheat overrides go at the
top, configuration in structured mod settings, gameplay control windows in bottom tabs, and simple
view/behavior toggles at bottom right. Save-specific settings are disabled from the main menu.
Settings normally inherit an editable global default, with an optional per-save override. A common
loading/storage/rendering path supplies each row's **Use global**, **Current save**, and
**Global default** controls. Exceptions require a documented reason.

From the repository root, use the typed MCP operations:

```text
release_profile_validate {"packageId":"fumblesneeze.megamod"}
mod_build {"packageId":"fumblesneeze.megamod","configuration":"Release","modsRoot":null}
package_validate {"packageId":"fumblesneeze.megamod"}
openspec_validate {}
```

`mod_build` builds and installs the allowlisted package into the canonical local
`RimWorld/Mods/fumblesneeze.megamod` folder. It does not enable the mod or subscribe to a Workshop
item. A repository-only build is `dotnet build mods/megamod/megamod.csproj -c Release --nologo`.
The build requires the repository SDK, a 1.7 installation, and the common
`tools/RimWorld.Build/VersionedGame.props` and `.targets` imports. Those versioned-tooling files must
be delivered with or before this scaffold. The project is registered in the solution for navigation
but excluded from its default 1.6 build configurations; build it through its owning operation or
project. See [repository setup](../../README.md) and [verification](docs/Verification.md).

## Dependencies and release state

The development build requires RimWorld 1.7 and Harmony (`brrainz.harmony`, Workshop `2009463077`).
It has no required DLC. XML Extensions becomes required with the first guarded XML modification.
Upstream mods are read-only research inputs, not bundled dependencies. The product never depends
on or includes the development Gateway.

Original-mod drop-in/API compatibility is outside scope. When a known original is active, the planned
startup behavior logs a warning and uses the ordinary feature-off settings path on a best-effort
basis, retaining saved preferences. This does not guarantee coexistence. See the
[scope decision](docs/refinement/2026-11-08-drug-policy-additions.md); semantic game-target checks
and deliberately supported third-party integrations remain required.

Seven baseline language catalogs accompany the implemented settings controls; their native rendering
and complete feature coverage remain unverified. The release profile is private and unpublished, pins
the inspected beta build, and uses a development-only SVG preview excluded from the package. No
release or 1.6 support is claimed. Before release, replace that preview, verify current build pins,
complete real feature/language workflows, and follow the repository release gates.
