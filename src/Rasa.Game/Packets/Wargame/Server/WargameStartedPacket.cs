using System.Collections.Generic;

namespace Rasa.Packets.Wargame.Server
{
    using Data;
    using Memory;

    /// <summary>
    /// client/wargame.py Recv_WargameStarted(wargameId, enemyUserIds), opcode 633, on entity 23.
    ///
    /// It sets bActive on the status, stores enemyUserIds, posts UI_ENABLE_WARGAME_TRACKER True,
    /// closes the challenge windows and plays audiosetdata.UI_ALERT_CLAN_WARGAME_START. There is
    /// no countdown anywhere in the client: the duel is live the moment this arrives.
    ///
    /// enemyUserIds is stored on the status and read by nothing else in the 1.16.5.0 bytecode, so
    /// its contents are inert - but it is the squad roster's id space, which is the account id the
    /// party manager calls userId, so that is what a duel puts in it.
    ///
    /// The status must already exist: _GetWargameStatusEnsured logs and returns None otherwise,
    /// and this handler then raises on status.bActive.
    /// </summary>
    public class WargameStartedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WargameStarted;

        public uint WargameId { get; set; }
        public List<uint> EnemyUserIds { get; set; }

        public WargameStartedPacket(uint wargameId, List<uint> enemyUserIds)
        {
            WargameId = wargameId;
            EnemyUserIds = enemyUserIds ?? new List<uint>();
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt(WargameId);
            pw.WriteList(EnemyUserIds.Count);

            foreach (var userId in EnemyUserIds)
                pw.WriteUInt(userId);
        }
    }
}
