using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TestClientCS.Common;
using TestClientCS.Common.Network;

namespace TestClientCS
{
    class Program
    {
        public static readonly int CLIENT_COUNT = 10;

        public static readonly string SERVER_IP = "localhost";
        public static readonly int SERVER_PORT = 19001;

        public static readonly int MIN_SEND_COUNT_FOR_CLIENT = 30;
        public static readonly int MAX_SEND_COUNT_FOR_CLIENT = 30;

        public static readonly int SEND_BUFFER_SIZE = 1024;
        public static readonly int READ_BUFFER_SIZE = 1024;
        public static readonly int MIN_SEND_INTERVAL_IN_MS = 30;
        public static readonly int MAX_SEND_INTERVAL_IN_MS = 30;

        public static readonly int LATENCY_LIMIT_IN_MS = 1000;

        static readonly Random RANDOM = new Random();
        
        public static int CONNECTED_CLIENT_COUNT = 0;
        public static object LOCK_CONNECTED_CLIENT_COUNT = new object();

        public static List<Client> list_client = new List<Client>();

        static void Main(string[] args)
        {
            List<Task> list_task = new List<Task>();

            Task log_task = Log.Start("log.txt");
            list_task.Add(log_task);

            Log.Write($"############## CONFIG ##############");
            Log.Write($"client count => {CLIENT_COUNT}");
            Log.Write($"server => {SERVER_IP}:{SERVER_PORT}");
            Log.Write($"min/max send count => {MIN_SEND_COUNT_FOR_CLIENT}/{MAX_SEND_COUNT_FOR_CLIENT}");
            Log.Write($"min/max send interval(ms) => {MIN_SEND_INTERVAL_IN_MS}/{MAX_SEND_INTERVAL_IN_MS}");
            Log.Write($"send buffer size => {SEND_BUFFER_SIZE}");
            Log.Write($"read buffer size => {READ_BUFFER_SIZE}");
            Log.Write($"latency limit(ms) => {LATENCY_LIMIT_IN_MS}");
            Log.Write($"###################################");

            for (int client_id = 1; client_id <= CLIENT_COUNT; ++client_id)
            {
                Client client = new Client(client_id, RANDOM.Next(MIN_SEND_COUNT_FOR_CLIENT, MAX_SEND_COUNT_FOR_CLIENT + 1));
                client.Connect(SERVER_IP, SERVER_PORT);

                list_client.Add(client);
            }

            Task.WaitAll(list_task.ToArray());
        }

        public static void Start()
        {
            foreach (Client client in list_client)
            {
                client.Start();
            }
        }
    }
}
