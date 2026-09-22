using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// 421 outfit rows filed under the wrong slot, on 159 NPCs, every one of them standing somewhere a player
    /// can reach - including all nine humans in the boot camp.
    ///
    /// The client's <c>equipmentdata.equipableClassEquipmentSlot</c> gives each of 6,935 equipable classes
    /// exactly one slot, and <c>creature_appearance.slot_id</c> is supposed to be that slot. Three classes were
    /// systematically filed wrong:
    ///
    /// <code>
    ///   4021  NPC_Clothing_Officer_1_Boots  -> 2 SHOES   : 104 rows sat in 3 GLOVES
    ///   4022  NPC_Clothing_Officer_1_Legs   -> 16 LEGS   : 157 rows sat in 15 TORSO
    ///   4023  NPC_Clothing_Officer_1_Torso  -> 15 TORSO  : 157 rows sat in 16 LEGS
    /// </code>
    ///
    /// Our own data corroborates the client table by a large majority before any of this is applied: 4022 is
    /// already under LEGS in 432 rows against TORSO in 157, 4023 under TORSO in 432 against LEGS in 157, and
    /// 4021 under SHOES in 525 against GLOVES in 104.
    ///
    /// <b>Why the key matters.</b> Mesh placement survives a wrong slot, because the client's
    /// <c>_ProcessSwapsets</c> re-derives the slot from that same table. Four other things do not:
    /// <c>Actor._GetSkinColor</c> reads the FACE key directly, <c>GetWeaponClassIdFromAppearanceData</c> reads
    /// WEAPON and then HANDTOHAND, <c>_ProcessAppearanceData</c> decides by key whether to run
    /// <c>_AddAccessorySFX</c>, and <c>Recv_AppearanceData</c> diffs an outfit change by key.
    ///
    /// <b>Where it came from.</b> Five creatures in the shipped seed carry the defect (8, 23, 67, 100, 101), and
    /// the other 154 are ours: every one is a copy of creature 100 (Outpost Commander Rogers, 84 NPCs), 101
    /// (Field Sgt. Witherspoon, 18) or 67 (Prestige Supply Vendor Twin Pillars, 52), taken as the analogue body
    /// under OD-11 and copied forward batch after batch. The donor literals are corrected at the same time as
    /// this migration, and <c>AppearanceSlotAuditTests</c> now checks every row in the world against the client's
    /// table so the next batch cannot reintroduce it. The officer set has no gloves at all - the client calls
    /// class 4024 <c>Z_DO_NOT_USE_NPC_Clothing_Officer_1_Gloves</c> - so slot 3 is left empty on all 104 rather
    /// than refilled.
    ///
    /// Two single rows come with it. <b>Creature 23</b> "Test Vendor 3" wears class 19242, which exists in no
    /// client table; it is row-for-row identical to six shipped vendors (61-66) that carry 4021 at the same hue
    /// in the same slot, and 19242 sits two ids past a legs entry in a family that allocates every fifth id, so
    /// it is a typo for the boots and is replaced by them. <b>Creature 8</b> "Hominis Machina" carries a human
    /// face swap under WING and again under EYEWEAR; the WING row is rekeyed to FACE and the duplicate dropped.
    /// Neither row can actually render - <c>equipableClassMeshSwapset</c> is keyed (class, mesh) and holds only
    /// the six human and Thrax avatar bodies, not this creature's mesh 15868 - so this is tidying an upstream
    /// mistake, not restoring a face.
    /// </summary>
    public static class AppearanceSlotRepairRows
    {
        public const string Migration = "AppearanceSlotRepair";

        public const uint Boots = 4021u, Legs = 4022u, Torso = 4023u;
        public const uint SlotShoes = 2u, SlotGloves = 3u, SlotTorso = 15u, SlotLegs = 16u, SlotFace = 17u,
            SlotWing = 18u, SlotEyewear = 19u;

        /// <summary>A slot no outfit uses, to hold the legs while the torso moves out of 16.</summary>
        private const uint Parking = 1015u;

        /// <summary>
        /// The creatures the repair touches, listed rather than derived, so the rollback puts back exactly what
        /// was moved. A rule of "every 4021 in slot 3" is right going forward and wrong coming back: after the
        /// move there are 629 rows of 4021 in slot 2, and only these 104 of them belong in 3.
        /// </summary>
        private static readonly uint[] BootsInTheGloveSlot =
        {
            100u, 101u, 198500u, 198501u, 198502u, 198504u, 198505u, 198508u, 198509u, 198514u, 198515u, 199000u,
            199003u, 199004u, 199005u, 199006u, 199007u, 199008u, 199009u, 199010u, 199011u, 199012u, 199013u,
            199014u, 199015u, 199016u, 199018u, 199020u, 199021u, 199023u, 199024u, 199025u, 199027u, 199029u,
            199030u, 199038u, 199039u, 199040u, 199049u, 199051u, 199055u, 199064u, 199067u, 199068u, 199069u,
            199072u, 199073u, 199100u, 199101u, 199102u, 199103u, 199104u, 199105u, 199106u, 199107u, 199108u,
            199109u, 199110u, 199111u, 199200u, 199201u, 199202u, 199203u, 199204u, 199205u, 199206u, 199207u,
            199208u, 199209u, 199210u, 199300u, 199301u, 199302u, 199303u, 199305u, 199400u, 199401u, 199402u,
            199403u, 199404u, 199405u, 199406u, 199408u, 199409u, 199410u, 199411u, 199412u, 199413u, 199500u,
            199501u, 199502u, 199503u, 199504u, 199505u, 199507u, 199509u, 199510u, 199600u, 199601u, 199602u,
            199603u, 199800u, 199802u, 199803u
        };

        /// <summary>The creatures whose officer torso and legs are keyed to each other's slots.</summary>
        private static readonly uint[] TorsoAndLegsSwapped =
        {
            67u, 100u, 101u, 198500u, 198501u, 198502u, 198504u, 198505u, 198508u, 198509u, 198514u, 198515u,
            199000u, 199003u, 199004u, 199005u, 199006u, 199007u, 199008u, 199009u, 199010u, 199011u, 199012u,
            199013u, 199014u, 199015u, 199016u, 199018u, 199020u, 199021u, 199023u, 199024u, 199025u, 199027u,
            199029u, 199030u, 199038u, 199039u, 199040u, 199049u, 199051u, 199055u, 199064u, 199067u, 199068u,
            199069u, 199072u, 199073u, 199100u, 199101u, 199102u, 199103u, 199104u, 199105u, 199106u, 199107u,
            199108u, 199109u, 199110u, 199111u, 199200u, 199201u, 199202u, 199203u, 199204u, 199205u, 199206u,
            199207u, 199208u, 199209u, 199210u, 199300u, 199301u, 199302u, 199303u, 199305u, 199400u, 199401u,
            199402u, 199403u, 199404u, 199405u, 199406u, 199408u, 199409u, 199410u, 199411u, 199412u, 199413u,
            199500u, 199501u, 199502u, 199503u, 199504u, 199505u, 199507u, 199509u, 199510u, 199600u, 199601u,
            199602u, 199603u, 199800u, 199802u, 199803u, 500003u, 500005u, 500015u, 500016u, 500028u, 500029u,
            500052u, 500053u, 500054u, 500055u, 500072u, 500073u, 500081u, 500082u, 500098u, 500099u, 500100u,
            500117u, 500118u, 500119u, 500156u, 500157u, 500184u, 500186u, 500212u, 500213u, 500214u, 500236u,
            500237u, 500238u, 500239u, 500254u, 500255u, 500279u, 500280u, 500281u, 500282u, 500286u, 500304u,
            500305u, 500306u, 500308u, 510011u, 510016u, 510030u, 510057u, 510069u, 510082u, 510096u, 510098u,
            510150u, 510202u
        };

        public const uint HominisMachina = 8u, TestVendor = 23u, FaceSwap = 7508u, UnknownBoots = 19242u;

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            // The two halves of the officer suit swap keys, so the legs park briefly: the table has no unique
            // key on (id, slot_id), and moving the legs onto 16 while the torso still sits there would leave
            // two rows in one slot rather than failing.
            Move(migrationBuilder, TorsoAndLegsSwapped, Legs, SlotTorso, Parking);
            Move(migrationBuilder, TorsoAndLegsSwapped, Torso, SlotLegs, SlotTorso);
            Move(migrationBuilder, TorsoAndLegsSwapped, Legs, Parking, SlotLegs);
            Move(migrationBuilder, BootsInTheGloveSlot, Boots, SlotGloves, SlotShoes);

            migrationBuilder.Sql($"delete from creature_appearance where id = {HominisMachina} and slot_id = {SlotEyewear} and Class_id = {FaceSwap};");
            migrationBuilder.Sql($"update creature_appearance set slot_id = {SlotFace} where id = {HominisMachina} and slot_id = {SlotWing} and Class_id = {FaceSwap};");
            migrationBuilder.Sql($"update creature_appearance set Class_id = {Boots} where id = {TestVendor} and slot_id = {SlotShoes} and Class_id = {UnknownBoots};");
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"update creature_appearance set Class_id = {UnknownBoots} where id = {TestVendor} and slot_id = {SlotShoes} and Class_id = {Boots};");
            migrationBuilder.Sql($"update creature_appearance set slot_id = {SlotWing} where id = {HominisMachina} and slot_id = {SlotFace} and Class_id = {FaceSwap};");
            migrationBuilder.InsertData(table: "creature_appearance", columns: new[] { "id", "slot_id", "Class_id", "color" },
                values: new object[] { HominisMachina, SlotEyewear, FaceSwap, 1111u });

            Move(migrationBuilder, BootsInTheGloveSlot, Boots, SlotShoes, SlotGloves);
            Move(migrationBuilder, TorsoAndLegsSwapped, Legs, SlotLegs, Parking);
            Move(migrationBuilder, TorsoAndLegsSwapped, Torso, SlotTorso, SlotLegs);
            Move(migrationBuilder, TorsoAndLegsSwapped, Legs, Parking, SlotTorso);
        }

        private static void Move(MigrationBuilder migrationBuilder, uint[] creatures, uint entityClass, uint from, uint to)
            => migrationBuilder.Sql($"update creature_appearance set slot_id = {to} where Class_id = {entityClass} " +
                $"and slot_id = {from} and id in ({string.Join(", ", creatures)});");
    }
}
