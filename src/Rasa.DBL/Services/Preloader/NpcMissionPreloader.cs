using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Services.Preloader
{

    using Structures.World;

    public class NpcMissionPreloader : PreloaderBase, IPreloader
    {
        public void Preload(MigrationBuilder migrationBuilder)
        {
            Insert(migrationBuilder, NpcMissionEntry.TableName, typeof(NpcMissionEntry));
        }

        protected override IEnumerable<object[]> GetRows()
        {
            // 321 "Assemble With Lieutenant Perkins": the client's LOGTEXT (missionconversation
            // (321,2)=14, original tier) names Cmd. Sgt. Price as giver and Field Lt. Perkins as
            // the reassembly point. creaturenamelanguage resolves those to name_id 205 and 204, but
            // neither has a creature row, and inventing class_id/level/hp/speeds for them is not
            // supported by any source. The previously seeded 101/100 are Field Sgt. Witherspoon and
            // Outpost Commander Rogers - mission 429's NPCs, borrowed in error. 0 is the
            // established unknown sentinel (NoContentReferences.MissionGiver), so the mission stays
            // unoffered rather than being offered by the wrong NPCs in the wrong zone.
            yield return new object[] { 321, 0, 0, 5, 1, 1, false, false, "Assemble With Lieutenant Perkins" };

            // 429 "River Recon": giver and receiver were transposed. The client's LOGTEXT
            // (missionconversation (429,2)=742, original tier) reads "Outpost Commander Rogers
            // wants you to search ... then report your findings to Field Sgt. Witherspoon", so the
            // giver is Rogers (creature 100) and the receiver Witherspoon (creature 101).
            // Corroborated by objectiveconversation: package 116 says "Get this new info to
            // Witherspoon" (sender = Rogers) and package 208 says "Rogers told me you'd be
            // reporting in" (receiver = Witherspoon); npc_package maps 100 -> 116 and 101 -> 208.
            // category_id 2 ("Instance (Arieki Communications Tower)") is semantically implausible
            // for an open-world Pinhole Falls mission, but no evidence exists for the correct
            // value, so it is left as recorded-suspect rather than guessed.
            yield return new object[] { 429, 100, 101, 3, 2, 2, true, true, "River Recon" };
        }
    }
}
