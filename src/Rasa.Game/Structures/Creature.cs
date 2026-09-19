using System.Collections.Generic;

namespace Rasa.Structures
{
    using Data;
    using System;
    using System.Linq;
    using World;
    public class Creature : Actor, ICloneable
    {
        public uint DbId { get; set; }
        /// <summary>The channel the creature was added to; two instances of a context hold different creatures.</summary>
        public MapChannel MapChannel { get; set; }
        /// <summary>The content placement this creature was materialized from; 0 for spawn-pool and world creatures.</summary>
        public uint ContentPlacementId { get; set; }
        /// <summary>Materialized from an escort placement: the client is told so, and marks it overhead.</summary>
        public bool IsEscort { get; set; }
        // npc data (only if creature is a NPC)
        public Npc Npc { get; set; }
        // loot data (only if creature is harvestable)
        public CreatureLootData LootData { get; set; }
        public Factions Faction { get; set; }
        public uint Level { get; set; }
        public uint MaxHitPoints { get; set; }
        public uint NameId { get; set; }
        public long UpdatePositionCounter;

        /// <summary>When an escort creature may be sent after its player again (2000 ms between paths).</summary>
        public long EscortRepathAt;                                       // decreases, when it hits 0 and the cell position changed, call creature_updateCellLocation()
        public Dictionary<EquipmentData, AppearanceData> AppearanceData { get; set; }
        //sint32 lastattack;
        //float velocity;
        //sint32 rottime; //rotation speed
        //float range; //attackrange
        //sint32 attack_habbit; //meelee or range fighter 
        //sint32 agression; // hunting timer for enemys
        // aggro info
        public float AggroRange = 18.0f; // how far away the creature can detect enemies, usually 24.0f but can be increased by having high-range attacks
        public long AggressionTime = 5000; // ToDo

        public float WalkSpeed { get; set; }
        public float RunSpeed { get; set; }
        //sint32 movestate;
        //float wx,wy,wz; // target destination (can be far away)
        public BaseBehaviorBaseNode HomePos = new BaseBehaviorBaseNode();  //--- spawn location (used for wander)
        // A guarding content placement: fights from and returns to HomePos, never wanders (CreatureManager.ApplyPlacementBehavior).
        public bool HoldsPosition { get; set; }
        public BaseBehaviorBaseNode Pathnodes { get; set; } //--entity patrol nodes
        //sint32** aggrotable; //stores enemydamage
        //sint32 aggrocount;
        public double Scale = 1.0d;
        // origin
        public SpawnPool SpawnPool { get; set; }    // the spawnpool that initiated the creation of this creature
        // behavior controller
        public BehaviorState Controller = new BehaviorState();
        // loot dispenser
        public ulong LootDispenserObjectEntityId { get; internal set; }

        /// <summary>
        /// The corpse loot dispenser made when this creature died, or 0. Distinct from
        /// LootDispenserObjectEntityId, which is the dynamic object that marks a lootable corpse
        /// in the world; this is the dispenser holding what is actually on it.
        /// </summary>
        public ulong CorpseLootEntityId { get; internal set; }
        // creature actions
        public List<CreatureAction> Actions = new List<CreatureAction>();

        // creature tumers
        public long LastAgression { get; internal set; }
        public long LastRestTime { get; internal set; }

        public Creature(CreatureEntry data)
        {
            DbId = data.Id;
            EntityClass = (EntityClasses)data.ClassId;
            Faction = (Factions)data.Faction;
            Level = data.Level;
            MaxHitPoints = data.MaxHitPoints;
            NameId = data.NameId;
            RunSpeed = data.RunSpeed;
            WalkSpeed = data.WalkSpeed;
        }

        /// <summary>Who may harvest this corpse: the player whose kill it was.</summary>
        public ulong HarvestOwnerEntityId { get; set; }

        /// <summary>Attempts left on this corpse. Zero on a living creature, and after depletion.</summary>
        public int HarvestAttemptsLeft { get; set; }

        public Creature()
        {
        }

        public Creature(Creature creature)
        {
            AppearanceData = creature.AppearanceData;
            DbId = creature.DbId;
            EntityClass = creature.EntityClass;
            Faction = creature.Faction;
            Level = creature.Level;
            MaxHitPoints = creature.MaxHitPoints;
            NameId = creature.NameId;
            Npc = creature.Npc;
            RunSpeed = creature.RunSpeed;
            WalkSpeed = creature.WalkSpeed;
            foreach (var action in creature.Actions)
                Actions.Add(new CreatureAction((CreatureAction)action.Clone()));
        }

        public object Clone()
        {
            return new Creature(this);
        }
    }
}
