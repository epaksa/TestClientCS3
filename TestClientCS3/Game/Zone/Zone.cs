using System.Collections.Concurrent;
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

        private ConcurrentQueue<PacketContext> _packet_context_queue = new ConcurrentQueue<PacketContext>();

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
                while (true)
                {
                    if (_packet_context_queue.TryDequeue(out PacketContext? context))
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

                        if (input._time_to_execute <= now)
                        {
                            HandleFakeInput(input);
                            FakeInputContainer.RemoveInput(object_info.Key);
                        }
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
            _object_info.Add(_tile[next_y][next_x]._object._id, new Pos(next_x, next_y));
        }

        private void RemoveObject(int x, int y, int object_id)
        {
            _tile[y][x]._object = null;
            _object_info.Remove(object_id);
        }

        private void HandlePacketContext(PacketContext context)
        {
            switch (context._packet._packet_id)
            {
                case PacketID.sc_login:

                    ProcessPacket((sc_login)context._packet);

                    if (false == FakeInputContainer.Exist(context._client._id))
                    {
                        long time_to_execute = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds() + _random.Next(1, 4); // 1 ~ 3초 추가
                        FakeInput input = new FakeInput(context._client, FakeInputType.move, time_to_execute);

                        Log.Write($"fake input => id : {input._client._id}, type : {input._type}, time : {input._time_to_execute}");

                        FakeInputContainer.PushInput(input);
                    }

                    break;
                case PacketID.sc_welcome:
                    ProcessPacket((sc_welcome)context._packet);
                    break;
                case PacketID.sc_move:

                    int move_client_id = ((sc_move)context._packet)._move_client_id;
                    bool my_packet = (move_client_id == context._client._id);

                    if (my_packet)
                    {
                        ProcessPacket((sc_move)context._packet); // dummy client에서는, 자기가 받았을때만 처리. unity client에서는 모두 처리

                        if (false == FakeInputContainer.Exist(context._client._id))
                        {
                            long time_to_execute = new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds() + _random.Next(1, 4); // 1 ~ 3초 추가
                            FakeInput input = new FakeInput(context._client, FakeInputType.move, time_to_execute);

                            Log.Write($"fake input => id : {input._client._id}, type : {input._type}, time : {input._time_to_execute}");

                            FakeInputContainer.PushInput(input);
                        }
                    }

                    break;
                case PacketID.sc_logout:

                    int logout_client_id = ((sc_logout)context._packet)._client_id;
                    ProcessPacket((sc_logout)context._packet, logout_client_id);

                    break;
                default:
                    break;
            }
        }

        private void ProcessPacket(sc_login packet)
        {
            if (false == _object_info.ContainsKey(packet._my_info._client_id))
            {
                if (CheckTile(packet._my_info._x, packet._my_info._y))
                {
                    Player player = new Player(packet._my_info._client_id);

                    SetObject(packet._my_info._x, packet._my_info._y, player);

                    Log.Write($"sc_login => id : {packet._my_info._client_id}, x : {packet._my_info._x}, y : {packet._my_info._y}");
                }
            }
            else
            {
                Log.Write($"sc_login => already exist! id : {packet._my_info._client_id}");
            }

            foreach (sc_login.ClientPos pos in packet._list_client)
            {
                if (_object_info.ContainsKey(pos._client_id))
                {
                    continue;
                }

                Player other_player = new Player(pos._client_id);

                SetObject(pos._x, pos._y, other_player);
            }
        }

        private void ProcessPacket(sc_welcome packet)
        {
            if (false == _object_info.ContainsKey(packet._client_id))
            {
                Player other_player = new Player(packet._client_id);

                SetObject(packet._x, packet._y, other_player);
            }

            Log.Write($"sc_welcome => id : {packet._client_id}, x : {packet._x}, y : {packet._y}");
        }

        private void ProcessPacket(sc_move packet)
        {
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

        private void ProcessPacket(sc_logout packet, int client_id)
        {
            int current_x = 0;
            int current_y = 0;

            if (false == GetCurrentPos(out current_x, out current_y, client_id))
            {
                Log.Write($"sc_logout => not found current pos. id : {client_id}");
                return;
            }

            RemoveObject(current_x, current_y, client_id);

            Log.Write($"sc_logout => id : {client_id}");
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

                int next_pos_x = pos._x + next_pos_delta_x;
                int next_pos_y = pos._y + next_pos_delta_y;

                if ((next_pos_delta_x == 0 && next_pos_delta_y == 0) || (false == CheckTile(next_pos_x, next_pos_y)))
                {
                    ProcessFakeInputMove(client);
                    return;
                }

                cs_move packet = new cs_move();
                packet._x = next_pos_x;
                packet._y = next_pos_y;

                Log.Write($"cs_move => id : {client._id}, x : {packet._x}, y : {packet._y}");

                client.Send(packet);
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
