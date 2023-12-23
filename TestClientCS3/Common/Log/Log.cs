using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestClientCS.Common
{
    public static class Log
    {
        private static ConcurrentQueue<string> _queue = new ConcurrentQueue<string>();

        public static Task Start(string file_name)
        {
            return Task.Run(async () =>
            {
                UTF8Encoding utf8 = new UTF8Encoding(false);

                using (StreamWriter writer = new StreamWriter(file_name, false, utf8))
                {
                    writer.AutoFlush = true;

                    string message = string.Empty;

                    while (true)
                    {
                        if (_queue.TryDequeue(out message))
                        {
                            await writer.WriteLineAsync(message).ConfigureAwait(false);
                            Console.WriteLine($"{message}");
                            message = string.Empty;
                        }
                    }
                }
            });
        }

        public static void Write(string message)
        {
            _queue.Enqueue($"[{DateTime.Now.ToString("yy/MM/dd H:mm:ss.fff")}][{Thread.CurrentThread.ManagedThreadId}] {message}");
        }
    }
}
