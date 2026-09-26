using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlChar
{
    public partial class BackfillRaceUnlocks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Carry earned unlocks forward, never infer eligibility from an existing hybrid's race.
            // Mission IDs and their evidence: docs/evidence/race-unlocks.json.
            migrationBuilder.Sql(@"INSERT INTO account_race_unlock (account_id, race_id)
                SELECT DISTINCT c.account_id,
                    CASE m.mission_id WHEN 1861 THEN 2 WHEN 1851 THEN 3 WHEN 1899 THEN 4 END
                FROM character_mission m JOIN `character` c ON c.id = m.character_id
                WHERE m.mission_state = 4 AND m.mission_id IN (1861, 1851, 1899)
                  AND NOT EXISTS (SELECT 1 FROM account_race_unlock u WHERE u.account_id = c.account_id
                    AND u.race_id = CASE m.mission_id WHEN 1861 THEN 2 WHEN 1851 THEN 3 WHEN 1899 THEN 4 END)");
        }

        // Earned grants cannot be distinguished from subsequent grants safely. Rolling
        // the schema back drops the table; reverting this data step alone retains eligibility.
        protected override void Down(MigrationBuilder migrationBuilder) { }
    }
}
