using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestClientCS.Common.DataStructure;
using TestClientCS.Common.Network.Packet;

namespace TestClientCS.Common.Network
{
    public class Client
    {
        static Random random = new Random();
        static object random_lock = new object();

        TcpClient client = new TcpClient();

        int id = 0;
        int required_send_count = 0;
        int current_send_count = 1;
        ConcurrentDictionary<int, long> map_send_history = new ConcurrentDictionary<int, long>(); // key : send count number, value : send time

        RingBuffer _temp_buffer = new RingBuffer();
        RingBuffer _read_buffer = new RingBuffer();

        public Client(int id, int required_send_count)
        {
            this.id = id;
            this.required_send_count = required_send_count;

            Log.Write($"created client. id : {id}, required send count : {required_send_count}");
        }

        public void Send()
        {
            try
            {
                cs_move packet = new cs_move();
                packet._x = id;
                packet._y = current_send_count;

                byte[] buffer = new byte[Program.SEND_BUFFER_SIZE];
                int buffer_length = packet.Serialize(ref buffer);

                // commented for broadcast test
                //Log.Write($"client sends. id : {id}, x : {packet._x}, y : {packet._y}");

                // 여기서부터 시간재는게 맞을까? 아니면 OnSend()에서 재는게 맞을까..?
                map_send_history.TryAdd(current_send_count, new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds());

                client.GetStream().BeginWrite(buffer, 0, buffer_length, OnSend, packet);
            }
            catch (Exception ex)
            {
                if (ex is ObjectDisposedException || ex is InvalidOperationException)
                {
                    Log.Write($"client disconnected(Send()). id : {id}");
                }
            }
        }

        public void Receive()
        {
            try
            {
                _read_buffer.Clear();
                client.GetStream().BeginRead(_read_buffer.Data(), 0, _read_buffer.GetLength() - 1, OnReceive, null);
            }
            catch (Exception ex)
            {
                if (ex is ObjectDisposedException || ex is InvalidOperationException)
                {
                    Log.Write($"client disconnected(Receive()). id : {id}");
                }
            }
        }

        public void Connect(string ip, int port)
        {
            Log.Write($"client try to connect. id : {id}");

            client.BeginConnect(ip, port, OnConnect, null);
        }

        public void Start()
        {
            Receive();
            Send();
        }

        private void OnConnect(IAsyncResult ar)
        {
            client.EndConnect(ar);

            lock (Program.LOCK_CONNECTED_CLIENT_COUNT)
            {
                ++(Program.CONNECTED_CLIENT_COUNT);
            }

            Log.Write($"client connected. id : {id}");

            if (Program.CLIENT_COUNT == Program.CONNECTED_CLIENT_COUNT)
            {
                Program.Start();
            }
        }

        private async void OnSend(IAsyncResult ar)
        {
            try
            {
                client.GetStream().EndWrite(ar);

                cs_move sent_packet = (cs_move)ar.AsyncState;

                if (current_send_count == required_send_count)
                {
                    Log.Write($"client finished to send required send times. id : {id}");
                    //client.Close();
                }
                else
                {
                    ++current_send_count;

                    int send_interval = 0;

                    lock (random_lock)
                    {
                        send_interval = random.Next(Program.MIN_SEND_INTERVAL_IN_MS, Program.MAX_SEND_INTERVAL_IN_MS + 1);
                    }

                    // commented for broadcast test
                    //Log.Write($"client sent. id : {id}. current send count : {current_send_count}, next interval time : {send_interval}, x : {sent_packet._x}, y : {sent_packet._y}");

                    await Task.Delay(send_interval).ConfigureAwait(false);

                    Send();
                }
            }
            catch (Exception ex)
            {
                if (ex is ObjectDisposedException || ex is InvalidOperationException)
                {
                    Log.Write($"client disconnected(OnSend()). id : {id}");
                }
            }
        }

        private void OnReceive(IAsyncResult ar)
        {
            try
            {
                int read_bytes = client.GetStream().EndRead(ar);

                if (false == _read_buffer.SetWriteIndex(read_bytes))
                {
                    Log.Write($"set write index error. read_bytes : {read_bytes}");
                    return;
                }

                byte[] packet_buffer = new byte[Program.READ_BUFFER_SIZE];
                int packet_size = 0;
                int remain_bytes = read_bytes;

                List<BasePacket> list_packet = new List<BasePacket>();

                while (remain_bytes > 0)
                {
                    if (false == CanMakePacket(ref packet_buffer, ref packet_size))
                    {
                        _temp_buffer.Clear();
                        _temp_buffer.Copy(ref _read_buffer);
                        break;
                    }

                    // worker thread로 원래는 넘어가야하고, buffer 및 packet instance life cycle에 대한 고민을 해야함
                    BasePacket packet = MakePacket(ref packet_buffer, packet_size);

                    if (null != packet)
                    {
                        list_packet.Add(packet);
                    }
                    else
                    {
                        Log.Write($"MakePacket() returned nullptr.");
                    }

                    Array.Clear(packet_buffer, 0, packet_buffer.Length);

                    remain_bytes -= packet_size;
                }

                Receive();

                foreach (BasePacket packet in list_packet)
                {
                    sc_move move_packet = (sc_move)packet;

                    //Log.Write($"received. _move_client_id : {move_packet._move_client_id}, _x : {move_packet._x}, _y : {move_packet._y}");

                    if (move_packet._x == id) // client 내부 id랑 서버 id랑 다름... 쓸모없지만 대략적인 latency 확인용 => x가 id였구나. 임시 해결
                    {
                        int send_count = move_packet._y;

                        if (send_count == Program.MAX_SEND_COUNT_FOR_CLIENT)
                        {
                            Log.Write($"received last. _move_client_id : {move_packet._move_client_id}, _x : {move_packet._x}, _y : {move_packet._y}");
                        }

                        long send_time = 0;
                        if (map_send_history.TryGetValue(send_count, out send_time))
                        {
                            long now = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
                            long elapsed_time = now - send_time;

                            if (elapsed_time >= Program.LATENCY_LIMIT_IN_MS)
                            {
                                Log.Write($"latency limit over. id : {id}. elapsed time : {elapsed_time}");
                            }
                        }
                        else
                        {
                            Log.Write($"send history not found. id : {id}. send count : {send_count}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is ObjectDisposedException || ex is InvalidOperationException)
                {
                    Log.Write($"client disconnected(OnReceive()). id : {id}");
                }
            }
        }

        private bool CanMakePacket(ref byte[] packet_buffer, ref int packet_size)
        {
            int current_size = 0;

            if (false == _temp_buffer.Empty())
            {
                Log.Write($"temp buffer is not empty!!");

                if (false == _temp_buffer.Pop(new ArraySegment<byte>(packet_buffer, 0, packet_buffer.Length), sizeof(int)))
                {
                    Log.Write($"get sizeof(int) failed in temp buffer.");
                    return false;
                }

                current_size = sizeof(int);
                current_size += _temp_buffer.PopAll(new ArraySegment<byte>(packet_buffer, current_size, packet_buffer.Length - current_size));
            }
            else
            {
                if (_read_buffer.Empty())
                {
                    Log.Write($"read buffer empty.");
                    return false;
                }

                if (false == _read_buffer.Pop(new ArraySegment<byte>(packet_buffer, 0, packet_buffer.Length), sizeof(int)))
                {
                    Log.Write($"get sizeof(int) failed in read buffer.");
                    return false;
                }

                current_size = sizeof(int);
            }

            packet_size = BitConverter.ToInt32(packet_buffer, 0);

            if (false == _read_buffer.Pop(new ArraySegment<byte>(packet_buffer, current_size, packet_buffer.Length - current_size), packet_size - current_size))
            {
                Log.Write($"get (packet_size({packet_size}) - current_size({current_size})) failed in read buffer.");
                return false;
            }

            return true;
        }

        private BasePacket MakePacket(ref byte[] packet_buffer, int packet_size) // 지금은 packet_size를 안써도, 가변 size의 패킷들이 있어서 나중에 필요함.
        {
            // todo : 생성자에 size집어넣는거 빼기. size는 보낼때 결정되야함... list형식이나 chat같은 패킷땜에

            PacketID packet_id = (PacketID)BitConverter.ToInt32(packet_buffer, sizeof(int));

            BasePacket packet = null;

            switch (packet_id)
            {
                case PacketID.sc_move:
                    packet = new sc_move();
                    packet.Deserialize(ref packet_buffer);
                    break;
                default:
                    break;
            }

            return packet;
        }
    }
}
