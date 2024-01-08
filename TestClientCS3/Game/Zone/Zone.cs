using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestClientCS.Common;
using TestClientCS.Common.Network.Packet;
using TestClientCS3.Game.Object;

namespace TestClientCS3.Game.Zone
{
    internal class Zone
    {
        private ConcurrentQueue<PacketContext> _packet_context_queue = new ConcurrentQueue<PacketContext> ();

        //private List<> _fake
        
        private List<List<Tile>> _tile = new List<List<Tile>>();

        public Zone(string map_file_name)
        {
            string[] list_line = File.ReadAllLines(map_file_name);

            foreach (string line in list_line)
            {
                List<Tile> list_tile = new List<Tile>();

                string[] list_tile_data = line.Split(',');

                foreach (string tile_data in list_tile_data)
                {
                    Tile tile = new Tile();

                    if (tile_data == "-1")
                    {
                        tile._type = TileType.road;
                    }
                    else
                    {
                        tile._type = TileType.wall;
                    }

                    list_tile.Add(tile);
                }

                _tile.Add(list_tile);
            }
        }

        public Task Start()
        {
            return Task.Run(() =>
            {
                PacketContext? context = null;

                while (true)
                {
                    if (_packet_context_queue.TryDequeue(out context))
                    {
                        HandlePacketContext(context);
                    }

                    // 가라 input 실행
                }
            });
        }

        public void PushPacketContext(PacketContext context)
        {
            _packet_context_queue.Enqueue(context);
        }

        private bool CheckTile(int x, int y)
        {
            if (y < 0 || y >= _tile.Count)
            {
                Log.Write($"not valid pos. x : {x}, y : {y}");
                return false;
            }

            if (x < 0 || x >= _tile[y].Count)
            {
                Log.Write($"not valid pos. x : {x}, y : {y}");
                return false;
            }

            return _tile[x][y].IsEmpty();
        }

        private void SetObject(int x, int y, IObject obj)
        {
            _tile[x][y]._object = obj;
        }

        private void HandlePacketContext(PacketContext context)
        {
            switch (context._packet._packet_id)
            {
                case PacketID.sc_login:
                    ProcessPacket((sc_login)context._packet);
                    break;
                case PacketID.sc_welcome:
                    ProcessPacket((sc_welcome)context._packet);
                    break;
                case PacketID.sc_move:
                    ProcessPacket((sc_move)context._packet);
                    break;
                default:
                    break;
            }
        }

        private void ProcessPacket(sc_login packet)
        {
            if (CheckTile(packet._x, packet._y))
            {
                Player player = new Player(packet._client_id);
                
                SetObject(packet._x, packet._y, player);

                Log.Write($"sc_login => id : {packet._client_id}, x : {packet._x}, y : {packet._y}");
            }
        }

        private void ProcessPacket(sc_welcome packet)
        {
            if (CheckTile(packet._x, packet._y))
            {
                Player player = new Player(packet._client_id);

                SetObject(packet._x, packet._y, player);

                Log.Write($"sc_welcome => id : {packet._client_id}, x : {packet._x}, y : {packet._y}");
            }
        }

        private void ProcessPacket(sc_move packet)
        {

        }
    }
}
