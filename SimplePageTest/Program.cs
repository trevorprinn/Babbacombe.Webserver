using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using Babbacombe.Webserver;

namespace SimplePageTest {
    class Program {
        static void Main(string[] args) {
            System.Diagnostics.Debug.Listeners.Add(new System.Diagnostics.ConsoleTraceListener());
            using (var ws = new HttpRPiTemperatureServer()) {
                ws.Start();
                Console.ReadLine();
            }
        }
    }

    class HttpRPiTemperatureServer : HttpServer {
        public HttpRPiTemperatureServer() {
            SessionType = typeof(HttpRPiTemperatureSession);
            BaseFolder = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Pages");
            TrackSessions = true;
        }
    }

    class HttpRPiTemperatureSession : HttpSession {
#if NORPI
        private int _temp = 1;
#endif

        protected override void Respond() {
            if (QueryItems.Contains("update")) {
                var data = new XElement("data", new XElement("temperature", new XAttribute("value", getTemperature().ToString("F02"))));
                Context.Response.ContentType = "text/xml";
                SetXmlResponse(data);
                return;
            }

            if (Context.Request.Url.AbsolutePath == "/temperature.html") {
                var page = HttpPage.Create(Path.Combine(BaseFolder, "temperature.html"), this);
                page.ReplaceValue("temperature", getTemperature().ToString("F02"));
                page.Send();
                return;
            }

            base.Respond();
        }

        private decimal getTemperature() {
#if NORPI
            return _temp++;
#else
            using (var s = new StreamReader("/sys/class/thermal/thermal_zone0/temp")) {
                return Convert.ToDecimal(s.ReadLine()) / 1000m;
            }
#endif
        }
    }
}
