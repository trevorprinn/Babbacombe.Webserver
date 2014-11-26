using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Babbacombe.WebSampleApp {

    /// <summary>
    /// A list box that displays the first line of any Trace messages.
    /// </summary>
    public partial class ListenerListBox : ListBox {
        private Listener _listener;

        public ListenerListBox() {
            InitializeComponent();

            // Create the handle here to ensure that InvokeRequired works
            // before the form is shown.
            CreateHandle();

            _listener = new Listener(this);
            Disposed += (s, e) => {
                if (_listener != null) {
                    _listener.Dispose();
                    _listener = null;
                }
            };
        }

        private void displayMessage(string message) {
            if (InvokeRequired) {
                BeginInvoke(new Action<string>(displayMessage), message);
                return;
            }
            int visibleCount = ClientSize.Height / ItemHeight;
            bool autoScroll = TopIndex + visibleCount >= Items.Count;
            Items.Add(message);
            if (autoScroll) TopIndex++;
        }

        private class Listener : System.Diagnostics.TraceListener {
            private ListenerListBox _listBox;
            private StringBuilder _buf = new StringBuilder();
            
            public Listener(ListenerListBox listBox) {
                _listBox = listBox;
                System.Diagnostics.Trace.Listeners.Add(this);
            }

            public override void Write(string message) {
                lock (this) _buf.Append(message);
            }

            public override void WriteLine(string message) {
                lock (this) {
                    if (_buf.Length > 0) message = _buf.ToString() + message;
                    _buf.Clear();
                    // Just take the first line.
                    if (message.Contains("\n")) message = message.Split('\r', '\n')[0];
                    _listBox.displayMessage(message);
                }
            }
        }
    }
}
