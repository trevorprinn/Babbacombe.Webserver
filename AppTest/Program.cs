using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Babbacombe.Webserver;

namespace AppTest {

    /// <summary>
    ///  Test program that creates sessions for different App types.
    /// </summary>
    class Program {
        static void Main(string[] args) {
            string exeFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            var defs = new HttpApplicationDef[] {
                new HttpApplicationDef("App1", typeof(App1.HttpApp1Session), 80, Path.Combine(exeFolder, "App1")),
                new HttpApplicationDef("App2", typeof(App2.HttpApp2Session), 80, Path.Combine(exeFolder, "App2"))
            };
            using (var ws = new HttpAppServer(defs)) {
                ws.TrackSessions = true;
                ws.TrapExceptions = true;
                ws.Exception += (object sender, HttpServer.ExceptionEventArgs e) => {
                    Console.WriteLine(e.Ex.ToString());
                };
                ws.Start();
                Console.WriteLine("Webserver started");
                Console.ReadLine();
            }
        }
    }
}

namespace AppTest.App1 {
    public class HttpApp1Session : HttpSession {
    }

    public class Handler : HttpRequestHandler {
        public void Identify() {
            Session.Response = "<html><body>You called App1</body></html>";
        }
    }
}

namespace AppTest.App2 {
    public class HttpApp2Session : HttpSession {
    }

    public class Handler : HttpRequestHandler {
        public void Identify() {
            Session.Response = "<html><body>You called App2</body></html>";
        }
    }
}
