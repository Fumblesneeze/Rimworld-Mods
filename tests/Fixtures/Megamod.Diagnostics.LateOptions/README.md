# Late native Options diagnostic fixture

Owner: `fumblesneeze.megamod`, S094. Development-only XML fixture, staged exclusively by the typed
selected E2E runner. No assembly, project or Workshop mutation is involved. Product packages never
depend on this fixture. The fixture itself declares its owning `fumblesneeze.megamod` dependency
in About metadata, matching its manifest owner and the selected group's order, as the fixture
planner requires. Although the single Def uses Core's OptionCategoryDef, this disposable fixture
belongs exclusively to the megamod verification workflow.

The exact default-loader toggle/reload scenario loads this after the existing diagnostics fixture.
It opens native Options exactly once, after disabling/reloading and enabling/reloading definition
context. Inspected 1.7.5000 Dialog_Options.PostOpen reports each unofficial category through ordinary
Log.Error; drawing ignores it before accessing its icon. This supplies a later same-thread native
diagnostic, not a direct Log call or manufactured retained record. The manifest expects its exact
message once. Intermediate accounting may observe zero; final accounting requires exactly one.

The existing diagnostics fixture remains unchanged and independently expects one MissingType
ErrorOnce occurrence for the process. Do not increase that count for reloads or clear once keys.
Native verification is pending; neither XML validity nor a passing build proves this workflow.
