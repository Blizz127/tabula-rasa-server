using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Packets.MapChannel.Server;
using Rasa.Structures;

namespace Rasa.Test
{
    /// <summary>
    /// Server enums whose values go on the wire, pinned to the client's own constants (1.16.5.0 game.zip,
    /// generated/shared/characterstatedata.pyo and generated/shared/damagetype.pyo, read 2026-09-19). The
    /// server's names may differ (its Laser is the client's LIGHT); the numbers may not.
    /// </summary>
    [TestClass]
    public class ClientConstantTests
    {
        [TestMethod]
        public void CharacterStatesAreTheClientsStateIds()
        {
            var client = new (CharacterState Server, int Client)[]
            {
                (CharacterState.Standing, 1), (CharacterState.Sitting, 2), (CharacterState.LyingDown, 3),
                (CharacterState.Swimming, 4), (CharacterState.Dead, 5), (CharacterState.Stopped, 6),
                (CharacterState.Slow, 7), (CharacterState.Fast, 8), (CharacterState.Flying, 9),
                (CharacterState.Flailing, 10), (CharacterState.Normal, 11), (CharacterState.Uncontrolled, 12),
                (CharacterState.Stunned, 13), (CharacterState.Crouched, 14), (CharacterState.AtPeace, 15),
                (CharacterState.CombatEngaged, 17), (CharacterState.Idle, 18), (CharacterState.Recovery, 19),
                (CharacterState.Windup, 20), (CharacterState.NoTool, 21), (CharacterState.ToolReady, 24),
                (CharacterState.Special, 25), (CharacterState.Dying, 26),
            };
            foreach (var (server, id) in client)
                Assert.AreEqual(id, (int)server, server.ToString());
        }

        [TestMethod]
        public void DamageTypesAreTheClientsDamageTypes()
        {
            string[] client = { "NORMAL", "FIRE", "ICE", "VIRULENT", "EMP", "LIGHT", "SONIC", "KNOCKBACK", "STUN", "SLEEP",
                "SYSTEM", "ENVIRONMENTAL", "ENERGY", "SNARE", "ROOT", "CONFUSION", "JAM", "FEAR", "LOGOS", "BLIND" };
            var server = new[] { DamageType.Physical, DamageType.Fire, DamageType.Ice, DamageType.Virulent, DamageType.EMP,
                DamageType.Laser, DamageType.Sonic, DamageType.KnockBack, DamageType.Stun, DamageType.Sleep, DamageType.System,
                DamageType.Environmental, DamageType.Electrical, DamageType.Snare, DamageType.Root, DamageType.Confusion,
                DamageType.Jam, DamageType.Fear, DamageType.Logos, DamageType.Blind };
            for (var i = 0; i < client.Length; i++)
                Assert.AreEqual(i + 1, (int)server[i], client[i]);
        }

        [TestMethod]
        public void ActorInfoSendsAPostureNotAStateType()
        {
            // The client looks desiredPostureId up as a state id and acts only on CROUCHED; the state type the
            // server used to send (Control = 5) is the id of DEAD.
            Assert.AreEqual(CharacterState.Standing, new ActorInfoPacket(new Manifestation { State = CharacterState.Normal }).DesiredPostureId);
            Assert.AreEqual(CharacterState.Crouched, new ActorInfoPacket(new Manifestation { State = CharacterState.Crouched }).DesiredPostureId);
        }
    }
}
