using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Babbacombe.Webserver;

namespace Babbacombe.WebSampleApp {
    public partial class FormServer : Form {
        private HttpAppServer _server;
        public static FormServer Instance { get; private set; }

        public FormServer() {
            InitializeComponent();

            Instance = this;

            LogFile.Log("Started: {0}", Application.ProductVersion.ToString());

            var appDefs = new HttpApplicationDef[] {
                new HttpApplicationDef("test", typeof(Test.HttpSession), 80, "Test"),
                new HttpApplicationDef("uploader", typeof(Uploader.HttpSession), 80, "Uploader")
            };
            _server = new HttpAppServer(appDefs);
            _server.TrackSessions = true;
            // Check for expiry every 10 secs, rather than 5 mins.
            _server.SessionsExpiryInterval = new TimeSpan(0, 0, 10);
            _server.Exception += _server_Exception;

            LogFile.Log("Server started");
            _server.Start();
        }

        void _server_Exception(object sender, HttpServer.ExceptionEventArgs e) {
            LogFile.Log(e.Ex.ToString());
        }
    }
}
