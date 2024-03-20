namespace TestClientCS3.Game.Object
{
    internal class Player : IObject
    {
        public int _id { get; set; }
        public int _hp { get; set; }

        public Player(int id)
        {
            _id = id;
            _hp = 100;
        }
    }
}
