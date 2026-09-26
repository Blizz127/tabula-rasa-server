namespace Rasa.Structures
{
    using Data;
    using World;

    public class Logos : DynamicObject
    {
        public uint Id { get; set; }
        public string Name { get; set; }

        public Logos(LogosEntry logos)
        {
            EntityClassId = (EntityClasses)logos.ClassId;
            Id = logos.Id;
            MapContextId = logos.MapContextId;
            Name = logos.Name;
            Position = logos.Position;
            DynamicObjectType = DynamicObjectType.Logos;
        }

        /// <summary>The same shrine for another copy of its map (MapChannelManager.PopulateContextCopy).</summary>
        public Logos(Logos template)
        {
            EntityClassId = template.EntityClassId;
            Id = template.Id;
            MapContextId = template.MapContextId;
            Name = template.Name;
            Position = template.Position;
            Rotation = template.Rotation;
            DynamicObjectType = DynamicObjectType.Logos;
        }
    }
}
