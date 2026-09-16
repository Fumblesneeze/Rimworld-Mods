# Prepared-ingredient texture remake

Owner: ImmersiveChefs. PF5 was selected for the development package on2026-09-14 after source and static review, then rejected by independent native visual review: close9, ordinary8, far7. The native production and hauling workflow passed, but visual acceptance remains open. The existing `ImmersiveChefs_PreparedFood` Def keeps `Graphic_Single`, `Cutout`, draw size1 and its existing production/ingredient behavior.

The selected source is PF5A: one shallow angled four-compartment tray with twelve original generated vegetable, starch and meat portions. Core `Simple_a`, `Pemmican_a` and `Medicine` were inspected as carried-item scale/style references, extracted read-only from Core1.6.4871rev590. No Core pixels are packaged. The initial builtin imagegen pair and subsequent paint/geometry trials remain under ignored `artifacts/VisualAssets/PortableRemake-20260913`; the backend model was not verified.

Exact prompts are `prepared-pf3-prompt.txt` in that directory; prospective finishing briefs are `prepared-pf4-brief.md` and `prepared-pf5-brief.md`. Each intact three-portion group is translated once into a measured well without scaling or rotation. The complete source receives one width216 registration in256square. Actual minimum source clearance is4.734191 normalized pixels and the final masks retain twelve separate portions. PF5 changes only surface paint: quieter148±3 dividers, brighter green, broad red top faces and a small reduction of food texture noise. Runtime material/recipe/capacity values do not change.

Selected identities:

- Original generated PF3A: `a02a863792115b7af584e8d5be600d39f3a7cf60844ab9fc65d5d01e69b07002`.
- Finished full source: `03ce51dcf80c6cf1403c285e7da9b07e03a0f1f0c7da1a0c47bb5af3c63dfa4c`.
- Normalized diffuse: `9f0ad1ec3dd35066033b4a82904be9c0a72fa5a0400ed80fbacb124916ad4fdd`.
- Final exterior6 diffuse: `a3d73a9c11db0f91667afc9366677a23ed3ba24ca4e15b399f5cd6cd59b7fc48`.

The Core outline processor preserves all occupied source pixels, adds5016 pixels, retains one foreground/no holes, and measures ring1/ring2=1.0. Ring3=.331633 is diagnostic for this class. No mask is packaged because the actual shader is fixed-color Cutout. Exact processing metadata and delta masks are retained with the sources. The source red-cap target is190/119/111; six final bicubic edge pixels overshoot it, reaching204/123/113. This is explicitly distinguished from source paint and was reviewed.

Independent mechanical review is `prepared-pf5-independent.md`; fresh context-free static review and root observations are under `artifacts/VisualReviews/20260914-SetBO`. Both alternatives scored9 at256/64/32/20 on light/dark backgrounds. Root selected A's slightly less regular leaves. The subsequent fresh native bill and hauling run20260914T103957237Z passed, but SetBU scored9/8/7 quality and9/8/8 fit at actual close/ordinary/far on Concrete and WoodPlankFloor. The acting agent's9 does not override that failure. Exact observations, source/build identities, screenshots and cleanup are in `artifacts/VisualAssets/PortableRemake-20260913/prepared-pf5-native-review.md`. The game reported1.6.4871rev591 with the same hashed Core reference bundle. PF6 prospectively replaces twelve small pieces with four broad connected ingredient piles while conserving the tray. PF5 is a rejected development trial; the full remake's accepted-original count stays48/100.
