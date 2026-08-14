# Showcase archetypes

These are evidence-grounded scene grammars. Adapt footprints to exact loaded Defs and camera size.

## Gastronomy restaurant and full kitchen

- **Recurrent (R01, R07, R20, R21):** Public approach leads to a furnished dining room with several four-seat
  tables or mixed table sizes.
- **Mechanical (Gastronomy) / recurrent (R07, R20, R21):** Cash register and waiter circulation sit near the
  public/staff boundary and cover the dining area and meal
  source under Gastronomy's actual radius rules.
- **Mechanical/recurrent (R02, R11, R19):** The kitchen is adjacent but visually separated: freezer/fridge
  buffer on one edge, stove and Immersive Chefs
  stations arranged around a clear shared work lane, dish return/cleaning near the service exit.
- **Recurrent (R02, R11, R19):** A pass-through fridge, meal shelf, or short service doorway explains how the
  waiter reaches finished meals
  without walking through every cook's work cell.
- **Recurrent (R01, R07, R20, R21):** Decor distinguishes the restaurant from a canteen: deliberate floor,
  lights, art/plants, consistent chairs,
  and stocked service furniture. Keep wider guest circulation than the back-of-house lane.
- **Mechanical (Workshop presentation contract):** Compose two hard-cut views if needed: order/service in
  dining, then simultaneous kitchen work. Do not pan.

## Dirty-dish collection and dishwasher

- **Recurrent (R02, R03, R11, R19):** Use a compact established colony kitchen/dining cluster, not a box
  containing only a dishwasher and pawn.
- **Candidate (workflow inference; needs more natural Dubs/Processor screenshots):** Put dirty ware where meals
  plausibly finish: beside tables, in a return shelf/counter, or near the dining exit.
  Overflow can be visually obvious but must not block every chair or doorway.
- **Mechanical (Immersive Chefs, Dubs, Processor Framework) / candidate visual grammar:** Place dishwasher(s),
  sink/plumbing, and clean-ware storage in a service lane adjacent to the kitchen. The clean
  shelf/cupboard should be a short return trip but distinct from dirty intake.
- **Recurrent (R01, R02, R03, R07, R11):** Show normal context: active stoves/stations, meal fridge, lighting,
  power/plumbing, tables, art, named colonists,
  and a consistent stone/wood/metal palette.
- **Mechanical (Immersive Chefs + Pick Up And Haul):** For the Pick Up And Haul beat, keep the dirty cluster
  dense enough for nearby collection, then hard-cut from
  loading/processing to ejected clean ware and final shelf haul.
- **Recurrent/mechanical (RR-20260814; Core/Dubs Defs):** Use table footprints that fit every visible chair;
  wall-align the stove/prep/dishwashing run or form one justified island; conceal power and plumbing in the
  shared service wall; place the water tower and non-rotatable chemfuel generator in an exterior service yard.
  Let native power/plumbing settle for several ticks before evaluating the appliance.
- **Mechanical (loaded 1.6 Defs):** The current domestic dishwasher is 2x1, Dubs `KitchenSink` is 3x1, the prep
  station is 3x1, and the electric stove is 3x1. A single straight run therefore needs eleven interior cells
  before gaps. In a compact nine-cell interior, use eight cells for dishwasher/sink/prep and turn the stove onto
  the adjacent wall with a clear inward-facing interaction cell. Do not embed any of these roots in the wall.

## Prison nutrient-paste dining

- **Recurrent (R08, R09, R10, R19):** Use an institutional common room with multiple tables/benches, visible
  cell or barracks doors, durable floors,
  bright practical lighting, and controlled access.
- **Mechanical/recurrent (R08, R09, R10):** Mount the nutrient-paste dispenser through the kitchen/service wall:
  prisoner-facing interaction side in the
  common room, hoppers and raw-food access on the secured staff/freezer side.
- **Mechanical (Immersive Chefs):** Put the plate supply on the prisoner side before the dispenser so the native
  path visibly becomes
  plate -> dispenser -> table. Do not give the room a normal kitchen the prisoners could access.
- **Archetype (R08, R09; requested Prison Jumpsuits presentation ecosystem):** Prison Jumpsuits, repeated seating,
  and a guarded/staff-side service area communicate the scenario more clearly
  than labels. Keep the final thought inspection readable and caused by native eating.

## Continuous prep and fine-meal production

- **Mechanical/recurrent (R02, R11, R19):** Connect a freezer/raw-food store to a prep station and stove through
  separate short lanes so the prep cook and
  meal cook can work simultaneously without occupying one another's cells.
- **Mechanical (Immersive Chefs) / recurrent (R02, R03):** Stock visibly different meat and vegetable sources.
  Put prepared ingredients in a dedicated high-priority
  shelf/fridge buffer between prep and cooking.
- **Mechanical (Immersive Chefs):** Place linked sauce/meat/vegetable/pastry stations around the cooking lane only
  when the exact link/worker cells
  remain usable. The cluster should look like one professional kitchen, not four isolated exhibit plinths.
- **Mechanical (Thermodynamics - Hot Meals) / recurrent service-buffer pattern (R11, R19):** Put the
  Thermodynamics hot storage on the dining/service edge, after cooking and before diners/waiters. It
  should not obstruct the stove interaction cell or raw-food traffic.

## Memories still

- **Mechanical (RimWorld + Immersive Chefs):** Frame a real dining context and the selected pawn's readable
  Needs/Thoughts panel after native ingestion.
- **Mechanical (Workshop presentation contract):** Keep enough room visible to explain the missing-table
  condition, cold meal source, and lack of cutlery without
  staging warning labels. The player UI may select the pawn for thought inspection; the separate clean gameplay
  crop should not show a selection bracket unless the requested evidence requires it.
