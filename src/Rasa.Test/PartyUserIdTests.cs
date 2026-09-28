using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Memory;
using Rasa.Packets.Party.Client;

namespace Rasa.Test
{
    [TestClass]
    public class PartyUserIdTests
    {
        private const uint BotAccountId = 4200000001;

        // Live 2026-09-28 02:50 UTC: four KickUserFromPartyById calls on DIT bots removed nobody. The client holds
        // the account id as a signed 32-bit int, so 4200000001 came back as -94967295 and read as no member.
        [TestMethod]
        public void AnAccountIdAboveIntMaxSurvivesTheClientsSignedIntAsLong()
        {
            Assert.AreEqual(BotAccountId, Read(writer => writer.WriteLong(unchecked((int)BotAccountId))));
        }

        [TestMethod]
        public void AnAccountIdAboveIntMaxSurvivesTheClientsSignedIntAsInt()
        {
            Assert.AreEqual(BotAccountId, Read(writer => writer.WriteInt(unchecked((int)BotAccountId))));
        }

        [TestMethod]
        public void OrdinaryIdsAndNonsenseAreUnchanged()
        {
            Assert.AreEqual(42U, Read(writer => writer.WriteLong(42)));
            Assert.AreEqual(0U, Read(writer => writer.WriteLong(0)));
            Assert.AreEqual(0U, Read(writer => writer.WriteLong(-5_000_000_000L)));
        }

        private static uint Read(System.Action<PythonWriter> write)
        {
            using var stream = new MemoryStream();
            using (var writer = new PythonWriter(new BinaryWriter(stream, System.Text.Encoding.UTF8, true)))
            {
                writer.WriteTuple(1);
                write(writer);
            }

            stream.Position = 0;
            var packet = new KickUserFromPartyByIdPacket();
            using (var reader = new PythonReader(new BinaryReader(stream)))
                packet.Read(reader);
            return packet.UserId;
        }
    }
}
