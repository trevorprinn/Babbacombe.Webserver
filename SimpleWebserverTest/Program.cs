using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Babbacombe.Webserver;

namespace SimpleWebserverTest {
    class Program {
        static void Main(string[] args) {
            try {
                Debug.Listeners.Add(new ConsoleTraceListener());
                using (var ws = new HttpServer()) {
                    ws.Start();
                    Console.ReadLine();
                    ws.Stop();
                    Console.ReadLine();
                }
            } catch (Exception ex) {
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
            }
        }
    }
}
