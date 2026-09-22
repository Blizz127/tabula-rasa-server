using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// The 3381 weapon templates that had no itemtemplate_weapon row, and the 123 rows that read wrong.
    ///
    /// <b>The rows that were missing.</b> A template needs a weapon row exactly when its item class is a weapon
    /// class, and the client says which those are twice over: itemclass.itemTemplateItemClass puts 5825 of its
    /// 30225 templates on one of the 2866 classes that carry a weaponclass.lookup row, and 2864 of those 2866
    /// also carry augmentation 2 WEAPON in entityclass.lookup - in fact all 2864 carry exactly
    /// (2 WEAPON, 4 EQUIPABLE, 6 ITEM), and no client class carries WEAPON without a weaponclass row. This world
    /// held 2444 of the 5825. ItemTemplateTooltipInfoPacket reaches WeaponInfo unconditionally inside its WEAPON
    /// case, so a tooltip on any of the other 3381 threw; it had not happened only because nothing a player can
    /// reach today is one of them.
    ///
    /// <b>The twenty uniform columns are placeholders, not recovered data.</b> aim_rate 1, reload_time 1500,
    /// alt_action_id 1, alt_action_arg_id 133, ae_type 0, ae_radius 1, recoil_amount 1, reuse_override 0,
    /// cool_rate 1, heat_per_shot 2, ammo_per_shot 1, windup 800, recovery 1, refire 800, range 80,
    /// alt_max_damage 25, alt_damage_type 1, alt_range 80, alt_ae_radius 1, alt_ae_type 1 - every new row gets
    /// exactly those, because that is what 2442 of the 2444 rows already in the table carry. The client holds
    /// these as weaponproperty ids (AIM_TIME 10, AMMO_PER_SHOT 50 ...) whose per-template values lived only in
    /// the retail server's database, which is not in the archive. This migration makes 3381 tooltips possible
    /// and self-consistent; it does not make their numbers retail-accurate, and a blade will advertise range 80
    /// and an 800 ms windup exactly as the 120 blades already shipped do. The two exceptions in the table,
    /// 116929 and 116930, keep their footage-observed windup/recovery/refire/range/alt_max_damage untouched.
    ///
    /// <b>tool_type</b> is the client's constant/tooltype (1-12; the enum has no 0 and no 13+). It is read from
    /// three client fields - the template's itemclass.itemTemplateSkillRequirement, and its class's attack
    /// action and weapon anim condition code (weaponclass.lookup columns 1, 2, 12). Skill 1 T1_RECRUIT_FIREARMS
    /// splits on the anim: RIFLES to 7, PISTOLS or S3_PISTOL to 8, SHOTGUNS to 9. Skill 14 T2_SPECIALIST_TOOLS
    /// splits on the action: TOOL_HEALING_DISC 147 to 1, TOOL_ARMOR_AUGMENTATION 199 to 2, TOOL_CIPHER 258 to 3,
    /// TOOL_FIELD_REPAIR 198 to 6. Skill 24 T3_COMMANDO_LAUNCHERS splits on the anim: SHOTGUNS to 10,
    /// ROCKETLAUNCHERS to 11. Skill 31 T2_SPECIALIST_LEECH_GUN is 12. A template with no skill requirement whose
    /// action is TOOL_HARVEST 172 splits on the arg: 168 HARVEST_SALVAGE to 5, 169 HARVEST_DNA to 4. Everything
    /// else is 0, which is the honest answer rather than a guess: those are the eight weapon families the enum
    /// names no type for (machine gun, staff, propellant gun, torque shell rifle, net gun, polarity gun,
    /// injection gun, spy blade), plus hand-to-hand, mech armour, the deprecated DEL_HARVEST_BOTANY harvest and
    /// the creature, NPC and test weapons that carry no skill requirement at all.
    ///
    /// Applied to the 2444 rows already in the table that rule reproduces 2441 exactly. The three it contradicts
    /// are the defect, not a counter-example: 116929 and 116930 (Vextronics pistols, so 8 PISTOL) and 122871
    /// (a field repair tool, so 6 FIELD_REPAIR) were inserted by WildernessArrivalTrainingDayRows and
    /// WildernessClassGearRows carrying the placeholder 15, after Retune_weapon_tool_type had already written
    /// its hand-built id lists; that migration's closing "set tool_type = 0 where tool_type = 15" then swept all
    /// three to 0. The fourth row in the same position, 122865, is a machine gun and lands on 0 legitimately, so
    /// it is left alone. Reading the rule off the class names instead of the numeric fields
    /// (Weapon_Avatar_&lt;Family&gt;_..., Tool_Avatar_&lt;Family&gt;_...) picks out the same 2441 and the same 3.
    ///
    /// <b>attack_type</b> is the client's constant/attacktype, and the rule is the client's own dispatch: the
    /// module actiondata.actionModules runs for the class's attack action. WEAPON_BLADE 418 is weapons.blade and
    /// WEAPON_MELEE 174 is weapons.meleeattackmovement, so both are 1 ATTACK_MELEE; every other action on a
    /// weapon class is a weapons.* or tools.* projectile module and stays 2 ATTACK_RANGED. Staffs have melee
    /// attack args in the client but weaponclass wires all 221 of their templates to the ranged ones
    /// (WEAPON_ATTACK_STAFF_*_RANGED), so they stay ranged.
    ///
    /// Every row in the table today reads 2, and 120 of them are on WEAPON_BLADE classes - the whole
    /// Weapon_Avatar_Blade_{Physical,Fire,Ice,Laser,Electric}_{CMN,UNC,RAR,ELT} grid. Those 120 are corrected
    /// here. <b>This is a behaviour change, not only a text change:</b> attack_type is what the client's
    /// KNOCKBACK_WHEN_HIT_BY_ATTACK_TYPE proc keys on and what a melee/ranged resistance would consult, and it
    /// is the first time anything in this world has been melee. It wants exercising in game - equip a blade,
    /// read the tooltip, check a knockback proc - rather than assuming it is inert. 145 of the new rows are
    /// melee too: 95 further blades the seed never covered and 50 WEAPON_MELEE classes (open hand, unarmed,
    /// knuckles and boxing gloves, the glowstick and flare Device_Light_* classes, the AnimCondForcer_Avatar_*
    /// poses and 36 creature melee weapons).
    ///
    /// <b>What is deliberately not changed.</b> The 711 tool templates keep attack_type 2 "Ranged".
    /// ATTACK_ABILITY 3 is a plausible retail value for a tool, but no client table says so - no client module
    /// imports constant/attacktype at all - and the 200 tool rows already in the table all carry 2, so changing
    /// them would be invention. Five creature and test templates whose attack arg names read melee
    /// (TEST_Weapon_Avatar_Forean_Club, the two Loper spears, Weapon_Avatar_Gauntlets_Bane_v01,
    /// Weapon_Human_Young_Hand_To_Hand) stay ranged, because the action-arg namespace is flat and its values
    /// collide across actions, so the name cannot be resolved from (action, arg) with certainty. Templates 117
    /// and 42219 (item classes 4327 and 6331) do get rows, on the strength of the client's weaponclass, even
    /// though those two classes have no client entityclass row and ours gives them no WEAPON augmentation, so
    /// the tooltip will never read the row; keeping them keeps the invariant WeaponRowAuditTests checks whole.
    ///
    /// Down restores exactly what was there: the 3381 new rows are deleted, the 120 blades go back to 2 and the
    /// three tool_type rows go back to 0. That last one matters - Retune_weapon_tool_type's Down only reverts
    /// rows still holding what it set, so unless this migration puts 116929, 116930 and 122871 back to 0 itself,
    /// rolling back through 2026-09-20 would leave them at 8, 8 and 6 instead of the 15 they started at.
    ///
    /// Sources: client Tabula Rasa 1.16.5.0 game.zip (pyc stamp 2009-02-09), tables itemclass.itemTemplateItemClass,
    /// itemclass.itemTemplateSkillRequirement, weaponclass.lookup, entityclass.lookup, actiondata.actionModules,
    /// constant/tooltype, constant/attacktype, constant/weaponanimconditioncode, skilldata.byvalue; read 2026-09-21.
    /// Data only - no schema changes.
    /// </summary>
    public static class WeaponRowRepairRows
    {
        public const string Migration = "WeaponRowRepair";

        private const string Table = "itemtemplate_weapon";

        /// <summary>
        /// The twenty columns no client table gives a per-template value for. Placeholders, copied from the
        /// 2442 rows the world seed already shipped with them; see the class comment.
        /// </summary>
        private static readonly object[] Placeholders =
        {
            1.0, 1500u, 1u, 133u, 0u, 1u, 1u, 0u, 1u, 2.0, 1u, 800u, 1u, 800u, 80u, 25u, 1u, 80u, 1u, 1u
        };

        private static readonly string[] Columns =
        {
            "id", "aim_rate", "reload_time", "alt_action_id", "alt_action_arg_id", "ae_type", "ae_radius",
            "recoil_amount", "reuse_override", "cool_rate", "heat_per_shot", "tool_type", "ammo_per_shot",
            "windup", "recovery", "refire", "range", "alt_max_damage", "alt_damage_type", "alt_range",
            "alt_ae_radius", "alt_ae_type", "attack_type"
        };

        /// <summary>One group of new rows: the two derived columns, and every template that takes them.</summary>
        private readonly struct WeaponRows
        {
            public WeaponRows(uint toolType, uint attackType, uint[] templates)
            {
                ToolType = toolType;
                AttackType = attackType;
                Templates = templates;
            }

            public uint ToolType { get; }
            public uint AttackType { get; }
            public uint[] Templates { get; }
        }

        /// <summary>
        /// The 3381 templates with no row, grouped by the only two columns that vary. Fourteen groups; every
        /// other column is <see cref="Placeholders"/>.
        /// </summary>
        private static readonly WeaponRows[] NewRows =
        {
            // tool_type 0 no ToolType entry, attack_type 1 ATTACK_MELEE - 145 templates
            new WeaponRows(0u, 1u, new[]
            {
                23u, 75u, 116u, 125u, 126u, 127u, 128u, 129u, 133u, 136u, 137u, 139u,
                141u, 144u, 154u, 477u, 493u, 510u, 598u, 642u, 643u, 667u, 668u, 685u,
                1877u, 1924u, 1938u, 2236u, 2246u, 2260u, 2263u, 2264u, 2337u, 2353u, 2357u, 2362u,
                2369u, 2378u, 2380u, 11472u, 11473u, 11474u, 11475u, 11476u, 42241u, 42649u, 42650u, 42651u,
                42652u, 42653u, 44833u, 44838u, 45739u, 45756u, 45834u, 47632u, 47653u, 47669u, 96198u, 96379u,
                96415u, 115575u, 115586u, 115596u, 115638u, 115696u, 115720u, 115724u, 115744u, 115760u, 115780u, 115812u,
                115892u, 115920u, 115940u, 115944u, 115954u, 115956u, 115967u, 116105u, 116133u, 116189u, 116216u, 116229u,
                116330u, 116455u, 116459u, 116463u, 116482u, 116498u, 116518u, 116545u, 116613u, 116617u, 116624u, 116659u,
                116898u, 116902u, 117140u, 117164u, 117246u, 117270u, 119095u, 119398u, 119440u, 119471u, 119621u, 119636u,
                119646u, 119709u, 119797u, 119821u, 119845u, 119881u, 119913u, 120029u, 120033u, 120061u, 120125u, 120378u,
                120446u, 120570u, 120576u, 120596u, 120607u, 120641u, 120672u, 120949u, 120973u, 121204u, 122002u, 122131u,
                122136u, 122137u, 122266u, 122459u, 123351u, 130516u, 131245u, 131246u, 131247u, 131979u, 132008u, 132021u,
                20000494u
            }),

            // tool_type 0 no ToolType entry, attack_type 2 ATTACK_RANGED - 1279 templates
            new WeaponRows(0u, 2u, new[]
            {
                33u, 55u, 62u, 64u, 65u, 66u, 71u, 79u, 80u, 98u, 104u, 111u,
                117u, 118u, 124u, 131u, 134u, 140u, 142u, 155u, 156u, 157u, 159u, 162u,
                163u, 181u, 462u, 469u, 485u, 486u, 492u, 511u, 644u, 645u, 646u, 647u,
                648u, 763u, 1764u, 1933u, 1939u, 1940u, 1948u, 2237u, 2238u, 2248u, 2250u, 2251u,
                2252u, 2253u, 2254u, 2255u, 2258u, 2261u, 2262u, 2287u, 2334u, 2335u, 2336u, 2338u,
                2341u, 2342u, 2343u, 2354u, 2361u, 2363u, 2370u, 2371u, 2379u, 2500u, 2519u, 2520u,
                2731u, 2753u, 2794u, 2927u, 2949u, 3141u, 3163u, 4016u, 11421u, 11432u, 11433u, 11434u,
                11435u, 11436u, 11437u, 11438u, 11439u, 11440u, 11441u, 11442u, 11446u, 11447u, 11448u, 11449u,
                11450u, 11451u, 11452u, 11453u, 11466u, 11467u, 11468u, 11469u, 11470u, 11471u, 11477u, 11478u,
                11479u, 11480u, 11481u, 11484u, 11494u, 11560u, 16680u, 16777u, 16825u, 16835u, 42219u, 42236u,
                42238u, 42239u, 42409u, 42412u, 42620u, 44828u, 44829u, 44830u, 44831u, 44832u, 44834u, 44837u,
                44897u, 44903u, 44904u, 44905u, 44906u, 44907u, 44908u, 44909u, 44910u, 44911u, 44912u, 44913u,
                45659u, 45660u, 45665u, 45682u, 45685u, 45691u, 45701u, 45702u, 45724u, 45752u, 45753u, 45758u,
                45760u, 45788u, 45791u, 45794u, 45810u, 45822u, 45824u, 48056u, 48057u, 48058u, 48060u, 48076u,
                48088u, 48089u, 48090u, 48105u, 48152u, 48153u, 48154u, 48156u, 48157u, 48158u, 48166u, 48236u,
                48249u, 48260u, 48261u, 48276u, 48280u, 48344u, 48345u, 48346u, 48361u, 48364u, 48365u, 48376u,
                48377u, 48378u, 48397u, 48629u, 48634u, 48660u, 48661u, 48662u, 48692u, 48693u, 48694u, 48749u,
                48769u, 48772u, 48793u, 49493u, 49497u, 49533u, 49573u, 49574u, 49578u, 49597u, 49633u, 49636u,
                49656u, 49657u, 50326u, 50334u, 50337u, 50338u, 50342u, 96194u, 96196u, 96199u, 96206u, 96207u,
                96208u, 96209u, 96218u, 96219u, 96226u, 96227u, 96229u, 96230u, 96268u, 96269u, 96270u, 96282u,
                96297u, 96313u, 96315u, 96330u, 96331u, 96332u, 96333u, 96334u, 96335u, 96349u, 96350u, 96360u,
                96361u, 96362u, 96363u, 96364u, 96365u, 96366u, 96368u, 96369u, 96371u, 96380u, 96381u, 96386u,
                96387u, 96388u, 96394u, 96396u, 96399u, 96400u, 96401u, 96402u, 96403u, 96414u, 96418u, 96419u,
                96915u, 96921u, 96925u, 96932u, 96940u, 96950u, 96954u, 96970u, 96983u, 96998u, 97001u, 97006u,
                97009u, 97089u, 97092u, 97096u, 97098u, 97099u, 97100u, 97107u, 97140u, 97157u, 97162u, 97178u,
                97180u, 97182u, 97185u, 97187u, 97196u, 97197u, 97198u, 97199u, 97200u, 97201u, 97202u, 97203u,
                97204u, 97205u, 97206u, 97207u, 97208u, 97209u, 97210u, 97211u, 97212u, 97213u, 97214u, 97215u,
                97216u, 97217u, 97218u, 97219u, 97220u, 97222u, 97224u, 97225u, 97229u, 97230u, 97232u, 97237u,
                97239u, 97241u, 97249u, 97251u, 97252u, 97253u, 97254u, 97256u, 97257u, 97259u, 97260u, 97261u,
                97262u, 97263u, 97266u, 97267u, 97268u, 97270u, 97274u, 97295u, 97303u, 97304u, 97307u, 97308u,
                97310u, 97312u, 97322u, 97323u, 97347u, 97358u, 97499u, 97500u, 97501u, 111318u, 115549u, 115555u,
                115556u, 115557u, 115576u, 115587u, 115588u, 115590u, 115591u, 115592u, 115597u, 115598u, 115605u, 115629u,
                115631u, 115639u, 115662u, 115697u, 115700u, 115701u, 115702u, 115708u, 115709u, 115710u, 115721u, 115722u,
                115733u, 115734u, 115736u, 115738u, 115745u, 115746u, 115754u, 115756u, 115768u, 115769u, 115781u, 115783u,
                115793u, 115808u, 115809u, 115810u, 115813u, 115814u, 115820u, 115821u, 115822u, 115828u, 115829u, 115831u,
                115836u, 115840u, 115843u, 115848u, 115860u, 115861u, 115862u, 115874u, 115875u, 115877u, 115878u, 115884u,
                115885u, 115900u, 115902u, 115904u, 115906u, 115922u, 115924u, 115925u, 115927u, 115936u, 115937u, 115941u,
                115945u, 115952u, 115953u, 115955u, 115957u, 115968u, 115969u, 115983u, 115987u, 115988u, 115989u, 115991u,
                115992u, 115993u, 115995u, 115996u, 116006u, 116008u, 116018u, 116025u, 116030u, 116036u, 116063u, 116067u,
                116068u, 116069u, 116073u, 116074u, 116081u, 116082u, 116083u, 116084u, 116089u, 116093u, 116094u, 116097u,
                116098u, 116106u, 116108u, 116121u, 116122u, 116125u, 116126u, 116127u, 116145u, 116146u, 116147u, 116149u,
                116151u, 116153u, 116154u, 116156u, 116161u, 116162u, 116165u, 116166u, 116169u, 116172u, 116177u, 116178u,
                116179u, 116191u, 116197u, 116198u, 116200u, 116204u, 116207u, 116218u, 116221u, 116228u, 116235u, 116255u,
                116323u, 116324u, 116326u, 116331u, 116332u, 116338u, 116339u, 116342u, 116357u, 116361u, 116379u, 116380u,
                116381u, 116382u, 116399u, 116401u, 116427u, 116430u, 116435u, 116436u, 116457u, 116458u, 116460u, 116461u,
                116464u, 116465u, 116471u, 116472u, 116473u, 116483u, 116490u, 116492u, 116519u, 116520u, 116523u, 116524u,
                116525u, 116529u, 116530u, 116541u, 116542u, 116543u, 116547u, 116550u, 116557u, 116561u, 116562u, 116564u,
                116567u, 116568u, 116569u, 116572u, 116581u, 116582u, 116589u, 116590u, 116592u, 116593u, 116596u, 116607u,
                116609u, 116610u, 116614u, 116618u, 116625u, 116635u, 116637u, 116638u, 116639u, 116640u, 116641u, 116646u,
                116647u, 116648u, 116657u, 116660u, 116663u, 116664u, 116665u, 116666u, 116668u, 116672u, 116681u, 116682u,
                116697u, 116699u, 116702u, 116706u, 116715u, 116721u, 116723u, 116726u, 116727u, 116735u, 116741u, 116747u,
                116748u, 116764u, 116773u, 116779u, 116780u, 116782u, 116783u, 116791u, 116795u, 116798u, 116800u, 116803u,
                116809u, 116815u, 116819u, 116821u, 116824u, 116839u, 116841u, 116842u, 116845u, 116847u, 116856u, 116899u,
                116903u, 116910u, 116911u, 116912u, 116918u, 116920u, 116922u, 116935u, 116944u, 117029u, 117031u, 117041u,
                117050u, 117057u, 117081u, 117086u, 117092u, 117094u, 117099u, 117100u, 117102u, 117111u, 117112u, 117123u,
                117125u, 117130u, 117141u, 117142u, 117143u, 117148u, 117149u, 117150u, 117156u, 117157u, 117165u, 117166u,
                117191u, 117232u, 117233u, 117234u, 117244u, 117245u, 117257u, 117258u, 117269u, 118835u, 118843u, 118844u,
                118852u, 118871u, 118880u, 118882u, 118891u, 119014u, 119015u, 119016u, 119023u, 119038u, 119044u, 119045u,
                119058u, 119060u, 119061u, 119070u, 119071u, 119073u, 119090u, 119091u, 119097u, 119103u, 119105u, 119115u,
                119122u, 119123u, 119125u, 119139u, 119145u, 119151u, 119152u, 119154u, 119160u, 119162u, 119172u, 119184u,
                119194u, 119195u, 119196u, 119210u, 119211u, 119212u, 119219u, 119235u, 119247u, 119262u, 119263u, 119275u,
                119289u, 119313u, 119316u, 119324u, 119336u, 119337u, 119346u, 119347u, 119358u, 119359u, 119361u, 119371u,
                119373u, 119374u, 119375u, 119376u, 119395u, 119397u, 119401u, 119406u, 119407u, 119416u, 119417u, 119421u,
                119427u, 119428u, 119429u, 119439u, 119447u, 119448u, 119460u, 119473u, 119482u, 119501u, 119504u, 119512u,
                119514u, 119515u, 119521u, 119523u, 119528u, 119529u, 119534u, 119545u, 119546u, 119552u, 119554u, 119561u,
                119564u, 119566u, 119567u, 119572u, 119574u, 119584u, 119585u, 119586u, 119600u, 119601u, 119604u, 119605u,
                119606u, 119607u, 119609u, 119620u, 119629u, 119630u, 119638u, 119645u, 119653u, 119675u, 119683u, 119684u,
                119685u, 119693u, 119694u, 119699u, 119700u, 119708u, 119718u, 119728u, 119736u, 119737u, 119741u, 119748u,
                119756u, 119762u, 119772u, 119784u, 119785u, 119790u, 119792u, 119795u, 119796u, 119800u, 119807u, 119808u,
                119809u, 119810u, 119811u, 119813u, 119814u, 119824u, 119833u, 119834u, 119848u, 119853u, 119854u, 119861u,
                119864u, 119865u, 119866u, 119884u, 119891u, 119898u, 119916u, 119921u, 119922u, 119923u, 119926u, 119937u,
                119938u, 119940u, 119950u, 119951u, 119959u, 119960u, 119969u, 119971u, 119983u, 119990u, 119991u, 119992u,
                119999u, 120000u, 120006u, 120007u, 120021u, 120022u, 120032u, 120035u, 120038u, 120040u, 120041u, 120042u,
                120055u, 120064u, 120081u, 120082u, 120084u, 120089u, 120090u, 120099u, 120105u, 120106u, 120107u, 120119u,
                120121u, 120122u, 120128u, 120135u, 120137u, 120138u, 120139u, 120147u, 120151u, 120152u, 120158u, 120160u,
                120167u, 120168u, 120172u, 120174u, 120175u, 120188u, 120189u, 120193u, 120203u, 120206u, 120208u, 120228u,
                120232u, 120233u, 120238u, 120239u, 120244u, 120249u, 120256u, 120264u, 120281u, 120297u, 120298u, 120310u,
                120311u, 120336u, 120350u, 120351u, 120360u, 120362u, 120363u, 120364u, 120366u, 120367u, 120381u, 120382u,
                120383u, 120385u, 120386u, 120387u, 120388u, 120415u, 120417u, 120435u, 120437u, 120449u, 120450u, 120451u,
                120452u, 120468u, 120469u, 120470u, 120471u, 120472u, 120483u, 120486u, 120487u, 120489u, 120503u, 120504u,
                120515u, 120516u, 120529u, 120535u, 120536u, 120539u, 120540u, 120542u, 120548u, 120550u, 120555u, 120558u,
                120568u, 120577u, 120587u, 120589u, 120597u, 120610u, 120621u, 120622u, 120631u, 120632u, 120634u, 120640u,
                120651u, 120668u, 120670u, 120673u, 120685u, 120692u, 120709u, 120710u, 120716u, 120718u, 120733u, 120738u,
                120757u, 120758u, 120766u, 120772u, 120777u, 120784u, 120805u, 120806u, 120814u, 120815u, 120825u, 120834u,
                120835u, 120845u, 120874u, 120877u, 120878u, 120882u, 120888u, 120898u, 120899u, 120900u, 120904u, 120905u,
                120911u, 120915u, 120925u, 120934u, 120940u, 120943u, 120952u, 120959u, 120976u, 120979u, 120981u, 120982u,
                120985u, 120986u, 120990u, 120992u, 121003u, 121009u, 121010u, 121011u, 121026u, 121044u, 121052u, 121066u,
                121070u, 121078u, 121106u, 121118u, 121120u, 121139u, 121146u, 121147u, 121158u, 121159u, 121161u, 121169u,
                121171u, 121181u, 121184u, 121185u, 121197u, 121202u, 121217u, 121219u, 121226u, 121228u, 121237u, 121239u,
                121249u, 121251u, 121266u, 121268u, 121273u, 121290u, 121294u, 121899u, 122001u, 122007u, 122019u, 122021u,
                122022u, 122028u, 122029u, 122062u, 122066u, 122068u, 122267u, 122268u, 122269u, 122270u, 122271u, 122273u,
                122373u, 122674u, 122755u, 122819u, 122820u, 122821u, 122827u, 122829u, 122836u, 122852u, 122873u, 122874u,
                123278u, 123279u, 130283u, 130397u, 130437u, 130438u, 130439u, 130443u, 130491u, 130496u, 130497u, 130499u,
                130500u, 130501u, 130502u, 130509u, 130510u, 130511u, 130512u, 130513u, 130515u, 130517u, 130518u, 130520u,
                130521u, 130522u, 130523u, 130524u, 130525u, 130526u, 130527u, 130528u, 131254u, 131255u, 131256u, 131257u,
                131258u, 131259u, 131269u, 131270u, 131271u, 131272u, 131273u, 131274u, 131275u, 131276u, 131277u, 131278u,
                131279u, 131280u, 131281u, 131282u, 131283u, 131284u, 131285u, 131286u, 131287u, 131288u, 131289u, 131290u,
                131302u, 131303u, 131304u, 131305u, 131306u, 131307u, 131308u, 131309u, 131310u, 131338u, 131339u, 131340u,
                131341u, 131342u, 131343u, 131346u, 131347u, 131407u, 131408u, 131410u, 131411u, 131412u, 131413u, 131416u,
                131479u, 131480u, 131482u, 131907u, 131961u, 131962u, 131963u, 131964u, 131965u, 131978u, 131981u, 131983u,
                131984u, 131985u, 131986u, 131990u, 131991u, 131999u, 132000u, 132006u, 132007u, 132009u, 132010u, 132011u,
                132015u, 132018u, 132019u, 132020u, 132023u, 132024u, 132026u, 132027u, 132028u, 20000002u, 20000009u, 20000033u,
                20000038u, 20000044u, 20000046u, 20000051u, 20000052u, 20000054u, 20000063u, 20000064u, 20000075u, 20000077u, 20000082u, 20000105u,
                20000107u, 20000108u, 20000109u, 20000119u, 20000274u, 20000279u, 20000280u, 20000292u, 20000300u, 20000302u, 20000304u, 20000311u,
                20000312u, 20000315u, 20000317u, 20000324u, 20000327u, 20000335u, 20000338u, 20000339u, 20000341u, 20000344u, 20000348u, 20000349u,
                20000351u, 20000355u, 20000358u, 20000361u, 20000366u, 20000367u, 20000375u, 20000378u, 20000380u, 20000381u, 20000382u, 20000471u,
                20000472u, 20000478u, 20000479u, 20000485u, 20000491u, 20000492u, 20000493u
            }),

            // tool_type 1 HEALING_DISC, attack_type 2 ATTACK_RANGED - 214 templates
            new WeaponRows(1u, 2u, new[]
            {
                96205u, 96912u, 96997u, 97271u, 110851u, 110852u, 110853u, 110854u, 110855u, 110856u, 110857u, 110858u,
                110859u, 110860u, 115565u, 115873u, 115913u, 115962u, 116477u, 116865u, 116937u, 117256u, 118870u, 119176u,
                119248u, 119311u, 119399u, 119438u, 119458u, 119470u, 119643u, 119707u, 119798u, 119846u, 119897u, 119914u,
                120034u, 120062u, 120126u, 120274u, 120319u, 120379u, 120447u, 120520u, 120567u, 120575u, 120595u, 120608u,
                120639u, 120671u, 120795u, 120822u, 120950u, 120974u, 121031u, 121107u, 121121u, 121201u, 121341u, 121342u,
                121343u, 121345u, 121347u, 121348u, 121349u, 121350u, 121351u, 121352u, 121353u, 121354u, 121355u, 121356u,
                121357u, 121358u, 121359u, 121360u, 121361u, 121362u, 121363u, 121364u, 121365u, 121366u, 121367u, 121368u,
                121369u, 121370u, 121402u, 121403u, 121404u, 121405u, 121406u, 121407u, 121408u, 121409u, 121410u, 121412u,
                121413u, 121414u, 121415u, 121416u, 121417u, 121418u, 121419u, 121420u, 121421u, 121422u, 121423u, 121424u,
                121425u, 121426u, 121427u, 121428u, 121429u, 121430u, 121470u, 121476u, 121477u, 121478u, 121479u, 121480u,
                121481u, 121482u, 121483u, 121484u, 121485u, 121486u, 121487u, 121488u, 121489u, 121490u, 121581u, 121582u,
                121583u, 121584u, 121585u, 121586u, 121587u, 121588u, 121589u, 121590u, 121591u, 121592u, 121593u, 121594u,
                121595u, 121596u, 121597u, 121598u, 121599u, 121600u, 121601u, 121602u, 121603u, 121604u, 121605u, 121606u,
                121607u, 121608u, 121609u, 121610u, 121611u, 121612u, 121613u, 121614u, 121615u, 121621u, 121622u, 121641u,
                121642u, 121643u, 121644u, 121645u, 121646u, 121647u, 121648u, 121649u, 121650u, 121651u, 121652u, 121653u,
                121654u, 121655u, 121656u, 121657u, 121658u, 121659u, 121660u, 121661u, 121662u, 121663u, 121664u, 121665u,
                121666u, 121667u, 121668u, 121669u, 121670u, 121999u, 131241u, 131242u, 131243u, 131244u, 132002u, 132003u,
                132031u, 20000299u, 20000316u, 20000321u, 20000331u, 20000333u, 20000353u, 20000362u, 20000365u, 20000420u
            }),

            // tool_type 2 ARMOR_AUG, attack_type 2 ATTACK_RANGED - 24 templates
            new WeaponRows(2u, 2u, new[]
            {
                11482u, 18100u, 18105u, 18110u, 18111u, 18120u, 18121u, 18130u, 18131u, 18140u, 18141u, 18142u,
                18155u, 18156u, 18157u, 18170u, 18171u, 18172u, 18185u, 18186u, 18187u, 18200u, 18201u, 18202u
            }),

            // tool_type 3 CIPHER, attack_type 2 ATTACK_RANGED - 12 templates
            new WeaponRows(3u, 2u, new[]
            {
                96995u, 97004u, 110835u, 115563u, 115797u, 115960u, 116767u, 116816u, 116938u, 20000329u, 20000352u, 20000376u
            }),

            // tool_type 4 TISSUE_EXTRACTOR, attack_type 2 ATTACK_RANGED - 16 templates
            new WeaponRows(4u, 2u, new[]
            {
                44876u, 44877u, 96203u, 116983u, 116984u, 117001u, 117007u, 117023u, 117024u, 117025u, 117026u, 117027u,
                20000306u, 20000320u, 20000360u, 20000363u
            }),

            // tool_type 5 SALVAGE, attack_type 2 ATTACK_RANGED - 11 templates
            new WeaponRows(5u, 2u, new[]
            {
                96204u, 110837u, 110838u, 110839u, 115562u, 115914u, 116476u, 116766u, 116951u, 20000275u, 20000350u
            }),

            // tool_type 6 FIELD_REPAIR, attack_type 2 ATTACK_RANGED - 233 templates
            new WeaponRows(6u, 2u, new[]
            {
                11483u, 97054u, 110871u, 110872u, 110873u, 110874u, 110875u, 110876u, 110877u, 110878u, 110879u, 110880u,
                115796u, 115799u, 115867u, 115872u, 115961u, 116320u, 116322u, 116811u, 116866u, 116928u, 116942u, 118861u,
                119042u, 119137u, 119159u, 119256u, 119283u, 119394u, 119414u, 119520u, 119551u, 119563u, 119599u, 119691u,
                119789u, 119793u, 119805u, 119949u, 119957u, 119997u, 120037u, 120149u, 120157u, 120214u, 120220u, 120241u,
                120288u, 120291u, 120414u, 120466u, 120502u, 120667u, 120721u, 120730u, 120747u, 120855u, 120870u, 120894u,
                120989u, 121063u, 121085u, 121101u, 121225u, 121265u, 121284u, 121311u, 121312u, 121313u, 121314u, 121315u,
                121316u, 121317u, 121318u, 121319u, 121320u, 121321u, 121322u, 121323u, 121324u, 121325u, 121326u, 121327u,
                121328u, 121329u, 121330u, 121331u, 121332u, 121333u, 121334u, 121335u, 121336u, 121337u, 121338u, 121339u,
                121340u, 121372u, 121374u, 121375u, 121376u, 121377u, 121378u, 121379u, 121380u, 121381u, 121382u, 121383u,
                121384u, 121385u, 121386u, 121387u, 121388u, 121389u, 121390u, 121391u, 121392u, 121393u, 121394u, 121395u,
                121396u, 121397u, 121398u, 121399u, 121400u, 121446u, 121447u, 121448u, 121449u, 121450u, 121451u, 121452u,
                121453u, 121454u, 121455u, 121456u, 121457u, 121458u, 121459u, 121460u, 121491u, 121492u, 121493u, 121494u,
                121495u, 121496u, 121497u, 121498u, 121499u, 121500u, 121501u, 121502u, 121503u, 121504u, 121505u, 121506u,
                121507u, 121508u, 121509u, 121510u, 121511u, 121512u, 121513u, 121514u, 121515u, 121516u, 121517u, 121518u,
                121519u, 121520u, 121521u, 121522u, 121523u, 121524u, 121525u, 121531u, 121532u, 121551u, 121552u, 121553u,
                121554u, 121555u, 121556u, 121557u, 121558u, 121559u, 121560u, 121561u, 121562u, 121563u, 121564u, 121565u,
                121566u, 121567u, 121568u, 121569u, 121570u, 121571u, 121572u, 121573u, 121574u, 121575u, 121576u, 121577u,
                121578u, 121579u, 121580u, 122027u, 122826u, 131236u, 131237u, 131238u, 131239u, 131240u, 132001u, 132004u,
                132012u, 20000017u, 20000018u, 20000117u, 20000285u, 20000288u, 20000293u, 20000322u, 20000326u, 20000332u, 20000345u, 20000346u,
                20000356u, 20000359u, 20000379u, 20000419u, 20000490u
            }),

            // tool_type 7 RIFLE, attack_type 2 ATTACK_RANGED - 286 templates
            new WeaponRows(7u, 2u, new[]
            {
                3222u, 3226u, 3252u, 3282u, 3311u, 3340u, 3344u, 11454u, 11455u, 11456u, 17337u, 17381u,
                17384u, 17389u, 17439u, 17443u, 17455u, 17496u, 17508u, 17571u, 42411u, 42421u, 42638u, 45459u,
                45657u, 45687u, 45800u, 48824u, 48825u, 48826u, 48828u, 48829u, 48830u, 48832u, 48833u, 48834u,
                48842u, 48865u, 48866u, 48882u, 48905u, 48906u, 48909u, 48918u, 48945u, 48946u, 48984u, 48985u,
                48986u, 48988u, 48989u, 48990u, 50341u, 96232u, 96259u, 96367u, 96370u, 96389u, 96395u, 96918u,
                96973u, 97022u, 97043u, 97046u, 97060u, 97110u, 97114u, 97116u, 97119u, 97121u, 97125u, 97276u,
                97277u, 97300u, 97316u, 97325u, 97330u, 97333u, 97334u, 97336u, 97337u, 97339u, 97340u, 97341u,
                97342u, 97343u, 97345u, 97346u, 97348u, 97351u, 97370u, 115608u, 115628u, 115691u, 115698u, 115727u,
                115755u, 115758u, 115771u, 115811u, 115851u, 115879u, 115893u, 115903u, 115907u, 115959u, 115990u, 116010u,
                116033u, 116092u, 116095u, 116107u, 116164u, 116168u, 116192u, 116205u, 116217u, 116325u, 116353u, 116358u,
                116532u, 116544u, 116552u, 116559u, 116594u, 116606u, 116620u, 116627u, 116656u, 116661u, 116671u, 116701u,
                116730u, 116734u, 116753u, 116756u, 116799u, 116814u, 116823u, 116858u, 116900u, 116949u, 117032u, 117045u,
                117061u, 117088u, 117114u, 117159u, 117243u, 117255u, 118842u, 118869u, 118890u, 119017u, 119041u, 119136u,
                119138u, 119161u, 119171u, 119197u, 119221u, 119231u, 119232u, 119245u, 119249u, 119264u, 119294u, 119312u,
                119348u, 119372u, 119377u, 119415u, 119441u, 119484u, 119507u, 119527u, 119544u, 119565u, 119583u, 119610u,
                119644u, 119686u, 119710u, 119735u, 119747u, 119760u, 119799u, 119815u, 119863u, 119892u, 119915u, 119939u,
                119998u, 120043u, 120056u, 120100u, 120127u, 120148u, 120159u, 120210u, 120216u, 120221u, 120251u, 120257u,
                120293u, 120365u, 120380u, 120389u, 120436u, 120473u, 120488u, 120507u, 120549u, 120557u, 120598u, 120609u,
                120620u, 120669u, 120674u, 120684u, 120708u, 120717u, 120749u, 120783u, 120813u, 120826u, 120844u, 120856u,
                120883u, 120903u, 120941u, 120960u, 120980u, 121012u, 121023u, 121032u, 121034u, 121037u, 121054u, 121079u,
                121100u, 121117u, 121122u, 121145u, 121170u, 121179u, 121227u, 121250u, 121272u, 121286u, 121901u, 122020u,
                122047u, 122276u, 122372u, 122720u, 130284u, 130441u, 131311u, 131312u, 131313u, 131314u, 131315u, 131316u,
                131317u, 131318u, 131319u, 131320u, 131321u, 131344u, 131345u, 131992u, 132005u, 132025u, 20000013u, 20000040u,
                20000066u, 20000111u, 20000282u, 20000313u, 20000325u, 20000328u, 20000336u, 20000357u, 20000481u, 20000483u
            }),

            // tool_type 8 PISTOL, attack_type 2 ATTACK_RANGED - 276 templates
            new WeaponRows(8u, 2u, new[]
            {
                782u, 802u, 820u, 822u, 839u, 2968u, 2972u, 3086u, 3090u, 11443u, 11444u, 11445u,
                17019u, 17020u, 17026u, 17077u, 17079u, 17131u, 17144u, 17186u, 17195u, 42437u, 45032u, 45466u,
                45671u, 45697u, 45725u, 48408u, 48409u, 48410u, 48412u, 48413u, 48414u, 48416u, 48417u, 48418u,
                48420u, 48421u, 48422u, 48426u, 48452u, 48453u, 48454u, 48461u, 48496u, 48497u, 48498u, 48549u,
                48584u, 48585u, 48586u, 48588u, 48589u, 48590u, 48592u, 48593u, 48594u, 96197u, 96201u, 96240u,
                96314u, 96397u, 96924u, 97038u, 97045u, 97055u, 97124u, 97127u, 97165u, 97245u, 97279u, 97283u,
                97287u, 97288u, 97289u, 97291u, 97294u, 97296u, 97298u, 97299u, 97313u, 97319u, 97332u, 97378u,
                97380u, 111200u, 115550u, 115578u, 115624u, 115652u, 115682u, 115687u, 115688u, 115699u, 115735u, 115752u,
                115759u, 115823u, 115838u, 115876u, 115894u, 115943u, 115947u, 115986u, 115994u, 116076u, 116096u, 116099u,
                116128u, 116148u, 116152u, 116180u, 116219u, 116362u, 116400u, 116493u, 116548u, 116560u, 116563u, 116571u,
                116591u, 116608u, 116616u, 116658u, 116662u, 116692u, 116713u, 116716u, 116746u, 116775u, 116829u, 116835u,
                116864u, 116901u, 116905u, 116919u, 116936u, 117073u, 117103u, 117120u, 117126u, 117151u, 117231u, 118833u,
                118860u, 118881u, 119072u, 119092u, 119096u, 119104u, 119124u, 119153u, 119185u, 119206u, 119220u, 119230u,
                119280u, 119282u, 119317u, 119335u, 119360u, 119408u, 119426u, 119461u, 119472u, 119503u, 119522u, 119568u,
                119573u, 119627u, 119676u, 119701u, 119742u, 119764u, 119783u, 119794u, 119806u, 119835u, 119847u, 119883u,
                119927u, 119958u, 119972u, 119989u, 120031u, 120083u, 120091u, 120120u, 120123u, 120136u, 120150u, 120176u,
                120186u, 120195u, 120205u, 120234u, 120245u, 120266u, 120290u, 120299u, 120337u, 120368u, 120384u, 120448u,
                120467u, 120484u, 120506u, 120514u, 120541u, 120578u, 120590u, 120633u, 120652u, 120699u, 120720u, 120731u,
                120740u, 120765u, 120779u, 120804u, 120871u, 120875u, 120876u, 120895u, 120897u, 120917u, 120926u, 120933u,
                120944u, 120951u, 120975u, 120987u, 121004u, 121017u, 121033u, 121042u, 121053u, 121086u, 121096u, 121116u,
                121130u, 121160u, 121195u, 121203u, 121267u, 121293u, 122010u, 122371u, 122719u, 122828u, 122875u, 123349u,
                123350u, 131291u, 131292u, 131293u, 131294u, 131295u, 131296u, 131297u, 131298u, 131299u, 131300u, 131301u,
                131905u, 131987u, 131988u, 131989u, 20000025u, 20000055u, 20000072u, 20000078u, 20000116u, 20000291u, 20000473u, 20000482u
            }),

            // tool_type 9 SHOTGUN, attack_type 2 ATTACK_RANGED - 295 templates
            new WeaponRows(9u, 2u, new[]
            {
                600u, 602u, 964u, 982u, 1000u, 1018u, 1036u, 3510u, 3514u, 3540u, 3544u, 3628u,
                11460u, 11461u, 11462u, 11463u, 11464u, 11465u, 14337u, 15003u, 17730u, 17766u, 17801u, 17814u,
                17843u, 17845u, 17898u, 17899u, 17953u, 17955u, 42422u, 42423u, 49184u, 49185u, 49186u, 49206u,
                49228u, 49229u, 49230u, 49232u, 49233u, 49234u, 49236u, 49237u, 49238u, 49240u, 49241u, 49242u,
                49272u, 49273u, 49274u, 49276u, 49277u, 49278u, 49280u, 49281u, 49282u, 49284u, 49285u, 49286u,
                49316u, 49317u, 49318u, 49404u, 49405u, 49406u, 49408u, 49409u, 49410u, 96221u, 96231u, 96244u,
                96352u, 96420u, 96961u, 97017u, 97024u, 97026u, 97031u, 97056u, 97120u, 97122u, 97130u, 97280u,
                97305u, 97306u, 97309u, 97320u, 97338u, 97350u, 97369u, 97372u, 97374u, 97375u, 97376u, 97377u,
                97379u, 97381u, 97382u, 97383u, 97384u, 97385u, 97387u, 97388u, 111160u, 115552u, 115558u, 115607u,
                115641u, 115703u, 115723u, 115725u, 115753u, 115795u, 115815u, 115839u, 115863u, 115864u, 115871u, 115901u,
                115939u, 115958u, 115998u, 116004u, 116011u, 116070u, 116090u, 116124u, 116136u, 116163u, 116170u, 116190u,
                116223u, 116247u, 116295u, 116402u, 116428u, 116437u, 116456u, 116474u, 116485u, 116501u, 116549u, 116575u,
                116583u, 116636u, 116674u, 116707u, 116740u, 116762u, 116812u, 116840u, 116846u, 116849u, 116913u, 116933u,
                117056u, 117082u, 117129u, 117167u, 117267u, 118851u, 118878u, 119022u, 119043u, 119059u, 119114u, 119146u,
                119178u, 119207u, 119213u, 119233u, 119257u, 119281u, 119295u, 119325u, 119396u, 119400u, 119418u, 119449u,
                119513u, 119535u, 119553u, 119562u, 119602u, 119603u, 119622u, 119637u, 119651u, 119692u, 119715u, 119726u,
                119773u, 119778u, 119791u, 119812u, 119823u, 119856u, 119868u, 119899u, 119924u, 119952u, 119984u, 120008u,
                120024u, 120036u, 120039u, 120063u, 120108u, 120140u, 120166u, 120173u, 120187u, 120204u, 120226u, 120240u,
                120243u, 120275u, 120279u, 120309u, 120320u, 120353u, 120361u, 120416u, 120453u, 120505u, 120521u, 120530u,
                120538u, 120569u, 120642u, 120690u, 120714u, 120734u, 120756u, 120773u, 120796u, 120824u, 120836u, 120889u,
                120901u, 120909u, 120984u, 120991u, 121018u, 121024u, 121038u, 121064u, 121080u, 121108u, 121119u, 121131u,
                121183u, 121218u, 121238u, 121291u, 122000u, 122030u, 122277u, 122370u, 122721u, 122818u, 122872u, 130442u,
                130514u, 131328u, 131329u, 131330u, 131331u, 131332u, 131333u, 131334u, 131335u, 131336u, 131337u, 131906u,
                131996u, 131997u, 131998u, 132029u, 132030u, 20000008u, 20000034u, 20000081u, 20000104u, 20000273u, 20000319u, 20000334u,
                20000340u, 20000343u, 20000354u, 20000364u, 20000484u, 20000486u, 20000495u
            }),

            // tool_type 10 GRENADE_LAUNCHER, attack_type 2 ATTACK_RANGED - 172 templates
            new WeaponRows(10u, 2u, new[]
            {
                1072u, 1089u, 11427u, 11428u, 11429u, 11430u, 11431u, 16623u, 42237u, 42644u, 45692u, 45757u,
                47896u, 47897u, 47898u, 47913u, 47928u, 47929u, 47930u, 47936u, 47945u, 47960u, 47977u, 47978u,
                48009u, 96316u, 96329u, 96416u, 96421u, 96984u, 97034u, 97042u, 97150u, 97189u, 97190u, 97193u,
                97194u, 97238u, 97246u, 97250u, 97292u, 97371u, 115593u, 115606u, 115640u, 115726u, 115737u, 115747u,
                115762u, 115792u, 115841u, 115850u, 115870u, 115895u, 115921u, 115938u, 115970u, 115984u, 116016u, 116022u,
                116034u, 116100u, 116135u, 116199u, 116222u, 116230u, 116340u, 116351u, 116429u, 116466u, 116491u, 116551u,
                116565u, 116573u, 116595u, 116611u, 116619u, 116626u, 116725u, 116742u, 116758u, 116789u, 116805u, 116813u,
                116827u, 116831u, 116837u, 116852u, 116904u, 116945u, 117116u, 117133u, 118853u, 118883u, 119024u, 119039u,
                119116u, 119135u, 119143u, 119177u, 119182u, 119310u, 119314u, 119322u, 119420u, 119459u, 119485u, 119505u,
                119559u, 119570u, 119654u, 119677u, 119717u, 119779u, 119889u, 119900u, 119928u, 119981u, 120053u, 120097u,
                120117u, 120133u, 120145u, 120185u, 120215u, 120222u, 120273u, 120289u, 120292u, 120318u, 120334u, 120358u,
                120485u, 120522u, 120527u, 120653u, 120701u, 120722u, 120729u, 120748u, 120797u, 120823u, 120857u, 120872u,
                120896u, 120957u, 120977u, 121001u, 121099u, 121180u, 121285u, 121902u, 122008u, 131248u, 131249u, 131250u,
                131251u, 131252u, 131253u, 131980u, 132017u, 132022u, 20000068u, 20000085u, 20000106u, 20000298u, 20000330u, 20000342u,
                20000347u, 20000377u, 20000384u, 20000496u
            }),

            // tool_type 11 ROCKET_LAUNCHER, attack_type 2 ATTACK_RANGED - 192 templates
            new WeaponRows(11u, 2u, new[]
            {
                11457u, 11458u, 11459u, 45668u, 45686u, 45733u, 49105u, 49137u, 49169u, 96200u, 96220u, 96398u,
                96404u, 96916u, 96989u, 96994u, 97091u, 97141u, 97264u, 97354u, 97356u, 97359u, 97360u, 97362u,
                97363u, 115551u, 115577u, 115675u, 115757u, 115761u, 115763u, 115770u, 115837u, 115849u, 115886u, 115923u,
                115926u, 115942u, 115946u, 115985u, 116009u, 116014u, 116019u, 116031u, 116091u, 116134u, 116150u, 116155u,
                116220u, 116315u, 116359u, 116360u, 116499u, 116521u, 116546u, 116558u, 116570u, 116574u, 116580u, 116605u,
                116615u, 116655u, 116685u, 116714u, 116752u, 116755u, 116778u, 116817u, 116848u, 116862u, 116921u, 117030u,
                117087u, 117093u, 117106u, 117119u, 117225u, 118862u, 118892u, 119093u, 119265u, 119349u, 119409u, 119446u,
                119533u, 119543u, 119608u, 119628u, 119702u, 119727u, 119743u, 119758u, 119816u, 119836u, 119855u, 119862u,
                119867u, 119970u, 120005u, 120023u, 120044u, 120092u, 120124u, 120165u, 120177u, 120202u, 120207u, 120255u,
                120352u, 120369u, 120537u, 120556u, 120588u, 120619u, 120686u, 120719u, 120732u, 120767u, 120771u, 120827u,
                120873u, 120884u, 120910u, 120924u, 120935u, 120939u, 120983u, 120988u, 121196u, 121220u, 121240u, 121252u,
                121292u, 122838u, 122839u, 122915u, 122916u, 122917u, 122918u, 122919u, 122920u, 122921u, 122922u, 122923u,
                122924u, 122925u, 122926u, 122927u, 122928u, 122929u, 122930u, 122931u, 122932u, 122933u, 122934u, 122935u,
                122936u, 122937u, 122938u, 122939u, 122940u, 122941u, 122942u, 122943u, 122944u, 122945u, 122946u, 131322u,
                131323u, 131324u, 131325u, 131326u, 131327u, 131908u, 131993u, 131994u, 131995u, 132016u, 20000039u, 20000045u,
                20000058u, 20000071u, 20000118u, 20000303u, 20000314u, 20000318u, 20000323u, 20000337u, 20000368u, 20000487u, 20000488u, 20000489u
            }),

            // tool_type 12 DENSITY_GUN, attack_type 2 ATTACK_RANGED - 226 templates
            new WeaponRows(12u, 2u, new[]
            {
                1303u, 1321u, 11344u, 11349u, 11363u, 11368u, 11401u, 11423u, 11424u, 11425u, 11426u, 42398u,
                42439u, 47704u, 47705u, 47706u, 47708u, 47709u, 47710u, 47712u, 47713u, 47714u, 47733u, 47744u,
                47745u, 47746u, 47748u, 47749u, 47750u, 47752u, 47753u, 47754u, 47761u, 47784u, 47788u, 47809u,
                47812u, 47824u, 47825u, 47826u, 96195u, 96228u, 96267u, 96271u, 96275u, 96290u, 96324u, 96328u,
                96336u, 96351u, 96405u, 96417u, 96952u, 96953u, 96990u, 97011u, 97102u, 97104u, 97147u, 97161u,
                97168u, 97170u, 97172u, 97173u, 97179u, 97181u, 97192u, 97223u, 97255u, 97258u, 97335u, 115589u,
                115599u, 115623u, 115673u, 115711u, 115732u, 115739u, 115782u, 115794u, 115830u, 115842u, 115887u, 115905u,
                115997u, 116024u, 116035u, 116075u, 116123u, 116167u, 116171u, 116206u, 116231u, 116333u, 116341u, 116352u,
                116414u, 116438u, 116462u, 116484u, 116500u, 116526u, 116531u, 116566u, 116612u, 116642u, 116649u, 116688u,
                116724u, 116761u, 116794u, 116834u, 116853u, 116940u, 117049u, 117055u, 117067u, 117069u, 117105u, 117113u,
                117122u, 117131u, 117158u, 117268u, 118834u, 118879u, 119025u, 119040u, 119117u, 119140u, 119144u, 119170u,
                119183u, 119218u, 119234u, 119246u, 119274u, 119288u, 119315u, 119323u, 119419u, 119483u, 119500u, 119506u,
                119560u, 119569u, 119652u, 119678u, 119716u, 119749u, 119763u, 119771u, 119890u, 119925u, 119982u, 120054u,
                120098u, 120118u, 120134u, 120146u, 120194u, 120209u, 120227u, 120242u, 120250u, 120265u, 120280u, 120335u,
                120359u, 120482u, 120528u, 120654u, 120691u, 120715u, 120739u, 120778u, 120785u, 120843u, 120890u, 120902u,
                120916u, 120942u, 120958u, 120978u, 121002u, 121025u, 121043u, 121051u, 121065u, 121069u, 121077u, 121105u,
                121138u, 121182u, 121274u, 121295u, 121900u, 122009u, 122275u, 130440u, 130519u, 131260u, 131261u, 131262u,
                131263u, 131264u, 131265u, 131266u, 131267u, 131268u, 131982u, 132013u, 132014u, 20000001u, 20000007u, 20000019u,
                20000021u, 20000057u, 20000065u, 20000074u, 20000083u, 20000110u, 20000301u, 20000383u, 20000480u, 20000497u
            }),
        };

        /// <summary>
        /// The 120 rows already in the table whose class attacks with WEAPON_BLADE 418, and which therefore read
        /// attack_type 2 "Ranged" today. They take 1 ATTACK_MELEE.
        /// </summary>
        private static readonly uint[] BladesThatReadRanged =
        {
            1487u, 1489u, 1492u, 1497u, 1499u, 1501u, 1504u, 1509u, 1511u, 1513u, 1516u, 1521u,
            1523u, 1525u, 1528u, 1533u, 1535u, 1537u, 1540u, 1545u, 3706u, 3714u, 3722u, 3730u,
            3738u, 12095u, 12096u, 12097u, 12098u, 12099u, 12100u, 12101u, 12102u, 12103u, 12104u, 12105u,
            12106u, 12107u, 12108u, 12109u, 15468u, 15469u, 15470u, 15474u, 15475u, 15476u, 15495u, 15496u,
            15497u, 18075u, 18076u, 18077u, 45726u, 45732u, 45818u, 47624u, 47625u, 47626u, 47628u, 47629u,
            47630u, 47633u, 47634u, 47640u, 47641u, 47642u, 47644u, 47645u, 47646u, 47648u, 47649u, 47650u,
            47652u, 47654u, 47660u, 47661u, 47662u, 47664u, 47665u, 47666u, 47668u, 47670u, 47672u, 47673u,
            47674u, 47680u, 47681u, 47682u, 47684u, 47685u, 47686u, 47688u, 47689u, 47690u, 47692u, 47693u,
            47694u, 47700u, 47701u, 47702u, 50435u, 50436u, 50437u, 50438u, 50439u, 50463u, 50464u, 50465u,
            50467u, 50468u, 50469u, 50471u, 50472u, 50473u, 50475u, 50476u, 50477u, 50479u, 50480u, 50481u
        };

        /// <summary>
        /// The three rows Retune_weapon_tool_type could not see, swept to 0 by its closing statement, with the
        /// tool_type the client's own tables give them.
        /// </summary>
        private static readonly (uint Template, uint ToolType)[] ToolTypesSweptToZero =
        {
            (116929u, 8u), // Vextronics Pistol, 8 PISTOL
            (116930u, 8u), // Vextronics Pulse Pistol, 8 PISTOL
            (122871u, 6u), // Field Repair Tool, 6 FIELD_REPAIR
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            foreach (var group in NewRows)
                migrationBuilder.InsertData(table: Table, columns: Columns, values: Values(group));

            migrationBuilder.Sql(
                $"update {Table} set attack_type = 1 where attack_type = 2 and id in ({Ids(BladesThatReadRanged)});");

            foreach (var (template, toolType) in ToolTypesSweptToZero)
                migrationBuilder.Sql(
                    $"update {Table} set tool_type = {toolType} where tool_type = 0 and id = {template};");
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            foreach (var group in NewRows)
                migrationBuilder.DeleteData(table: Table, keyColumn: "id",
                    keyValues: System.Array.ConvertAll(group.Templates, id => (object)id));

            migrationBuilder.Sql(
                $"update {Table} set attack_type = 2 where attack_type = 1 and id in ({Ids(BladesThatReadRanged)});");

            // Retune_weapon_tool_type's Down only reverts rows still holding what its Up set, and these three
            // were never in its lists, so this migration has to put them back to 0 for that Down to find them.
            foreach (var (template, toolType) in ToolTypesSweptToZero)
                migrationBuilder.Sql(
                    $"update {Table} set tool_type = 0 where tool_type = {toolType} and id = {template};");
        }

        /// <summary>The group's templates as full rows: id, the twenty placeholders, tool_type, attack_type.</summary>
        private static object[,] Values(WeaponRows group)
        {
            var values = new object[group.Templates.Length, 23];

            for (var row = 0; row < group.Templates.Length; row++)
            {
                values[row, 0] = group.Templates[row];

                // Placeholders is the column order with id, tool_type and attack_type taken out: the first ten
                // sit between id and tool_type, the other ten between tool_type and attack_type.
                for (var column = 0; column < 10; column++)
                    values[row, column + 1] = Placeholders[column];

                values[row, 11] = group.ToolType;

                for (var column = 10; column < 20; column++)
                    values[row, column + 2] = Placeholders[column];

                values[row, 22] = group.AttackType;
            }

            return values;
        }

        private static string Ids(uint[] templates) => string.Join(",", templates);
    }
}
