using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Babbacombe.Webserver;

namespace SimpleWebserverTest {
    class Program {
        static void Main(string[] args) {
            using (var ws = new HttpServer()) {
                ws.Start();
                Console.ReadLine();
            }
        }
    }
}
