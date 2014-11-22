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

        public FormServer() {
            InitializeComponent();

            LogFile.Log("Started: {0}", Application.ProductVersion.ToString());

            var appDefs = new HttpApplicationDef[] {
                new HttpApplicationDef("test", typeof(Test.HttpSession), 80, "Test"),
                new HttpApplicationDef("uploader", typeof(Uploader.HttpSession), 80, "Uploader")
            };
            _server = new HttpAppServer(appDefs);
            Disposed += (s, e) => {
                if (_server != null) {
                    _server.Dispose();
                    _server = null;
                    LogFile.Log("Server disposed");
                }
            };
            _server.SessionType = typeof(HttpSession);
            _server.TrackSessions = true;
            _server.Exception += _server_Exception;

            CreateHandle();

            LogFile.Log("Server started");
            _server.Start();
        }

        void _server_Exception(object sender, HttpServer.ExceptionEventArgs e) {
            LogFile.Log(e.Ex.ToString());
        }
    }
}
