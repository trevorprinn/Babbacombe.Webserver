using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        }
    }

    class HttpRPiTemperatureSession : HttpSession {
        protected override void Respond() {
            var page = HttpPage.Create(Path.Combine(BaseFolder, "temperature.xml"), this);
            page.ReplaceValue("temperature", getTemperature().ToString("F02"));
            page.Send();
        }

        private decimal getTemperature() {
            using (var s = new StreamReader("/sys/class/thermal/thermal_zone0/temp")) {
                return Convert.ToDecimal(s.ReadLine()) / 1000m;
            }
        }
    }
}
