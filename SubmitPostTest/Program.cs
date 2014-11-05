using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Babbacombe.Webserver;

namespace SubmitPostTest {
    class Program {
        static void Main(string[] args) {
            using (var server = new HttpServer()) {
                server.SessionType = typeof(Session);
                server.Start();
                Console.ReadLine();
            }
        }
    }

    class Session : HttpSession {
    }

    class Handler : HttpRequestHandler {
        public void Submitted() {
            var page = HttpPage.Create("reply.html", Session);
            foreach (var item in GetPostedItems()) {
                page.ReplaceValue(item.Name, item.Value);
            }
            page.Send();
        }
    }
}
