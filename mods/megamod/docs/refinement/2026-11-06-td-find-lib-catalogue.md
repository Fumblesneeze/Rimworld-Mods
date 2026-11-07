# S121 — Installed query catalog

Owner **Make Rimworld Great Again** (`fumblesneeze.megamod`), target **1.7 beta**.
[Research provenance and remaining work](2026-11-06-td-find-lib-research.md).
This is a **reviewed source-behavior inventory**, not a normative spec or a parity claim.
Names below identify installed filters; implementation identifiers stay in this refinement/design
material rather than the eventual player-observable contract. Read together with S033's already
drafted search semantics. New owned corrections must be explicit in the eventual capability spec.

## Shared source conventions

- Unless stated otherwise, the predicate is false for an incompatible candidate type or missing
  required pawn tracker/component. Source null dereferences and exceptional cases are not a desired
  replacement behavior. Excluding a predicate negates its match; disabling a predicate omits it.
- Numeric ranges are inclusive. `IntRangeUB`/`FloatRangeUB` treat a selected endpoint at its slider
  boundary as unbounded in that direction. Thus a 0–100% full slider can include values over 100%;
  ordinary displayed slider maxima are not gameplay caps. Finite non-endpoint selections stay exact.
  NaN does not match. Infinity behavior and invalid-input admission need an owned contract.
- Source fresh-constructor defaults and missing-field load defaults sometimes differ. The table
  calls out observed differences rather than asserting a single stable upstream default.
- Most Def dropdowns use a named constructor default or the base default; categories and options can
  depend on loaded content. Read the editor initialization before promising a concrete first choice.
- Corpse/minified wrapping, include/exclude, All/Any/At-least groups, listing/map scope and unavailable
  whole-query behavior are already owned by S033. The source applies each predicate to wrapper OR
  inner object, while presenting one physical result. That can produce surprising HP matches.
- Source god mode broadens ordinary visibility and exposes developer choices. The owned UI rules
  require an explicitly separate debug scope, including suggestions and derived results.
- Five XML preselection shortcuts reuse other predicates; they are not separate evaluation engines.

## Base selectors, items and buildings

| Source entry | Actual selection and match | Defaults, qualifications and proposed corrections |
| --- | --- | --- |
| Name | Case-insensitive name substring; S033 owns the ordinary contract | Do not infer regular expressions |
| Specific Thing | Exact Thing type, optional inclusive stack count; category variant uses the source presentation category | Category identities are often translated strings; S033 requires stable identity |
| General category | Person = Pawn not AnimalOrWildMan; Animal = Pawn and AnimalOrWildMan; Item = always-haulable; Building/Natural split by rock-rubble filth; Plant; Other = none of those | Source “Person” is broader than simply human colonist; verify native property semantics before wording |
| Stat | A stat's displayed/processed value when its worker says it should be shown for that candidate; inclusive range | GeneralLaborSpeed initially; selecting a stat uses its min/max boundaries; ordinary options exclude hidden stats |
| Market value shortcut | Stat with MarketValue selected | Same evaluation, no second price cache |
| Item category | Candidate type belongs to selected category or its descendants | Root initially; category ancestry matters |
| Quality | Candidate has quality and it is in the selected quality range | All qualities initially; non-quality Things do not match |
| Made from | Exact material, material category, any or not made from material; plan/frame material supported | S033 owns detailed contract |
| Special filter | Native special Thing filter matches candidate | AllowFresh initially; no separate reimplementation of every native filter |
| Apparel layer | Candidate is apparel; contains every required layer and no prohibited layer | OnSkin initially required; mask choices persist |
| Apparel coverage | Candidate is apparel; contains every required body-part group and no prohibited group | Legs initially required |
| Forbidden | Spawned, forbiddable candidate; forbidden / allowed / either | Forbidden initially; not a pawn-specific reachability query |
| Mineable (obsolete) | Resource / non-resource rock / any mineable | Kept for serialized legacy queries, not a new visible duplicate |
| Mineable | Specific mined product, any resource, non-resource rock, or all mineables | Any resource initially; tests source building mineable properties |
| Building category | Candidate's Architect designation category equals selected category | Production initially; not restricted by an actual Building cast |
| Door status | Actual door open, closed, held open, or momentarily blocked open | Open initially; held-open is distinct from currently open |
| Hit points | Pawn overall health percentage; otherwise hit points / max hit points for Things using HP | Wrapper/inner OR means corpse and dead pawn can expose different ratios; retain one physical result |
| Art | Active art component | No quality or market-value threshold implied |
| Recoverable parts | Candidate can potentially produce selected or any material through added-part removal, qualifying build costs, smelt/butcher products or death leavings | Steel initially; prospective outputs, not guaranteed current action eligibility or yield. Stone-chunk butcher output excluded. Intricate build costs excluded. Player/hostile leavings both considered |
| From mod | Candidate type's defining content source equals selected package | Core initially; only running packages with Thing definitions offered; package identity saved |

## Plants, environment and relationships between Things

| Source entry | Actual selection and match | Defaults, qualifications and proposed corrections |
| --- | --- | --- |
| Crop | Plant type is a crop | Does not require that it is currently in a growing zone |
| Harvest product | Plant has selected harvest product, any product, or none | Any initially |
| Harvestable | Plant is harvestable now | Observation only; does not issue harvest |
| Growth | Plant growth fraction in range | Full range initially; endpoint behavior above |
| Growth rate | Actual plant growth rate in range | Slider upper bound derived from maximum plant fertility sensitivity and buildable fertility |
| Freshness | Fresh/rotting/dessicated stage, spoils, refrigerated, or frozen | Fresh initially; refrigerated/frozen extras use ambient rot-rate alone and do not require a rottable component |
| Time to rot | Rottable candidate's estimated time until rot at its current temperature | 0–10 days initially; slider upper boundary 20 days; 60,000 ticks/day |
| Dies leafless | Plant type dies if leafless | Source label suggesting death from cold is an approximation, not a temperature simulation |
| Blighted | Plant is blighted | No automatic cutting implied |
| Area | Held cell in selected allowed area, home, build-roof, no-roof, snow/sand-clear or roofed region | Home initially; source resolves named areas by label. A sixth AnyArea code branch exists but is not exposed by the five-extra-options UI |
| Zone | Held cell in exact selected zone, any stockpile, any growing zone, or any zone | Fresh source default Any zone (extra 3), but menu exposes only extras 1–2, so Any zone cannot be reselected there. S033 already specifies all four choices with stable map/zone identity and no same-label substitution |
| Temperature | Candidate's ambient temperature in selected range | 10–30°C initially; slider −100 to 100°C, unbounded endpoints; display follows native units |
| Deteriorating | Native final deterioration rate at least 0.001 | Current conditions; not merely a deterioratable type |
| Nearby | Some grid Thing in the square centered on candidate's held cell satisfies nested filters | Radius 0 initially, 0–10 UI; center/self included; editor forces minimum 0. Source lacks explicit edge clipping/knowledge checks; owned correction needed |
| Inventory relationship | Holder has a recursively contained match, or candidate is in a matching valid parent holder | Fresh direction “holds”; missing saved direction “held by.” Parent climb requires enclosing holders and excludes minified wrappers; known-container boundary applies |
| Designation | Candidate Thing or held cell has chosen designation; any or none also offered | Any initially; smoothing, harvesting, extracting/skulls, paint removal have special mapping. Selecting a filter does not designate |
| Can designate | Some or selected native reverse designator can designate candidate | Any initially. Source None branch always false; do not describe that as a functioning “no eligible designator” test |
| Faction | Own faction or pawn host faction; hostile/neutral/ally, player/mech/insect, any visible faction, no faction, permanently hostile | Player initially; there is no arbitrary exact-faction selector. NoFaction also includes hidden faction in source. Ordinary relation categories require non-null nonplayer faction |
| Reserved | Candidate Thing has a reservation by a player-faction pawn; optionally filter reserver with nested query | Any reserver initially; not cell reservations or all factions. It is not a prediction that a future pawn can reserve |
| On screen | Occupied rectangle overlaps current camera view; forces current-map scope | XML class spelling differs only by case; native type lookup ignores case, so this is not an unresolved type |
| All/Any/At least group | Nested logical combination | S033 owns empty-group and disabled/unresolved rules |

## Pawn condition and activity

| Source entry | Actual selection and match | Defaults, qualifications and proposed corrections |
| --- | --- | --- |
| General pawn property | Native colonist/free-colonist/colony-prisoner/colony-slave/any-prisoner/any-slave/colony-mech/downed/dead/psylink properties | Generic and colony custody are separate choices; do not turn the label into an implicit colonist-only restriction |
| Dies in | Finite estimated death time **from blood loss** lies in range | 0–24 hours initially; slider 0–48 hours; non-bleeding indefinite estimate does not match |
| Skill | Specific or any non-totally-disabled skill in level range, with no/minor/major/any/ignored passion | Animals, 0–20 and ignore passion fresh; missing saved passion defaults to no passion |
| Trait | Exact trait and degree | Pyromaniac initially; changing trait selects its first defined degree |
| Thought | Matching current mood memory or situational mood thought, selected stage range, not nullified | AteWithoutTable initially; stages ordered by mood/opinion effect then original index. Not a general search of social-opinion thoughts. Source stage visibility/index mismatch needs owned correction |
| Need | Current percentage of selected need | Food, 0–50% initially; wild unfactioned animal needs hidden unless source god mode |
| Health condition | Any visible condition, bleeding, tendable now, none, exact condition with severity, repeated-part any/all/none, or condition category | None initially. Severity checks only first matching visible occurrence. Part inference uses recipe's first fixed part and recipe users. Category helper does not check visibility. Proposed owned any-occurrence and consistent visibility corrections |
| Capacity | Selected or any capacity level in range | Manipulation exactly 100% initially; slider 0–400%, unbounded endpoints |
| Mental state | Specific, none, any, or aggressive mental state | None on fresh nullable selection; no mental-state mutation |
| Temperature comfort | Cold below safe min; cool between safe min and comfort min; comfortable within comfort bounds; warm above comfort max to safe max; hot above safe max | Cold enum default; actual pawn ranges and ambient temperature |
| Incapable | Selected disabled work tag; any disabled tag; no disabled tags | None means no disabled tags, not missing work tracker |
| Area restriction | Pawn's assigned current-map allowed area equals selected area, Home, or unrestricted | Named source bindings use labels; owned exact identity/rebinding required |
| Gear | Pawn's primary weapon, other equipment, worn apparel, inventory, carried item, unused inventory+carried, or Any worn/equipped gear; Has / Any matching / All matching nested query | Any gear + Has initially. “Any” scope excludes inventory/carry. All requires at least one member; does not pass empty gear |
| Ability | Selected, any, none, or provider category among abilities; filter by has, cooldown, can cast, active, or remaining charges | Any + Has initially; charges 1–5 with 5 unbounded. Active approximates a give-hediff ability whose hediff is on the owner, not every possible active effect. CanCast is not target-specific cast success |
| Drafted | Drafted; undrafted but draftable; has draft controller | Drafted initially; an undraftable animal is not undrafted here |
| Current action | Pawn's current job equals selected job, or no current job | No action issued |
| Schedule | Current assignment; all specified hours equal a 24-hour template; or any specified hour differs | Current Work initially. Blank template cells ignored; all-blank All succeeds and Any-difference fails. No timetable tracker fails |
| Inspiration | Selected, any, or no current inspiration | None fresh |
| Guest status | Native guest/prisoner/slave state, secure prisoner, or secure slave | Not a Hospitality identity or bed assignment |
| Prisoner | Specific interaction mode, any prisoner, in prison cell, currently escaping, mean break interval, resistance, will, non-recruitable | MaintainOnly initially. Cell test also accepts non-pawn Things. Break estimate ignores asleep; ranges 0–120 days, 0–50 resistance, 0–10 will. Non-recruitable tests any guest tracker, not only prisoners |
| Quest status | Quest lodger or quest helper | Lodger initially |
| Work enabled | Selected work type active in pawn's work settings | Firefighter initially; does not promise current job reachability |

## Pawn policies, identity and history

| Source entry | Actual selection and match | Defaults, qualifications and proposed corrections |
| --- | --- | --- |
| Apparel policy | Pawn assigned exact selected apparel policy | Current game's default, otherwise an “Anything” label placeholder; source label binding is unsafe across saves |
| Food restriction | Pawn assigned exact selected food policy | Current default, otherwise translated Lavish placeholder |
| Drug policy | Pawn assigned exact selected drug policy | Current default, otherwise SocialDrugs label placeholder |
| Allowed by apparel policy | **Pawn's** apparel policy permits selected apparel type | Parka initially. Category helper wrongly tests candidate's own type; owned category must retain pawn-policy meaning |
| Allowed by food restriction | **Pawn's** food policy permits selected food type | Simple meal initially; options have positive abstract nutrition; same category-helper issue |
| Allowed by drug policy | **Pawn's** policy permits selected drug for joy, schedule, or addiction when pawn is addicted/chemically dependent | Penoxycyline initially; not actual due-now ingestion eligibility; same category-helper issue |
| Keep in inventory | **Pawn's** drug policy asks to carry at least one of selected drug | Penoxycyline initially; not actual inventory contents or a generic loadout; same category-helper issue |
| Age group | Exact developmental stage | Adult initially |
| Age | Biological or chronological years | Biological 0–50 initially; sliders 200 biological / 1,000 chronological, unbounded endpoints |
| Relation | Selected relationship to another pawn, optional gender; has / any related pawn matches / all related pawns match | Parent, male, Has fresh; missing saved gender None. Direct and implied relations; ordinary social-card-visible relations only. All requires nonempty related set |
| Record time | Selected time record in seconds/hours/days/years, truncated to whole selected units | Time as colonist/colony animal; 0–60 years fresh, missing saved unit seconds. 60 ticks/s, 2,500/hour, 60,000/day, 3,600,000/year; endpoints unbounded |
| Gender | Exact native gender | Male initially |
| Missing body part | Any missing common ancestor, none, or selected part type with any/all/none occurrences missing | None initially. Specific part absent from that species entirely fails even None. Ancestor loss counts via native missing-part lookup |

## Race, animal production and training

| Source entry | Actual selection and match | Defaults, qualifications and proposed corrections |
| --- | --- | --- |
| Race | Intelligence, herd/pack/predator/prey flags, wildness, petness, retaliation on failed tame/harm, or trainability | Selecting intelligence initializes Humanlike; wildness/petness 25–75%; trainability Advanced. Wildness uses current stat; other probabilities are race properties |
| Meat | Pawn's meat type, any or none, plus rounded current MeatAmount | Any and full range initially; slider maximum from loaded adult meat amounts. No actual slaughter/butchery action or worker yield implied |
| Leather | Pawn's leather type, any or none, plus rounded current LeatherAmount | Same structure, loaded adult leather maximum |
| Eggs per year | Species' unfertilized egg type and rounded theoretical annual output | Respects female-only; does not predict age, food, fertility or current production interruptions |
| Milk per year | Species' milk type and rounded theoretical annual output | Respects female-only; annual production differs from current fullness |
| Wool per year | Species' wool type and rounded theoretical annual output | No worker harvest yield computation |
| Product progress | Egg laying, egg hatching, milk fullness, wool growth, milkable now, shearable now | Progress full range initially; last two require active-and-full. Component required, sex restrictions for eggs/milk |
| Trained | Selected training's completed step count | Tameness; selecting training initializes 1 through its required steps; full upper endpoint unbounded |
| Bonded | Pawn has any colonist bond reported by native training utility | Does not choose a master or require a hunting-trained animal |

## Royalty

| Source entry | Actual selection and match | Defaults, qualifications and proposed corrections |
| --- | --- | --- |
| Title | Exact royal title, any title, or no title | No title fresh; royalty tracker required |
| Title range | Highest/current title seniority for selected faction or any faction lies between chosen titles | Any faction initially; source numeric range starts 0–0 and lacks explicit initialization to available bounds; owned useful default needed |
| Title requirement unmet | Any or selected: no assigned throne, undignified throne room, no personal bedroom, undignified bedroom, no acceptable food, required apparel missing, apparel quality too low | Any initially. Native grace periods/suspension respected on room cases; food uses actual pawn food search with specified restrictions; source minimum-apparel-quality path has empty-title-in-effect hazard |
| Honor | Favor with selected faction or any recorded faction lies in range | Any, full range initially; slider max twice largest title favor cost. Exact absent faction honor can be 0; any does not manufacture missing entries |
| Permit | Selected/any permit from selected/any faction, optionally not on cooldown | TradeSettlement if present, otherwise Any; ready-only true; source omits faction/ready state from clone/save overrides |
| Permit points | Sum of unspent points over visible factions in range | 1–5 initially, 5+ at upper boundary |
| Psyfocus | Current psyfocus for pawn with psylink | Full percent range initially |
| Neural heat | Current entropy/heat amount for pawn with psylink | 0–80 slider, endpoints unbounded |

## Ideology

| Source entry | Actual selection and match | Defaults, qualifications and proposed corrections |
| --- | --- | --- |
| Style | Exact style, any, none or style category on candidate | Any initially; category requires an actual style. Other category matches a style without a category, not a Thing with no style |
| Ideoligion | Exact ideology, player's primary ideology, or another ideology | Player primary initially; pawn with no ideology fails all these choices |
| Meme | Pawn's ideology has selected meme | Darkness initially; structural memes not offered |
| Precept | Pawn's ideology has selected ordinary visible precept | Meat-eating abhorrent initially; only exact base Precept class entries offered; issue grouping does not change predicate |
| Role | Selected/any/single-holder/multiple-holder role currently held or role requirements met; extra unmet role apparel | Leader, currently held initially. None uses null current role. “Can be” checks requirements, not an actual completed appointment. Multi/single labels are broader than just leader/specialist names |
| Ideology xenotype preference | Preferred/nonpreferred/no opinion for pawn's, player's or selected ideology | Pawn's ideology initially; both Ideology and Biotech relevant. No opinion requires no preferred default or custom xenotypes. Source preference mode lacks save/clone override |
| Venerated animal | Pawn species venerated by owner's faction primary, player's primary or selected ideology | Owner's faction primary initially; wild animal may have no owner ideology |
| Weapon class | Candidate weapon or pawn's primary weapon has selected class | Ultratech initially |
| Weapon disposition | Candidate weapon or primary weapon is noble/despised/etc. under wielder's ideology; ground weapon uses player's primary | Noble initially; nonweapon fails |
| Gauranlen | Tree connection, selected mode, current/desired connection strength, max/current dryads, time until next dryad | Connection initially; mode Gaumaker; strength 50–100%; dryads 1–2 on 0–4 slider; time 0–1 day on 0–8 day slider. Runtime values are observed, not altered |

## Biotech

| Source entry | Actual selection and match | Defaults, qualifications and proposed corrections |
| --- | --- | --- |
| Is mechanitor shortcut | Health predicate selects MechlinkImplant | This shortcut is not a broader mechanitor eligibility algorithm |
| Mechanitor bandwidth | Total, used or available bandwidth in range | Total initially; 0–40 slider, unbounded endpoints |
| Mech/overseer relation | Candidate's overseer matches nested query, or candidate mechanitor's any/all controlled mechs match | Fresh any-controlled-mech direction; missing saved flags select overseer/all. Source all-controlled permits empty list, unlike gear/relation All; owned policy must be explicit |
| Is work mech shortcut | Specific-Thing category “Work Mechanoid” | Source translated string binding, not a new predicate |
| Mech weight class | Actual mechanoid's exact weight class | Base default choice requires UI initialization verification |
| Mech bandwidth cost | Mechanoid type's abstract bandwidth cost, cast to integer, in range | Slider 1 through largest loaded cost, unbounded endpoints |
| Mech work mode | Mechanoid's current work mode | Work initially |
| Uncontrolled mech | Player mechanoid not Overseen; delay until feral check minus accumulated delay lies in range | Slider maximum from loaded overseer-subject delays; displayed hours. Not every uncontrolled mech is already feral |
| Mech energy shortcut | Need predicate selects MechEnergy | Reuses need range, not separate energy rules |
| Xenotype | Exact default xenotype; common unnamed; unique; hybrid; or exact custom name | Baseliner initially. Custom names also match gene assembler and xenogerm. Suggestions include current game's custom database |
| Has no xenogenes shortcut | Gene predicate: no xenogene, Has mode | Internal identifier Query_Xenogerm is not its displayed label; it does not find all xenogerm items |
| Gene | Specific/category/any/none; either/endogene/xenogene; has/active/inactive | Inbred, either, Has initially. Pawn list filtered by origin/activity. Embryos only endogene/either. Genepacks/xenogerms/containers and assembler-accessible packs only xenogene/either. Active means not overridden within that gene set, not implanted action availability |

For pawn None-active/None-inactive, source requires at least one gene in the chosen origin list;
None-has accepts an empty list. Embryo and stored-set paths differ. Containers with no gene holders
fail, rather than automatically matching NoGene; with holders, None means none of their filtered
genes match. Assemblers inspect connected packs including unpowered banks. Source uses shared mutable
temporary lists and category iteration; the owned implementation must not retain stale or reentrant
results or expose unknown container contents.

## Odyssey and optional integrations

| Source entry | Actual selection and match | Availability and qualifications |
| --- | --- | --- |
| Unique weapon | Has unique-weapon component | Does not independently test quality/name |
| Gravship fuel | Pilot console's engine total fuel / maximum fuel in range | Full range initially; source assumes engine and positive capacity exist |
| Gravship capacity | Pilot console's connected substructure count / support stat in range | Full range initially; not transport cargo capacity |
| Gravship cooldown | Pilot console's engine cooldown completion tick is in the future | Observation only |
| CAI FOW visible | Provider FOW enabled and held cell not fogged; absent grid passes in source | Conditional module; provider-disabled FOW returns false. Owned knowledge rule needs safe unavailable behavior |
| LTO colony group | Pawn in exact selected group; None source branch requires successful group lookup and no PawnGroups | Conditional module; name-based source binding; no entire LTO replacement implied |
| RealFOW | Compiled predicate returns true | Definition not selected on 1.7. Record as dormant/stub, not working compatibility |

## Debug/custom inspection

| Source entry | Actual behavior | Owned placement/limitations to specify |
| --- | --- | --- |
| Selectable | Candidate type's selectable flag | Debug inspection; ordinary listing already has an appropriate known selectable mode |
| Class type | Candidate runtime type assignable to selected class | Default Thing; suggested classes from nonabstract subclasses/entities across all loaded maps, with the source availability/knowledge rules |
| Drawer type | Exact native drawer type | Debug only |
| Fogged | Held cell's native fog flag | Debug only; never a way to expose fogged objects in ordinary counts |
| Custom | Start from Thing/subclass; traverse fields, zero-argument readable properties, component lookup, class casts, class-valued extension methods or enumerables; compare final value | Debug only under owned decision tree. Source exposes public members ordinarily and private members with god mode. A getter or extension can run code; “inspection” does not prove side-effect freedom |

Custom final comparers: Boolean true (invert with outer exclusion); inclusive integer/float finite
ranges initially 10–15; case-sensitive substring or exact string, initial empty substring; enum
equality or required/prohibited flag mask; exact Def identity; null/not-null. Enumerable traversal
means any member completes the remaining chain. Null intermediate or wrong-type traversal fails;
terminal null can match an explicit null test. The source stores member/type names and operands,
with errors retained for unresolved chains; base Enabled can silently omit an unfinished custom
predicate. Owned incomplete expressions must not broaden ordinary queries.

Source suggestions exclude selected infrastructure classes, but are not a proven safe whitelist.
It can discover non-public extension methods independently of the ordinary field/property visibility
choice. The final design must distinguish admitted non-mutating inspection from unsupported unknown
getters/extensions, bound traversals and preserve invalid expressions without executing imported code.

## Coverage still requiring closeout

This table accounts for the127 query/preselection declarations by family, including obsolete and
unselected entries. Generated helper classes are not additional player menu entries. Independent
source review confirmed122 query and5 preselection definitions, excluding16 category definitions;
its Zone/default and Class-type suggestion corrections are applied. The40 samples, editor paths and
actual consumer checks are recorded in the linked research. The separate observable draft has now
passed its own independent review/validation, including restored thought-stage ranges and separate
colony custody choices; this source inventory alone does not establish gameplay acceptance.
