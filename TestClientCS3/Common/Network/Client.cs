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
        TcpClient _client = new TcpClient();

        int _id = 0;
        int _current_send_count = 1;
        ConcurrentDictionary<int, long> _map_send_history = new ConcurrentDictionary<int, long>(); // key : send count number, value : send time

        RingBuffer _temp_buffer = new RingBuffer();
        RingBuffer _read_buffer = new RingBuffer();

        public void Send()
        {
            try
            {
                cs_move packet = new cs_move();
                packet._x = _id;
                packet._y = _current_send_count;

                byte[] buffer = new byte[Program.SEND_BUFFER_SIZE];
                int buffer_length = packet.Serialize(ref buffer);

                // commented for broadcast test
                //Log.Write($"client sends. id : {id}, x : {packet._x}, y : {packet._y}");

                // 여기서부터 시간재는게 맞을까? 아니면 OnSend()에서 재는게 맞을까..?
                _map_send_history.TryAdd(_current_send_count, new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds());

                _client.GetStream().BeginWrite(buffer, 0, buffer_length, OnSend, packet);
            }
            catch (Exception ex)
            {
                if (ex is ObjectDisposedException || ex is InvalidOperationException)
                {
                    Log.Write($"client disconnected(Send()). id : {_id}");
                }
            }
        }

        public void Receive()
        {
            try
            {
                _read_buffer.Clear();
                _client.GetStream().BeginRead(_read_buffer.Data(), 0, _read_buffer.GetLength() - 1, OnReceive, null);
            }
            catch (Exception ex)
            {
                if (ex is ObjectDisposedException || ex is InvalidOperationException)
                {
                    Log.Write($"client disconnected(Receive()). id : {_id}");
                }
            }
        }

        public void Connect(string ip, int port)
        {
            _client.BeginConnect(ip, port, OnConnect, null);
        }

        public void Start()
        {
            Receive();
        }

        private void OnConnect(IAsyncResult ar)
        {
            _client.EndConnect(ar);

            bool ready = false;

            lock (Program.LOCK_CONNECTED_CLIENT_COUNT)
            {
                ++(Program.CONNECTED_CLIENT_COUNT);
                
                if (Program.CLIENT_COUNT == Program.CONNECTED_CLIENT_COUNT)
                {
                    ready = true;
                }
            }

            if (ready)
            {
                Program.Start();
            }
        }

        private void OnSend(IAsyncResult ar)
        {
            try
            {
                _client.GetStream().EndWrite(ar);

                cs_move sent_packet = (cs_move)ar.AsyncState;

                ++_current_send_count;

                // commented for broadcast test
                //Log.Write($"client sent. id : {id}. current send count : {current_send_count}, x : {sent_packet._x}, y : {sent_packet._y}");

                Thread.Sleep(1); // 이거 빼니까... boost asio에서 crash남.. release로 빌드해도 남

                Send();
            }
            catch (Exception ex)
            {
                if (ex is ObjectDisposedException || ex is InvalidOperationException)
                {
                    Log.Write($"client disconnected(OnSend()). id : {_id}");
                }
            }
        }

        private void OnReceive(IAsyncResult ar)
        {
            try
            {
                int read_bytes = _client.GetStream().EndRead(ar);

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

                    BasePacket packet = MakePacket(ref packet_buffer, packet_size);

                    if (null != packet)
                    {
                        if (packet._packet_id == PacketID.sc_login)
                        {
                            _id = ((sc_login)packet)._client_id;
                        }

                        Program.ZONE?.PushPacket(packet);
                    }
                    else
                    {
                        Log.Write($"MakePacket() returned nullptr.");
                    }

                    Array.Clear(packet_buffer, 0, packet_buffer.Length);

                    remain_bytes -= packet_size;
                }

                Receive();
            }
            catch (Exception ex)
            {
                if (ex is ObjectDisposedException || ex is InvalidOperationException)
                {
                    Log.Write($"client disconnected(OnReceive()). id : {_id}");
                }
            }
        }

        private bool CanMakePacket(ref byte[] packet_buffer, ref int packet_size)
        {
            int current_size = 0;

            if (false == _temp_buffer.Empty())
            {
                //Log.Write($"temp buffer is not empty!!");

                current_size += _temp_buffer.PopAll(new ArraySegment<byte>(packet_buffer, current_size, packet_buffer.Length));
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
                    //Log.Write($"get sizeof(int) failed in read buffer.");
                    return false;
                }
                //else
                //{
                //    Log.Write($"pop 1 => id : {id}, current read index : {_read_buffer._read_index}");
                //}

                current_size = sizeof(int);
            }

            packet_size = BitConverter.ToInt32(packet_buffer, 0);

            if (false == _read_buffer.Pop(new ArraySegment<byte>(packet_buffer, current_size, packet_buffer.Length - current_size), packet_size - current_size))
            {
                //Log.Write($"get (packet_size({packet_size}) - current_size({current_size})) failed in read buffer.");
                return false;
            }
            //else
            //{
            //    Log.Write($"pop 2 => id : {id}, current read index : {_read_buffer._read_index}");
            //}

            return true;
        }

        private BasePacket MakePacket(ref byte[] packet_buffer, int packet_size) // 지금은 packet_size를 안써도, 가변 size의 패킷들이 있어서 나중에 필요함.
        {
            // todo : 생성자에 size집어넣는거 빼기. size는 보낼때 결정되야함... list형식이나 chat같은 패킷땜에

            PacketID packet_id = (PacketID)BitConverter.ToInt32(packet_buffer, sizeof(int));

            BasePacket packet = null;

            switch (packet_id)
            {
                case PacketID.sc_login:
                    packet = new sc_login();
                    packet.Deserialize(ref packet_buffer);
                    break;
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
