using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using Babbacombe.Logger;

namespace Babbacombe.WebSampleApp {
    static class Program {

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            // Make sure all exceptions are logged.
            var lastChanceHandler = new LastChanceHandler();

            // Create a log file and route messages to it.
            var logFile = new LogFile(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                Application.CompanyName, Application.ProductName, Application.ProductName + ".log"));
            logFile.IsTraceListener = true;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FormServer());
        }
    }
}
