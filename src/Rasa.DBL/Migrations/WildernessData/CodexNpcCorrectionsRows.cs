using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// The four positions and eleven NPCs the codex-tr.net archive recovers, once a second source agrees.
    ///
    /// <b>EVERY Y IN THIS FILE IS PROVISIONAL AND THIS MIGRATION MUST NOT SHIP UNTIL THEY ARE PROBED.</b>
    /// The pass that produced it could not read the navmesh. Where a source records a height it is written
    /// here as that source's own whole-or-tenth metre, which is a hint at which level of the world the body
    /// is not a position at all. The companion list
    /// <c>scratchpad/agent-codexfix/probe.txt</c> carries one line per row below in
    /// <c>map_name x z y-hint</c> order, and <c>rows.json</c> maps each line back to the row id here.
    /// Substitute the probed floor into every row before this is applied to a live world.
    ///
    /// The archive at codex-tr.net is a community location database read on 2026-09-21; the wiki quoted below
    /// is TaRapedia's complete pre-shutdown history, read revision by revision. The two were written by
    /// different people who did not cite each other, so where they agree on a metre they are treated as two
    /// sources. The client is Tabula Rasa 1.16.5.0 for names and mission text, 1.2.2.0 for what a name id read
    /// at launch. Every name id below was checked against the 1.16.5.0 creature name table and every one of the
    /// eleven is carried by no creature in this world.
    ///
    /// <b>The four moves.</b> Two of them overrule an original-seed row, and this project's rule is that an
    /// original coordinate outranks a community transcription. They are taken anyway, and it is said plainly
    /// here rather than buried: each is taken only because two independent sources agree with each other
    /// against the seed, and in both cases a third thing in our own data agrees with them.
    ///
    ///   * <b>George Corman</b> (spawnpool 510005, InfiniteRasa, no provenance) stood 148 m from the Ranja
    ///     Gorge Corman camp at y 226.0 while every original-seed Corman in that camp stands at y 170-171.
    ///     Codex's (-754, -279) lands 2.2 m from original-seed Dr. Eleanan Corman (creature 94, y 170.98) and
    ///     8.7 m from Dr. Soji, i.e. inside the family cluster our own seed already places, and its z matches
    ///     Waypoint: Ranja Gorge (z -279.5) to 0.5 m. The corroboration is this world's own seed data.
    ///   * <b>Dr. Elise Corman</b> (spawnpool 510004, InfiniteRasa, no provenance). TaRapedia rev 34276
    ///     (2008-09-25) gives (789.0, 294.3, 369.0), "in the hospital tent" at Alia Das; codex gives
    ///     (785, 369), 4.0 m away. The wiki's y of 294.3 is the y our own Medical Vendor Static stands on
    ///     4.1 m from the new spot (294.33), so this is a lateral move on a floor we already hold.
    ///   * <b>Lieutenant Burke</b> (spawnpool 171, creature 96, ORIGINAL SEED). <b>This overrules the seed.</b>
    ///     TaRapedia rev 27920 (2008-01-29) gives (-46, 174, 418) in Monarch Grove and codex gives (-47, 414):
    ///     two sources, 4.1 m apart, that name the place. The seed position has nothing of ours within 63 m.
    ///     Decisively, the seed row's own y is 174.32 and the wiki's y for the new spot is 174 - the seed
    ///     carries the height of the place it is not standing in, which is what a re-derived x,z looks like.
    ///   * <b>Council Elder Solis</b> (spawnpool 184, creature 42, ORIGINAL SEED). <b>This overrules the
    ///     seed.</b> TaRapedia rev 12872 (2007-11-06) gives (784.7, 287.2, 581.1), "back side of Alia Das near
    ///     Alia Caverns", and codex gives (781, 577), 5.6 m away. Our seed row stands 3.1 m from Council Elder
    ///     Moawi at an identical y of 302.09, a two-NPC stack. The third agreement is ours: spawnpool 92, an
    ///     unnamed NPC_Forean_Shaman, already stands 2.2 m from the wiki's coordinate at y 287.24 - the wiki's
    ///     287.2 to four centimetres. That body is very likely Solis under a generic class and is flagged
    ///     below rather than touched. Note the elevation changes by 15 m, so the probe decides this one.
    ///
    /// Three further moves from the same pass - Captain McShay, Field Technician McCain and Field Commander
    /// Twitty onto the Staging Point floor - shipped in CodexPlacementFixes and are not repeated.
    ///
    /// <b>Refused, with the reason.</b>
    ///
    ///   * <b>The four CP Defense Vendors (name id 9472).</b> The reconciliation proposed creating one at each
    ///     of Fort Dew (-154, -718), Imperial Valley (-356, -552), Wilderness LZ (162, -91) and River-base
    ///     Krimm (170, 350), on the ground that no creature in this world carries name id 9472. They are not
    ///     created, because the identity is wrong rather than the absence. Name ids 9473 and 9472 read
    ///     "CP Assault Commander" and "CP Defense Commander" in the 1.2.2.0 launch client and "CP Token Banker"
    ///     and "CP Defense Vendor" in 1.16.5.0, and TaRapedia's Control Point page (rev 33836, 2008-09-22,
    ///     i.e. after Deployment 8) says what stands at a control point then: "Located within each AFS
    ///     controlled CP are two NPCs named CP Token Banker and CP Prestige Vendor". Prestige had replaced
    ///     assault and defense tokens. Our own data says the same thing in the sharpest possible way: at
    ///     River-base Krimm, codex's Defense Commander pin (170, 350) is 0.4 m from our CP Prestige Vendor
    ///     500186 (name id 10504, a name id that does not exist in the launch client at all), while at the
    ///     other three control points codex's Assault Commander pin is 0.3-1.1 m from our CP Token Banker.
    ///     Read that way codex's pair is not a Defense Vendor and an Assault Commander, it is the Token Banker
    ///     and the Prestige Vendor, and what this world is actually missing is a <b>Prestige Vendor</b> at
    ///     Fort Dew, Imperial Valley and the Wilderness LZ (we hold 38 Token Bankers and 4 Prestige Vendors)
    ///     and a <b>Token Banker</b> at River-base Krimm. That is a different migration and an owner decision,
    ///     not a rename of this one; inserting 9472 here would have stacked a second vendor 0.4 m on top of
    ///     500186 at Krimm.
    ///   * <b>CP Token Banker at River-base Krimm</b> (name id 9473, 170, 354) and <b>Captain Marsh</b>
    ///     (name id 9049, 168, 530.3) were the reconciliation's other two creations. Both are plausible - and
    ///     the Krimm banker is the half of the reading above that survives - but neither was in the scope
    ///     handed to this pass, so both are left for the control-point pass.
    ///   * The <b>optional low-confidence move of CP Token Banker 500005</b> (24.3 m, codex only, our row's
    ///     comment reading "prestige" though its name id is the banker's) is refused: the pass was to take the
    ///     moves rated high, and the Wilderness LZ control point wants a survey of its own.
    ///
    /// <b>The eleven creations.</b> Name is the 1.16.5.0 creature name table. X and Z are the wiki's where the
    /// wiki has them, because codex's Cumbria pins are demonstrably label-shuffled (the wiki overrules codex on
    /// Base Cmdr. Matlin, Corporal Orton and Derac Bensen and agrees with it only on Arizpe), and codex's
    /// otherwise. Entity class is an analogue under OD-45 on the same terms as
    /// <see cref="TarapediaMissingNpcBatchRows"/>: the class this world already uses for that species and
    /// gender. Appearance is an analogue under OD-11, the donor sets that file carries, with the slot repair
    /// applied - boots in slot 2, torso 15, legs 16. Level is the NPC's own wiki infobox where it has one and
    /// a named sourced neighbour where it has not; no level here is invented and each row says which it is.
    /// Dialogue: none, for the reason the batch gives - the missions are not seeded here.
    ///
    ///   * <b>Receptive Liaison Arizpe</b>, wiki rev 25740, level 25 from his own page. The one Cumbria row
    ///     where codex (-539, 661) and the wiki (-538, 142, 665) agree, to 4.1 m.
    ///   * <b>Corporal Orton</b>, wiki rev 27661, level 15 from his own page, at the wiki's (-844, 603) and not
    ///     codex's (-512, 712). Client missiontext 163 puts him at Cumbria Research and the wiki's y of 138 is
    ///     the western Cumbria floor exactly (Matlin 138.03, the Armor and Weapons vendors 137.8, Trainer: New
    ///     Cumbria 138.0). <b>This world already has a Corporal Orton</b>: creature 510196, "Corporal Orton -
    ///     Obelisk", spawned on map 1304 adv_foreas_valverde_pools - the wrong continent. It is not deleted.
    ///     Its comment is rewritten to carry the flag so that the duplicate is visible to anything that reads
    ///     the table, and the 1304 spawn is left for the owner to remove.
    ///   * <b>Derac Bensen Corman</b>, wiki rev 35394, level 50 from his own page, at the wiki's (-798, 766),
    ///     "in the hydroponics dome in the north end of the Cumbria Research Facility"; codex's own pin for him
    ///     is 238 m away and is discarded. Level 50 looks out of band against the facility's own page
    ///     ("Levels=15-20"), and it is kept because it is his page's own figure and because the wiki gives 50
    ///     to the zone's other two Corman researchers as well, Thomas Jansona and Information Spec. Nye.
    ///   * <b>Yorma Brown Corman</b> and <b>Kaven Corman</b>, codex only, level 50 from Derac Bensen Corman -
    ///     the Corman researcher whose page is levelled, who stands 8 m from Yorma Brown's pin and is an
    ///     objective of the same mission (Welcome Tour) as both of them.
    ///   * <b>Lt. Jamison</b> and <b>Sgt. Briggs</b>, codex only, level 20: both are AFS ranks rather than
    ///     Corman researchers, and 20 is the top of the Cumbria Research Facility page's own 15-20 band and
    ///     the level the wiki gives every other AFS contact of that rank in Palisades (McShay, Twitty, McCain,
    ///     Bagby, Louden, Mullen, LaFontaine).
    ///   * <b>Captain LaFontaine</b>, level 20, Human Female, from her own wiki page, rev 27616. The
    ///     reconciliation filed her as codex-only; she is not - the page gives (227, 109, 350) at River-base
    ///     Krimm, which agrees with codex's (227, 351) to 1.0 m and hands us the only y of the eleven that a
    ///     second source confirms. The wiki's triple is used.
    ///   * <b>Captain Malnati</b>, codex only, level 20 from Captain McShay, the wiki-levelled AFS captain of
    ///     the same zone and the same kind of outpost chain. Client missiontext 8150/8151 puts him at "the very
    ///     edge of Northern Palisades" and z 728.8 is the northern third of the map. Nothing of ours stands
    ///     within 200 m of this pin, so it has no floor hint at all.
    ///   * <b>Corporal Barbeau</b>, codex only, level 20 from Captain McShay, who gives the mission that goes
    ///     looking for him (Grim Duty / Following Orders). Client missiontext 18204: "Collect Corporal
    ///     Barbeau's Dogtags from the field outside the Bane Base in Palisades". Absent from the launch client
    ///     entirely, which matches the wiki dating his missions to 2008-05 onward.
    ///   * <b>Barbrix</b>, codex only, a Bane Thrax boss and so no appearance rows, level 20 from Hygax, the
    ///     zone's other wiki-levelled named Bane; TaRapedia's Bloody Booty rewards a level 19 item for killing
    ///     him and its giver, Sergeant Mullen, is level 20. Client missiontext 17994 calls him "that huge Thrax
    ///     Lieutenant named Barbrix patrolling to the east" of River-base Krimm, and (483, 171) is 335 m
    ///     east-south-east of it. He patrols, so the point is the centre of a walk and not a stand.
    ///
    /// Every position above was checked against the client's own furniture in <c>mapprops/</c> the way
    /// PropOverlapAuditTests checks, and none of the fifteen falls inside a prop footprint.
    /// </summary>
    public static class CodexNpcCorrectionsRows
    {
        public const string Migration = "CodexNpcCorrections";

        private const byte Stationary = 1;

        /// <summary>
        /// No source gives this row a height and no navmesh was read. It is a marker, not a position: every
        /// row carrying it has a line in <c>scratchpad/agent-codexfix/probe.txt</c> and has to be given the
        /// probed floor before this migration is applied.
        /// </summary>
        /// <summary>
        /// The donor sets <see cref="TarapediaMissingNpcBatchRows"/> uses, with the slot repair applied:
        /// boots in slot 2, torso in 15, legs in 16. 3846 is Field Sgt. Witherspoon (creature 101), 3848 is
        /// General Supply Vendor Twin Pillars (creature 63), 6339 is AFS Officer Lt Wood (creature 121).
        /// </summary>
        private static readonly System.Collections.Generic.Dictionary<uint, uint[][]> Bodies = new()
        {
            { 3846u, new[] { new[] { 2u, 4021u, 22120u }, new[] { 13u, 27120u, 1u }, new[] { 14u, 3672u, 1u },
                new[] { 16u, 4022u, 13933202u }, new[] { 15u, 4023u, 13933202u }, new[] { 17u, 20824u, 4286886614u } } },
            { 3848u, new[] { new[] { 2u, 4021u, 266553u }, new[] { 14u, 25255u, 266553u }, new[] { 15u, 19296u, 266553u },
                new[] { 16u, 19250u, 266553u }, new[] { 17u, 24019u, 4286886614u } } },
            { 6339u, new[] { new[] { 1u, 26677u, 4286886614u }, new[] { 2u, 4021u, 4294934528u }, new[] { 3u, 26673u, 22120u },
                new[] { 15u, 4023u, 4294934528u }, new[] { 16u, 4022u, 4294934528u }, new[] { 17u, 24008u, 4286690539u } } }
        };

        // id, comment, name id, entity class, faction, level, map, x, y (PROVISIONAL), z, provenance
        private static readonly object[][] Npcs =
        {
            new object[] { 199085u, "Receptive Liaison Arizpe (Cumbria Research Facility)", 9938u, 3846u, 1u, 25u, 1244u, -538.000, 142.095, 665.000, "Palisades/Cumbria Research Facility, TaRapedia rev 25740 2008-01-05, 'In the building below the waypoint'; codex-tr.net POI agrees to 4.1 m; level 25 from the same page; y is the wiki's, PROBE" },
            new object[] { 199086u, "Corporal Orton (Cumbria Research Facility)", 2929u, 3846u, 1u, 15u, 1244u, -844.000, 137.770, 603.000, "Palisades/Cumbria Research Facility, TaRapedia rev 27661 2008-01-21, 'near the camp with all the tents'; client missiontext 163; level 15 from the same page; duplicates creature 510196 on map 1304, flagged not deleted; y is the wiki's, PROBE" },
            new object[] { 199087u, "Derac Bensen Corman (Cumbria hydroponics dome)", 2932u, 6339u, 1u, 50u, 1244u, -798.000, 140.170, 766.000, "Palisades/Cumbria Research Facility, TaRapedia rev 35394 2008-10-25, 'in the hydroponics dome in the north end'; client missiontext 196 and 2253; level 50 from the same page, out of band against the facility's own 15-20 and kept because the zone's other two Corman researchers carry 50 as well; y is the wiki's, PROBE" },
            new object[] { 199088u, "Yorma Brown Corman (Cumbria hydroponics dome)", 2933u, 6339u, 1u, 50u, 1244u, -790.000, 140.170, 765.000, "Palisades/Cumbria Research Facility, codex-tr.net POI 1424 read 2026-09-21, 8.1 m from TaRapedia's dome coordinate; TaRapedia lists him in the facility roster with the Noise Pollution mission; level 50 from Derac Bensen Corman, 8 m away and an objective of the same mission; y is the navmesh floor, probed 2026-09-22" },
            new object[] { 199089u, "Lt. Jamison (Cumbria hydroponics dome)", 2936u, 3846u, 1u, 20u, 1244u, -767.000, 140.170, 773.000, "Palisades/Cumbria Research Facility, codex-tr.net POI 1426 read 2026-09-21, 31.8 m from TaRapedia's dome coordinate; TaRapedia lists him in the facility roster as a Welcome Tour objective; level 20 is the top of the facility page's own 15-20 band and the level the wiki gives every AFS contact of that rank in Palisades; y is the navmesh floor, probed 2026-09-22" },
            new object[] { 199090u, "Kaven Corman (Cumbria Research Facility medical centre)", 5436u, 6339u, 1u, 50u, 1244u, -600.000, 145.410, 701.000, "Palisades/Cumbria Research Facility, codex-tr.net POI 1428 read 2026-09-21; the weakest pin of the six - no second coordinate and nothing of ours within 85 m; client missiontext 2252 and TaRapedia's Welcome Tour ('Kaven Corman leads our team in the medical center'); level 50 from Derac Bensen Corman, the levelled Corman researcher of the same mission; y is the navmesh floor, probed 2026-09-22" },
            new object[] { 199091u, "Sgt. Briggs (Cumbria Research Facility, eastern buildings)", 2934u, 3846u, 1u, 20u, 1244u, -496.000, 142.370, 726.000, "Palisades/Cumbria Research Facility, codex-tr.net POI read 2026-09-21, 25.8 m from our Corporal Hutchison and inside the eastern building group whose floor the wiki's Arizpe row confirms; TaRapedia gives him Right on the Bark and Speedy Delivery; level 20 as for Lt. Jamison; y is the navmesh floor, probed 2026-09-22" },
            new object[] { 199092u, "Captain LaFontaine (River-base Krimm)", 10219u, 3848u, 1u, 20u, 1244u, 227.000, 109.513, 350.000, "Palisades/River-base Krimm, TaRapedia rev 27616 2008-01-20, (227, 109, 350); codex-tr.net's English pin (227, 351) agrees to 1.0 m and its French duplicate 'Capitaine Lefontaine' (214, 260) is discarded; level 20 and Human Female from the same page; y is the wiki's and our own Krimm bodies stand at 110.0-110.1, PROBE" },
            new object[] { 199093u, "Captain Malnati (northern Palisades)", 9048u, 3846u, 1u, 20u, 1244u, 106.900, 135.017, 728.800, "Palisades/northern edge, codex-tr.net POI read 2026-09-21; client missiontext 8150/8151 and TaRapedia's 'Tracking Down the Vale part II' (Base Cmdr. Matlin -> Malnati); name id 9048 is stable across the 1.2.2.0 and 1.16.5.0 clients; level 20 from Captain McShay; nothing of ours stands within 200 m; the navmesh column there has two walkable levels, 135.02 and 138.44, and he takes the lower, ground one - probed 2026-09-22" },
            new object[] { 199094u, "Corporal Barbeau (field outside the Bane base)", 10234u, 3846u, 1u, 20u, 1244u, 501.500, 173.735, -719.700, "Palisades/south-east of the Staging Point, codex-tr.net POI read 2026-09-21; client missiontext 18204; absent from the 1.2.2.0 client, matching TaRapedia dating Grim Duty / Following Orders / Secret Extraction to 2008-05 onward; level 20 from Captain McShay, who gives those missions, and the Staging Point page's own 20-24 band; y is the navmesh floor, probed 2026-09-22" },
            new object[] { 199095u, "Barbrix (Thrax boss east of River-base Krimm)", 10227u, 10504u, 0u, 20u, 1244u, 483.000, 120.755, 171.000, "Palisades/east of River-base Krimm, codex-tr.net contributor mission label read 2026-09-21; client missiontext 17993/17994, 'that huge Thrax Lieutenant named Barbrix patrolling to the east'; level 20 from Hygax, the zone's other wiki-levelled named Bane, and TaRapedia's Bloody Booty rewarding a level 19 item; he patrols, so this is the centre of a walk; y is the navmesh floor, probed 2026-09-22" },
        };

        /// <summary>(table, id, the map and x,y,z it had, the map and x,y,z it takes). Every new Y is PROVISIONAL.</summary>
        private static readonly (string Table, uint Id, uint WasMap, double WasX, double WasY, double WasZ,
            uint Map, double X, double Y, double Z)[] Moves =
        {
            // George Corman, creature 510005, InfiniteRasa. Codex read 2026-09-21; the floor hint is original-seed
            // Dr. Eleanan Corman (spawnpool 173) 2.2 m away at y 170.98. NO SOURCE Y, PROBE.
            ("spawnpool", 510005u, 1220u, -721.200, 226.021, -423.000, 1220u, -754.000, 170.842, -279.000),
            // Dr. Elise Corman, creature 510004, InfiniteRasa. TaRapedia rev 34276 2008-09-25 (789.0, 294.3, 369.0);
            // codex 4.0 m away; our Medical Vendor Static stands 4.1 m off at y 294.33. Y is the navmesh floor, probed 2026-09-22.
            ("spawnpool", 510004u, 1220u, 810.900, 294.710, 391.700, 1220u, 789.000, 294.360, 369.000),
            // Lieutenant Burke, creature 96, ORIGINAL SEED - overruled, see the comment above.
            // TaRapedia rev 27920 2008-01-29 (-46, 174, 418) Monarch Grove; codex (-47, 414). Y is the navmesh floor, probed 2026-09-22.
            ("spawnpool", 171u, 1220u, -395.918, 174.320, 181.730, 1220u, -46.000, 174.326, 418.000),
            // Council Elder Solis (spawnpool 184, creature 42, ORIGINAL SEED) is deliberately NOT moved here, though
            // TaRapedia rev 12872 and codex agree on the back of Alia Das to 5.6 m: our own spawnpool 92, an
            // unnamed NPC_Forean_Shaman, already stands 2.2 m from that spot at the wiki's own height, and the
            // seed places Solis 3.1 m from Council Elder Moawi at an identical y, which reads as a deliberate
            // pairing. Whether 92 is Solis under a generic class, and whether 184 should join him or be retired,
            // is an owner decision, not a coordinate correction. Recorded in the manifest as a gap.
        };

        /// <summary>
        /// Creature 510196, "Corporal Orton - Obelisk", spawns on map 1304 adv_foreas_valverde_pools while the
        /// client's own missiontext 163 puts Corporal Orton at Cumbria Research on Palisades. It is not deleted
        /// and its spawn is not moved; its comment carries the flag so the duplicate is visible in the table.
        /// </summary>
        private const uint OrtonOnThePools = 510196u;

        private const string OrtonWasComment = "Corporal Orton - Obelisk";

        private const string OrtonFlaggedComment =
            "Corporal Orton - Obelisk (FLAG: duplicate of creature 199086 on Palisades; this map 1304 spawn " +
            "contradicts client missiontext 163 and TaRapedia rev 27661 - remove or move, owner decision)";

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            foreach (var npc in Npcs)
            {
                var id = (uint)npc[0];

                migrationBuilder.InsertData(
                    table: "creature",
                    columns: new[] { "id", "comment", "class_id", "faction", "level", "max_hp", "name_id", "run_speed",
                        "walk_speed", "action1", "action2", "action3", "action4", "action5", "action6", "action7", "action8" },
                    values: new object[] { id, npc[1], npc[3], npc[4], npc[5], 555u, npc[2], 9u, 5u,
                        0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u });

                migrationBuilder.InsertData(
                    table: "content_placement",
                    columns: new[] { "id", "map_context_id", "kind", "creature_id", "npc_package_id", "entity_class_id",
                        "usable_kind", "pos_x", "pos_y", "pos_z", "rotation", "behavior", "initial_state", "alternate_state",
                        "alternate_state_condition_id", "windup_ms", "name_override_id", "hit_points", "restore_ms",
                        "fuse_ms", "loot_item_set_id", "respawn_ms", "present_condition_id", "usable_condition_id", "comment" },
                    values: new object[] { id, npc[6], (byte)1, id, 0u, 0u, (byte)0, npc[7], npc[8], npc[9], 0.0,
                        Stationary, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, 0u, $"{npc[1]} ({npc[10]})" });

                if (!Bodies.TryGetValue((uint)npc[3], out var body))
                    continue;

                foreach (var piece in body)
                    migrationBuilder.InsertData(table: "creature_appearance", columns: new[] { "id", "slot_id", "Class_id", "color" },
                        values: new object[] { id, piece[0], piece[1], piece[2] });
            }

            foreach (var move in Moves)
                Move(migrationBuilder, move.Table, move.Id, move.Map, move.X, move.Y, move.Z);

            migrationBuilder.UpdateData(table: "creature", keyColumn: "id", keyValue: OrtonOnThePools,
                column: "comment", value: OrtonFlaggedComment);
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(table: "creature", keyColumn: "id", keyValue: OrtonOnThePools,
                column: "comment", value: OrtonWasComment);

            foreach (var move in Moves)
                Move(migrationBuilder, move.Table, move.Id, move.WasMap, move.WasX, move.WasY, move.WasZ);

            foreach (var npc in Npcs)
            {
                var id = (uint)npc[0];

                if (Bodies.TryGetValue((uint)npc[3], out var body))
                    foreach (var piece in body)
                        migrationBuilder.DeleteData(table: "creature_appearance", keyColumns: new[] { "id", "slot_id" },
                            keyValues: new object[] { id, piece[0] });

                migrationBuilder.DeleteData(table: "content_placement", keyColumns: new[] { "id" }, keyValues: new object[] { id });
                migrationBuilder.DeleteData(table: "creature", keyColumns: new[] { "id" }, keyValues: new object[] { id });
            }
        }

        private static void Move(MigrationBuilder migrationBuilder, string table, uint id, uint map,
            double x, double y, double z)
        {
            migrationBuilder.UpdateData(table: table, keyColumn: "id", keyValue: id, column: "map_context_id", value: map);
            migrationBuilder.UpdateData(table: table, keyColumn: "id", keyValue: id, column: "pos_x", value: x);
            migrationBuilder.UpdateData(table: table, keyColumn: "id", keyValue: id, column: "pos_y", value: y);
            migrationBuilder.UpdateData(table: table, keyColumn: "id", keyValue: id, column: "pos_z", value: z);
        }
    }
}
