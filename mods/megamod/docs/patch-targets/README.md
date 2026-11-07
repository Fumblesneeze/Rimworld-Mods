# Patch target specs

**Patch inventory: empty.** The scaffold installs no patches and has no admission runtime. Create
one document per patch from [TEMPLATE.md](TEMPLATE.md) before introducing any Harmony or XML mutation.
Index every real patch here with its stable ID, owning feature spec, target document, validator/code,
and verification state. Do not populate a registry with illustrative fake game targets.

The normative requirements are in
[megamod-patch-target-specs](../../../../openspec/changes/add-megamod-foundation/specs/megamod-patch-target-specs/spec.md)
and the executable development rules in [AGENTS.md](../../AGENTS.md). Documentation and checks must
agree: every enabled patch is admitted on its minimal semantic predicates or skipped with a startup
warning. Another Harmony owner is not a rejection condition by itself.

An observer's target spec can be as small as existence plus the signature it consumes. A prefix
changing a parameter must prove its relevant use, including effective-code changes that affect that
assumption. Internal combinations of ported behaviors use direct code, not patches against megamod.

XML-only targets use XML Extensions presence/shape checks and external package gating where needed.
Append/merge additions while preserving other entries. Replacing a whole category or filter list
needs an explicit feature requirement and documented removed-content/conflict effects.
