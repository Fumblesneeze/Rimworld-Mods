# Make Rimworld Great Again

One RimWorld mod for independently engineered quality-of-life features, with shared infrastructure
and explicit compatibility contracts. Package ID: `fumblesneeze.megamod`. Author: Fumblesneeze.
Initial target: **RimWorld 1.7 beta**.

**Current state: development scaffold only.** There are no gameplay features, patches, caches, or
replacement implementations yet. Allow Tool, Cherry Picker, Ctrl+F-style search, and hauling are
selected candidates, not compatibility or parity claims. See the [coverage index](docs/replacements/README.md).

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
137 feature drafts in 27 chunks across nine integration parts. This is a plan, not delivered gameplay.
Implementers inspect referenced mods, independently implement our observable contracts through
focused tests and review, and author native verification scenarios. After all chunks in a part are
ready, the orchestrator runs and personally inspects the integrated workflows in isolated minimized
games. It may revise the plan on evidence while preserving feature obligations and acceptance gates.
When selected source mods modify one another, specify their composed result and implement it directly.

Every future patch needs an executable, documented [target spec](docs/patch-targets/README.md).
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

The inert scaffold needs only RimWorld 1.7; it has no external mod or DLC dependency. Harmony becomes
required with the first Harmony integration, and XML Extensions becomes required with the first
guarded XML modification. Upstream mods are read-only research inputs, not bundled dependencies.

The seven baseline language catalogs are intentionally empty because there is no runtime UI or Def
text. They do not claim translated gameplay. The release profile is private and unpublished, pins
the inspected beta build, and uses a development-only SVG preview excluded from the package. No
release or 1.6 support is claimed. Before release, replace that preview, verify current build pins,
complete real feature/language workflows, and follow the repository release gates.
