# Plate and chef’s knife redraws — 2026-09-16

Owner: Immersive Chefs. Built-in imagegen; backend model identity is unverified. The user approved simplifying portable sets for map readability and direct pixel finishing. No Core or Workshop pixels are shipped. These 14 PNGs passed independent static and fresh native review, bringing the original remake total to 62/100. The broader remake remains incomplete.

The plate retains its256-square canvas,0.65 draw size and existing selector/shader. The knife becomes one broad chef’s knife; its256-square canvas,0.8 draw size, CutoutComplex shader, belt slot and gameplay identity stay unchanged.

## Sources and finishing

Ignored originals, exact recipes and change masks are under `artifacts/VisualAssets/PortableRemake-20260913`. Plate P8 source b16afc0dd7dd1eeb233ca5eac8640bec825e37fb6908e9214f3fa32e777971a4 is authored by `plate-p8-author.py` before uniform216/1044 registration. The new authored top216×180 and near face6 follow `plate-p8v2-authoring-brief.md`. `finishing-p8v2/source-authored-change-mask.png` records actual source edits. The independent `plate-p8v2-source-review.md` reproduces source and normalization and measures the inner well~171×140. Four-pixel exterior expansion is separate from source geometry.

`plate-p8-family-v2.py` transfers generated wood/stone material residuals into the base’s material regions while preserving the base alpha and contour. The generated residue donor is registered uniformly and restricted to the well. Dark residue is authored warm brown at luminance75–110; the old v1 grime contrast failure is retained. Base, wood and stone v2 each have13.2079% fixed and high-contrast grime by the existing Steel test. The per-member `*.normalized-change-mask.png`, donor hashes, registrations and `family.json` retain the recipe evidence. Exact full-authoring recipes accompany this record as fenced source below.

Knife K7 source ef9c517c5d129d2fc3657f7614560b6b08ab2459305f6eead0ab586e41ff2d5e is authored by `knife-k7-finish.py` before one uniform scale0.183199824793 and whole rotation2.7750824549degrees. The localized grip widening and collar repaint are explicit source edits, recorded by `finishing-k7v2/source-authored-change-mask.png`. Independent `knife-k7v2-source-review.md` measures blade148.12×40.50, handle65.59×28.28, collar5.43–6.00 and grip axisapproximately20degrees. Face~239, bevel~180, handle~55, collar~185; blade and collar tint, grip and contour stay fixed. Three-pixel contour expansion follows normalization.

Static reviews: `artifacts/VisualReviews/20260916-SetEZ/review.md` covers every plate material/state at64/32/20 on both grounds, all quality/style9. `20260916-SetEY/review.md` covers knife white/Steel/Silver/Gold/Uranium/Plasteel at256/64/32/20 on both grounds, all9. Low contrast in dark materials is recorded. No failed earlier candidate is relabeled as passing.

## Native acceptance

The reviewed package digest is `EBF7B66A8F45EE2C02DB36350F5B68674454CD0378BEFF16F84551917B00471D`; product assembly `43DCE064DB8B2156C950C42B54D7F0D9A9977D2F631A87D1C3DDB108DFE9ED8E`. Fresh minimized base run `artifacts/EndToEndRuns/Grouped/20260916T002013737Z` (PID11596) and VTEX run `20260916T002221061Z` (PID4668) passed. Root personally inspected all 24 base and 27 optional native captures. Native Force equip put the knife in Gear; equipping a replacement dropped the same steel knife. Native ingestion dirtied the physical plate; prioritized cleaning carried it to water, visibly performed timed washing, then returned that plate clean. This was observed for Steel in base and Wood/Granite with VTEX.

Both runs compared all representative colors against original Core items on concrete and wood at local noon, actual zooms 11.00543, 15 and 20.04951. Native texture checkpoints resolve base, wood and stone clean/dirty paths as expected for the exact active mod group. Independent blind native reports `artifacts/VisualReviews/20260916-SetFA/review.md` and `20260916-SetFB/review.md` score every subject/ground/zoom 9/10 quality and style fit. The 100/55/15 percent hit-point views retain coherent geometry but do not show discernible damage progression; this is not a claim of new damage artwork.

The durable local action, process, configuration, screenshot and render ledger is `artifacts/VisualAssets/PortableRemake-20260913/plate-knife-native-acceptance.json`. Both exact processes closed gracefully with exit0 and no forced termination; fixture cleanup passed, credentials were sanitized, and normal ModsConfig/Prefs hashes matched before and after. Full player logs have no relevant texture or gameplay failure. Live game reported1.6.4871rev591 while the configured version reportsrev590; that difference is retained. Earlier failed fixture runs are kept as failed: the native utility command was Force equip rather than Force wear, and Gear required explicit sole-pawn selection. Independent review accepted both fixture corrections; no product pixels changed after review. OpenSpec tasks33/34 remain unchecked for the remaining art.

## Generated donor identities and registrations

```json
[
  {
    "kind": "wood",
    "source": "artifacts\\VisualAssets\\PortableRemake-20260913\\plate-p8-wood-generated.png",
    "sha256": "5982ad479e07d18ef5735f3a2faab076e5b912d7cd14a51ecfa7a6dac2709fe4",
    "uniformScale": 0.20475319926873858,
    "translation": [
      -0.3802559414990867,
      -0.07312614259598149
    ]
  },
  {
    "kind": "stone",
    "source": "artifacts\\VisualAssets\\PortableRemake-20260913\\plate-p8-stone-generated.png",
    "sha256": "d2d30020c7dea17fe072f4def89c36fe1db0d9e838b947c90b6b78a956154a2b",
    "uniformScale": 0.2119205298013245,
    "translation": [
      -4.556291390728489,
      -5.298013245033104
    ]
  },
  {
    "kind": "dirty",
    "source": "artifacts\\VisualAssets\\PortableRemake-20260913\\plate-p8-dirty-generated.png",
    "sha256": "e575c2a5ee49a2fe78a8f762c0153775e507faa88f817ecf98752f9a51362f21",
    "uniformScale": 0.20550458715596331,
    "translation": [
      -0.8513761467889935,
      -0.9541284403669863
    ]
  }
]
```

## Exact selected files

```json
[
  {
    "path": "mods\\ImmersiveChefs\\Textures\\ImmersiveChefs\\Things\\Item\\Kitchenware\\Plate\\Plate.png",
    "previousSha256": "D2CCCE13DE98A64BADDBEDD57A4FB75C97DC8CAFF04175A40DBDB39969451968",
    "preOutlineSha256": "ACB138B9B27DD2AADB00B4D0828E69078AB79364AA6D6B83E719F2A2556C737B",
    "diffuseSha256": "F3998CF9E111DF1508F6489F9037F8C151A0CC4569B69455E03E5FA1E84AE3A8",
    "maskSha256": "F13A397B96AA82B7607651F826B75E626E61231A710393F7DF6256DC46608261"
  },
  {
    "path": "mods\\ImmersiveChefs\\Textures\\ImmersiveChefs\\Things\\Item\\Kitchenware\\Plate\\Plate_Dirty.png",
    "previousSha256": "15786DB38A693155EEF1E7DEB7B710E78460484B7960C561934C67D1FB859E1A",
    "preOutlineSha256": "91905B9E66E4C140D36F1783AC29047C12E3DE5BED89BAE29A31AC628AF37467",
    "diffuseSha256": "A5B40D5BA58F77583198D069AA7D3F64B0DB6957FD46BA811DCE9B4C5F9AF347",
    "maskSha256": "175D06081C19F91E4BB77F0305FA59C680AF2DFE8F8DABEF84B5AF724A9D381E"
  },
  {
    "path": "mods\\ImmersiveChefs\\Textures\\ImmersiveChefs\\Things\\Item\\Kitchenware\\Plate\\Plate_Wood.png",
    "previousSha256": "923C2DF3D232F623282925BF8F654745D2504E72C4F6FA14F9A3E397120C76D9",
    "preOutlineSha256": "91BCC7AF48D27CAE0FA8CF1808B217E09AA75193AACDF10170174157BA691A68",
    "diffuseSha256": "F571614055E614138C58431F0A1D86C59528F3849B227D3CD4631890EEAA6B35",
    "maskSha256": "F13A397B96AA82B7607651F826B75E626E61231A710393F7DF6256DC46608261"
  },
  {
    "path": "mods\\ImmersiveChefs\\Textures\\ImmersiveChefs\\Things\\Item\\Kitchenware\\Plate\\Plate_WoodDirty.png",
    "previousSha256": "304E7EB7D7798534ECEF3FBCA1937935B7D9422300355F542935B4E2A69313C0",
    "preOutlineSha256": "FFA877CA763DBAD9E580C39C973C213A2AEF578A313FFFB3F5A27D4AC420C927",
    "diffuseSha256": "B04504D6237D6266C81B3FB3B6367ACD93462FA3FAC7B7F73DA3C659D0284775",
    "maskSha256": "091A9DD345A4E8663A035EE9B1D9AB46F694D8AC46048C2CEFF3BD52D6C817A3"
  },
  {
    "path": "mods\\ImmersiveChefs\\Textures\\ImmersiveChefs\\Things\\Item\\Kitchenware\\Plate\\Plate_Stone.png",
    "previousSha256": "604AD5CEC11E32A09CE39E51106E16086519529E05AAD84869470B9A154C71EE",
    "preOutlineSha256": "9FF3C5710A2D813D8C294BF573AD91729C2286DD695592511D17E3CD567E440D",
    "diffuseSha256": "974800EE3EC8732A425AC30E5B4DF525BF0B78D2F083E16F01674BED8249D4EF",
    "maskSha256": "F13A397B96AA82B7607651F826B75E626E61231A710393F7DF6256DC46608261"
  },
  {
    "path": "mods\\ImmersiveChefs\\Textures\\ImmersiveChefs\\Things\\Item\\Kitchenware\\Plate\\Plate_StoneDirty.png",
    "previousSha256": "671F233A621F69D704DB46AF4491206F7E71E15C30F78DC759B14D96572C663D",
    "preOutlineSha256": "985AAFCAEAB172B2E10A94CFE5075B01F0FB8BC96F8BA121B09945D56B4F9E6A",
    "diffuseSha256": "2D408FA4D2CC572C6B31FDEDF17DAFC0B25C6E57B2BACC16D3B0515628BBF9B9",
    "maskSha256": "F5697C790E8123D4D43D864854F9F1B5161FE8E003DFAF7928BAAB489829C22C"
  },
  {
    "path": "mods\\ImmersiveChefs\\Textures\\ImmersiveChefs\\Things\\Item\\Kitchenware\\ChefsKnife\\ChefsKnife.png",
    "previousSha256": "58098567C9794058393D3EDA4FC3BFAF947CCF67653B538DCC1411F41F3B0888",
    "preOutlineSha256": "8BA9AEAB15663DFB9547622A2BADA97357FD26595F7EDE51D2EC710C2B934E71",
    "diffuseSha256": "465BBAB4647084931315053503522343E8869945023BDE451D73D2D9ABB3B22C",
    "maskSha256": "2E82ABB5B04843FBF0C09136D043DBF0317D66B122FF3C6B02BC12D9F30FC225"
  }
]
```

## plate-p8-prompt.txt

```text
Edit image1, one completely empty shallow RimWorld dinner plate. Images2–4 are original Core style references only. Keep the exact overall camera, outline shape, flat shallow construction and broad lip from image1. At256square the upper ellipse is216wide180deep, with only6pixels of near-side thickness, inner well170wide141deep. No bowl, no raised button or coin, no extra objects.

Repaint its surfaces in the economical matte hand-painted style of the vanilla references. Remove the many subtle concentric gradient bands. Give it ONE broad gently uneven darker inner-recess crescent strongest on the upper-left, fading softly into a quiet pale open center; on the opposite lower-right inner edge keep only a faint short transition. Slightly irregular broad pale lip, bright on the upper-left, subdued on the lower-right. This asymmetric light should make the face visibly concave without drawing a black inner circle. Only a few broad paint masses, no noise, speckles, scratches or glossy highlights. The perimeter remains decisive dark drawn ink, with small natural drawn variation, not a mathematically perfect vector ring.

Material illumination: pale rim235–245, quiet center210–220, upper inner shadow165–180, thin near face160–180, exterior near23/19/15. Bright neutral gray because it will be tinted to wood, stone and metal by the game. Preserve the complete shallow plate and generous transparent padding. Genuine transparent background. No handles, contents, decorative rim, text, ground, cast shadow or contact sheet.
```

## plate-p8-wood-prompt.txt

```text
Edit image1 ONLY to give the same shallow empty plate a quietly hand-painted wooden surface, while retaining its exact outline, broad rim, shallow interior and fixed overhead camera. Image2 is a RimWorld vanilla material-style reference only; do not copy the log or pixels. Keep the plate's dimensions/proportions and all edges unchanged.

This is a NEUTRAL GRAYSCALE material texture for game tinting, not a finished brown wooden plate. Keep the selected bright lip, pale center, upper-left recess shadow and thin near-side face. Add only two or three broad soft gently curving gray wood-grain streaks flowing coherently across the open face and following the lip's surface. Subtle original matte brushwork, no thin lines, knot, tree rings, radial segments, planks, carved decoration, scratches or speckles. Grain should survive as calm broad tonal variation at64pixels but not dominate the plate. Base illumination stays rim235–245, center210–220, recess165–180, thin near face170–180, exterior nearblack. Grain only about10–22gray levels different from the local surface.

Exactly one clean empty plate, no contents or other objects. Preserve genuine transparency, centered placement, complete silhouette and generous margins. No floor, cast shadow, text or contact sheet.
```

## plate-p8-stone-prompt.txt

```text
Edit image1 ONLY to give this same shallow empty plate a restrained matte carved-stone surface. Image2 is an original RimWorld vanilla material-style reference; do not copy its block geometry or pixels. Preserve the plate's complete outline, lip, shallow interior, thin near edge, overhead camera and placement exactly.

Use NEUTRAL GRAYSCALE because the game will tint the material. Retain the bright upper lip, pale quiet center, broad upper-left inner-recess shade and very thin near face. Add only three or four large soft irregular low-contrast gray patches, like broad economical vanilla stone paint. Each patch is a broad mass, not a tiny speck. No cracks, chips, gravel, veins, spots, noise, faceted polygons or decoration. Local surface variation about10–22gray levels. Overall rim235–245, center210–220, recess165–180, nearface170–180, outer contour nearblack. It should still look smooth enough to be a usable clean dish, with modest stone character visible at64pixels.

One complete clean empty plate with identical geometry, genuine transparent background, generous margins. No contents, extra objects, text, ground or cast shadow. Not a sheet.
```

## plate-p8-dirty-prompt.txt

```text
Edit this exact shallow empty RimWorld plate by adding ONLY a small amount of visible food residue after someone ate. Keep the plate silhouette, lip, gray material, lighting, camera and placement identical. Paint two or three broad irregular FLAT smears inside the shallow center: one tan sauce streak (luminance140–160), one dark muted brown/olive smear (75–110), and optionally a small connecting greasy residue mark. Together cover about15–20percent of the plate's visible material area. Keep them broad enough to read when reduced to32pixels; no tiny specks.

The marks are thin stains on the dish, no whole food, raised chunks, new bowl, decoration, text, cracks, black hole or floating crumbs. Their irregular edges should look hand-painted like vanilla RimWorld food art. Leave most of the pale plate empty and preserve the inner recess shadow. Keep residue away from the exterior contour and outer lip. Warm tan/brown/olive are the ONLY colored pixels; the plate itself remains neutral grayscale so the residue can be separated for a game material mask.

One complete dirty plate sprite with genuine transparent background and the same generous padding. No ground or cast shadow, no other objects or sheet.
```

## knife-k7-prompt.txt

```text
Edit this complete original chef's knife sprite. Keep one broad triangular kitchen knife with a short dark handle, the same hand-painted RimWorld style, the same long-axis direction and approximately the same overall length and silhouette. Make these precise corrections coherently across the whole illustration:

The actual metal blade, excluding black outline, must be about19percent broader perpendicular to its length. Keep its straight spine and smooth convex cutting belly, but make the heel broader. Make the actual dark handle about17percent broader too. Make the metal collar20percent shorter along the knife's long axis. Keep all joins continuous with no protruding guard or seam. Tilt the entire tool slightly farther upward so the straight long axis is17degrees above screen horizontal, handle lower-left and tip upper-right.

Repaint the broad blade face as matte nearwhite235–245gray so it remains bright after game material tint. Its lower-edge bevel must be a NARROW restrained gray edge, only2–4pixels at the equivalent256canvas; remove the broad dark blade band. Face and bevel are broad slightly uneven hand-painted values, no shiny white streak. Collar gray170–200, handle dark charcoal45–65 with one quiet broad patch, exterior ink near23/19/15. At256-equivalent full-tool length216, true blade144long44broad, true handle68long30broad, collar6long; black outline is outside those material dimensions.

One isolated coherent raster sprite on genuine transparency. No extra knife, roll, sheath, strap, rivet, text, hands, ground or cast shadow. Preserve the simple economical vanilla paint; no chrome or photoreal texture.
```

## Exact finishing recipe: plate-p8-author.py

```python
from pathlib import Path
from PIL import Image
import numpy as np,json,hashlib,xml.etree.ElementTree as ET
from scipy.ndimage import label,map_coordinates
r=Path('artifacts/VisualAssets/PortableRemake-20260913');d=r/'finishing-p8v2';d.mkdir(exist_ok=False)
p=r/'plate-p8-generated.png';a=np.array(Image.open(p).convert('RGBA'));lab,n=label(a[:,:,3]>0,np.ones((3,3)));ct=np.bincount(lab.ravel());ct[0]=0;keep=lab==ct.argmax();removed=(a[:,:,3]>0)&~keep;a[~keep]=0
h,w=a.shape[:2];yy,xx=np.mgrid[:h,:w];rgb=a[:,:,:3].astype(float);Y=rgb@np.array([.2126,.7152,.0722]);valid=keep&(Y>55)
mapped=np.interp(Y,[0,55,85,100,135,160,190,200,210,220,235,255],[0,55,95,130,165,178,202,212,220,239,245,249])
q=((xx-625)/414)**2+((yy-622)/349)**2;rim=np.clip((q-.985)/.035,0,1)*(yy<1078)*(Y>100)
rimY=np.clip(240+.35*(Y-220),235,245);mapped=mapped*(1-rim)+rimY*rim
face=(yy>=1078)&(yy<1090)&(Y>90)&keep;mapped[face]=np.clip(173+.3*(Y[face]-155),170,180)
b=a.copy();b[:,:,:3][valid]=np.clip(np.rint(rgb[valid]+(mapped-Y)[valid,None]),0,255).astype('uint8');b[b[:,:,3]==0]=0
Image.fromarray(b).save(d/'painted-source.png')
oldy=np.where(yy<=1046,176+(yy-176)*902/870,np.where(yy<1075,1078+(yy-1046)*12/29,yy+15)).astype(float)
prem=b.astype(float);prem[:,:,:3]*=prem[:,:,3,None]/255
warped=np.stack([map_coordinates(prem[:,:,c],[oldy,xx.astype(float)],order=1,mode='constant',cval=0) for c in range(4)],axis=2)
out=warped.copy();out[:,:,:3]=np.divide(warped[:,:,:3]*255,warped[:,:,3,None],out=np.zeros_like(warped[:,:,:3]),where=warped[:,:,3,None]>0);out=np.clip(np.rint(out),0,255).astype('uint8');out[out[:,:,3]==0]=0
Image.fromarray(out).save(d/'source-authored.png')
s=216/1044;cx=625;cy=625.5;inv=(1/s,0,cx-128/s,0,1/s,cy-128/s)
im=Image.fromarray(out).convert('RGBa').transform((256,256),Image.Transform.AFFINE,inv,Image.Resampling.BICUBIC).convert('RGBA');z=np.array(im);z[z[:,:,3]==0]=0;Image.fromarray(z).save(d/'normalized.png')
l=z[:,:,:3]@np.array([.2126,.7152,.0722]);m=np.zeros_like(z);m[:,:,0]=np.rint(np.clip((l-40)/60,0,1)*255).astype('uint8');m[:,:,3]=z[:,:,3];m[z[:,:,3]==0]=0;Image.fromarray(m).save(d/'normalized_m.png')
sha=lambda f:hashlib.sha256(f.read_bytes()).hexdigest();root=ET.Element('spriteOutlineApprovals');ET.SubElement(root,'sprite',id='plate',**{'class':'portable-broad','preOutlineSha256':sha(d/'normalized.png').upper(),'finalWidth':'64','finalHeight':'64','foregroundComponents':'1','backgroundHoles':'0','reviewEvidence':'P8v2 explicit source projection and paint authoring; root observed P8 source; new derivative pending independent static review.'});ET.indent(root);ET.ElementTree(root).write(d/'outline-topology.xml',encoding='utf-8',xml_declaration=True)
rec={'source':str(p),'sourceSha256':sha(p),'authoredSha256':sha(d/'source-authored.png'),'normalizedSha256':sha(d/'normalized.png'),'scale':s,'translation':[128-s*cx,128-s*cy],'removedDetachedPixels':int(removed.sum()),'alphaBounds':Image.fromarray(z).getbbox(),'newTopDepth':870*s,'newFaceDepth':29*s,'status':'authored candidate; not static/native accepted'};(d/'normalization.json').write_text(json.dumps(rec,indent=2)+'\n');print(json.dumps(rec,indent=2))
```

## Exact finishing recipe: plate-p8-family-v2.py

```python
from pathlib import Path
from PIL import Image
from scipy.ndimage import label,gaussian_filter,distance_transform_edt
import numpy as np,json,hashlib,xml.etree.ElementTree as ET
r=Path('artifacts/VisualAssets/PortableRemake-20260913');d=r/'family-p8v2';d.mkdir(exist_ok=False)
base=np.array(Image.open(r/'finishing-p8v2/normalized.png'));mask=np.array(Image.open(r/'finishing-p8v2/normalized_m.png'));yy,xx=np.mgrid[:256,:256];L=np.array([.2126,.7152,.0722]);by=base[:,:,:3]@L;material=(mask[:,:,0]>240)&(base[:,:,3]>128);ys,xs=np.where(base[:,:,3]>=128);bw=xs.max()+1-xs.min();bcx=(xs.max()+1+xs.min())/2;bcy=(ys.max()+1+ys.min())/2
records=[];sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
def donor(kind):
 p=r/f'plate-p8-{kind}-generated.png';a=np.array(Image.open(p).convert('RGBA'));lab,n=label(a[:,:,3]>0,np.ones((3,3)));ct=np.bincount(lab.ravel());ct[0]=0;k=lab==ct.argmax();a[~k]=0;y,x=np.where(a[:,:,3]>=128);s=bw/(x.max()+1-x.min());cx=(x.max()+1+x.min())/2;cy=(y.max()+1+y.min())/2;iv=(1/s,0,cx-bcx/s,0,1/s,cy-bcy/s);im=Image.fromarray(a).convert('RGBa').transform((256,256),Image.Transform.AFFINE,iv,Image.Resampling.BICUBIC).convert('RGBA');im.save(d/f'donor-{kind}-registered.png');records.append({'kind':kind,'source':str(p),'sha256':sha(p),'uniformScale':s,'translation':[bcx-s*cx,bcy-s*cy]});return np.array(im)
q=np.sqrt(((xx-128)/86)**2+((yy-124)/69.7)**2);fade=np.clip((distance_transform_edt(material)-2)/4,0,1)*np.clip((abs(q-1)*69.7-2)/3,0,1);clean={'':base.copy()}
for kind,sigma in [('wood',14),('stone',18)]:
 a=donor(kind);lum=a[:,:,:3]@L;valid=(a[:,:,3]>128)&(lum>100);smooth=np.divide(gaussian_filter(lum*valid,sigma),gaussian_filter(valid.astype(float),sigma),out=np.zeros_like(lum),where=gaussian_filter(valid.astype(float),sigma)>1e-6);residual=(gaussian_filter(lum,1.0)-smooth)*fade
 # Plane-local centering keeps the reviewed base illumination as the family baseline.
 for region in [q<.95,q>1.05]:
  sel=region&material&(fade>.5);residual[sel]-=np.mean(residual[sel])
 delta=np.clip(residual,-22,22)*fade;z=base.copy();z[:,:,:3]=np.clip(np.rint(base[:,:,:3].astype(float)+delta[:,:,None]),0,255).astype('uint8');z[~material]=base[~material];z[z[:,:,3]==0]=0;clean['_'+kind.title()]=z;Image.fromarray(np.clip(np.rint(delta+128),0,255).astype('uint8')).save(d/f'{kind}-paint-delta.png')
a=donor('dirty');rgb=a[:,:,:3].astype(float);lum=rgb@L;sat=rgb[:,:,0]-rgb[:,:,2];t=np.clip((sat-8)/18,0,1)*(a[:,:,3]/255)*(q<.90)*material
target=np.where(lum>165,np.clip(150+.2*(lum-195),140,160),np.clip(95+.3*(lum-130),75,110));residue=np.clip(rgb+(target-lum)[:,:,None],0,255);dark=lum<=165;residue[dark]=np.clip(np.column_stack((target[dark]+44,target[dark]-7,target[dark]-60)),0,255);layer=np.zeros_like(a);layer[:,:,:3]=np.rint(residue).astype('uint8');layer[:,:,3]=np.rint(t*255).astype('uint8');layer[layer[:,:,3]==0]=0;Image.fromarray(layer).save(d/'residue-layer.png')
root=ET.Element('spriteOutlineApprovals');files=[]
for suffix,arr in clean.items():
 for dirty in [False,True]:
  stem='Plate'+suffix+('Dirty' if suffix and dirty else '_Dirty' if dirty else '');z=arr.copy();m=mask.copy()
  if dirty:
   z[:,:,:3]=np.rint(arr[:,:,:3]*(1-t[:,:,None])+residue*t[:,:,None]).astype('uint8');zy=z[:,:,:3]@L;ay=arr[:,:,:3]@L;m[:,:,0]=np.rint(np.divide(mask[:,:,0]*ay*(1-t),zy,out=np.zeros_like(zy),where=zy>0)).clip(0,255).astype('uint8')
  z[z[:,:,3]==0]=0;m[z[:,:,3]==0]=0;assert np.array_equal(z[:,:,3],base[:,:,3]) and np.array_equal(m[:,:,3],base[:,:,3]);Image.fromarray(z).save(d/f'{stem}.normalized.png');Image.fromarray(m).save(d/f'{stem}.normalized_m.png')
  ET.SubElement(root,'sprite',id=stem,**{'class':'portable-broad','preOutlineSha256':sha(d/f'{stem}.normalized.png').upper(),'finalWidth':'64','finalHeight':'64','foregroundComponents':'1','backgroundHoles':'0','reviewEvidence':'P8 reviewed geometry; generated material/residue donors; exact family alpha and contour conservation; final family review pending.'});files.append({'stem':stem,'sha256':sha(d/f'{stem}.normalized.png'),'changedMaterialPixels':int(np.any(z[:,:,:3]!=base[:,:,:3],2).sum())})
ET.indent(root);ET.ElementTree(root).write(d/'outline-topology.xml',encoding='utf-8',xml_declaration=True)
coverage=float((t>.5).sum()/material.sum());rec={'donors':records,'files':files,'opaqueDirtCoverage':coverage,'allAlphaMatchesBase':True,'dirtLightMedian':float(np.median((residue@L)[(t>.9)&(lum>165)])),'dirtDarkMedian':float(np.median((residue@L)[(t>.9)&(lum<=165)])),'status':'candidate family, static/native review pending'};(d/'family.json').write_text(json.dumps(rec,indent=2)+'\n');print(json.dumps(rec,indent=2))
```

## Exact finishing recipe: knife-k7-finish.py

```python
from pathlib import Path
from PIL import Image
from scipy.ndimage import label,map_coordinates
import numpy as np,math,json,hashlib,xml.etree.ElementTree as ET
r=Path('artifacts/VisualAssets/PortableRemake-20260913');d=r/'finishing-k7v2';d.mkdir(exist_ok=False);p=r/'knife-k7-generated.png';a=np.array(Image.open(p).convert('RGBA'));h,w=a.shape[:2];yy,xx=np.mgrid[:h,:w]
lab,n=label(a[:,:,3]>0,np.ones((3,3)));ct=np.bincount(lab.ravel());ct[0]=0;keep=lab==ct.argmax();removed=(a[:,:,3]>0)&~keep;a[~keep]=0;Y=a[:,:,:3]@np.array([.2126,.7152,.0722])
metal,n=label((Y>120)&(a[:,:,3]>128),np.ones((3,3)));blade=metal==metal[600,700];collar=metal==metal[700,425];assert blade.sum()>100000 and 2000<collar.sum()<7000
tc=math.radians(18.04927256);cu=xx*math.cos(tc)-yy*math.sin(tc);cv=np.rint(xx*math.sin(tc)+yy*math.cos(tc)).astype(int);cut=np.zeros_like(keep)
for row in np.unique(cv[collar]):
 sl=collar&(cv==row);cut[sl]=cu[sl]<cu[sl].min()+3
collar=collar&~cut
handle=(xx<430)&(yy>630)&(Y>33)&(Y<120)&keep
b=a.copy();rgb=b[:,:,:3].astype(float);target=Y.copy();face=blade&(Y>=200);bevel=blade&~face
target[face]=np.clip(240+.30*(Y[face]-228),235,245);target[bevel]=np.clip(180+.55*(Y[bevel]-170),170,195);target[handle]=np.clip(55+.40*(Y[handle]-63),45,65);target[collar]=np.clip(185+.45*(Y[collar]-169),170,200)
sel=blade|handle|collar;rgb[sel]+= (target-Y)[sel,None];b[:,:,:3]=np.clip(np.rint(rgb),0,255).astype('uint8');b[:,:,:3][cut]=[23,19,15];b[b[:,:,3]==0]=0
m=np.zeros_like(b);m[:,:,0][blade|collar]=255;m[:,:,3]=b[:,:,3]
Image.fromarray(b).save(d/'painted-source.png');Image.fromarray(m).save(d/'painted-source_m.png')
t=math.radians(22.7250824549);u=xx*math.cos(t)-yy*math.sin(t);v=xx*math.sin(t)+yy*math.cos(t);weight=np.clip((125-u)/40,0,1);weight=weight*weight*(3-2*weight);factor=1+.04*weight;oldv=823.5+(v-823.5)/factor;ox=u*math.cos(t)+oldv*math.sin(t);oy=-u*math.sin(t)+oldv*math.cos(t)
def warp(z):
 q=z.astype(float);q[:,:,:3]*=q[:,:,3,None]/255;o=np.stack([map_coordinates(q[:,:,c],[oy,ox],order=1,mode='constant',cval=0) for c in range(4)],2);o[:,:,:3]=np.divide(o[:,:,:3]*255,o[:,:,3,None],out=np.zeros_like(o[:,:,:3]),where=o[:,:,3,None]>0);o=np.clip(np.rint(o),0,255).astype('uint8');o[o[:,:,3]==0]=0;return o
b2=warp(b);m2=warp(m);m2[:,:,3]=b2[:,:,3];Image.fromarray(b2).save(d/'source-authored.png');Image.fromarray(m2).save(d/'source-authored_m.png')
rot=math.radians(22.7250824549-19.95);co=math.cos(rot);si=math.sin(rot);sy,sx=np.where(b2[:,:,3]>=128);corners=np.vstack([np.column_stack((sx+dx,sy+dy)) for dx,dy in [(0,0),(1,0),(0,1),(1,1)]]);rx=corners[:,0]*co-corners[:,1]*si;ry=corners[:,0]*si+corners[:,1]*co;s=216/(rx.max()-rx.min());tx=128-s*(rx.min()+rx.max())/2;ty=128-s*(ry.min()+ry.max())/2;inv=(co/s,si/s,(-co*tx-si*ty)/s,-si/s,co/s,(si*tx-co*ty)/s)
def normalize(z):return np.array(Image.fromarray(z).convert('RGBa').transform((256,256),Image.Transform.AFFINE,inv,Image.Resampling.BICUBIC).convert('RGBA'))
z=normalize(b2);zm=normalize(m2);zm[:,:,3]=z[:,:,3];z[z[:,:,3]==0]=0;zm[z[:,:,3]==0]=0;Image.fromarray(z).save(d/'normalized.png');Image.fromarray(zm).save(d/'normalized_m.png')
sha=lambda f:hashlib.sha256(f.read_bytes()).hexdigest();root=ET.Element('spriteOutlineApprovals');ET.SubElement(root,'sprite',id='knife',**{'class':'portable-fine','preOutlineSha256':sha(d/'normalized.png').upper(),'finalWidth':'64','finalHeight':'64','foregroundComponents':'1','backgroundHoles':'0','reviewEvidence':'K7v2 explicit handle/collar/paint authoring and whole rotation; new candidate pending independent visual review.'});ET.indent(root);ET.ElementTree(root).write(d/'outline-topology.xml',encoding='utf-8',xml_declaration=True)
rec={'sourceSha256':sha(p),'normalizedSha256':sha(d/'normalized.png'),'removedDetachedPixels':int(removed.sum()),'collarPaintNarrowedPixels':int(cut.sum()),'handlePerpendicularFactor':1.04,'uniformScale':s,'wholeRotationDegrees':math.degrees(rot),'translation':[tx,ty],'alphaBounds':Image.fromarray(z).getbbox(),'strongHeight':s*(ry.max()-ry.min()),'predictedTrueBlade':[808.45*s,221.08*s],'predictedTrueHandle':[353.27*s,147.54*1.04*s],'status':'authored candidate, independent/new native acceptance pending'};(d/'normalization.json').write_text(json.dumps(rec,indent=2)+'\n');print(json.dumps(rec,indent=2))
```
