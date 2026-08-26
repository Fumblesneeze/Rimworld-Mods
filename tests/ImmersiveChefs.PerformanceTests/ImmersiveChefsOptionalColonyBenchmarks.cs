using RimWorldDevGateway.PerformanceTesting;

namespace ImmersiveChefs.PerformanceTests;

internal static class OptionalColonyContract
{
    public const string ProcessorDubs = "immersive-chefs.processor-dubs";
    public const string GuestService = "immersive-chefs.guest-service";
    public const string VarietyVnpeMaterialDlc = "immersive-chefs.variety-vnpe-material-dlc";
    public const string AllSupported = "immersive-chefs.all-supported";
}

// Every concrete optional declaration repeats the base workload contract deliberately. The host
// reads attributes without loading this assembly, so hidden runtime inheritance would make an
// optional group look complete while permitting the substantial colony to stall.

[RimWorldPerformanceTest("immersive-chefs.processor-dubs.instrumented", ColonyContract.Owner,
    ColonyContract.Owner, "brrainz.harmony", "ludeon.rimworld", PerformanceTestContract.CircinusPackageId, "imranfish.xmlextensions",
    "syrchalis.processor.framework", "Dubwise.DubsBadHygiene", ColonyContract.Owner,
    WorkloadVersion = "immersive-chefs-colony/processor-dubs/v1",
    ComparisonId = OptionalColonyContract.ProcessorDubs, WarmUpTicks = ColonyContract.WarmUpTicks,
    SampleTicks = ColonyContract.SampleTicks, Repetitions = 3,
    EvidenceLens = PerformanceEvidenceLens.ProductInstrumented)]
[PerformanceMethodSelector(PerformanceMethodSelectorKind.HarmonyOwner, ColonyContract.Owner, "product-harmony")]
[PerformanceMethodSelector(PerformanceMethodSelectorKind.Method, "ImmersiveChefs.WorkGiver_DoDishes::JobOnThing", "dish-search")]
[PerformanceThroughputCheckpoint("native-map-ticks", 11_900)]
[PerformanceThroughputCheckpoint("observer-scans", 48)]
[PerformanceThroughputCheckpoint("meals-produced", 1)]
[PerformanceThroughputCheckpoint("simple-meals-produced", 1)]
[PerformanceThroughputCheckpoint("fine-meals-produced", 1)]
[PerformanceThroughputCheckpoint("lavish-meals-produced", 1)]
[PerformanceThroughputCheckpoint("assisted-cooking-sessions", 1)]
[PerformanceThroughputCheckpoint("ware-cleaned", 1)]
[PerformanceThroughputCheckpoint("domestic-dishwasher-cycles", 1)]
[PerformanceThroughputCheckpoint("industrial-dishwasher-cycles", 1)]
[PerformanceThroughputCheckpoint("map-meals-ingested", 1)]
[PerformanceThroughputCheckpoint("animal-meals-ingested", 4)]
[PerformanceThroughputCheckpoint("patients-fed", 1)]
[PerformanceThroughputCheckpoint("caravan-meals-ingested", 2)]
[PerformanceThroughputCheckpoint("microwave-reheats", 1)]
[PerformanceThroughputCheckpoint("optional-group-branches", 1)]
[PerformanceThroughputCheckpoint("immersive-chefs.processor-dubs-branch", 1)]
[PerformanceThroughputCheckpoint("processor-dubs-domestic-cycles", 1)]
[PerformanceThroughputCheckpoint("processor-dubs-industrial-cycles", 1)]
[PerformanceThroughputCheckpoint("processor-dubs-water-milliliters", 1)]
public sealed class ProcessorDubsInstrumentedBenchmark : ImmersiveChefsColonyBenchmark { }

[RimWorldPerformanceTest("immersive-chefs.processor-dubs.armed-disabled", ColonyContract.Owner,
    ColonyContract.Owner, "brrainz.harmony", "ludeon.rimworld", PerformanceTestContract.CircinusPackageId, "imranfish.xmlextensions",
    "syrchalis.processor.framework", "Dubwise.DubsBadHygiene", ColonyContract.Owner,
    WorkloadVersion = "immersive-chefs-colony/processor-dubs/v1",
    ComparisonId = OptionalColonyContract.ProcessorDubs, WarmUpTicks = ColonyContract.WarmUpTicks,
    SampleTicks = ColonyContract.SampleTicks, Repetitions = 3,
    EvidenceLens = PerformanceEvidenceLens.ArmedDisabledWrapper)]
[PerformanceMethodSelector(PerformanceMethodSelectorKind.HarmonyOwner, ColonyContract.Owner, "product-harmony")]
[PerformanceMethodSelector(PerformanceMethodSelectorKind.Method, "ImmersiveChefs.WorkGiver_DoDishes::JobOnThing", "dish-search")]
[PerformanceThroughputCheckpoint("native-map-ticks", 11_900)]
[PerformanceThroughputCheckpoint("observer-scans", 48)]
[PerformanceThroughputCheckpoint("meals-produced", 1)]
[PerformanceThroughputCheckpoint("simple-meals-produced", 1)]
[PerformanceThroughputCheckpoint("fine-meals-produced", 1)]
[PerformanceThroughputCheckpoint("lavish-meals-produced", 1)]
[PerformanceThroughputCheckpoint("assisted-cooking-sessions", 1)]
[PerformanceThroughputCheckpoint("ware-cleaned", 1)]
[PerformanceThroughputCheckpoint("domestic-dishwasher-cycles", 1)]
[PerformanceThroughputCheckpoint("industrial-dishwasher-cycles", 1)]
[PerformanceThroughputCheckpoint("map-meals-ingested", 1)]
[PerformanceThroughputCheckpoint("animal-meals-ingested", 4)]
[PerformanceThroughputCheckpoint("patients-fed", 1)]
[PerformanceThroughputCheckpoint("caravan-meals-ingested", 2)]
[PerformanceThroughputCheckpoint("microwave-reheats", 1)]
[PerformanceThroughputCheckpoint("optional-group-branches", 1)]
[PerformanceThroughputCheckpoint("immersive-chefs.processor-dubs-branch", 1)]
[PerformanceThroughputCheckpoint("processor-dubs-domestic-cycles", 1)]
[PerformanceThroughputCheckpoint("processor-dubs-industrial-cycles", 1)]
[PerformanceThroughputCheckpoint("processor-dubs-water-milliliters", 1)]
public sealed class ProcessorDubsArmedDisabledBenchmark : ImmersiveChefsColonyBenchmark { }

[RimWorldPerformanceTest("immersive-chefs.processor-dubs.disarmed", ColonyContract.Owner,
    ColonyContract.Owner, "brrainz.harmony", "ludeon.rimworld", PerformanceTestContract.CircinusPackageId, "imranfish.xmlextensions",
    "syrchalis.processor.framework", "Dubwise.DubsBadHygiene", ColonyContract.Owner,
    WorkloadVersion = "immersive-chefs-colony/processor-dubs/v1",
    ComparisonId = OptionalColonyContract.ProcessorDubs, WarmUpTicks = ColonyContract.WarmUpTicks,
    SampleTicks = ColonyContract.SampleTicks, Repetitions = 3,
    EvidenceLens = PerformanceEvidenceLens.FullyDisarmed)]
[PerformanceMethodSelector(PerformanceMethodSelectorKind.HarmonyOwner, ColonyContract.Owner, "product-harmony")]
[PerformanceMethodSelector(PerformanceMethodSelectorKind.Method, "ImmersiveChefs.WorkGiver_DoDishes::JobOnThing", "dish-search")]
[PerformanceThroughputCheckpoint("native-map-ticks", 11_900)]
[PerformanceThroughputCheckpoint("observer-scans", 48)]
[PerformanceThroughputCheckpoint("meals-produced", 1)]
[PerformanceThroughputCheckpoint("simple-meals-produced", 1)]
[PerformanceThroughputCheckpoint("fine-meals-produced", 1)]
[PerformanceThroughputCheckpoint("lavish-meals-produced", 1)]
[PerformanceThroughputCheckpoint("assisted-cooking-sessions", 1)]
[PerformanceThroughputCheckpoint("ware-cleaned", 1)]
[PerformanceThroughputCheckpoint("domestic-dishwasher-cycles", 1)]
[PerformanceThroughputCheckpoint("industrial-dishwasher-cycles", 1)]
[PerformanceThroughputCheckpoint("map-meals-ingested", 1)]
[PerformanceThroughputCheckpoint("animal-meals-ingested", 4)]
[PerformanceThroughputCheckpoint("patients-fed", 1)]
[PerformanceThroughputCheckpoint("caravan-meals-ingested", 2)]
[PerformanceThroughputCheckpoint("microwave-reheats", 1)]
[PerformanceThroughputCheckpoint("optional-group-branches", 1)]
[PerformanceThroughputCheckpoint("immersive-chefs.processor-dubs-branch", 1)]
[PerformanceThroughputCheckpoint("processor-dubs-domestic-cycles", 1)]
[PerformanceThroughputCheckpoint("processor-dubs-industrial-cycles", 1)]
[PerformanceThroughputCheckpoint("processor-dubs-water-milliliters", 1)]
public sealed class ProcessorDubsDisarmedBenchmark : ImmersiveChefsColonyBenchmark { }

[RimWorldPerformanceTest("immersive-chefs.guest-service.instrumented", ColonyContract.Owner,
    ColonyContract.Owner, "brrainz.harmony", "ludeon.rimworld", PerformanceTestContract.CircinusPackageId, "imranfish.xmlextensions",
    "Orion.Hospitality", "Orion.CashRegister", "Orion.Gastronomy", "avilmask.CommonSense", ColonyContract.Owner,
    WorkloadVersion = "immersive-chefs-colony/guest-service/v1",
    ComparisonId = OptionalColonyContract.GuestService, WarmUpTicks = ColonyContract.WarmUpTicks,
    SampleTicks = ColonyContract.SampleTicks, Repetitions = 3,
    EvidenceLens = PerformanceEvidenceLens.ProductInstrumented)]
[PerformanceMethodSelector(PerformanceMethodSelectorKind.HarmonyOwner, ColonyContract.Owner, "product-harmony")]
[PerformanceMethodSelector(PerformanceMethodSelectorKind.Method, "ImmersiveChefs.WorkGiver_DoDishes::JobOnThing", "dish-search")]
[PerformanceThroughputCheckpoint("native-map-ticks", 11_900)]
[PerformanceThroughputCheckpoint("observer-scans", 48)]
[PerformanceThroughputCheckpoint("meals-produced", 1)]
[PerformanceThroughputCheckpoint("simple-meals-produced", 1)]
[PerformanceThroughputCheckpoint("fine-meals-produced", 1)]
[PerformanceThroughputCheckpoint("lavish-meals-produced", 1)]
[PerformanceThroughputCheckpoint("assisted-cooking-sessions", 1)]
[PerformanceThroughputCheckpoint("ware-cleaned", 1)]
[PerformanceThroughputCheckpoint("domestic-dishwasher-cycles", 1)]
[PerformanceThroughputCheckpoint("industrial-dishwasher-cycles", 1)]
[PerformanceThroughputCheckpoint("map-meals-ingested", 1)]
[PerformanceThroughputCheckpoint("animal-meals-ingested", 4)]
[PerformanceThroughputCheckpoint("patients-fed", 1)]
[PerformanceThroughputCheckpoint("caravan-meals-ingested", 2)]
[PerformanceThroughputCheckpoint("microwave-reheats", 1)]
[PerformanceThroughputCheckpoint("optional-group-branches", 1)]
[PerformanceThroughputCheckpoint("immersive-chefs.guest-service-branch", 1)]
[PerformanceThroughputCheckpoint("guest-service-orders-served", 1)]
[PerformanceThroughputCheckpoint("guest-service-colony-settings-returned", 1)]
[PerformanceThroughputCheckpoint("guest-service-gastronomy-clearing-owned", 1)]
public sealed class GuestServiceInstrumentedBenchmark : ImmersiveChefsColonyBenchmark { }

[RimWorldPerformanceTest("immersive-chefs.guest-service.armed-disabled", ColonyContract.Owner,
    ColonyContract.Owner, "brrainz.harmony", "ludeon.rimworld", PerformanceTestContract.CircinusPackageId, "imranfish.xmlextensions",
    "Orion.Hospitality", "Orion.CashRegister", "Orion.Gastronomy", "avilmask.CommonSense", ColonyContract.Owner,
    WorkloadVersion = "immersive-chefs-colony/guest-service/v1",
    ComparisonId = OptionalColonyContract.GuestService, WarmUpTicks = ColonyContract.WarmUpTicks,
    SampleTicks = ColonyContract.SampleTicks, Repetitions = 3,
    EvidenceLens = PerformanceEvidenceLens.ArmedDisabledWrapper)]
[PerformanceMethodSelector(PerformanceMethodSelectorKind.HarmonyOwner, ColonyContract.Owner, "product-harmony")]
[PerformanceMethodSelector(PerformanceMethodSelectorKind.Method, "ImmersiveChefs.WorkGiver_DoDishes::JobOnThing", "dish-search")]
[PerformanceThroughputCheckpoint("native-map-ticks", 11_900)]
[PerformanceThroughputCheckpoint("observer-scans", 48)]
[PerformanceThroughputCheckpoint("meals-produced", 1)]
[PerformanceThroughputCheckpoint("simple-meals-produced", 1)]
[PerformanceThroughputCheckpoint("fine-meals-produced", 1)]
[PerformanceThroughputCheckpoint("lavish-meals-produced", 1)]
[PerformanceThroughputCheckpoint("assisted-cooking-sessions", 1)]
[PerformanceThroughputCheckpoint("ware-cleaned", 1)]
[PerformanceThroughputCheckpoint("domestic-dishwasher-cycles", 1)]
[PerformanceThroughputCheckpoint("industrial-dishwasher-cycles", 1)]
[PerformanceThroughputCheckpoint("map-meals-ingested", 1)]
[PerformanceThroughputCheckpoint("animal-meals-ingested", 4)]
[PerformanceThroughputCheckpoint("patients-fed", 1)]
[PerformanceThroughputCheckpoint("caravan-meals-ingested", 2)]
[PerformanceThroughputCheckpoint("microwave-reheats", 1)]
[PerformanceThroughputCheckpoint("optional-group-branches", 1)]
[PerformanceThroughputCheckpoint("immersive-chefs.guest-service-branch", 1)]
[PerformanceThroughputCheckpoint("guest-service-orders-served", 1)]
[PerformanceThroughputCheckpoint("guest-service-colony-settings-returned", 1)]
[PerformanceThroughputCheckpoint("guest-service-gastronomy-clearing-owned", 1)]
public sealed class GuestServiceArmedDisabledBenchmark : ImmersiveChefsColonyBenchmark { }

[RimWorldPerformanceTest("immersive-chefs.guest-service.disarmed", ColonyContract.Owner,
    ColonyContract.Owner, "brrainz.harmony", "ludeon.rimworld", PerformanceTestContract.CircinusPackageId, "imranfish.xmlextensions",
    "Orion.Hospitality", "Orion.CashRegister", "Orion.Gastronomy", "avilmask.CommonSense", ColonyContract.Owner,
    WorkloadVersion = "immersive-chefs-colony/guest-service/v1",
    ComparisonId = OptionalColonyContract.GuestService, WarmUpTicks = ColonyContract.WarmUpTicks,
    SampleTicks = ColonyContract.SampleTicks, Repetitions = 3,
    EvidenceLens = PerformanceEvidenceLens.FullyDisarmed)]
[PerformanceMethodSelector(PerformanceMethodSelectorKind.HarmonyOwner, ColonyContract.Owner, "product-harmony")]
[PerformanceMethodSelector(PerformanceMethodSelectorKind.Method, "ImmersiveChefs.WorkGiver_DoDishes::JobOnThing", "dish-search")]
[PerformanceThroughputCheckpoint("native-map-ticks", 11_900)]
[PerformanceThroughputCheckpoint("observer-scans", 48)]
[PerformanceThroughputCheckpoint("meals-produced", 1)]
[PerformanceThroughputCheckpoint("simple-meals-produced", 1)]
[PerformanceThroughputCheckpoint("fine-meals-produced", 1)]
[PerformanceThroughputCheckpoint("lavish-meals-produced", 1)]
[PerformanceThroughputCheckpoint("assisted-cooking-sessions", 1)]
[PerformanceThroughputCheckpoint("ware-cleaned", 1)]
[PerformanceThroughputCheckpoint("domestic-dishwasher-cycles", 1)]
[PerformanceThroughputCheckpoint("industrial-dishwasher-cycles", 1)]
[PerformanceThroughputCheckpoint("map-meals-ingested", 1)]
[PerformanceThroughputCheckpoint("animal-meals-ingested", 4)]
[PerformanceThroughputCheckpoint("patients-fed", 1)]
[PerformanceThroughputCheckpoint("caravan-meals-ingested", 2)]
[PerformanceThroughputCheckpoint("microwave-reheats", 1)]
[PerformanceThroughputCheckpoint("optional-group-branches", 1)]
[PerformanceThroughputCheckpoint("immersive-chefs.guest-service-branch", 1)]
[PerformanceThroughputCheckpoint("guest-service-orders-served", 1)]
[PerformanceThroughputCheckpoint("guest-service-colony-settings-returned", 1)]
[PerformanceThroughputCheckpoint("guest-service-gastronomy-clearing-owned", 1)]
public sealed class GuestServiceDisarmedBenchmark : ImmersiveChefsColonyBenchmark { }

[RimWorldPerformanceTest("immersive-chefs.variety-vnpe-material-dlc.instrumented", ColonyContract.Owner,
    ColonyContract.Owner, "brrainz.harmony", "ludeon.rimworld", PerformanceTestContract.CircinusPackageId, "imranfish.xmlextensions",
    "ludeon.rimworld.royalty", "ludeon.rimworld.biotech", "Argon.CoreLib",
    "OskarPotocki.VanillaFactionsExpanded.Core", "Argon.ExpandedMaterials.Masonry",
    "Argon.ExpandedMaterials.Metals", "Evyatar108.VarietyMattersImprovedRedux",
    "VanillaExpanded.VanillaFoodVarietyExpanded", "VanillaExpanded.VNutrientE", ColonyContract.Owner,
    WorkloadVersion = "immersive-chefs-colony/variety-vnpe-material-dlc/v1",
    ComparisonId = OptionalColonyContract.VarietyVnpeMaterialDlc, WarmUpTicks = ColonyContract.WarmUpTicks,
    SampleTicks = ColonyContract.SampleTicks, Repetitions = 3,
    EvidenceLens = PerformanceEvidenceLens.ProductInstrumented)]
[PerformanceMethodSelector(PerformanceMethodSelectorKind.HarmonyOwner, ColonyContract.Owner, "product-harmony")]
[PerformanceMethodSelector(PerformanceMethodSelectorKind.Method, "ImmersiveChefs.WorkGiver_DoDishes::JobOnThing", "dish-search")]
[PerformanceThroughputCheckpoint("native-map-ticks", 11_900)]
[PerformanceThroughputCheckpoint("observer-scans", 48)]
[PerformanceThroughputCheckpoint("meals-produced", 1)]
[PerformanceThroughputCheckpoint("simple-meals-produced", 1)]
[PerformanceThroughputCheckpoint("fine-meals-produced", 1)]
[PerformanceThroughputCheckpoint("lavish-meals-produced", 1)]
[PerformanceThroughputCheckpoint("assisted-cooking-sessions", 1)]
[PerformanceThroughputCheckpoint("ware-cleaned", 1)]
[PerformanceThroughputCheckpoint("domestic-dishwasher-cycles", 1)]
[PerformanceThroughputCheckpoint("industrial-dishwasher-cycles", 1)]
[PerformanceThroughputCheckpoint("map-meals-ingested", 1)]
[PerformanceThroughputCheckpoint("animal-meals-ingested", 4)]
[PerformanceThroughputCheckpoint("patients-fed", 1)]
[PerformanceThroughputCheckpoint("caravan-meals-ingested", 2)]
[PerformanceThroughputCheckpoint("microwave-reheats", 1)]
[PerformanceThroughputCheckpoint("optional-group-branches", 1)]
[PerformanceThroughputCheckpoint("immersive-chefs.variety-vnpe-material-dlc-branch", 1)]
public sealed class VarietyVnpeMaterialDlcInstrumentedBenchmark : ImmersiveChefsColonyBenchmark { }

[RimWorldPerformanceTest("immersive-chefs.variety-vnpe-material-dlc.armed-disabled", ColonyContract.Owner,
    ColonyContract.Owner, "brrainz.harmony", "ludeon.rimworld", PerformanceTestContract.CircinusPackageId, "imranfish.xmlextensions",
    "ludeon.rimworld.royalty", "ludeon.rimworld.biotech", "Argon.CoreLib",
    "OskarPotocki.VanillaFactionsExpanded.Core", "Argon.ExpandedMaterials.Masonry",
    "Argon.ExpandedMaterials.Metals", "Evyatar108.VarietyMattersImprovedRedux",
    "VanillaExpanded.VanillaFoodVarietyExpanded", "VanillaExpanded.VNutrientE", ColonyContract.Owner,
    WorkloadVersion = "immersive-chefs-colony/variety-vnpe-material-dlc/v1",
    ComparisonId = OptionalColonyContract.VarietyVnpeMaterialDlc, WarmUpTicks = ColonyContract.WarmUpTicks,
    SampleTicks = ColonyContract.SampleTicks, Repetitions = 3,
    EvidenceLens = PerformanceEvidenceLens.ArmedDisabledWrapper)]
[PerformanceMethodSelector(PerformanceMethodSelectorKind.HarmonyOwner, ColonyContract.Owner, "product-harmony")]
[PerformanceMethodSelector(PerformanceMethodSelectorKind.Method, "ImmersiveChefs.WorkGiver_DoDishes::JobOnThing", "dish-search")]
[PerformanceThroughputCheckpoint("native-map-ticks", 11_900)]
[PerformanceThroughputCheckpoint("observer-scans", 48)]
[PerformanceThroughputCheckpoint("meals-produced", 1)]
[PerformanceThroughputCheckpoint("simple-meals-produced", 1)]
[PerformanceThroughputCheckpoint("fine-meals-produced", 1)]
[PerformanceThroughputCheckpoint("lavish-meals-produced", 1)]
[PerformanceThroughputCheckpoint("assisted-cooking-sessions", 1)]
[PerformanceThroughputCheckpoint("ware-cleaned", 1)]
[PerformanceThroughputCheckpoint("domestic-dishwasher-cycles", 1)]
[PerformanceThroughputCheckpoint("industrial-dishwasher-cycles", 1)]
[PerformanceThroughputCheckpoint("map-meals-ingested", 1)]
[PerformanceThroughputCheckpoint("animal-meals-ingested", 4)]
[PerformanceThroughputCheckpoint("patients-fed", 1)]
[PerformanceThroughputCheckpoint("caravan-meals-ingested", 2)]
[PerformanceThroughputCheckpoint("microwave-reheats", 1)]
[PerformanceThroughputCheckpoint("optional-group-branches", 1)]
[PerformanceThroughputCheckpoint("immersive-chefs.variety-vnpe-material-dlc-branch", 1)]
public sealed class VarietyVnpeMaterialDlcArmedDisabledBenchmark : ImmersiveChefsColonyBenchmark { }

[RimWorldPerformanceTest("immersive-chefs.variety-vnpe-material-dlc.disarmed", ColonyContract.Owner,
    ColonyContract.Owner, "brrainz.harmony", "ludeon.rimworld", PerformanceTestContract.CircinusPackageId, "imranfish.xmlextensions",
    "ludeon.rimworld.royalty", "ludeon.rimworld.biotech", "Argon.CoreLib",
    "OskarPotocki.VanillaFactionsExpanded.Core", "Argon.ExpandedMaterials.Masonry",
    "Argon.ExpandedMaterials.Metals", "Evyatar108.VarietyMattersImprovedRedux",
    "VanillaExpanded.VanillaFoodVarietyExpanded", "VanillaExpanded.VNutrientE", ColonyContract.Owner,
    WorkloadVersion = "immersive-chefs-colony/variety-vnpe-material-dlc/v1",
    ComparisonId = OptionalColonyContract.VarietyVnpeMaterialDlc, WarmUpTicks = ColonyContract.WarmUpTicks,
    SampleTicks = ColonyContract.SampleTicks, Repetitions = 3,
    EvidenceLens = PerformanceEvidenceLens.FullyDisarmed)]
[PerformanceMethodSelector(PerformanceMethodSelectorKind.HarmonyOwner, ColonyContract.Owner, "product-harmony")]
[PerformanceMethodSelector(PerformanceMethodSelectorKind.Method, "ImmersiveChefs.WorkGiver_DoDishes::JobOnThing", "dish-search")]
[PerformanceThroughputCheckpoint("native-map-ticks", 11_900)]
[PerformanceThroughputCheckpoint("observer-scans", 48)]
[PerformanceThroughputCheckpoint("meals-produced", 1)]
[PerformanceThroughputCheckpoint("simple-meals-produced", 1)]
[PerformanceThroughputCheckpoint("fine-meals-produced", 1)]
[PerformanceThroughputCheckpoint("lavish-meals-produced", 1)]
[PerformanceThroughputCheckpoint("assisted-cooking-sessions", 1)]
[PerformanceThroughputCheckpoint("ware-cleaned", 1)]
[PerformanceThroughputCheckpoint("domestic-dishwasher-cycles", 1)]
[PerformanceThroughputCheckpoint("industrial-dishwasher-cycles", 1)]
[PerformanceThroughputCheckpoint("map-meals-ingested", 1)]
[PerformanceThroughputCheckpoint("animal-meals-ingested", 4)]
[PerformanceThroughputCheckpoint("patients-fed", 1)]
[PerformanceThroughputCheckpoint("caravan-meals-ingested", 2)]
[PerformanceThroughputCheckpoint("microwave-reheats", 1)]
[PerformanceThroughputCheckpoint("optional-group-branches", 1)]
[PerformanceThroughputCheckpoint("immersive-chefs.variety-vnpe-material-dlc-branch", 1)]
public sealed class VarietyVnpeMaterialDlcDisarmedBenchmark : ImmersiveChefsColonyBenchmark { }

[RimWorldPerformanceTest("immersive-chefs.all-supported.instrumented", ColonyContract.Owner,
    ColonyContract.Owner, "brrainz.harmony", "ludeon.rimworld", PerformanceTestContract.CircinusPackageId, "imranfish.xmlextensions",
    "ludeon.rimworld.royalty", "ludeon.rimworld.biotech", "Argon.CoreLib",
    "OskarPotocki.VanillaFactionsExpanded.Core", "syrchalis.processor.framework",
    "Argon.ExpandedMaterials.Masonry", "Argon.ExpandedMaterials.Metals", "Dubwise.DubsBadHygiene",
    "Orion.Hospitality", "Orion.CashRegister", "Orion.Gastronomy", "avilmask.CommonSense",
    "Evyatar108.VarietyMattersImprovedRedux", "VanillaExpanded.VanillaFoodVarietyExpanded",
    "VanillaExpanded.VNutrientE", ColonyContract.Owner,
    WorkloadVersion = "immersive-chefs-colony/all-supported/v1",
    ComparisonId = OptionalColonyContract.AllSupported, WarmUpTicks = ColonyContract.WarmUpTicks,
    SampleTicks = ColonyContract.SampleTicks, Repetitions = 3,
    EvidenceLens = PerformanceEvidenceLens.ProductInstrumented)]
[PerformanceMethodSelector(PerformanceMethodSelectorKind.HarmonyOwner, ColonyContract.Owner, "product-harmony")]
[PerformanceMethodSelector(PerformanceMethodSelectorKind.Method, "ImmersiveChefs.WorkGiver_DoDishes::JobOnThing", "dish-search")]
[PerformanceThroughputCheckpoint("native-map-ticks", 11_900)]
[PerformanceThroughputCheckpoint("observer-scans", 48)]
[PerformanceThroughputCheckpoint("meals-produced", 1)]
[PerformanceThroughputCheckpoint("simple-meals-produced", 1)]
[PerformanceThroughputCheckpoint("fine-meals-produced", 1)]
[PerformanceThroughputCheckpoint("lavish-meals-produced", 1)]
[PerformanceThroughputCheckpoint("assisted-cooking-sessions", 1)]
[PerformanceThroughputCheckpoint("ware-cleaned", 1)]
[PerformanceThroughputCheckpoint("domestic-dishwasher-cycles", 1)]
[PerformanceThroughputCheckpoint("industrial-dishwasher-cycles", 1)]
[PerformanceThroughputCheckpoint("map-meals-ingested", 1)]
[PerformanceThroughputCheckpoint("animal-meals-ingested", 4)]
[PerformanceThroughputCheckpoint("patients-fed", 1)]
[PerformanceThroughputCheckpoint("caravan-meals-ingested", 2)]
[PerformanceThroughputCheckpoint("microwave-reheats", 1)]
[PerformanceThroughputCheckpoint("optional-group-branches", 4)]
[PerformanceThroughputCheckpoint("immersive-chefs.all-supported-branch", 1)]
[PerformanceThroughputCheckpoint("guest-service-orders-served", 1)]
[PerformanceThroughputCheckpoint("guest-service-colony-settings-returned", 1)]
[PerformanceThroughputCheckpoint("guest-service-gastronomy-clearing-owned", 1)]
[PerformanceThroughputCheckpoint("processor-dubs-domestic-cycles", 1)]
[PerformanceThroughputCheckpoint("processor-dubs-industrial-cycles", 1)]
[PerformanceThroughputCheckpoint("processor-dubs-water-milliliters", 1)]
public sealed class AllSupportedInstrumentedBenchmark : ImmersiveChefsColonyBenchmark { }

[RimWorldPerformanceTest("immersive-chefs.all-supported.armed-disabled", ColonyContract.Owner,
    ColonyContract.Owner, "brrainz.harmony", "ludeon.rimworld", PerformanceTestContract.CircinusPackageId, "imranfish.xmlextensions",
    "ludeon.rimworld.royalty", "ludeon.rimworld.biotech", "Argon.CoreLib",
    "OskarPotocki.VanillaFactionsExpanded.Core", "syrchalis.processor.framework",
    "Argon.ExpandedMaterials.Masonry", "Argon.ExpandedMaterials.Metals", "Dubwise.DubsBadHygiene",
    "Orion.Hospitality", "Orion.CashRegister", "Orion.Gastronomy", "avilmask.CommonSense",
    "Evyatar108.VarietyMattersImprovedRedux", "VanillaExpanded.VanillaFoodVarietyExpanded",
    "VanillaExpanded.VNutrientE", ColonyContract.Owner,
    WorkloadVersion = "immersive-chefs-colony/all-supported/v1",
    ComparisonId = OptionalColonyContract.AllSupported, WarmUpTicks = ColonyContract.WarmUpTicks,
    SampleTicks = ColonyContract.SampleTicks, Repetitions = 3,
    EvidenceLens = PerformanceEvidenceLens.ArmedDisabledWrapper)]
[PerformanceMethodSelector(PerformanceMethodSelectorKind.HarmonyOwner, ColonyContract.Owner, "product-harmony")]
[PerformanceMethodSelector(PerformanceMethodSelectorKind.Method, "ImmersiveChefs.WorkGiver_DoDishes::JobOnThing", "dish-search")]
[PerformanceThroughputCheckpoint("native-map-ticks", 11_900)]
[PerformanceThroughputCheckpoint("observer-scans", 48)]
[PerformanceThroughputCheckpoint("meals-produced", 1)]
[PerformanceThroughputCheckpoint("simple-meals-produced", 1)]
[PerformanceThroughputCheckpoint("fine-meals-produced", 1)]
[PerformanceThroughputCheckpoint("lavish-meals-produced", 1)]
[PerformanceThroughputCheckpoint("assisted-cooking-sessions", 1)]
[PerformanceThroughputCheckpoint("ware-cleaned", 1)]
[PerformanceThroughputCheckpoint("domestic-dishwasher-cycles", 1)]
[PerformanceThroughputCheckpoint("industrial-dishwasher-cycles", 1)]
[PerformanceThroughputCheckpoint("map-meals-ingested", 1)]
[PerformanceThroughputCheckpoint("animal-meals-ingested", 4)]
[PerformanceThroughputCheckpoint("patients-fed", 1)]
[PerformanceThroughputCheckpoint("caravan-meals-ingested", 2)]
[PerformanceThroughputCheckpoint("microwave-reheats", 1)]
[PerformanceThroughputCheckpoint("optional-group-branches", 4)]
[PerformanceThroughputCheckpoint("immersive-chefs.all-supported-branch", 1)]
[PerformanceThroughputCheckpoint("guest-service-orders-served", 1)]
[PerformanceThroughputCheckpoint("guest-service-colony-settings-returned", 1)]
[PerformanceThroughputCheckpoint("guest-service-gastronomy-clearing-owned", 1)]
[PerformanceThroughputCheckpoint("processor-dubs-domestic-cycles", 1)]
[PerformanceThroughputCheckpoint("processor-dubs-industrial-cycles", 1)]
[PerformanceThroughputCheckpoint("processor-dubs-water-milliliters", 1)]
public sealed class AllSupportedArmedDisabledBenchmark : ImmersiveChefsColonyBenchmark { }

[RimWorldPerformanceTest("immersive-chefs.all-supported.disarmed", ColonyContract.Owner,
    ColonyContract.Owner, "brrainz.harmony", "ludeon.rimworld", PerformanceTestContract.CircinusPackageId, "imranfish.xmlextensions",
    "ludeon.rimworld.royalty", "ludeon.rimworld.biotech", "Argon.CoreLib",
    "OskarPotocki.VanillaFactionsExpanded.Core", "syrchalis.processor.framework",
    "Argon.ExpandedMaterials.Masonry", "Argon.ExpandedMaterials.Metals", "Dubwise.DubsBadHygiene",
    "Orion.Hospitality", "Orion.CashRegister", "Orion.Gastronomy", "avilmask.CommonSense",
    "Evyatar108.VarietyMattersImprovedRedux", "VanillaExpanded.VanillaFoodVarietyExpanded",
    "VanillaExpanded.VNutrientE", ColonyContract.Owner,
    WorkloadVersion = "immersive-chefs-colony/all-supported/v1",
    ComparisonId = OptionalColonyContract.AllSupported, WarmUpTicks = ColonyContract.WarmUpTicks,
    SampleTicks = ColonyContract.SampleTicks, Repetitions = 3,
    EvidenceLens = PerformanceEvidenceLens.FullyDisarmed)]
[PerformanceMethodSelector(PerformanceMethodSelectorKind.HarmonyOwner, ColonyContract.Owner, "product-harmony")]
[PerformanceMethodSelector(PerformanceMethodSelectorKind.Method, "ImmersiveChefs.WorkGiver_DoDishes::JobOnThing", "dish-search")]
[PerformanceThroughputCheckpoint("native-map-ticks", 11_900)]
[PerformanceThroughputCheckpoint("observer-scans", 48)]
[PerformanceThroughputCheckpoint("meals-produced", 1)]
[PerformanceThroughputCheckpoint("simple-meals-produced", 1)]
[PerformanceThroughputCheckpoint("fine-meals-produced", 1)]
[PerformanceThroughputCheckpoint("lavish-meals-produced", 1)]
[PerformanceThroughputCheckpoint("assisted-cooking-sessions", 1)]
[PerformanceThroughputCheckpoint("ware-cleaned", 1)]
[PerformanceThroughputCheckpoint("domestic-dishwasher-cycles", 1)]
[PerformanceThroughputCheckpoint("industrial-dishwasher-cycles", 1)]
[PerformanceThroughputCheckpoint("map-meals-ingested", 1)]
[PerformanceThroughputCheckpoint("animal-meals-ingested", 4)]
[PerformanceThroughputCheckpoint("patients-fed", 1)]
[PerformanceThroughputCheckpoint("caravan-meals-ingested", 2)]
[PerformanceThroughputCheckpoint("microwave-reheats", 1)]
[PerformanceThroughputCheckpoint("optional-group-branches", 4)]
[PerformanceThroughputCheckpoint("immersive-chefs.all-supported-branch", 1)]
[PerformanceThroughputCheckpoint("guest-service-orders-served", 1)]
[PerformanceThroughputCheckpoint("guest-service-colony-settings-returned", 1)]
[PerformanceThroughputCheckpoint("guest-service-gastronomy-clearing-owned", 1)]
[PerformanceThroughputCheckpoint("processor-dubs-domestic-cycles", 1)]
[PerformanceThroughputCheckpoint("processor-dubs-industrial-cycles", 1)]
[PerformanceThroughputCheckpoint("processor-dubs-water-milliliters", 1)]
public sealed class AllSupportedDisarmedBenchmark : ImmersiveChefsColonyBenchmark { }
