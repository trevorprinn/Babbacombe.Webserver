using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Babbacombe.Webserver;

namespace SimpleHandlerTest {
    class Program {
        static void Main(string[] args) {
            try {
                Debug.Listeners.Add(new ConsoleTraceListener());
                using (var ws = new HttpServer()) {
                    ws.SessionType = typeof(TestSession);
                    ws.TrackSessions = true;
                    ws.TrapExceptions = true;
                    ws.Start();
                    ws.Exception += (object s, HttpServer.ExceptionEventArgs a) => {
                        Console.WriteLine(a.Ex.ToString());
                    };
                    Console.ReadLine();
                }
            } catch (Exception ex) {
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
            }
        }

    }

    public class TestSession : HttpSession {
    }
    
    public class Handler1 : HttpRequestHandler {
        private int _count;

        public void Test1() {
            Session.Response = string.Format("<html><body>{0}<br/>{1}</body></html>", Session.Context.Request.Url, ++_count);
        }
    }
}
