using TestClientCS.Common;
using TestClientCS.Common.Network;
using TestClientCS3.Game.Zone;

namespace TestClientCS
{
    class Program
    {
        public static readonly string LOG_FILE_NAME = "log.txt";
        public static readonly string MAP_FILE_NAME = "map.csv";

        public static readonly int CLIENT_COUNT = 10;

        public static readonly string SERVER_IP = "localhost";
        public static readonly int SERVER_PORT = 19001;

        public static readonly int SEND_BUFFER_SIZE = 1024;
        public static readonly int READ_BUFFER_SIZE = 1024;

        public static int CONNECTED_CLIENT_COUNT = 0; // client 모두 접속되면, 시작... 나중에 기능 동작 확인되면 삭제
        public static object LOCK_CONNECTED_CLIENT_COUNT = new object();

        public static List<Client> LIST_CLIENT = new List<Client>();
        public static Zone? ZONE = null;

        static void Main(string[] args)
        {
            List<Task> list_task = new List<Task>();

            Task log_task = Log.Start(LOG_FILE_NAME);
            list_task.Add(log_task);

            Log.Write($"############## CONFIG ##############");
            Log.Write($"client count => {CLIENT_COUNT}");
            Log.Write($"server => {SERVER_IP}:{SERVER_PORT}");
            Log.Write($"send buffer size => {SEND_BUFFER_SIZE}");
            Log.Write($"read buffer size => {READ_BUFFER_SIZE}");
            Log.Write($"###################################");

            ZONE = new Zone(MAP_FILE_NAME);
            Task zone_task = ZONE.Start();
            list_task.Add(zone_task);

            for (int client_id = 1; client_id <= CLIENT_COUNT; ++client_id)
            {
                Client client = new Client();
                client.Connect(SERVER_IP, SERVER_PORT);

                LIST_CLIENT.Add(client);
            }

            Task.WaitAll(list_task.ToArray());
        }

        public static void Start()
        {
            Parallel.ForEach(LIST_CLIENT, client => {
                client.Start(); 
            });
        }
    }
}
