using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestClientCS3.Game
{
    internal class Zone
    {
        //private 타일[][]
        // tile -> object, 벽여부
        // object -> player, monster의 base class

        public Zone(string map_file_name) 
        {
            string[] list_line = File.ReadAllLines(map_file_name);

            foreach (string line in list_line)
            {
                Console.WriteLine(line); // 바꾸기
            }
        }
    }
}
