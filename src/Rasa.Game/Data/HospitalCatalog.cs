using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Data
{
    public sealed class HospitalData
    {
        /// <summary>graveyardlanguage key: the id PlayerDead advertises and ReviveMe returns.</summary>
        public int GraveyardId { get; }

        /// <summary>waypointlanguage key: the id GraveyardGained announces and the character persists.</summary>
        public uint WaypointId { get; }

        public uint MapContextId { get; }

        /// <summary>Original uimapmarker.factionedmarkers entity id of the HOSPITAL/SAFE_ZONE marker.</summary>
        public ulong MarkerEntityId { get; }

        /// <summary>The marker's original coordinates, used as the respawn point (see docs/evidence/hospital-catalog.json).</summary>
        public Vector3 Position { get; }

        /// <summary>GraveyardInfo isSafe: selects the client's icon only; it does not gate the destination.</summary>
        public bool IsSafe { get; }

        /// <summary>Offered only while its control point is AFS-held (official control-point guide).</summary>
        public bool IsControlPoint { get; }

        /// <summary>Offered without a GraveyardGained discovery (no gain line in the boot-camp footage).</summary>
        public bool KnownWithoutDiscovery { get; }

        internal HospitalData(int graveyardId, uint waypointId, uint mapContextId, ulong markerEntityId,
            Vector3 position, bool isSafe, bool isControlPoint, bool knownWithoutDiscovery)
        {
            GraveyardId = graveyardId;
            WaypointId = waypointId;
            MapContextId = mapContextId;
            MarkerEntityId = markerEntityId;
            Position = position;
            IsSafe = isSafe;
            IsControlPoint = isControlPoint;
            KnownWithoutDiscovery = knownWithoutDiscovery;
        }
    }

    /// <summary>
    /// Every hospital the client's own map data puts on a map this server loads: each HOSPITAL (3) or
    /// SAFE_ZONE (19) marker of uimapmarker, joined to graveyardlanguage and waypointlanguage by its
    /// name, with the world seed's type-5 teleporter row naming the marker whose own text has no
    /// waypoint entry. Every field's tier and citation is recorded in
    /// docs/evidence/hospital-catalog.json, which CatalogMatchesItsEvidenceRecord compares against
    /// these rows.
    ///
    /// The 2026-09-14 pass built seven of these by hand and reached two maps, so a player who died
    /// anywhere else was revived where they fell. The same recipe, run over every map
    /// (research/20260917-hospital-coverage), reproduces all seven exactly - ids, marker entities and
    /// positions - and resolves 102 hospitals on 41 maps. The markers it cannot resolve, because no
    /// graveyardlanguage entry reads their name, are listed as GAP-HOSPITAL-UNRESOLVED-GRAVEYARD;
    /// their maps keep the revive-in-place fallback.
    /// </summary>
    public static class HospitalCatalog
    {
        /// <summary>
        /// Horizontal distance from a hospital within which it is gained. Inferred lower bound:
        /// "You just gained Alia Das Hospital." was already in chat in the first frame at Alia Das,
        /// measured 98.9 m from the hospital marker (footage B2-019). The original trigger is
        /// unrecovered.
        /// </summary>
        public const float DiscoveryRadius = 100f;

        public static IReadOnlyList<HospitalData> Entries { get; } = Array.AsReadOnly(new[]
        {
            // adv_foreas_concordia_divide (context 1148, map template 1306).
            new HospitalData(200, 96, 1148, 132770324754369UL,
                new Vector3(172.02880859375f, 85.24999237060547f, -83.5672836303711f), false, false, false),   // Hospital: Foxtrot Outpost
            new HospitalData(201, 94, 1148, 132770324754368UL,
                new Vector3(457.09295654296875f, 81.49970245361328f, -338.5539855957031f), false, false, false),   // Hospital: Delta Outpost
            new HospitalData(203, 97, 1148, 132770324754372UL,
                new Vector3(289.8951110839844f, 171.00997924804688f, 1082.0887451171875f), false, false, false),   // Hospital: Thoria Das
            new HospitalData(240, 220, 1148, 132770324755387UL,
                new Vector3(-285.3243408203125f, 57.07631301879883f, 14.20481014251709f), false, true, false),   // Hospital: Hydro Plant (Control Point)
            new HospitalData(241, 222, 1148, 132770324755168UL,
                new Vector3(-683.0f, 121.93602752685547f, -664.5f), false, true, false),   // Hospital: Purgas (Control Point)

            // adv_foreas_concordia_wilderness (context 1220, map template 1378).
            new HospitalData(3, 103, 1220, 133079561961146UL,
                new Vector3(787.0088500976562f, 294.3324279785156f, 366.5991516113281f), false, false, false),   // Alia Das Hospital
            new HospitalData(5, 106, 1220, 133079561961147UL,
                new Vector3(-693.8690185546875f, 170.1329803466797f, -342.3608093261719f), false, false, false),   // Ranja Gorge Hospital
            new HospitalData(6, 105, 1220, 133079561961145UL,
                new Vector3(-125.88742065429688f, 220.74998474121094f, -468.61846923828125f), true, false, false),   // Twin Pillars Hospital
            new HospitalData(20, 108, 1220, 133079561961149UL,
                new Vector3(-654.5541381835938f, 283.6459655761719f, 882.8185424804688f), false, false, false),   // Daghda's Urn Hospital
            new HospitalData(122, 218, 1220, 133079561961338UL,
                new Vector3(-330.0f, 172.7716827392578f, -532.0f), false, true, false),   // Hospital: Imperial Valley (Control Point)
            new HospitalData(136, 216, 1220, 133079561962699UL,
                new Vector3(156.02780151367188f, 163.0747833251953f, -86.59078979492188f), false, true, false),   // Hospital: Landing Zone (Control Point)

            // adv_foreas_concordia_palisades (context 1244, map template 1402).
            new HospitalData(136, 216, 1244, 133182640964490UL,
                new Vector3(-143.5533447265625f, 172.3076934814453f, -752.7608642578125f), false, true, false),   // Hospital: Landing Zone (Control Point)
            new HospitalData(217, 114, 1244, 133182640965835UL,
                new Vector3(868.220947265625f, 126.30036926269531f, -220.63720703125f), false, false, false),   // Staging Point First Aid Station
            new HospitalData(220, 224, 1244, 133182640964388UL,
                new Vector3(219.139404296875f, 108.71795654296875f, 290.06982421875f), false, true, false),   // Hospital: River-base Krimm (Control Point)

            // adv_foreas_valverde_pools (context 1304, map template 1461).
            new HospitalData(148, 290, 1304, 133436044038302UL,
                new Vector3(214.9423828125f, 865.2113037109375f, 394.3936767578125f), false, true, false),   // Hospital: Retread Outpost (Control Point)
            new HospitalData(149, 292, 1304, 133436044038399UL,
                new Vector3(-448.33966064453125f, 679.6659545898438f, -434.5720520019531f), false, true, false),   // Hospital: Bane Comm Center (Control Point)
            new HospitalData(150, 294, 1304, 133436044038496UL,
                new Vector3(548.814697265625f, 669.6635131835938f, -129.92364501953125f), false, true, false),   // Hospital: Bane Guard Station (Control Point)

            // adv_foreas_concordia_divide_minoscaverns (context 1347, map template 1504).
            new HospitalData(39, 460, 1347, 133620727551304UL,
                new Vector3(-0.9948493242263794f, 30.71072769165039f, 112.28780364990234f), false, false, false),   // Minos Caverns Field Medic

            // adv_foreas_concordia_palisades_treebackcamp (context 1397, map template 1554).
            new HospitalData(223, 115, 1397, 133835475920611UL,
                new Vector3(-281.3327331542969f, 100.04835510253906f, 287.1845703125f), false, false, false),   // Treeback Medic

            // adv_foreas_concordia_wilderness_guardianprom (context 1416, map template 1573).
            new HospitalData(115, 352, 1416, 133921375261863UL,
                new Vector3(152.21275329589844f, 316.7974548339844f, 191.10617065429688f), false, false, false),   // Guardian Prominence First Aid Station

            // adv_foreas_concordia_wilderness_pravusresearch (context 1430, map template 1587).
            new HospitalData(208, 107, 1430, 133981504855237UL,
                new Vector3(-237.9490966796875f, 6.822786331176758f, -125.17790985107422f), false, false, false),   // AFS Preparation Camp

            // adv_foreas_valverde_marshes_banesupplydepot (context 1451, map template 1608).
            new HospitalData(193, 384, 1451, 134071699115249UL,
                new Vector3(-356.1842041015625f, 149.60740661621094f, -2.4957666397094727f), false, false, false),   // Entrance
            new HospitalData(194, 385, 1451, 134071699115264UL,
                new Vector3(129.877685546875f, 125.55077362060547f, 312.864501953125f), false, false, false),   // Survivor's Camp

            // adv_foreas_valverde_marshes (context 1454, map template 1611).
            new HospitalData(44, 123, 1454, 134084584059673UL,
                new Vector3(-141.48641967773438f, 219.0768280029297f, 615.4169311523438f), false, false, false),   // Hospital: Falcon Hold
            new HospitalData(45, 124, 1454, 134084584059682UL,
                new Vector3(-315.9443359375f, 218.37440490722656f, 130.2781982421875f), true, false, false),   // Hospital: Paludos
            new HospitalData(156, 296, 1454, 134084584060012UL,
                new Vector3(-296.6475524902344f, 216.49998474121094f, -344.92620849609375f), false, true, false),   // Hospital: Research Point (Control Point)
            new HospitalData(158, 298, 1454, 134084584060115UL,
                new Vector3(130.2562255859375f, 218.30331420898438f, -327.513916015625f), false, true, false),   // Hospital: Bane Assertion Camp (Control Point)

            // adv_foreas_valverde_plateau (context 1497, map template 1654).
            new HospitalData(50, 127, 1497, 134269267734265UL,
                new Vector3(-20.95772933959961f, 413.6238098144531f, 839.5672607421875f), true, false, false),   // Fort Defiance Hospital
            new HospitalData(64, 132, 1497, 134269267734228UL,
                new Vector3(-634.029296875f, 442.3580322265625f, -237.59375f), false, false, false),   // Camp Resistance Hospital
            new HospitalData(65, 129, 1497, 134269267734229UL,
                new Vector3(362.8266906738281f, 369.4112854003906f, 373.9657287597656f), false, false, false),   // Wedge Rock Hospital
            new HospitalData(144, 281, 1497, 134269267734395UL,
                new Vector3(126.041015625f, 369.7477722167969f, 439.656982421875f), false, true, false),   // Hospital: Northeast AFS (Control Point)
            new HospitalData(145, 284, 1497, 134269267734489UL,
                new Vector3(-403.1534729003906f, 375.912841796875f, 374.7436218261719f), false, true, false),   // Hospital: Northwest AFS (Control Point)
            new HospitalData(146, 286, 1497, 134269267734586UL,
                new Vector3(104.560791015625f, 369.3558654785156f, -111.93731689453125f), false, true, false),   // Hospital: Southeast Bane (Control Point)
            new HospitalData(147, 288, 1497, 134269267734678UL,
                new Vector3(-343.6121826171875f, 374.7445373535156f, -83.90869140625f), false, true, false),   // Hospital: Southwest Bane (Control Point)

            // adv_arieki_ligo_ashendesert (context 1734, map template 1734).
            new HospitalData(102, 207, 1734, 134419591468914UL,
                new Vector3(594.2335205078125f, 248.50001525878906f, -309.5002136230469f), false, false, false),   // Ashoka Settlement Hospital
            new HospitalData(163, 322, 1734, 134419591473007UL,
                new Vector3(450.6573486328125f, 271.5506896972656f, -67.5615234375f), false, true, false),   // Hospital: Conscription Garrison (Control Point)
            new HospitalData(166, 318, 1734, 134419591473171UL,
                new Vector3(-484.2029113769531f, 281.50347900390625f, 192.75169372558594f), false, true, false),   // Hospital: White Oasis Post (Control Point)

            // adv_foreas_valverde_marshes_villageruins (context 1743, map template 1743).
            new HospitalData(211, 246, 1743, 134419591470209UL,
                new Vector3(-116.57867431640625f, 63.729942321777344f, -272.8157958984375f), false, false, false),   // Healing Shaman

            // adv_arieki_torden_mires (context 1759, map template 1759).
            new HospitalData(121, 240, 1759, 134419591474027UL,
                new Vector3(-177.0745849609375f, 206.80528259277344f, 24.209457397460938f), false, true, false),   // Hospital: Orsa (Control Point)
            new HospitalData(123, 238, 1759, 134419591474145UL,
                new Vector3(538.5057373046875f, 258.5631408691406f, -309.79052734375f), false, true, false),   // Hospital: Iapyx (Control Point)

            // adv_arieki_torden_incline (context 1761, map template 1761).
            new HospitalData(63, 146, 1761, 134419591485477UL,
                new Vector3(-182.2227783203125f, 235.23594665527344f, -244.20729064941406f), false, false, false),   // Hospital: Nyxroq Post
            new HospitalData(131, 234, 1761, 134419591485611UL,
                new Vector3(443.4337158203125f, 279.0110168457031f, 384.2257080078125f), false, true, false),   // Hospital: Badlands (Control Point)
            new HospitalData(137, 236, 1761, 134419591485696UL,
                new Vector3(347.83721923828125f, 264.85992431640625f, 27.79014015197754f), false, true, false),   // Hospital: Ortho (Control Point)

            // adv_arieki_torden_plains (context 1764, map template 1764).
            new HospitalData(61, 145, 1764, 134419591472344UL,
                new Vector3(207.93905639648438f, 425.4999694824219f, -213.51202392578125f), true, false, false),   // Hospital: Irendas Penal Colony
            new HospitalData(90, 152, 1764, 134419591472342UL,
                new Vector3(699.5739135742188f, 413.4999694824219f, 237.4461669921875f), false, false, false),   // Hospital: Eir Post
            new HospitalData(91, 154, 1764, 134419591472380UL,
                new Vector3(-534.5572509765625f, 441.99993896484375f, 416.8391418457031f), false, false, false),   // Hospital: Mt. Hellas Outpost
            new HospitalData(124, 231, 1764, 134419591472575UL,
                new Vector3(164.313720703125f, 433.3558349609375f, 272.3486328125f), false, true, false),   // Hospital: Geyser Chimney Basin (Control Point)
            new HospitalData(127, 232, 1764, 134419591472711UL,
                new Vector3(-142.3101348876953f, 430.5406188964844f, 487.4844970703125f), false, true, false),   // Hospital: Lightning Fields (Control Point)
            new HospitalData(129, 228, 1764, 134419591472834UL,
                new Vector3(787.72705078125f, 430.3551025390625f, 548.80810546875f), false, true, false),   // Hospital: Irendas Support Facility (Control Point)

            // adv_foreas_concordia_divide_purgasstation2 (context 1806, map template 1806).
            new HospitalData(239, 360, 1806, 134419591470249UL,
                new Vector3(87.91918182373047f, 65.60694885253906f, 215.43637084960938f), false, false, false),   // Level 01: Entrance Hall

            // adv_foreas_valverde_plateau_sanctusgrotto (context 1823, map template 1823).
            new HospitalData(36, 120, 1823, 134419591467905UL,
                new Vector3(358.1881103515625f, 212.8375701904297f, -134.24093627929688f), false, false, false),   // AFS Field Medic

            // adv_foreas_valverde_plateau_maligobasev3 (context 1830, map template 1830).
            new HospitalData(198, 398, 1830, 134419591466294UL,
                new Vector3(165.89849853515625f, -130.392578125f, 124.41682434082031f), false, false, false),   // Level 01: Entrance
            new HospitalData(199, 399, 1830, 134419591466305UL,
                new Vector3(-12.697265625f, -90.41950988769531f, 69.69526672363281f), false, false, false),   // Level 02: Barricade

            // adv_arieki_torden_incline_ojasaattahive (context 1865, map template 1865).
            new HospitalData(152, 345, 1865, 134419591468790UL,
                new Vector3(439.2326354980469f, 72.72594451904297f, 126.34949493408203f), false, false, false),   // Ojasa Atta Hive Entrance

            // adv_arieki_ligo_thunderhead (context 1911, map template 1911).
            new HospitalData(75, 173, 1911, 134419591471541UL,
                new Vector3(664.0f, 383.96258544921875f, -627.0f), true, false, false),   // Thunderhead Base Hospital
            new HospitalData(83, 177, 1911, 134419591471537UL,
                new Vector3(954.0f, 440.4999694824219f, 452.0f), false, false, false),   // Substation E-104 Field Hospital
            new HospitalData(167, 324, 1911, 134419591472444UL,
                new Vector3(-543.5642700195312f, 384.12872314453125f, 193.2474365234375f), false, true, false),   // Hospital: Western Rim (Control Point)
            new HospitalData(168, 326, 1911, 134419591472542UL,
                new Vector3(695.4716796875f, 369.3401794433594f, 145.652099609375f), false, true, false),   // Hospital: Eastern Rim (Control Point)
            new HospitalData(185, 328, 1911, 134419591474008UL,
                new Vector3(766.574951171875f, 384.2108459472656f, -194.11566162109375f), false, true, false),   // Hospital: Outpost Aurora (Control Point)
            new HospitalData(186, 330, 1911, 134419591474122UL,
                new Vector3(88.59033203125f, 323.0788879394531f, -62.4854736328125f), false, true, false),   // Hospital: Fault Lever (Control Point)
            new HospitalData(196, 172, 1911, 134419591471539UL,
                new Vector3(0.253815621137619f, 337.2181091308594f, -437.2283935546875f), false, false, false),   // Viddia Camp Hospital

            // adv_arieki_ligo_burningsteps_magmacaverns (context 1977, map template 1977).
            new HospitalData(36, 120, 1977, 134419591467275UL,
                new Vector3(-368.1123962402344f, 206.59341430664062f, -441.9260559082031f), false, false, false),   // AFS Field Medic
            new HospitalData(251, 486, 1977, 134419591467250UL,
                new Vector3(-396.1268615722656f, 168.986572265625f, -93.2828369140625f), false, false, false),   // Field Medic: Penumbra Landing Zone
            new HospitalData(252, 485, 1977, 134419591467251UL,
                new Vector3(-314.19244384765625f, 131.99998474121094f, 557.6046142578125f), false, false, false),   // Field Medic: Rebel Camp

            // adv_bootcamp (context 1985, map template 1985).
            new HospitalData(20000001, 500, 1985, 134419591469927UL,
                new Vector3(357.9005432128906f, 120.32544708251953f, 156.51882934570312f), false, false, true),   // Refugee Base Medic

            // adv_arieki_ligo_ashendesert_baneconscriptfacility (context 1988, map template 1988).
            new HospitalData(36, 120, 1988, 134419591475767UL,
                new Vector3(7.270895004272461f, 156.1740264892578f, -636.6821899414062f), false, false, false),   // AFS Field Medic

            // adv_arieki_ligo_burningsteps (context 1993, map template 1993).
            new HospitalData(159, 314, 1993, 134419591475338UL,
                new Vector3(730.7578125f, 167.94613647460938f, 567.0394287109375f), false, true, false),   // Hospital: Fort Intrepid (Control Point)
            new HospitalData(161, 316, 1993, 134419591475431UL,
                new Vector3(407.8728942871094f, 168.43228149414062f, 431.3149719238281f), false, true, false),   // Hospital: Prometheus Outpost (Control Point)

            // adv_arieki_torden_abyss (context 2028, map template 2028).
            new HospitalData(160, 279, 2028, 134419591469579UL,
                new Vector3(213.2125244140625f, 549.88427734375f, 493.949462890625f), false, true, false),   // Hospital: Dybukkar Forward Camp (Control Point)
            new HospitalData(162, 277, 2028, 134419591469800UL,
                new Vector3(-66.67415618896484f, 534.1665649414062f, -164.4309844970703f), false, true, false),   // Hospital: Charon's Crossing (Control Point)

            // adv_foreas_valverde_descent (context 2047, map template 2047).
            new HospitalData(118, 184, 2047, 134419591466200UL,
                new Vector3(-309.5f, 668.1631469726562f, -277.75f), false, false, false),   // Camp Cato Medic Post
            new HospitalData(170, 183, 2047, 134419591466199UL,
                new Vector3(-234.0f, 744.6907958984375f, -616.0f), true, false, false),   // Fort Virgil Hospital
            new HospitalData(172, 210, 2047, 134419591466208UL,
                new Vector3(126.25f, 382.4999694824219f, 589.5f), false, false, false),   // Antaeus Hollow Medic Station
            new HospitalData(179, 300, 2047, 134419591468246UL,
                new Vector3(-418.91162109375f, 715.1746215820312f, 90.9638671875f), false, true, false),   // Hospital: Virgil's Resonator (Control Point)
            new HospitalData(180, 302, 2047, 134419591468283UL,
                new Vector3(578.545166015625f, 469.86474609375f, 301.1544189453125f), false, true, false),   // Hospital: Mal Dys Upper Resonator (Control Point)
            new HospitalData(181, 304, 2047, 134419591468317UL,
                new Vector3(230.763671875f, 332.0810852050781f, 19.739013671875f), false, true, false),   // Hospital: Mal Dys Lower Resonator (Control Point)
            new HospitalData(182, 306, 2047, 134419591468356UL,
                new Vector3(36.271484375f, 240.30868530273438f, 161.860107421875f), false, true, false),   // Hospital: Drill Resonator (Control Point)

            // adv_foreas_howlingmaw1 (context 2051, map template 2051).
            new HospitalData(165, 365, 2051, 134419591474106UL,
                new Vector3(-580.3209838867188f, 200.40000915527344f, -1055.2991943359375f), true, false, false),   // Gangus Outpost Hospital
            new HospitalData(184, 308, 2051, 134419591475385UL,
                new Vector3(-1197.053466796875f, 245.05984497070312f, -494.66436767578125f), false, true, false),   // Hospital: Cuthah Scout Post (Control Point)
            new HospitalData(191, 310, 2051, 134419591475784UL,
                new Vector3(-535.361572265625f, 200.54898071289062f, -354.6510314941406f), false, true, false),   // Hospital: Dead Zone Power Station (Control Point)
            new HospitalData(192, 312, 2051, 134419591475890UL,
                new Vector3(1278.45263671875f, 243.55947875976562f, 369.20703125f), false, true, false),   // Hospital: Cuthah Ammo Depot (Control Point)
            new HospitalData(213, 409, 2051, 134419591469844UL,
                new Vector3(990.0f, 195.5f, -914.25f), false, false, false),   // Dia Uyona Hospital

            // adv_arieki_ligo_ashendesert_indracaverns (context 2055, map template 2055).
            new HospitalData(36, 120, 2055, 134419591467680UL,
                new Vector3(-59.95016098022461f, 7.378549575805664f, -404.4630126953125f), false, false, false),   // AFS Field Medic

            // adv_foreas_concordia_elohcommtower2 (context 2084, map template 2084).
            new HospitalData(279, 171, 2084, 134419591467094UL,
                new Vector3(55.465576171875f, 185.2952423095703f, -228.4051513671875f), false, false, false),   // Hospital: Forean Pyramid
            new HospitalData(280, 478, 2084, 134419591467095UL,
                new Vector3(-47.6590576171875f, 170.7975616455078f, -456.01171875f), false, false, false),   // Forward Recon Medic

            // adv_arieki_ligo_thunderhead_faultlever (context 2103, map template 2103).
            new HospitalData(210, 431, 2103, 134419591467820UL,
                new Vector3(-2.6827826499938965f, 325.5865173339844f, 133.3858642578125f), false, false, false),   // Fault Lever Entrance

            // adv_arieki_ligo_thunderhead_quassostation (context 2105, map template 2105).
            new HospitalData(151, 344, 2105, 134419591473829UL,
                new Vector3(9.535249710083008f, 46.007694244384766f, -182.37879943847656f), false, false, false),   // Quasso Station Entrance

            // adv_arieki_torden_incline_wardenbotfactory (context 2111, map template 2111).
            new HospitalData(36, 120, 2111, 134419591466694UL,
                new Vector3(-289.1764831542969f, 104.23150634765625f, 76.07638549804688f), false, false, false),   // AFS Field Medic
            new HospitalData(268, 516, 2111, 134419591470631UL,
                new Vector3(-81.02997589111328f, 10.051852226257324f, -14.587703704833984f), false, true, false),   // Hospital: Raksha Robotics (Control Point)

            // adv_arieki_torden_mires_tahrendrabase (context 2125, map template 2125).
            new HospitalData(218, 606, 2125, 134419591464564UL,
                new Vector3(194.32373046875f, 119.49857330322266f, 41.72578811645508f), false, false, false),   // Tahrendra Base Field Medic

            // adv_foreas_howlingmaw_deathburrow (context 2136, map template 2136).
            new HospitalData(214, 414, 2136, 134419591472211UL,
                new Vector3(165.23631286621094f, 77.28186798095703f, -520.2821044921875f), false, false, false),   // Hospital: Velon Hollow

            // adv_arieki_ligo_crucible_wbfacility (context 2146, map template 2146).
            new HospitalData(164, 364, 2146, 134419591464130UL,
                new Vector3(-25.901691436767578f, 17.232723236083984f, -273.0149230957031f), false, false, false),   // Chaukas Entrance

            // adv_foreas_howlingmaw_cuthahbase (context 2162, map template 2162).
            new HospitalData(209, 407, 2162, 134419591468845UL,
                new Vector3(-55.826744079589844f, 108.23229217529297f, -31.991342544555664f), false, false, false),   // Sublevel Medic Station

            // adv_arieki_torden_abyss_dybukkar (context 2190, map template 2190).
            new HospitalData(261, 499, 2190, 134419591471134UL,
                new Vector3(-41.00615692138672f, 321.6073913574219f, -583.9042358398438f), false, false, false),   // Dybukkar Medical Team

            // adv_arieki_torden_abyss_omegalabs (context 2203, map template 2203).
            new HospitalData(230, 458, 2203, 134419591466068UL,
                new Vector3(-16.33629035949707f, 268.50146484375f, -428.60992431640625f), false, false, false),   // Entrance Medic Post
            new HospitalData(231, 459, 2203, 134419591466066UL,
                new Vector3(154.00762939453125f, 248.0015411376953f, -116.09347534179688f), false, false, false),   // Network Operations Medic Unit

            // adv_earth_unitedstates_manhattan_01 (context 2327, map template 2331).
            new HospitalData(270, 517, 2327, 134419591465261UL,
                new Vector3(-276.5034484863281f, 185.29441833496094f, -493.4731750488281f), false, false, false),   // Medic: 23rd Street Station
            new HospitalData(271, 518, 2327, 134419591466017UL,
                new Vector3(149.75335693359375f, 213.60877990722656f, -148.3151092529297f), false, false, false),   // Hospital: A.F.S. Outpost Lexington
            new HospitalData(272, 519, 2327, 134419591466025UL,
                new Vector3(-50.418514251708984f, 208.88693237304688f, 280.3808288574219f), false, false, false),   // Field Medic: Empire Sector

            // adv_earth_unitedstates_manhattan_01_shared (context 2375, map template 2378).
            new HospitalData(270, 517, 2375, 134419591465261UL,
                new Vector3(-276.5034484863281f, 185.29441833496094f, -493.4731750488281f), false, false, false),   // Medic: 23rd Street Station
            new HospitalData(271, 518, 2375, 134419591466017UL,
                new Vector3(149.75335693359375f, 213.60877990722656f, -148.3151092529297f), false, false, false),   // Hospital: A.F.S. Outpost Lexington
            new HospitalData(272, 519, 2375, 134419591466025UL,
                new Vector3(-50.418514251708984f, 208.88693237304688f, 280.3808288574219f), false, false, false)   // Field Medic: Empire Sector
        });

        public static IEnumerable<HospitalData> ForMap(uint mapContextId)
            => Entries.Where(hospital => hospital.MapContextId == mapContextId);
    }
}
