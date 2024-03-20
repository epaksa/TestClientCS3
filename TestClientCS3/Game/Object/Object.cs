namespace TestClientCS3.Game.Object
{
    public enum ObjectType : int
    {
        player,
        monster
    }

    internal interface IObject
    {
        public int _id { get; set; }
        public int _hp { get; set; }
    }
}
