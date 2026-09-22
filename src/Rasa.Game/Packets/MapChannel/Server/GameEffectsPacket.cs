namespace Rasa.Packets.MapChannel.Server
{
    using System.Collections.Generic;
    using Data;
    using Memory;

    /// <summary>
    /// Every effect already running on an entity, in one send. PhysicalEntity.Recv_GameEffects is two lines -
    /// `for data in effectDataSeq: effect = apply(self.AttachGameEffect, data); effect.AnnounceAttach(self)` -
    /// so each element is AttachGameEffect's own argument tuple, (typeId, effectId, level, sourceId, tooltipDict,
    /// *args): the GameEffectAttached tuple with the announce flag taken out.
    /// (verify/dis/trpython-client-physicalentity.pyo.dis :: Recv_GameEffects first=725, AttachGameEffect first=488)
    ///
    /// AnnounceAttach is called unconditionally and is idempotent - it returns early on its own __announced flag
    /// (gameeffects/basegameeffect.py AnnounceAttach first=162) - which is only right for effects that are already
    /// running. This is the catch-up send for an entity coming into view, and CreatePhysicalEntity already names
    /// GameEffects among the packets that may ride its creation payload.
    /// </summary>
    public sealed class GameEffectsPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.GameEffects;

        public List<GameEffectAttachedPacket> Effects { get; } = new List<GameEffectAttachedPacket>();

        public GameEffectsPacket(IEnumerable<GameEffectAttachedPacket> effects)
        {
            Effects.AddRange(effects);
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(Effects.Count);

            foreach (var effect in Effects)
                effect.WriteEffectData(pw, false);
        }
    }
}
