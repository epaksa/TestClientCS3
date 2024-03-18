using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestClientCS.Common.Network.Packet
{
    public enum PacketID : int
    {
        none,
        cs_login,
        sc_login,
        sc_welcome,
        cs_move,
        sc_move,
        cs_logout,
        sc_logout
    }

    public class PacketContext
    {
        public Client _client;
        public BasePacket _packet;

        public PacketContext(Client client, BasePacket packet)
        {
            _client = client;
            _packet = packet;
        }
    }

    public class BasePacket
    {
        public int _size;
        public PacketID _packet_id;

        public BasePacket()
        {
            _size = sizeof(int) + sizeof(PacketID);
        }

        public virtual int Serialize(ref byte[] result)
        {
            int result_size = 0;

            byte[] _size_arr = BitConverter.GetBytes(_size);
            Array.Copy(_size_arr, 0, result, result_size, _size_arr.Length);
            result_size += _size_arr.Length;

            byte[] _packet_id_arr = BitConverter.GetBytes((int)_packet_id);
            Array.Copy(_packet_id_arr, 0, result, result_size, _packet_id_arr.Length);
            result_size += _packet_id_arr.Length;

            return result_size;
        }

        public virtual int Deserialize(ref byte[] data)
        {
            int result_size = 0;

            _size = BitConverter.ToInt32(data, result_size);
            result_size += sizeof(int);

            _packet_id = (PacketID)BitConverter.ToInt32(data, result_size);
            result_size += sizeof(PacketID);

            return result_size;
        }
    }

    public class cs_login : BasePacket
    {
        public int _client_id;

        public cs_login() : base()
        {
            _size += sizeof(int);
            _packet_id = PacketID.cs_login;
        }

        public override int Serialize(ref byte[] result)
        {
            int result_size = 0;

            result_size += base.Serialize(ref result);

            byte[] _client_id_arr = BitConverter.GetBytes(_client_id);
            Array.Copy(_client_id_arr, 0, result, result_size, _client_id_arr.Length);
            result_size += _client_id_arr.Length;

            return result_size;
        }

        public override int Deserialize(ref byte[] data)
        {
            int result_size = 0;

            result_size += base.Deserialize(ref data);

            _client_id = BitConverter.ToInt32(data, result_size);
            result_size += sizeof(int);

            return result_size;
        }
    }

    public class sc_login : BasePacket
    {
        public class ClientPos
        {
            public int _client_id;
            public int _x;
            public int _y;
        }

        public ClientPos _my_info = new ClientPos();
        public List<ClientPos> _list_client = new List<ClientPos>();

        public sc_login() : base()
        {
            _size += sizeof(int) + sizeof(int) + sizeof(int); // size of _my_info
            _size += sizeof(int); // size of _list_client
            _size += (sizeof(int) + sizeof(int) + sizeof(int)) * _list_client.Count; // size of _list_client's data

            _packet_id = PacketID.sc_login;
        }

        public override int Serialize(ref byte[] result)
        {
            int result_size = 0;

            result_size += base.Serialize(ref result);

            byte[] _my_info_client_id_arr = BitConverter.GetBytes(_my_info._client_id);
            Array.Copy(_my_info_client_id_arr, 0, result, result_size, _my_info_client_id_arr.Length);
            result_size += _my_info_client_id_arr.Length;

            byte[] _my_info_x_arr = BitConverter.GetBytes(_my_info._x);
            Array.Copy(_my_info_x_arr, 0, result, result_size, _my_info_x_arr.Length);
            result_size += _my_info_x_arr.Length;

            byte[] _my_info_y_arr = BitConverter.GetBytes(_my_info._y);
            Array.Copy(_my_info_y_arr, 0, result, result_size, _my_info_y_arr.Length);
            result_size += _my_info_y_arr.Length;

            byte[] _list_client_size_arr = BitConverter.GetBytes(_list_client.Count);
            Array.Copy(_list_client_size_arr, 0, result, result_size, _list_client_size_arr.Length);
            result_size += _list_client_size_arr.Length;

            foreach (ClientPos data in _list_client)
            {
                byte[] data_client_id_arr = BitConverter.GetBytes(data._client_id);
                Array.Copy(data_client_id_arr, 0, result, result_size, data_client_id_arr.Length);
                result_size += data_client_id_arr.Length;

                byte[] data_x_arr = BitConverter.GetBytes(data._x);
                Array.Copy(data_x_arr, 0, result, result_size, data_x_arr.Length);
                result_size += data_x_arr.Length;

                byte[] data_y_arr = BitConverter.GetBytes(data._y);
                Array.Copy(data_y_arr, 0, result, result_size, data_y_arr.Length);
                result_size += data_y_arr.Length;
            }

            return result_size;
        }

        public override int Deserialize(ref byte[] data)
        {
            int result_size = 0;

            result_size += base.Deserialize(ref data);

            _my_info._client_id = BitConverter.ToInt32(data, result_size);
            result_size += sizeof(int);

            _my_info._x = BitConverter.ToInt32(data, result_size);
            result_size += sizeof(int);

            _my_info._y = BitConverter.ToInt32(data, result_size);
            result_size += sizeof(int);

            int _list_client_size = BitConverter.ToInt32(data, result_size);
            result_size += sizeof(int);

            for (int i = 0; i<_list_client_size; ++i)
            {
                ClientPos data_in_list = new ClientPos();

                data_in_list._client_id = BitConverter.ToInt32(data, result_size);
                result_size += sizeof(int);

                data_in_list._x = BitConverter.ToInt32(data, result_size);
                result_size += sizeof(int);

                data_in_list._y = BitConverter.ToInt32(data, result_size);
                result_size += sizeof(int);

                _list_client.Add(data_in_list);
            }

            return result_size;
        }
    }

    public class sc_welcome : BasePacket
    {
        public int _client_id;
        public int _x;
        public int _y;

        public sc_welcome() : base()
        {
            _size += sizeof(int) + sizeof(int) + sizeof(int);
            _packet_id = PacketID.sc_welcome;
        }

        public override int Serialize(ref byte[] result)
        {
            int result_size = 0;

            result_size += base.Serialize(ref result);

            byte[] _client_id_arr = BitConverter.GetBytes(_client_id);
            Array.Copy(_client_id_arr, 0, result, result_size, _client_id_arr.Length);
            result_size += _client_id_arr.Length;

            byte[] _x_arr = BitConverter.GetBytes(_x);
            Array.Copy(_x_arr, 0, result, result_size, _x_arr.Length);
            result_size += _x_arr.Length;

            byte[] _y_arr = BitConverter.GetBytes(_y);
            Array.Copy(_y_arr, 0, result, result_size, _y_arr.Length);
            result_size += _y_arr.Length;

            return result_size;
        }

        public override int Deserialize(ref byte[] data)
        {
            int result_size = 0;

            result_size += base.Deserialize(ref data);

            _client_id = BitConverter.ToInt32(data, result_size);
            result_size += sizeof(int);

            _x = BitConverter.ToInt32(data, result_size);
            result_size += sizeof(int);

            _y = BitConverter.ToInt32(data, result_size);
            result_size += sizeof(int);

            return result_size;
        }
    }

    public class cs_move : BasePacket 
    {
        public int _x;
        public int _y;

        public cs_move() : base()
        {
            _size += sizeof(int) + sizeof(int);
            _packet_id = PacketID.cs_move;
        }

        public override int Serialize(ref byte[] result)
        {
            int result_size = 0;

            result_size += base.Serialize(ref result);

            byte[] _x_arr = BitConverter.GetBytes(_x);
            Array.Copy(_x_arr, 0, result, result_size, _x_arr.Length);
            result_size += _x_arr.Length;

            byte[] _y_arr = BitConverter.GetBytes(_y);
            Array.Copy(_y_arr, 0, result, result_size, _y_arr.Length);
            result_size += _y_arr.Length;

            return result_size;
        }

        public override int Deserialize(ref byte[] data)
        {
            int result_size = 0;

            result_size += base.Deserialize(ref data);

            _x = BitConverter.ToInt32(data, result_size);
            result_size += sizeof(int);

            _y = BitConverter.ToInt32(data, result_size);
            result_size += sizeof(int);

            return result_size;
        }
    }

    public class sc_move : BasePacket
    {
        public int _move_client_id;
        public int _x;
        public int _y;

        public sc_move() : base()
        {
            _size += sizeof(int) + sizeof(int) + sizeof(int);
            _packet_id = PacketID.sc_move;
        }

        public override int Serialize(ref byte[] result)
        {
            int result_size = 0;

            result_size += base.Serialize(ref result);

            byte[] _move_client_id_arr = BitConverter.GetBytes(_move_client_id);
            Array.Copy(_move_client_id_arr, 0, result, result_size, _move_client_id_arr.Length);
            result_size += _move_client_id_arr.Length;

            byte[] _x_arr = BitConverter.GetBytes(_x);
            Array.Copy(_x_arr, 0, result, result_size, _x_arr.Length);
            result_size += _x_arr.Length;

            byte[] _y_arr = BitConverter.GetBytes(_y);
            Array.Copy(_y_arr, 0, result, result_size, _y_arr.Length);
            result_size += _y_arr.Length;

            return result_size;
        }

        public override int Deserialize(ref byte[] data)
        {
            int result_size = 0;

            result_size += base.Deserialize(ref data);

            _move_client_id = BitConverter.ToInt32(data, result_size);
            result_size += sizeof(int);

            _x = BitConverter.ToInt32(data, result_size);
            result_size += sizeof(int);

            _y = BitConverter.ToInt32(data, result_size);
            result_size += sizeof(int);

            return result_size;
        }
    }

    public class cs_logout : BasePacket
    {
        public int _client_id;

        public cs_logout() : base()
        {
            _size += sizeof(int);
            _packet_id = PacketID.cs_logout;
        }

        public override int Serialize(ref byte[] result)
        {
            int result_size = 0;

            result_size += base.Serialize(ref result);

            byte[] _client_id_arr = BitConverter.GetBytes(_client_id);
            Array.Copy(_client_id_arr, 0, result, result_size, _client_id_arr.Length);
            result_size += _client_id_arr.Length;

            return result_size;
        }

        public override int Deserialize(ref byte[] data)
        {
            int result_size = 0;

            result_size += base.Deserialize(ref data);

            _client_id = BitConverter.ToInt32(data, result_size);
            result_size += sizeof(int);

            return result_size;
        }
    }

    public class sc_logout : BasePacket
    {
        public int _client_id;

        public sc_logout() : base()
        {
            _size += sizeof(int);
            _packet_id = PacketID.sc_logout;
        }

        public override int Serialize(ref byte[] result)
        {
            int result_size = 0;

            result_size += base.Serialize(ref result);

            byte[] _client_id_arr = BitConverter.GetBytes(_client_id);
            Array.Copy(_client_id_arr, 0, result, result_size, _client_id_arr.Length);
            result_size += _client_id_arr.Length;

            return result_size;
        }

        public override int Deserialize(ref byte[] data)
        {
            int result_size = 0;

            result_size += base.Deserialize(ref data);

            _client_id = BitConverter.ToInt32(data, result_size);
            result_size += sizeof(int);

            return result_size;
        }
    }
}
