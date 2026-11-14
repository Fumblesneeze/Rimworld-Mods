Owner: `fumblesneeze.megamod`, S141-R1–R3. English component pilot only.

`megamod.settings-search.native-recorder` uses the actual installed Dreadarm Pick Up And Haul
page (Workshop3813653998), with no product registration or package-specific adapter. Test-only
instrumentation surrounds the existing native dialog's virtual page call and forwards literal
TipRegion arguments. It never constructs a replacement settings page or calls a foreign draw twice.

The workflow opens the native page, captures before values, enables recording and captures after
values, observes actual help containing `bleeding`, checks failure/backend replacement cleanup,
then deliberately edits Haul on the way through its original checkbox. Native close writes the
foreign file; reopen retains the option; another native click restores it and native close persists
the restoration. Supporting reads inspect the fixture's actual values and output XML. These are
diagnostics, not substitute search UI or automatic-discovery acceptance. Screenshots show the real
before/after page, edit, reopening, restoration and native console. The acting root must inspect them.

Failure cleanup removes only this test's patches, restores fixture values and closes only owned
windows. The grouped runner owns isolated configuration, exact process, pointers and final cleanup.
No ordinary-user settings file is touched. The underlying Workshop installation stays read-only.
