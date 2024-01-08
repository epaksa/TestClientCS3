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

    internal static class FakeInputGenerator
    {
        private static Dictionary<int, FakeInput> _map = new Dictionary<int, FakeInput>(); // key : client id

        public static bool RegisterInput(FakeInput input)
        {
            _map.ContainsKey()
        }
    }
}
