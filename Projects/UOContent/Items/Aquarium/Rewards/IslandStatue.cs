namespace Server.Items
{
    public class IslandStatue : Item
    {
        [Constructible]
        public IslandStatue() : base(0x3B0F)
        {
        }

        public IslandStatue(Serial serial) : base(serial)
        {
        }

        public override int LabelNumber => 1074600; // An island statue
        public override double DefaultWeight => 1.0;

        public override void AddNameProperties(ObjectPropertyList list)
        {
            base.AddNameProperties(list);

            list.Add(1073634); // An aquarium decoration
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
