# Localization and release checks

Use these rules for every distributable RimWorld mod in this repository. Development-only tooling may be exempt only when its project explicitly declares that classification.

## Classify the package

- Add `<RimWorldDistributionKind>Product</RimWorldDistributionKind>` to a distributable mod project.
- Add `<RimWorldDistributionKind>DevelopmentOnly</RimWorldDistributionKind>` only to tooling that is not shipped as a player mod.
- Never infer the exemption from a folder name, package ID, or missing language directory. The release test must fail when the classification is absent or unknown.

## Choose the correct localization mechanism

- Keep stable canonical English wording in source Def XML. Translate Def-owned fields such as `label`, `description`, `jobString`, recipe text, research text, filter labels, and thought stages through `Languages/<Language>/DefInjected/<DefType>/*.xml`.
- Put runtime C# strings in `Languages/English/Keyed/*.xml` and call `"Stable_Key".Translate(...)`. This includes settings, inspect rows, alerts, float-menu options, work reports assembled in code, gizmo labels, rejection messages, and dynamic state bands.
- Give dynamic families a stable finite key convention, then inventory every allowed member in the release test. Do not synthesize unbounded user-visible keys.
- Include player-facing Defs introduced by conditional `PatchOperation` XML. An optional Def is still part of the product's translation inventory.
- Leave package IDs, Def names, internal diagnostics, exceptions, and log-only messages invariant. Proper names and technical tokens may be identical across languages when translation would be wrong.

The repository baseline for every new distributable mod is `English`, `German`, `Spanish`, `French`, `ChineseSimplified`, `Russian`, and `Japanese`. A product may add more locales, but it may not omit one of these seven defaults.

## Author contextual catalogs

- Author translations from the gameplay meaning; do not submit a machine-translation dump. Read the source screen and distinguish cookware, tableware, cutlery, sanitation, culinary quality, food poisoning, and toxic buildup.
- Maintain a glossary when one concept occurs across settings, jobs, alerts, and inspect text. Record authorship/provenance and any intentionally invariant terms in the product's localization documentation.
- Preserve positional placeholders exactly, including repeated indices, numeric units, and rich-text tags. Translate surrounding grammar, not placeholder syntax.
- Prefer concise RimWorld-style UI wording. Check narrow settings rows, gizmos, alerts, float menus, and inspect panes for destructive clipping.

## Test without lying about the host

- Ordinary host-safe unit tests must not call `Translate()` or initialize `LanguageDatabase`; RimWorld's `Translator` and Unity globals are not available there. Extract a pure translation-key decision and test that function, then validate catalog values statically.
- The release gate must derive exact keyed and Def-field inventories, parse every source and packaged XML file, reject missing, duplicate, stale, malformed, placeholder/tag-mismatched, or guarded raw UI text, and prove packaged catalogs are byte-identical to reviewed source.
- A directory or file-count assertion is not a localization gate. Require exact key sets for every required locale.
- Keep an explicit, reviewed allowlist for any cross-language value equality that might otherwise look untranslated; do not weaken the whole catalog comparison.

## Accept rendered behavior

Static completeness is necessary but not sufficient. On the final reviewed Release build:

1. Launch one fresh isolated process for each required language, with developer mode enabled and the exact owned PID/configuration evidence.
2. Select the language through RimWorld's native setting or stage that isolated process's language preference before launch.
3. Observe representative product settings, a Def/info card, a live job report, an alert, and a runtime action such as a float-menu or gizmo label.
4. Inspect the exact screenshots personally for missing-key markers, unintended English fallback, broken tags/placeholders, tofu, and meaning-destroying clipping.
5. Inspect the native developer console and complete flushed log. Restore the user's normal configuration and clean only the disposable run.

Do not accept a locale from catalog parity alone, and do not reuse screenshots from a build changed after review.
