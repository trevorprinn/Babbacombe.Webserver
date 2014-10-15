using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Babbacombe.Webserver;

namespace SessionCookieTest {
    class Program {
        static void Main(string[] args) {
            try {
                Debug.Listeners.Add(new ConsoleTraceListener());
                using (var ws = new HttpServer()) {
                    ws.SessionType = typeof(TestSession);
                    ws.TrackSessions = true;
                    ws.Start();
                    Console.ReadLine();
                }
            } catch (Exception ex) {
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
            }
        }

        public class TestSession : HttpSession {
            private int _count;
            protected override void Respond() {
                Response = string.Format("<html><body>{0}</body></html>", ++_count);
            }
        }
    }
}
