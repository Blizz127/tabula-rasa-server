using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test
{
    /// <summary>
    /// Every weapon template has an itemtemplate_weapon row, and every row reads what the client's own
    /// weaponclass data says it should.
    ///
    /// ItemTemplateTooltipInfoPacket reaches ItemTemplate.WeaponInfo unconditionally inside its WEAPON case, so a
    /// template whose class carries the WEAPON augmentation and whose weapon row is missing throws the moment
    /// anything asks for its tooltip. The world shipped 2444 rows against the client's 5825 weapon templates;
    /// WeaponRowRepairRows filled the other 3381. This audit is the guard that keeps it filled: a new template
    /// added to a weapon class without a weapon row fails here rather than in a player's tooltip.
    ///
    /// The evidence is docs/evidence/client-weaponclass.csv - the raw fields of the client's own tables, not a
    /// conclusion drawn from them. This test re-derives tool_type and attack_type from those fields with the rule
    /// WeaponRowRepairRows documents, so if the rule and the table ever disagree the test says which row and
    /// which column, and the rule is here to be read rather than trusted.
    ///
    /// The eighteen other columns are not checked. They are placeholders the world seed chose, not values any
    /// client table gives per template (see WeaponRowRepairRows), so there is nothing here to check them against.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class WeaponRowAuditTests
    {
        /// <summary>The client's constant/tooltype. It has no 0 and stops at 12; 0 here means "the enum names none".</summary>
        private const uint NoToolType = 0;

        private const uint HealingDisc = 1, ArmorAug = 2, Cipher = 3, TissueExtractor = 4, Salvage = 5, FieldRepair = 6;
        private const uint Rifle = 7, Pistol = 8, Shotgun = 9, GrenadeLauncher = 10, RocketLauncher = 11, DensityGun = 12;

        /// <summary>The client's constant/attacktype, the two values a weapon class can reach.</summary>
        private const uint AttackMelee = 1, AttackRanged = 2;

        /// <summary>constant/weaponanimconditioncode.</summary>
        private const long AnimPistols = 1, AnimRifles = 2, AnimShotguns = 3, AnimRocketLaunchers = 4, AnimS3Pistol = 23;

        /// <summary>actiondata action ids. The two melee modules, the harvest tool, and the four named tools.</summary>
        private const long WeaponBlade = 418, WeaponMelee = 174, ToolHarvest = 172;
        private const long ToolHealingDisc = 147, ToolArmorAugmentation = 199, ToolCipher = 258, ToolFieldRepair = 198;

        /// <summary>TOOL_HARVEST's arg ids.</summary>
        private const long HarvestSalvage = 168, HarvestDna = 169;

        /// <summary>itemclass skill requirement ids. Empty is "this template requires no skill".</summary>
        private const long SkillFirearms = 1, SkillSpecialistTools = 14, SkillCommandoLaunchers = 24, SkillLeechGun = 31;

        /// <summary>
        /// One line of the evidence file. Arg, Anim and Skill are nullable because the client itself leaves them
        /// empty: eight weapon classes carry no anim condition code, and around 200 templates - creature, NPC,
        /// holographic, vehicle and test weapons - require no skill at all. A lifted comparison against a null
        /// field is false, which is the right answer: the rule falls through to 0.
        /// </summary>
        private readonly struct WeaponClass
        {
            public WeaponClass(uint template, uint itemClass, long action, long? arg, long? anim, long? skill)
            {
                Template = template;
                ItemClass = itemClass;
                Action = action;
                Arg = arg;
                Anim = anim;
                Skill = skill;
            }

            public uint Template { get; }
            public uint ItemClass { get; }
            public long Action { get; }
            public long? Arg { get; }
            public long? Anim { get; }
            public long? Skill { get; }
        }

        [TestMethod]
        public void EveryWeaponTemplateHasARowThatMatchesTheClient()
        {
            var root = RepositoryRoot();
            var client = ReadClientWeaponClasses(root);
            Assert.AreEqual(5825, client.Count, "the client evidence file holds one line per client weapon template");

            using var connection = OpenWorld(root);
            var stored = new Dictionary<uint, (uint ToolType, uint AttackType)>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT id, tool_type, attack_type FROM itemtemplate_weapon";
                using var reader = command.ExecuteReader();
                while (reader.Read())
                    stored[(uint)reader.GetInt64(0)] = ((uint)reader.GetInt64(1), (uint)reader.GetInt64(2));
            }

            var problems = new List<string>();

            foreach (var weapon in client)
            {
                if (!stored.TryGetValue(weapon.Template, out var row))
                {
                    problems.Add($"template {weapon.Template} (item class {weapon.ItemClass}) is a client weapon " +
                                 "class and has no itemtemplate_weapon row, so its tooltip throws");
                    continue;
                }

                var toolType = ToolTypeOf(weapon);
                if (row.ToolType != toolType)
                    problems.Add($"template {weapon.Template} (item class {weapon.ItemClass}) reads tool_type " +
                                 $"{row.ToolType}; the client's action {weapon.Action}, arg " +
                                 $"{weapon.Arg?.ToString() ?? "none"}, anim {weapon.Anim?.ToString() ?? "none"} " +
                                 $"and skill {weapon.Skill?.ToString() ?? "none"} give {toolType}");

                var attackType = AttackTypeOf(weapon);
                if (row.AttackType != attackType)
                    problems.Add($"template {weapon.Template} (item class {weapon.ItemClass}) reads attack_type " +
                                 $"{row.AttackType}; the client runs action {weapon.Action} for it, which is " +
                                 $"{attackType}");
            }

            foreach (var template in stored.Keys.Except(client.Select(weapon => weapon.Template)).OrderBy(id => id))
                problems.Add($"template {template} has an itemtemplate_weapon row and is not a weapon in the client");

            Assert.AreEqual(0, problems.Count,
                $"{problems.Count} weapon rows disagree with the client: {string.Join(" | ", problems.Take(40))}");
        }

        /// <summary>
        /// The rule WeaponRowRepairRows derives: the skill requirement picks the weapon family, and the anim
        /// condition code (for the firearm and launcher skills) or the attack action (for the tool skill) picks
        /// which member of that family it is. Anything the client's tooltype enum names no entry for is 0.
        /// </summary>
        private static uint ToolTypeOf(WeaponClass weapon)
        {
            switch (weapon.Skill)
            {
                case SkillFirearms:
                    if (weapon.Anim == AnimRifles) return Rifle;
                    if (weapon.Anim == AnimPistols || weapon.Anim == AnimS3Pistol) return Pistol;
                    if (weapon.Anim == AnimShotguns) return Shotgun;
                    return NoToolType;

                case SkillSpecialistTools:
                    if (weapon.Action == ToolHealingDisc) return HealingDisc;
                    if (weapon.Action == ToolArmorAugmentation) return ArmorAug;
                    if (weapon.Action == ToolCipher) return Cipher;
                    if (weapon.Action == ToolFieldRepair) return FieldRepair;
                    return NoToolType;

                case SkillCommandoLaunchers:
                    if (weapon.Anim == AnimShotguns) return GrenadeLauncher;
                    if (weapon.Anim == AnimRocketLaunchers) return RocketLauncher;
                    return NoToolType;

                case SkillLeechGun:
                    return DensityGun;

                case null when weapon.Action == ToolHarvest:
                    if (weapon.Arg == HarvestSalvage) return Salvage;
                    if (weapon.Arg == HarvestDna) return TissueExtractor;
                    return NoToolType;

                default:
                    return NoToolType;
            }
        }

        /// <summary>
        /// The client's own dispatch: actiondata.actionModules runs weapons.blade for WEAPON_BLADE and
        /// weapons.meleeattackmovement for WEAPON_MELEE. Every other action on a weapon class is a projectile
        /// module, staffs included - weaponclass wires all of theirs to the ranged attack args.
        /// </summary>
        private static uint AttackTypeOf(WeaponClass weapon)
            => weapon.Action == WeaponBlade || weapon.Action == WeaponMelee ? AttackMelee : AttackRanged;

        private static List<WeaponClass> ReadClientWeaponClasses(string root)
        {
            var path = Path.Combine(root, "docs", "evidence", "client-weaponclass.csv");
            Assert.IsTrue(File.Exists(path), $"the client weapon-class evidence is missing: {path}");

            var weapons = new List<WeaponClass>();

            foreach (var line in File.ReadLines(path))
            {
                if (line.Length == 0 || line[0] == '#' || line.StartsWith("item_template_id", StringComparison.Ordinal))
                    continue;

                var fields = line.Split(',');
                Assert.AreEqual(6, fields.Length, $"malformed evidence line: {line}");

                weapons.Add(new WeaponClass(
                    uint.Parse(fields[0], CultureInfo.InvariantCulture),
                    uint.Parse(fields[1], CultureInfo.InvariantCulture),
                    long.Parse(fields[2], CultureInfo.InvariantCulture),
                    Optional(fields[3]),
                    Optional(fields[4]),
                    Optional(fields[5])));
            }

            return weapons;
        }

        /// <summary>An empty field is the client holding no value there, not a zero.</summary>
        private static long? Optional(string field)
            => field.Length == 0 ? null : long.Parse(field, CultureInfo.InvariantCulture);

        private static string RepositoryRoot()
        {
            var directory = AppContext.BaseDirectory;
            while (directory != null && !Directory.Exists(Path.Combine(directory, "navmesh")))
                directory = Path.GetDirectoryName(directory.TrimEnd(Path.DirectorySeparatorChar));
            Assert.IsNotNull(directory, "the repository root carries the navmesh folder");
            return directory;
        }

        private static SqliteConnection OpenWorld(string root)
        {
            var path = Path.Combine(root, "rasaworld.db");
            if (!File.Exists(path) || new FileInfo(path).Length == 0)
                Assert.Inconclusive("rasaworld.db is not in the repository root; this audit reads the world database");
            var connection = new SqliteConnection($"Data Source={path};Mode=ReadOnly");
            connection.Open();
            return connection;
        }
    }
}
