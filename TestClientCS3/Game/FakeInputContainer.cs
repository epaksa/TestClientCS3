using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestClientCS.Common.Network;

namespace TestClientCS3.Game
{
    public enum FakeInputType : int
    {
        move,
        attack,
        disconnect
    }

    public class FakeInput
    {
        public Client _client { get; set; }
        public FakeInputType _type { get; set; }
        public long _time_to_execute { get; set; }
        
        public FakeInput(Client client, FakeInputType type, long time)
        {
            _client = client;
            _type = type;
            _time_to_execute = time;
        }
    }

    internal static class FakeInputContainer
    {
        private static Dictionary<int, FakeInput> _map = new Dictionary<int, FakeInput>(); // key : client id

        public static bool Exist(int client_id)
        {
            return _map.ContainsKey(client_id);
        }
        
        public static void PushInput(FakeInput input)
        {
            _map.Add(input._client._id, input);
        }

        public static FakeInput? GetInput(int client_id)
        {
            FakeInput? input;
            if (_map.TryGetValue(client_id, out input))
            {
                return input;
            }

            return null;
        }

        public static void RemoveInput(int client_id)
        {
            _map.Remove(client_id);
        }
    }
}
