# Initial drug preset fixture

Owner fumblesneeze.megamod; RimWorld 1.7. Development-only exact XML fixture for
`megamod.drug-policy.initial-preset-native-controls`. Root registers its manifest with the owning
E2E project; the grouped runner stages/removes the allowlisted files. No product package includes it.

The ordinary Game constructor creates every DrugPolicyDef before scenario Arrange. The fixture adds
one preset with existing Core Steel (a valid reference, but not a drug) first, then Beer and Smokeleaf
with distinct explicit choices. Broad pleasure permission must still apply to other pleasure drugs
such as Yayo. The owned preset guard must skip Steel's invalid policy lookup and preserve both
following explicit copies. Creating a blank or duplicate policy in the dialog does not exercise this.

All references exist in Core; no missing-reference or loader error is expected or allowlisted.
The fixture introduces no Thing, health effect, patch operation, code or runtime dependency beyond
the owning mod. XML Extensions is unnecessary because no existing Def is patched. Without admitted
preset recovery, native policy construction can throw; do not use this fixture for an ordinary
playthrough, an original-provider comparison or a disabled-feature fallback run.

Only the fresh isolated quicktest/start path is covered initially. The fixture does not prove saved
removal, full policy reconciliation, display sorting, original-provider behavior or hot-reload recovery.
