using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestClientCS3.Game.Object;

namespace TestClientCS3.Game.Zone
{
    public enum TileType : int
    {
        road,
        wall
    }

    internal class Tile
    {
        public IObject? _object { get; set; }
        public TileType _type { get; set; }

        public Tile()
        {
            _object = null;
        }

        public bool IsEmpty()
        {
            if (_type != TileType.road)
            {
                return false;
            }

            if (_object != null)
            {
                return false;
            }

            return true;
        }
    }
}
