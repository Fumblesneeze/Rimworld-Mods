# SS-READ-CONTROL-EXIT: retain settings control values

## Ownership and traceability

Owner `fumblesneeze.megamod`; S141 SS-06, paired with
[SS-READ-CONTROL-ENTRY](SS-READ-CONTROL-ENTRY.md). Concrete callbacks retain typed input in
prefix state and restore returned/ref values in a guaranteed finalizer, preserving exceptions.
Each `<targetId>.finalizer` is an independently owned receipt in the same atomic reader group.
Implementation: SettingsSearch/Harmony/SettingsReadControlGuard.cs and Runtime/SettingsReadControlGuards.cs.

## Target and minimal predicates

All methods below are nongeneric static methods except the exact numeric closures. `GUI` means
UnityEngine.GUI in UnityStubs; `Widgets` means Verse.Widgets in Assembly-CSharp. All are public
except GUI.BeginScrollViewImpl. Every binder checks the exact declared signature, including refs.

| Target ID | Native target | Retained outward value |
| --- | --- | --- |
| SS-READ-GUI-TEXT | GUI.TextField(Rect,string,int,GUIStyle):string | original text, including null |
| SS-READ-GUI-AREA | GUI.TextArea(Rect,string,int,GUIStyle):string | original text |
| SS-READ-GUI-PASSWORD | GUI.PasswordField(Rect,string,char,GUIStyle):string | original text |
| SS-READ-WIDGET-TEXT | Widgets.TextField(Rect,string):string | original text, including null |
| SS-READ-WIDGET-TEXT-LIMIT | Widgets.TextField(Rect,string,int,Regex):string | original text |
| SS-READ-WIDGET-AREA | Widgets.TextArea(Rect,string,bool):string | original text |
| SS-READ-DELAYED-TEXT | Widgets.DelayedTextField(Rect,string,ref string,string,string):string | text and buffer |
| SS-READ-NUMERIC-INT | Widgets.TextFieldNumeric<int>(Rect,ref int,ref string,float,float):void | value and buffer |
| SS-READ-NUMERIC-FLOAT | Widgets.TextFieldNumeric<float>(Rect,ref float,ref string,float,float):void | exact float and buffer |
| SS-READ-PERCENT | Widgets.TextFieldPercent(Rect,ref float,ref string,float,float):void | exact float and buffer |
| SS-READ-VECTOR | Widgets.TextFieldVector(Rect,ref Vector3,ref string[],float,float):void | vector, array identity and original entries |
| SS-READ-SLIDER | Widgets.HorizontalSlider(Rect,float,float,float,bool,string,string,string,float):float | exact original value |
| SS-READ-SLIDER-REF | Widgets.HorizontalSlider(Rect,ref float,FloatRange,string,float):void | exact original ref value |
| SS-READ-FREQUENCY | Widgets.FrequencyHorizontalSlider(Rect,float,float,float,bool):float | exact original value |
| SS-READ-GUI-SCROLL | private GUI.BeginScrollViewImpl(Rect,Vector2,Rect,GUIStyle,GUIStyle):Vector2 | original scroll position |
| SS-READ-GUI-SCROLL-DEFAULT | GUI.BeginScrollView(Rect,Vector2,Rect):Vector2 | original public-entry scroll position |
| SS-READ-GUI-SCROLL-STYLE | GUI.BeginScrollView(Rect,Vector2,Rect,GUIStyle,GUIStyle):Vector2 | original public-entry scroll position |
| SS-READ-GUI-SCROLL-BARS | GUI.BeginScrollView(Rect,Vector2,Rect,bool,bool):Vector2 | original public-entry scroll position |
| SS-READ-GUI-SCROLL-BARS-STYLE | GUI.BeginScrollView(Rect,Vector2,Rect,bool,bool,GUIStyle,GUIStyle):Vector2 | original public-entry scroll position |
| SS-READ-WIDGET-SCROLL | Widgets.BeginScrollView(Rect,ref Vector2,Rect,bool):void | original scroll position |
| SS-READ-GUI-SLIDER | GUI.Slider(Rect,float,float,float,float,GUIStyle,GUIStyle,bool,int,GUIStyle):float | exact original value |
| SS-READ-FLOAT-RANGE | Widgets.FloatRange(Rect,int,ref FloatRange,float,float,string,ToStringStyle,float,GameFont,Color?,float):void | original range |
| SS-READ-INT-RANGE | Widgets.IntRange(Rect,int,ref IntRange,int,int,string,int):void | original range |
| SS-READ-RANGE-TEXT | Widgets.FloatRangeWithTypeIn(Rect,int,ref FloatRange,float,float,ToStringStyle,string):void | original range before text formatting/parsing |

Input snapshots are bounded to the consumed values. Vector input needs only the three entries the
native field can touch, preserving array identity and untouched additional entries. Do not reflect
over arbitrary settings or copy unbounded user collections. Null arrays remain null after the call;
short arrays may cause native exceptions, which are preserved after restoring existing entries.
The first real native run still returned a clamped position through the public three-argument
GUI.BeginScrollView despite an admitted private implementation guard. The exact compiled-call
mechanism is unproved; each public entry therefore has its own outward preservation guard and
native invocation coverage. A published private patch alone is not evidence of its callers' behavior.

## Composition, ordering, and invalidation

Prove this finalizer is last in the effective outer finalizer order and its prefix is first.
Earlier compatible finalizers may suppress exceptions, but retained original values still leave
the call unchanged. A paired prefix/finalizer uses one exact concrete state type/declaring type;
do not co-install another registration consuming that slot, even with the same callback/state type.
Unresolved postfix factories cannot prove state isolation and refuse paired admission without
being executed as an inspection probe. Preserve input
float bits rather than normalize/recalculate them. Return/ref restoration never invokes parsing,
formatting, provider callbacks or lazy getters. Unknown relevant factories/order disable the read.
The controlled string family retains its outermost original return while nested native string
calls use normal intermediate normalization. This is required by the limited Widgets.TextField:
its native body dereferences the simpler field's normalized string. A thread-owned nesting token
is always released in the finalizer, including exceptions; later independent calls must still
preserve null. This is native wrapper composition, not rollback of arbitrary reentrant foreign
callbacks. No native argument is rewritten and rendering/validation is not replaced.
Read current publication/owned slots before entry; arbitrary repatching during a call is not a
rollback guarantee. Original body variations are allowed because the consumed contract is outward
value preservation, not reconstruction of its arithmetic. Internal foreign I/O remains a limit.

## Admission, failure, and diagnostics

Follow the entry contract's atomic C01 acquisition, bounded warnings and ordinary-page fallback.
Always restore from acquired call state even on a thrown native/foreign callback. State acquisition
failure leaves the read failed rather than returning an apparently successful partial observation.
Disabling or rollback never removes another owner or another owned patch kind.

## Verification matrix and final observation

Host tests cover actual typed return/ref mutation and throw paths, first/last order, compatible
composition and exact closed numeric siblings. Native fixtures must use distinct would-be changed
values: truncating strings, uninitialized buffers, non-round-trip/over-limit percent values, vector
buffers, rounded sliders and out-of-bounds scroll positions. Capture exact before/action/after
native views and verify subsequent normal edits. The reviewed component workflow passed 31 steps
with five personally inspected captures; text/slider preservation and later deliberate edits were
visible. Numeric/vector/range/scroll assertions, including all four public scroll entries and both
Widgets modes, are supporting diagnostics. See local discovery/values/root/verification.md.
This does not establish complete generic page discovery or all remaining control contracts.
