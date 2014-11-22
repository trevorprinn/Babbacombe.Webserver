using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Babbacombe.WebSampleApp {

    /// <summary>
    /// A simple logging class as a trace listener. Writes to a log file under the App Data folder.
    /// Messages sent to the log file or to Trace are output with the date/time.
    /// </summary>
    class LogFile : System.Diagnostics.TraceListener {
        private string _filename;
        private StringBuilder _buf = new StringBuilder();

        /// <summary>
        /// Creates a log file and makes it a trace listener.
        /// </summary>
        /// <param name="filename">
        /// By default, the name is %appdata%\CompanyName\ProductName\ProductName.txt
        /// </param>
        public LogFile(string filename = null) {
            if (filename == null) {
                filename = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Application.CompanyName, Application.ProductName, Application.ProductName + ".log");
            }
            _filename = filename;
            string folder = Path.GetDirectoryName(filename);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            System.Diagnostics.Trace.Listeners.Add(this);
        }

        public override void Write(string message) {
            // Store up any part lines until WriteLine is called.
            lock (this) _buf.Append(message);
        }

        public override void WriteLine(string message) {
            lock (this) {
                using (var s = new StreamWriter(_filename, true)) {
                    s.WriteLine(string.Format("{0:dd/MM/yyyy HH:mm:ss} {1}{2}", DateTime.Now, _buf.ToString(), message));
                    _buf.Clear();
                }
            }
        }

        /// <summary>
        /// Writes a message to all Trace listeners, including any LogFile objects.
        /// </summary>
        /// <param name="fmt"></param>
        /// <param name="args"></param>
        public static void Log(string fmt, params object[] args) {
            System.Diagnostics.Trace.WriteLine(string.Format(fmt, args));
        }
    }
}
