using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Babbacombe.Logger;
using Babbacombe.Webserver;

namespace Babbacombe.WebSampleApp {
    public partial class FormServer : Form {
        private HttpAppServer _server;
        public static FormServer Instance { get; private set; }

        public FormServer() {
            InitializeComponent();

            Instance = this;

            LogFile.Log("Started: {0}", Application.ProductVersion.ToString());

            /* Set up 2 applications to be run by the server.
             * Urls starting exercise/ are handled by Exercise.HttpSession objects using files in the Exercise subfolder.
             * Urls starting uploader/ are handled by the Uploader.HttpSession objects using files in the Uploader subfolder.
             */
            var appDefs = new HttpApplicationDef[] {
                new HttpApplicationDef("exercise", typeof(Exercise.HttpSession), 80, "Exercise"),
                new HttpApplicationDef("uploader", typeof(Uploader.HttpSession), 80, "Uploader")
            };
            // Set up the web server
            _server = new HttpAppServer(appDefs);
            // Check for expiry every 10 secs, rather than 5 mins.
            _server.SessionsExpiryInterval = new TimeSpan(0, 0, 10);
            // Log any exceptions
            _server.Exception += _server_Exception;

            LogFile.Log("Server started");
            _server.Start();

            // NB The web server is stopped and disposed of in FormServer.Dispose (in FormServer.Designer.cs).
        }

        void _server_Exception(object sender, HttpServer.ExceptionEventArgs e) {
            LogFile.Log(e.Ex.ToString());
        }
    }
}
