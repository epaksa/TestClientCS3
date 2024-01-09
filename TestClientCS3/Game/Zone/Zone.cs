using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestClientCS.Common;
using TestClientCS.Common.Network;
using TestClientCS.Common.Network.Packet;
using TestClientCS3.Game.Object;

namespace TestClientCS3.Game.Zone
{
    internal class Zone
    {
        struct Pos
        {
            public int _x;
            public int _y;

            public Pos(int x, int y)
            {
                _x = x;
                _y = y;
            }
        }

        private ConcurrentQueue<PacketContext> _packet_context_queue = new ConcurrentQueue<PacketContext> ();

        private List<List<Tile>> _tile = new List<List<Tile>>();
        private Dictionary<int, Pos> _object_info = new Dictionary<int, Pos>(); // key : client id

        private Random _random = new Random();

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

                    foreach (KeyValuePair<int, Pos> object_info in _object_info)
                    {
                        FakeInput? input = FakeInputContainer.GetInput(object_info.Key);

                        if (input == null)
                        {
                            continue;
                        }

                        long now = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds();

                        if (input._time_to_execute > now)
                        {
                            continue;
                        }

                        HandleFakeInput(input);
                        FakeInputContainer.RemoveInput(object_info.Key);
                    }
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
                return false;
            }

            if (x < 0 || x >= _tile[y].Count)
            {
                return false;
            }

            return _tile[y][x].IsEmpty();
        }

        private void SetObject(int x, int y, IObject obj)
        {
            _tile[y][x]._object = obj;
            _object_info.Add(obj._id, new Pos(x, y));
        }

        private void SetObject(int current_x, int current_y, int next_x, int next_y)
        {
            _tile[next_y][next_x]._object = _tile[current_y][current_x]._object;
            _tile[current_y][current_x]._object = null;

            _object_info.Remove(_tile[next_y][next_x]._object._id);
            _object_info.Add(_tile[next_y][next_x]._object._id, new Pos(current_x, current_y));
        }

        private void HandlePacketContext(PacketContext context)
        {
            switch (context._packet._packet_id)
            {
                case PacketID.sc_login:
                    
                    ProcessPacket((sc_login)context._packet);

                    if (false == FakeInputContainer.Exist(context._client._id))
                    {
                        long time_to_execute = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds() + _random.Next(1, 6); // 1 ~ 5초 추가
                        FakeInput input = new FakeInput(context._client, FakeInputType.move, time_to_execute); // move할지 attack(아직 미구현)할지 random으로 나중에 돌리기

                        Log.Write($"fake input => id : {input._client._id}, type : {input._type}, time : {input._time_to_execute}");

                        FakeInputContainer.PushInput(input);
                    }

                    break;
                case PacketID.sc_welcome:
                    ProcessPacket((sc_welcome)context._packet);
                    break;
                case PacketID.sc_move:
                    bool my_packet = (((sc_move)context._packet)._move_client_id == context._client._id);
                    ProcessPacket((sc_move)context._packet, my_packet);
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
            // sc_login을 받는 multi client라서 로그만 찍음
            Log.Write($"sc_welcome => id : {packet._client_id}, x : {packet._x}, y : {packet._y}");
        }

        private void ProcessPacket(sc_move packet, bool my_packet)
        {
            if (false == my_packet)
            {
                Log.Write($"sc_move => not my packet. moved client id : {packet._move_client_id}, x : {packet._x}, y : {packet._y}");
                return;
            }

            int current_x = 0;
            int current_y = 0;

            if (false == GetCurrentPos(out current_x, out current_y, packet._move_client_id))
            {
                Log.Write($"sc_move => not found current pos. id : {packet._move_client_id}");
                return;
            }

            if (current_x == packet._x && current_y == packet._y)
            {
                Log.Write($"sc_move => move failed. id : {packet._move_client_id}, x : {packet._x}, y : {packet._y}");
                return; // 이동 실패
            }

            if (CheckTile(packet._x, packet._y))
            {
                SetObject(current_x, current_y, packet._x, packet._y);

                Log.Write($"sc_move => moved. id : {packet._move_client_id}, x : {packet._x}, y : {packet._y}");
            }
            else
            {
                Log.Write($"sc_move => move failed (client). id : {packet._move_client_id}, x : {packet._x}, y : {packet._y}");
                return; // 서버는 승인했는데, client에서 이동 실패
            }
        }

        private void HandleFakeInput(FakeInput input)
        {
            switch (input._type)
            {
                case FakeInputType.move:
                    ProcessFakeInputMove(input._client);
                    break;
                case FakeInputType.attack:
                    break;
                case FakeInputType.disconnect:
                    break;
                default:
                    break;
            }
        }

        private void ProcessFakeInputMove(Client client)
        {
            Pos pos;
            if (_object_info.TryGetValue(client._id, out pos))
            {
                int next_pos_delta_x = _random.Next(-1, 2); // -1 : move left, 1 : move right 
                int next_pos_delta_y = _random.Next(-1, 2);

                if (next_pos_delta_x == 0 && next_pos_delta_y == 0)
                {
                    ProcessFakeInputMove(client);
                    return;
                }

                int next_pos_x = pos._x + next_pos_delta_x;
                int next_pos_y = pos._y + next_pos_delta_y;

                if (CheckTile(next_pos_x, next_pos_y))
                {
                    cs_move packet = new cs_move();
                    packet._x = next_pos_x;
                    packet._y = next_pos_y;

                    Log.Write($"cs_move => id : {client._id}, x : {packet._x}, y : {packet._y}");
                    
                    client.Send(packet);
                }
            }
        }

        private bool GetCurrentPos(out int out_x, out int out_y, int object_id)
        {
            Pos pos;
            
            if (_object_info.TryGetValue(object_id, out pos))
            {
                out_x = pos._x;
                out_y = pos._y;
                return true;
            }
            else
            {
                out_x = 0;
                out_y = 0;
                return false;
            }
        }
    }
}
