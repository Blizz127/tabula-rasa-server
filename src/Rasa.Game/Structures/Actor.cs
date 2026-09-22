using System.Collections.Generic;
using System.Numerics;

namespace Rasa.Structures
{
    using Data;
    using Managers;
    using Interfaces;

    public class Actor : IHasPosition
    {
        public ulong EntityId = EntityManager.Instance.GetEntityId;
        public Vector3 Position { get; set; }
        public double Rotation { get; set; }
        public bool IsCrouching { get; set; }
        public EntityClasses EntityClass { get; set; }
        public string Name { get; set; }
        public string FamilyName { get; set; }
        public uint MapContextId { get; set; }
        public bool IsRunning { get; set; }
        public bool InCombatMode { get; set; }
        public CharacterState State { get; set; }
        public ulong Target { get; set; }
        /// <summary>
        /// The target last broadcast to this actor's cell as TargetId, so the aim goes out when it changes and not
        /// on every behaviour tick. Distinct from Target, which a minion order sets without a fight starting.
        /// </summary>
        public ulong AnnouncedTarget { get; set; }
        public double MovementSpeed { get; set; }
        public bool WeaponReady { get; set; }
        // action data
        public int CurrentAction { get; set; }
        public Dictionary<Attributes, ActorAttributes> Attributes = new Dictionary<Attributes, ActorAttributes>();
        public Dictionary<int, GameEffect> ActiveEffects { get; set; } = new Dictionary<int, GameEffect>();
        public uint[,] Cells = new uint[5 ,5];
        // sometimes we only have access to the actor, the owner variable allows us to access the client anyway (only if actor is a player manifestation)
        //public MapChannelClient Owner { get; set; }
    }
}
