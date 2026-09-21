using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// The whole world put back on its floor: 126 bodies, 113 spawn pools and 13 content
    /// placements, across 32 maps.
    ///
    /// A live report on 2026-09-20 ("npc placement seems off too", with a shot of an NPC standing in a cot at
    /// Alia Das) sent every body in the world through the navmesh again. The 2026-09-17 sweep
    /// (<see cref="WorldPlacementFloorSnapRows"/>) had only read content_placement - 107 rows - so the 819 spawn
    /// pools taken in from upstream had never been measured against the ground at all. They are most of what is
    /// below: vendors and field medics standing two to six metres in the air, their Y evidently copied from a
    /// nearby map marker rather than from the floor, the worst of them the Torcastra Prison vendor at +5.90 m and
    /// the two A.F.S. Outpost Lexington hospitals at +4.72 m.
    ///
    /// The floor is the same one that batch measured and the same one WorldFloorSweepTests now guards:
    /// <c>navmesh surface - 0.276 m</c>, the offset the original server's own creature spawns sit at. Only rows
    /// more than 0.5 m off it are moved, so a body standing on a step or a platform the navmesh carries is left
    /// where it is.
    ///
    /// Three kinds of correction, marked in the comments:
    ///   * <b>snap</b> - the floor is under the body's own x,z; only Y moves, and no sourced coordinate changes.
    ///   * <b>column</b> - nothing was within the query's 8 m vertical reach, so the column at that x,z was
    ///     scanned. These are the rows whose Y was never sourced: four TaRapedia /loc NPCs whose wiki entry gives
    ///     x and z only, and whose placeholder Y was out by 14 to 40 m, and the CP Token Banker at the Wilderness
    ///     landing zone, 10.7 m above the pad.
    ///   * <b>move</b> - no floor in that column either, so the nearest walkable point was taken. Eight are the
    ///     OD-48 analogue ring at Torden Mires, which sat just inside the rock face; one is upstream's Lamna
    ///     Medical Team; one is upstream's Boargar General spawn, buried 15.7 m under the Wilderness terrain.
    ///
    /// Not moved: content_placement 198677, the bomb on the wreck's hull, and the three AFS_Turret_Mini pools,
    /// which are mounted rather than standing.
    /// </summary>
    public static class WorldFloorSweepRows
    {
        public const string Migration = "WorldFloorSweep";

        /// <summary>(id, the x,y,z it had, the x,y,z it is moved to) for content_placement.</summary>
        public static readonly (uint Id, double WasX, double WasY, double WasZ, double X, double Y, double Z)[] Placements =
        {
            (199101u, -230.000, 166.000, -462.000, -230.000, 180.241, -462.000), // +14.24 column-far Ranger Urialia (TaRapedia /loc)
            (199103u, -439.000, 166.000, 37.000, -439.000, 186.170, 37.000), // +20.17 column-far Warden Lagori (TaRapedia /loc)
            (199106u, -337.200, 103.600, 353.700, -337.200, 137.794, 353.700), // +34.19 column-far Field Lt. Bagby (TaRapedia /loc)
            (199107u, -122.200, 100.300, 128.200, -122.200, 140.442, 128.200), // +40.14 column-far Lt. Galloway (TaRapedia /loc)
            (199301u, -777.000, 270.000, -552.000, -777.000, 240.902, -552.000), // -29.10 column-far Retread Jeska (TaRapedia /loc)
            (199826u, 642.000, 225.000, 373.000, 643.000, 224.284, 374.732), // -0.72 move Professor Long's area (OD-48 analogue)
            (199827u, 640.876, 225.000, 366.000, 646.672, 224.284, 367.553), // -0.72 move Professor Long's area (OD-48 analogue)
            (199829u, 653.000, 225.000, 365.000, 653.000, 224.284, 369.000), // -0.72 move Professor Long's area (OD-48 analogue)
            (199830u, 658.500, 225.000, 363.474, 659.535, 224.284, 367.338), // -0.72 move Professor Long's area (OD-48 analogue)
            (199831u, 665.124, 225.000, 366.000, 665.124, 224.284, 368.000), // -0.72 move Professor Long's area (OD-48 analogue)
            (199847u, 644.000, 224.000, 374.000, 646.000, 224.284, 374.000), // +0.28 move Dr. Robertson's area (OD-48 analogue)
            (199850u, 655.552, 224.000, 363.276, 656.587, 224.284, 367.140), // +0.28 move Dr. Robertson's area (OD-48 analogue)
            (199851u, 661.115, 224.000, 360.351, 663.186, 224.284, 368.078), // +0.28 move Dr. Robertson's area (OD-48 analogue)
        };

        /// <summary>(id, the x,y,z it had, the x,y,z it is moved to) for spawnpool.</summary>
        public static readonly (uint Id, double WasX, double WasY, double WasZ, double X, double Y, double Z)[] Pools =
        {
            (510104u, 282.700, 170.930, 1076.200, 282.700, 170.124, 1076.200), // -0.81 snap Elder Q'uoa - Thoria Das
            (520015u, 524.000, 47.400, -644.000, 524.000, 46.569, -644.000), // -0.83 snap Caretaker Mordra - Divide
            (34u, -700.160, 170.137, -337.969, -700.160, 170.783, -337.969), // +0.65 snap Test Vendor 3
            (41u, 600.176, 270.914, 269.203, 600.176, 286.295, 269.203), // +15.38 column-far Boargar General Spawn
            (70u, 349.973, 230.160, 346.578, 349.973, 228.204, 346.578), // -1.96 snap Caretaker
            (73u, 349.973, 230.160, 346.578, 349.973, 228.204, 346.578), // -1.96 snap Bane_Thrax_Grenadier
            (110u, 194.188, 166.176, 166.176, 194.188, 170.203, 166.176), // +4.03 snap Thrax Soldier
            (123u, 21.984, 183.289, -240.137, 21.984, 183.995, -240.137), // +0.71 snap Creature_Filcher
            (500005u, 149.000, 174.300, -115.700, 149.000, 163.292, -115.700), // -11.01 column prestige (Adv_Foreas_Concordia_Wilderness)
            (501012u, -98.500, 220.300, -520.400, -98.500, 221.042, -520.400), // +0.74 snap Biotechnician Trainer: Twin Pillars
            (510005u, -721.200, 227.060, -423.000, -721.200, 226.021, -423.000), // -1.04 snap George Corman - Ranja Gorge
            (500174u, -807.500, 139.000, 583.800, -807.500, 137.970, 583.800), // -1.03 snap Military Surplus: Cumbria Research Facility
            (510121u, 741.400, 150.580, -267.600, 741.400, 150.046, -267.600), // -0.53 snap Captain McShay - Staging Point
            (510123u, -513.000, 141.780, 705.000, -513.000, 140.970, 705.000), // -0.81 snap Commander Aldrin - Cumbria Research Facility
            (510124u, -515.000, 141.730, 708.500, -515.000, 140.970, 708.500), // -0.76 snap Corporal Hutchison - Cumbria Research Facility
            (510125u, 739.400, 152.230, -264.100, 739.400, 151.676, -264.100), // -0.55 snap Field Commander Twitty - Staging Point
            (510131u, -45.500, 154.240, 47.600, -45.500, 153.544, 47.600), // -0.70 snap Ranger Cyrida - Hightower Outpost
            (510132u, -312.200, 173.380, -671.100, -312.200, 171.109, -671.100), // -2.27 snap Ranger Gorodai - Treeback Ridge
            (510136u, -32.700, 119.320, 390.800, -32.700, 118.599, 390.800), // -0.72 snap Thomas Jansona - Cumbria Weald
            (520037u, 437.000, 117.000, -308.000, 437.000, 117.501, -308.000), // +0.50 snap Davinx - Palisades
            (520040u, 305.000, 114.000, 295.000, 305.000, 112.355, 295.000), // -1.64 snap Kennilaxx - Palisades
            (520045u, 590.000, 116.500, -560.000, 590.000, 115.791, -560.000), // -0.71 snap Sinatrix - Palisades
            (510199u, -147.700, 862.260, 524.000, -147.700, 861.662, 524.000), // -0.60 snap Lieutenant Holloway - Retread Camp
            (520056u, 748.000, 676.900, 213.000, 748.000, 677.525, 213.000), // +0.62 snap Goriam - Pools
            (520059u, 260.000, 686.700, -254.000, 260.000, 688.694, -254.000), // +1.99 snap Krammitron - Pools
            (520060u, -655.000, 810.000, 115.000, -655.000, 809.063, 115.000), // -0.94 snap Lililax - Pools
            (520061u, 580.000, 686.500, 620.000, 580.000, 685.813, 620.000), // -0.69 snap Orax - Pools
            (500168u, -272.600, 16.900, 310.500, -272.600, 13.396, 310.500), // -3.50 snap Torcastra Prison AFS Medical Officer
            (500169u, -94.100, 100.100, -271.300, -94.100, 96.611, -271.300), // -3.49 snap Torcastra Prison Field Medic
            (500170u, -97.500, 99.700, -272.100, -97.500, 96.767, -272.100), // -2.93 snap Vendor: Torcastra Prison
            (500171u, -266.800, 19.300, 314.800, -266.800, 13.396, 314.800), // -5.90 snap Vendor: Torcastra Prison
            (510120u, -97.500, 97.990, -272.100, -97.500, 96.767, -272.100), // -1.22 snap Airman Hamilton - Torcastra Prison
            (520030u, 362.000, 217.600, -168.000, 362.000, 216.253, -168.000), // -1.35 snap Commander Sto - Marshes
            (520034u, 459.000, 219.100, 563.000, 459.000, 216.802, 563.000), // -2.30 snap Overseer Nivvik - Marshes
            (520035u, 350.000, 217.800, -525.000, 350.000, 217.264, -525.000), // -0.54 snap Overseer Vesh - Marshes
            (500312u, 376.000, 12.900, -38.500, 377.932, 9.739, -37.982), // -3.16 move Lamna Medical Team
            (500270u, -403.200, 375.900, 374.700, -403.200, 376.460, 374.700), // +0.56 snap Hospital: Northwest AFS (Control Point)
            (500275u, -6.000, 410.000, 920.700, -6.000, 407.602, 920.700), // -2.40 snap Armor Vendor: Fort Defiance
            (500284u, -29.700, 409.100, 922.200, -29.700, 410.602, 922.200), // +1.50 snap Weapons Vendor: Fort Defiance
            (501013u, -58.300, 399.400, 944.000, -58.300, 398.751, 944.000), // -0.65 snap Commando Trainer: Fort Defiance
            (501014u, -59.200, 399.400, 947.500, -59.200, 398.455, 947.500), // -0.94 snap Ranger Trainer: Fort Defiance
            (501015u, -61.800, 399.400, 950.100, -61.800, 398.257, 950.100), // -1.14 snap Sapper Trainer: Fort Defiance
            (501016u, -65.300, 399.400, 951.000, -65.300, 398.389, 951.000), // -1.01 snap Biotechnician Trainer: Fort Defiance
            (501017u, -68.800, 399.400, 950.100, -68.800, 398.342, 950.100), // -1.06 snap Grenadier Trainer: Fort Defiance
            (501023u, -61.800, 399.400, 937.900, -61.800, 398.202, 937.900), // -1.20 snap Medic Trainer: Fort Defiance
            (501024u, -59.200, 399.400, 940.500, -59.200, 398.602, 940.500), // -0.80 snap Exobiologist Trainer: Fort Defiance
            (510177u, 480.900, 425.560, 425.500, 480.900, 426.084, 425.500), // +0.52 snap Colonel Thibodeau - Wedge Rock Outpost
            (510181u, 339.200, 366.050, 643.200, 339.200, 364.541, 643.200), // -1.51 snap Elder Hundra - New Velon Village
            (510185u, 339.200, 364.760, 647.600, 339.200, 363.166, 647.600), // -1.59 snap Loremaster Talisen - New Velon Village
            (510189u, 339.200, 367.030, 638.800, 339.200, 366.146, 638.800), // -0.88 snap Ranger Kaely - New Velon Village
            (510191u, 482.300, 425.320, 421.000, 482.300, 424.083, 421.000), // -1.24 snap Science Officer Clark - Wedge Rock Outpost
            (520051u, -440.000, 442.900, -230.000, -440.000, 442.387, -230.000), // -0.51 snap Goliath - Plateau
            (500293u, 355.700, 170.400, 5.300, 355.700, 169.233, 5.300), // -1.17 snap Ustor Yard Field Medic
            (500262u, 820.000, 324.800, -104.900, 820.000, 321.497, -104.900), // -3.30 snap Logos Research Field Medic
            (500263u, 820.000, 325.100, -102.100, 820.000, 321.497, -102.100), // -3.60 snap Logos Research Field Medic
            (500198u, 26.000, 142.300, 182.700, 26.000, 137.576, 182.700), // -4.72 snap Crater Lake Medic
            (500199u, 24.000, 140.400, 184.800, 24.000, 137.754, 184.800), // -2.65 snap Crater Lake Medic
            (520012u, -272.000, 168.000, -54.000, -272.000, 168.763, -54.000), // +0.76 snap Hominis Machina Lt. Casper - Crater Lake Research
            (510009u, 179.300, 316.850, -557.200, 179.300, 316.288, -557.200), // -0.56 snap Captain Marcus Emmons - Stonewall Ridge
            (520003u, -43.000, 281.400, 233.000, -43.000, 280.442, 233.000), // -0.96 snap Overseer Himbri - Ashen Desert
            (500264u, -116.600, 63.700, -272.800, -116.600, 64.511, -272.800), // +0.81 snap Healing Shaman
            (500265u, -109.100, 62.900, -360.800, -109.100, 59.895, -360.800), // -3.00 snap Healing Shaman
            (501025u, -616.000, 228.500, -520.100, -616.000, 226.684, -520.100), // -1.82 snap Commando Trainer: Torden Mires
            (501026u, -616.900, 228.500, -516.600, -616.900, 229.084, -516.600), // +0.58 snap Ranger Trainer: Torden Mires
            (501031u, -630.000, 228.500, -520.100, -630.000, 226.684, -520.100), // -1.82 snap Sniper Trainer: Torden Mires
            (501036u, -616.900, 228.500, -523.600, -616.900, 226.684, -523.600), // -1.82 snap Exobiologist Trainer: Torden Mires
            (510063u, 396.900, 259.510, 33.000, 396.900, 258.715, 33.000), // -0.80 snap Colonel Deiley - Ortho
            (510065u, 400.100, 260.030, 37.000, 400.100, 259.420, 37.000), // -0.61 snap Coordinator Melsi - Ortho
            (510066u, -234.600, 234.670, -253.500, -234.600, 233.758, -253.500), // -0.91 snap Field Commander Foletto - Nyxroq Trench
            (510068u, -238.300, 234.960, -253.500, -238.300, 233.758, -253.500), // -1.20 snap Lieutenant Epp - Nyxroq Trench
            (510070u, 395.700, 259.800, 38.000, 395.700, 258.981, 38.000), // -0.82 snap Medic Rahish - Ortho Post
            (510072u, 392.200, 259.580, 35.200, 392.200, 258.758, 35.200), // -0.82 snap Protector Starl - Ortho
            (510073u, 392.200, 259.190, 30.800, 392.200, 258.652, 30.800), // -0.54 snap Retread Otto - Ortho Post
            (510075u, -409.400, 162.510, -285.900, -409.400, 161.906, -285.900), // -0.60 snap Science Officer Brandeis - Pax Oasis
            (510076u, -172.400, 277.420, 277.100, -172.400, 276.024, 277.100), // -1.40 snap Science Officer Collier - Raintree Post
            (520047u, -213.000, 424.010, -471.000, -213.000, 422.987, -471.000), // -1.02 snap Cracked Tooth - Plains
            (520049u, 330.000, 432.100, 370.000, 330.000, 431.166, 370.000), // -0.93 snap Plains Devil Lord - Plains
            (520050u, -598.000, 432.000, -278.000, -598.000, 429.724, -278.000), // -2.28 snap The Red Kraken - Plains
            (510096u, -12.900, 314.580, 181.300, -12.900, 313.457, 181.300), // -1.12 snap Lieutenant Donners - Kardash Atta Colony
            (500188u, -1012.200, 27.400, -251.100, -1012.200, 23.861, -251.100), // -3.54 snap Temple of the Bowed Patriarch Entrance
            (500189u, -760.200, 27.200, -272.400, -760.200, 23.921, -272.400), // -3.28 snap Temple of the Proud Patriarch Entrance
            (500190u, -530.800, 27.700, -209.800, -530.800, 23.861, -209.800), // -3.84 snap Temple of the Raging Patriarch Entrance
            (500046u, 632.600, 341.400, -546.800, 632.600, 340.124, -546.800), // -1.28 snap Class Trainer
            (510043u, 678.000, 369.850, 201.200, 678.000, 370.424, 201.200), // +0.57 snap Field Sgt. Barnes - Eastern Rim
            (520063u, 550.000, 393.600, 350.000, 550.000, 392.490, 350.000), // -1.11 snap Painrox - Thunderhead
            (520064u, -830.000, 390.900, 110.000, -830.000, 388.585, 110.000), // -2.31 snap Tiamox - Thunderhead
            (500019u, 7.300, 156.200, -636.700, 7.300, 153.190, -636.700), // -3.01 snap AFS Field Medic
            (500020u, 10.500, 155.900, -636.600, 10.500, 153.190, -636.600), // -2.71 snap AFS Field Medic
            (510015u, -15.700, 188.430, 323.100, -15.700, 187.806, 323.100), // -0.62 snap Awol Captain Cheung - Staal
            (510016u, -11.000, 189.150, 326.500, -11.000, 188.396, 326.500), // -0.75 snap Awol Lieutenant Deirdre - Staal
            (510021u, 404.200, 168.820, 387.900, 404.200, 169.687, 387.900), // +0.87 snap Captain Tiersky - Prometheus Outpost
            (510024u, -13.900, 188.770, 328.600, -13.900, 187.904, 328.600), // -0.87 snap Kappa Grupa Lohen - Staal
            (510025u, -17.500, 188.270, 328.600, -17.500, 187.752, 328.600), // -0.52 snap Labbna Grupa Rigs - Staal
            (510029u, 402.200, 169.070, 391.400, 402.200, 169.759, 391.400), // +0.69 snap Larai Zupa Madias - Prometheus Outpost
            (510031u, 402.200, 168.630, 384.400, 402.200, 169.673, 384.400), // +1.04 snap Major Keplinger - Prometheus Outpost
            (510035u, -17.500, 188.410, 317.600, -17.500, 187.806, 317.600), // -0.60 snap Special Agent Capriulo - Staal
            (510036u, -13.900, 188.680, 317.600, -13.900, 187.806, 317.600), // -0.87 snap Viddea Scientist Eugin - Staal
            (510037u, -11.000, 188.850, 319.700, -11.000, 188.140, 319.700), // -0.71 snap Viddia Grupa Donal - Staal
            (520013u, 757.100, 171.100, 549.100, 757.100, 170.183, 549.100), // -0.92 snap Executioner Derge - Crucible
            (520014u, 757.100, 171.100, 549.100, 757.100, 170.183, 549.100), // -0.92 snap Thrax Commander - Crucible
            (510054u, 350.000, 529.880, -110.100, 350.000, 528.705, -110.100), // -1.17 snap Larai Zupa Monlo - Tampeii Settlement
            (510155u, 543.900, 472.300, 401.500, 543.900, 471.272, 401.500), // -1.03 snap Commander Merrick - Mal Dys
            (510156u, 540.200, 472.290, 401.500, 540.200, 471.580, 401.500), // -0.71 snap Fortuna Corman - Mal Dys
            (510147u, 382.400, 214.820, 108.700, 382.400, 216.139, 108.700), // +1.32 snap Recon Commander McReddy - Dia Toma
            (500101u, -15.300, 147.700, -12.700, -15.300, 144.524, -12.700), // -3.18 snap Fluxite Mines Field Medic
            (500102u, -12.600, 148.300, -14.700, -12.600, 144.754, -14.700), // -3.55 snap Fluxite Mines Field Medic
            (500104u, 194.300, 119.500, 41.700, 194.300, 116.400, 41.700), // -3.10 snap Tahrendra Base Field Medic
            (500105u, 196.700, 120.200, 45.500, 196.700, 116.924, 45.500), // -3.28 snap Tahrendra Base Field Medic
            (500243u, -190.500, 4.000, 143.300, -190.500, 1.271, 143.300), // -2.73 snap AFS Field Medic
            (500128u, 149.800, 213.600, -148.300, 149.800, 208.879, -148.300), // -4.72 snap Hospital: A.F.S. Outpost Lexington
            (500325u, 71.400, 366.100, -51.700, 71.400, 363.433, -51.700), // -2.67 snap Vendors
            (500319u, -32.600, 366.100, -397.500, -32.600, 363.324, -397.500), // -2.78 snap Vendors
            (500134u, 149.800, 213.600, -148.300, 149.800, 208.879, -148.300), // -4.72 snap Hospital: A.F.S. Outpost Lexington
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            foreach (var row in Placements)
                Move(migrationBuilder, "content_placement", row.Id, row.X, row.Y, row.Z);

            foreach (var row in Pools)
                Move(migrationBuilder, "spawnpool", row.Id, row.X, row.Y, row.Z);
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            foreach (var row in Placements)
                Move(migrationBuilder, "content_placement", row.Id, row.WasX, row.WasY, row.WasZ);

            foreach (var row in Pools)
                Move(migrationBuilder, "spawnpool", row.Id, row.WasX, row.WasY, row.WasZ);
        }

        private static void Move(MigrationBuilder migrationBuilder, string table, uint id, double x, double y, double z)
        {
            migrationBuilder.UpdateData(table: table, keyColumn: "id", keyValue: id, column: "pos_x", value: x);
            migrationBuilder.UpdateData(table: table, keyColumn: "id", keyValue: id, column: "pos_y", value: y);
            migrationBuilder.UpdateData(table: table, keyColumn: "id", keyValue: id, column: "pos_z", value: z);
        }
    }
}
