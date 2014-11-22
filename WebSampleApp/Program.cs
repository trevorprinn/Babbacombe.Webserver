using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Babbacombe.WebSampleApp {
    static class Program {
        // Write any messages to a log file under App Data
        private static LogFile _logFile = new LogFile();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            // Ensure all exceptions get logged
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FormServer());
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
            LogFile.Log("Unhandled Exception!!!");
            if (e.IsTerminating) LogFile.Log("App is terminating");
            LogFile.Log(e.ExceptionObject.ToString());
            MessageBox.Show("Unhandled Exception");
        }

        static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e) {
            LogFile.Log("Unexpected Thread Exception!!!");
            LogFile.Log(e.ToString());
            MessageBox.Show("Unexpected Exception");
        }
    }
}
