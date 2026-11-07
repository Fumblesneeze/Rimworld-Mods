## Context

Owner: **Make Rimworld Great Again** (`fumblesneeze.megamod`, `mods/megamod`), target **1.7 beta**.
Origin: original S012 composition of the confirmed Add bill interview. The
[refinement document](../../../mods/megamod/docs/refinement/2026-11-02-integrated-add-bill-browser-refinement.md)
records the intent and native inputs; S009/S010/S011 retain their independently researched provenance.
This change is prose only. No patch, UI build, test or runtime acceptance exists.

The existing proposed capabilities are dependencies in specification order, not runtime library
mods: [comparison](../specify-megamod-quartermaster-comparison/),
[inspection](../specify-megamod-recipe-inspection/) and
[list controls](../specify-megamod-recipe-list-controls/). Compose their policies directly inside the
one product. Do not recreate Nice Bill Tab plus a Harmony-patched Expansion inside megamod.

## Goals / Non-Goals

**Goals:** one contextual native-entry workflow; product-derived filtering; explicit meaningful stat
choice; Normal/material identity; read-only inspection; discoverable one-bill actions; ordinary
fallback and shared data. The observable draft chooses concrete search and gesture behavior instead
of leaving “like Nice Bill Tab” as the requirement.

**Non-Goals:** a whole queue replacement; global equipment/loadout optimizer; arbitrary actor combat
predictions; material reservation; ingredient subtask generation; provider clone/tag automation;
whole-list linking implementation; copying source libraries/assets; migrating source settings/saves.
Those omissions are not a denial of separately selected features with their own later owners.

## Decisions

### 1. Keep the native entry and queue boundary narrow

The inspected ITab_Bills.FillTab supplies OptionsMaker to BillStack.DoListing. It filters the bench's
AllRecipes through AvailableNow/AvailableOnNow, appends ideological styles, and uses MakeNewBill(precept)
with native warnings and tutorial/concept notifications. The browser must preserve that semantic
envelope. Do not generate a parallel list from all ThingDefs or intercept unrelated queue editing.

Choose the smallest actual integration boundary after AB-Q1 research. The native method being a
local function is not a commitment to patch compiler-generated names. Required target specifications
must establish bench context, original/effective behavior and the relevant add path before hooks.
The source's whole FillTab replacement is an alternative deliberately rejected for this slice:
it would unnecessarily take responsibility for protected bills, clipboard/linking and provider rows.

Retain an explicit ordinary bill route for unsupported special/provider actions. Its behavior must
be genuinely usable, not a button returning into the same unsupported browser. A safe bypass context
may be needed, but must not use reentrant recursion or disable another mod's patches. No generic
interop framework is justified until an actual supported provider needs a narrow adapter.

### 2. One ephemeral browser context and one identity model

Use the live game/map/bench identity and S009's recipe/style/material tuple throughout list, details,
comparison and add actions. Keep browsing state local to that opened view. Native info cards and a
material popup suspend/resume the same context; closing or switching to another bench/map/save
invalidates it. Never reuse a static bench thingID alone across games.

Model an unrestricted material choice explicitly rather than using a dummy/default Stuff. With no
stat chosen, ordinary planning uses native ingredient filters and details show no material-dependent
claim until a preview material is selected. A chosen stat supplies automatic row variants; explicit
preview material is separate from automatic ranking, and an alternate Add choice lasts only for that
action. An ordinary inspected row follows direct stat/Use all materials control changes until the
player explicitly chooses a preview material. Then it is a labeled preview override; Use list choice
clears it in the single detail view. Returning a following preview to Recipe order means unrestricted
planning, not a concealed stale material lock. Pending/absent new choices cannot commit a different
bill under the old display. Pinned reference/partner snapshots stay fixed across list-control changes;
an explicit new comparison selection changes the partner. Background stock/provider refresh is not
a direct control change and retains S009's review-before-substitution rule. The visible action label
identifies the variant actually consumed. This avoids changing other rows or a compared reference
through a shared mutable selected-material field.

The comparison gesture enters a visibly labeled mode with the current choice as reference. Subsequent
explicit row/variant selections choose the partner; a third replaces the partner. Main Add uses the
active detail variant, row Add uses the row tuple, and alternate Add uses its chosen tuple. All invoke
one S009 action once. Double-clicking does not add. Consume row/menu inputs once; don't layer a
source quick-add prefix on top of a double-click-to-add handler. Native UI acceptance must prove
causality and repeat-click behavior, not only calling the shared function directly.

### 3. Separate product metadata filtering from mutable availability

Build search fields from localized recipe labels, native style labels and all declared product labels.
Match all whitespace-separated terms case-insensitively across those fields. This intentionally
does not replicate NBT's special categories or ingredient-stat searches. Preserve distinct recipe/style
identities even when their labels match. Exact culture/Unicode comparison needs AB-Q2 testing;
localized names, not internal IDs, are the player-facing contract.

The category catalog uses all currently addable bench recipe outputs before text/visibility filtering,
with actual category identities and ancestors. No hardcoded weapons/meals-only classification or
first-product shortcut. Root is omitted; same leaf labels carry parent context. OR across selected
categories, then AND with text and ordinary/hidden visibility, is an explicit custom default. Recipes
without any known categorized output belong to Other recipes rather than being guessed from inputs.
Guard malformed/cyclic provider category ancestry in the implementation; bounded failure for a bad
branch must leave other recipes and ordinary access intact. Do not copy the source's unbounded chain.

Product/label/category projections can share the immutable recipe catalog with the inspection/list
capabilities. Supply and colonist facts consume the shared map data/evaluator, not another searchable
Thing list. Changing language invalidates display/search/order, not ingredient quantity truth. Real
recipe/provider changes invalidate available choices; stock updates refresh affected results while
paused. Keep updating/unknown separate from a current ready result and revalidate every commit.

### 4. Define useful ordering without inventing a combat model

Recipe order means the ordinary native Add bill ordering for the exact supported recipe/style set;
inspect FloatMenu ordering and provider priorities under AB-Q1 before implementation. It is not the
source category-tree order or an alphabetical rewrite of provider priorities. Stat sorting delegates
to S009, including stable ties and unknowns after known values. Derive menu relevance after all current
search/category/visibility filters. Clearing an inapplicable stat is an explained state transition,
not an opportunity to pick another automatically. S011's reviewed correction applies here too.

The initial priority list is deliberately narrow and explicit: separate armor/insulation, weapon
accuracy/ranged cooldown, nutrition, medicine properties, HP, mass and market value. It is a proposed
usability order, not telemetry. Other supported scalar product facts follow alphabetically after
these. Availability requires meaningful stat applicability, not a nonzero result: valid zero/negative
values remain values. Duplicate labels need semantic identity/unit disambiguation. Unknown candidates
cannot silently lose a best-material contest; S009's incomplete-comparison rule remains authoritative.

Native Defs establish property names but not safe evaluation in every provider context. Do not infer
final damage from hidden multipliers or actual shot hit chance from weapon accuracy. Extra per-hit,
range, conditional DPS, equipped bonuses or provider properties need the safe meaning/context
required by QM-Q1/NB-Q1 before being offered. The initial fixed priority list does not promise an
unbounded reflection-driven catalog. This is a documented coverage boundary, not a fallback to a
source armor/combat score. Multiple distinct outputs remain unranked unless a later owned rule defines
their ranking; a batch of one product uses per-item stats and the full batch's ingredients.

### 5. Explicit material and action transitions

With a stat selected, Use all materials follows the confirmed checkbox polarity and S009's complete
one-execution allocation. Without a stat it is inactive with explanation; ordinary/unrestricted
planning remains available. A no-feasible-material row has no automatic best choice to pretend to
add, but offers explicit alternate planning or the ordinary route. Material-independent output
does not acquire a fake picker. The main Add button text and detail material state must agree.

A visible Add with another material action and a right-click shortcut reach the same popup. Opening
or canceling is read-only; selecting commits one material-specific bill without replacing the active
preview. A separate preview-material choice changes only inspection. Do not overload a material
click to sometimes preview and sometimes add without a labeled context. Popup closure resumes the
same valid browser; failure/stale context reports a reason and preserves the queue.

Every successful action leaves the browser open and reports the actual added variant. Preserve
native limits and warnings even when stock/skills are insufficient; they govern planning separately
from highlight eligibility. S014 whole-list linking may later propagate the one native add according
to its own contract. This browser must not independently iterate linked benches or double-propagate.

### 6. Common settings and responsive composition

Add Enhanced Add bill browser enabled (true), then use existing proposed Recipe comparisons enabled,
Show row Add action and Show ingredient shortage cues in that order under Crafting / Add bill.
Browser/comparison enablement applies next opening; S011's decorations apply next redraw. Use the
foundation renderer/storage/resolver, including inherited values, dormant overrides, main-menu
guards and save switching. No new per-port settings file or persistent search state.

Comparison off disables stat ranking/automatic optimization and two-choice comparison, not the
single inspection/material-planning/native-add path. Whole browser off preserves ordinary entry and
native bills; hiding records remain dormant per S011. A disabled parent feature does not erase its
subordinate preferences. Explain subordinate settings' scope in their labels/help rather than moving
entity choices into mod configuration. There is no global bottom-right toggle for this local view.

Keep the user's top search/pills and adjacent stat/Use all materials controls. Actual dimensions,
overflow presentation and keyboard focus are AB-Q2. They must satisfy all observable reachability
requirements and seven-language coverage without duplicating fixed source columns. Native assets
are preferred; any new raster follows the repository asset workflow. No speculative UI abstraction
or screenshots presented as accepted gameplay are needed for this prose change.

### 7. Admission and provider boundaries

Create per-hook target documents and a registry before implementing Add bill interception, context
callbacks or data invalidation. Use minimal semantic prerequisites: an existence/callable check is
enough only when no body behavior is relied on. Any changed argument/removed block needs exact
required use/data-flow proof in the effective path. Transpilers validate each incoming stream and
leave it unchanged on failure. Compatible foreign owners and unrelated edits are allowed; hashes
identify inspected evidence only. Atomically skip an unsafe entry/add group and log a bounded startup
warning with IDs, reason, unavailable behavior and working fallback. Never remove foreign patches.

Optional adapters require canonical package IDs, consumed member/Def shapes and actual native
workflow evidence. Retain the original provider route if enhanced rendering cannot represent an
action. Missing optional providers are normal. No blanket CE/VEF/Research Reinvented/Dubs/Compositable
support follows from S009–S011 type-name observations. Source Nice Bill Tab co-installation is handled
by effective behavior, not blanket package rejection; an incompatible active replacement keeps its UI.
No XML mutation is selected; any later declarative change must use appropriate guarded merge-preserving
XML Extensions operations and dependency metadata rather than unguarded runtime Def rewriting.

## Risks / Trade-offs

- **Many gestures/material states:** visible choice labels and separate preview/add contexts; verify
  complete native workflows including cancellation and two compared variants.
- **Categories/labels change under mods or translations:** use identity/ancestry, preserve distinct
  recipes, bound malformed input and test non-English same-label cases.
- **Priority list does not cover every desired stat:** record AB-Q3 and add only facts with an explicit
  observable meaning; do not guess compound outcomes to fill a dropdown.
- **Extended browser could hide special gameplay:** preserve working ordinary entry and provider
  restrictions, and skip unsafe enhancement instead of replacing every bill action wholesale.
- **Large lists and shared consumers:** measure query/update/render costs and paused freshness under
  AB-Q4. No optimization claim based on source code inspection alone.

## Migration Plan

No runtime deployment or migration in this draft. Browser state is ephemeral; one new setting uses
the common missing-value default. Created bills are native and retain their ordinary save behavior.
No import of source settings, source window positions, source hide keys or clone files. Future requested
migration requires a separate backup/atomicity/failure-restoration/postcondition contract.

## Open Questions

- **AB-Q1:** exact native entry/order/special recipe/provider routes and target admission; coordinate
  with QM-Q1/NB-Q1/NE-Q1 before code, without weakening safe fallback or ordinary production rules.
- **AB-Q2:** final responsive layout, focus/navigation, Unicode matching and uncommon category graphs;
  the proposed matching/pill/gesture behavior is specified, not awaiting explanations of source mods.
- **AB-Q3:** expanding the supported stat catalog beyond the explicit initial priority list, with
  safe native/provider contexts and no invented multi-output/combat score. Additional preferences
  can revise the proposed priority later; no interview blocks this draft.
- **AB-Q4:** combined-consumer cost/freshness, seven-language UI and actual reviewed-build native
  add/produce/save/provider workflows. Earlier source observations are not acceptance evidence.
