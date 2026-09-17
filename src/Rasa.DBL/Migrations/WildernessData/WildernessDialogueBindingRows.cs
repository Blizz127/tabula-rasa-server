using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// Twelve Wilderness NPCs that were already standing in the world with nothing to say, given the dialogue
    /// package the client's own table says they speak. Fourteen objectives over nine missions stop being dead
    /// ends.
    ///
    /// The mission-link audit of 2026-09-17 named 34 objectives whose completion package no spawned creature
    /// carried. The first assumption was that those NPCs did not exist and would have to be built by the OD-45
    /// pipeline, as Mining Coord. Richards was. That was wrong: <b>every one of these twelve is already in the
    /// world seed</b>, with its client name, class, level and health, drawn by a spawnpool slot in the right
    /// place - it simply has no <c>npc_package</c> row, so the server has no conversation to offer from it and
    /// the objective can never complete. The fix is one row each, and it invents no position, no appearance and
    /// no level.
    ///
    /// Each binding is the client's own <c>objectiveconversation</c> row read back to the NPC the client's own
    /// mission text names:
    ///
    /// <list type="table">
    /// <item><term>210 -> 116</term><description>Information Spec. Saviours. 421 "A Father's Goodbye" sends the
    ///   dogtags to Lt. Saviours' son "on the far side of the Lower Eloh Creek bridge"; the only other Saviours
    ///   the client names is 3074, and the seed's creature 116 carries exactly that name.</description></item>
    /// <item><term>254 -> 103</term><description>Arms Supplier Oliver. 427 "Lurking In The Shadows": "Locate Arms
    ///   Supplier Oliver to find out what's delaying the shipment."</description></item>
    /// <item><term>251 -> 138</term><description>Surveyor Hugh Corman. 431 "Distress On The River": "see if you
    ///   can find one of them, a Corman surveyor named Hugh".</description></item>
    /// <item><term>252 -> 139</term><description>Tribal Leader Oingin. The second of 431's three field
    ///   coordinators: the line is a Forean's ("Do not worry about us, human. My people are fearless warriors")
    ///   and it points at the third, Lt. Wood. The client's name table runs 3093 Hugh Corman, 3094 Oingin, 3095
    ///   Wood against packages 251, 252, 253, with the outer two confirmed by mission text.</description></item>
    /// <item><term>253 -> 121</term><description>Lt. Wood, named by the package-252 line: "Lieutenant Wood? I hear
    ///   that he has lost many men. You'll find him on the far side of Eloh Creek."</description></item>
    /// <item><term>218 -> 125</term><description>Medical Assistant Duncan. The package completes objectives in four
    ///   missions, two of which name him outright: 441 "inform Medical Assistant Duncan at the Infirmary in Twin
    ///   Pillars" and 700 "Medical Assistant Duncan at the Twin Pillars infirmary".</description></item>
    /// <item><term>117 -> 94</term><description>Dr. Eleanor Corman at Ranja Gorge. 444 "Take the test results to
    ///   Dr. Eleanor at Ranja"; her line answers "Victor sent you? Do you have the test results?" The same package
    ///   completes 698 "Elixir Vitae", which delivers the Tinctu essence to "the Corman doctor ... in Ranja
    ///   Gorge".</description></item>
    /// <item><term>566 -> 92</term><description>Council Luminary Doyan. 451 "Seek Council Luminary Doyan"; his line
    ///   is "my oldest boy, my Pundi, is lost to me". The same package completes 682's turn-in, "What news of my
    ///   son?"</description></item>
    /// <item><term>567 -> 91</term><description>Council Advisor Todae, named by Doyan's own line: "Speak to Todae,
    ///   my advisor, if you must".</description></item>
    /// <item><term>382 -> 115</term><description>Engineer Salter. 549 "Get a new catalyzer from Engineer Salter at
    ///   Alia Das", and the same package completes 623, "Engineer Salter at Alia Das". The seed has two Salters;
    ///   115 is the one whose spawnpool stands at Alia Das (764.0, 293.98, 409.0), 117 is not.</description></item>
    /// <item><term>102 -> 98</term><description>Ranger Anjuhi. 682 sends the player to "the Rangers of the Anvil ...
    ///   at the foot of Stone Anvil"; the package's second line ends "speak to Ranger Tirna", so the speaker is the
    ///   other Ranger there. The seed has exactly two named Rangers at Stone Anvil - Anjuhi (98) and Tirna (99) -
    ///   and Tirna is the one package 570 is. By elimination among the seed's own roster.</description></item>
    /// <item><term>570 -> 99</term><description>Ranger Tirna, who says it himself: "Yes, I was once called Pundi."
    ///   The seed's creature carries name 135, which the client renders <b>"Ranger Tarina"</b> while every mission
    ///   text says "Ranger Tirna" (the client's own 6711). The name is left as the seed has it rather than
    ///   corrected on a guess: GAP-W3-TIRNA-NAME.</description></item>
    /// </list>
    ///
    /// Still unbound in the Wilderness after this: <b>451/3 package 569</b>, the villager who directs the player to
    /// the Urn ("This is the Village of Dagdha's Urn ... reached through the caverns"), whose client name is not
    /// established - the seed's village roster holds Council Elders, Apprentice Juvak and Elder Gadfly, and nothing
    /// chooses between them - and <b>442/2 package 1486</b>, which is not an NPC at all but the blood analyzer at
    /// Twin Pillars ("ANALYZING... ANALYZATION COMPLETE"), a usable the content layer would have to dispense.
    /// Both stay in GAP-W3-UNBOUND-CONVERSATION-PACKAGE.
    /// </summary>
    public static class WildernessDialogueBindingRows
    {
        public const string Migration = "WildernessDialogueBinding";

        /// <summary>(world-seed creature, the client conversation package it carries, who it is).</summary>
        public static readonly (uint Creature, uint Package, string Who)[] Rows =
        {
            (91u, 567u, "Council Advisor Todae (client name 6708)"),
            (92u, 566u, "Council Luminary Doyan (client name 6707)"),
            (94u, 117u, "Dr. Eleanor Corman at Ranja Gorge (client name 2975)"),
            (98u, 102u, "Ranger Anjuhi at Stone Anvil (client name 201)"),
            (99u, 570u, "Ranger Tirna at Stone Anvil (seed name 135, GAP-W3-TIRNA-NAME)"),
            (103u, 254u, "Arms Supplier Oliver (client name 3096)"),
            (115u, 382u, "Engineer Salter at Alia Das (client name 4525)"),
            (116u, 210u, "Information Spec. Saviours (client name 3074)"),
            (121u, 253u, "Lt. Wood (client name 3095)"),
            (125u, 218u, "Medical Assistant Duncan (client name 3082)"),
            (138u, 251u, "Surveyor Hugh Corman (client name 3093)"),
            (139u, 252u, "Tribal Leader Oingin (client name 3094)")
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            var values = new object[Rows.Length, 3];
            for (var i = 0; i < Rows.Length; i++)
            {
                values[i, 0] = Rows[i].Creature;
                values[i, 1] = Rows[i].Package;
                values[i, 2] = Rows[i].Who;
            }

            migrationBuilder.InsertData(table: "npc_package", columns: new[] { "id", "package_id", "comment" },
                values: values);
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            foreach (var row in Rows)
                migrationBuilder.DeleteData(table: "npc_package", keyColumn: "id", keyValue: row.Creature);
        }
    }
}
