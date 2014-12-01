using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Babbacombe.WebSampleApp.Exercise {
    partial class FormSession : Form {
        private HttpSession _session;

        public FormSession(HttpSession session) {
            InitializeComponent();

            _session = session;
            numExpiry.Value = _session.ExpireSecs;
            labelHost.Text = _session.Context.Request.UserHostName;
        }

        private void numExpiry_ValueChanged(object sender, EventArgs e) {
            _session.ExpireSecs = (int)numExpiry.Value;
        }

        public void AddUrl(Uri url, string method) {
            if (InvokeRequired) {
                BeginInvoke(new Action<Uri, string>(AddUrl), url, method);
                return;
            }
            // Autoscroll the list box if it is showing the last url.
            int visibleCount = listUrls.ClientSize.Height / listUrls.ItemHeight;
            bool autoScroll = listUrls.TopIndex + visibleCount >= listUrls.Items.Count;
            listUrls.Items.Add(string.Format("{0} {1}", url, method));
            if (autoScroll) listUrls.TopIndex++;
        }

        public IEnumerable<string> GetServerValues() {
            if (InvokeRequired) {
                return (IEnumerable<string>)Invoke(new Func<IEnumerable<string>>(GetServerValues));
            }
            return new string[] { textItem1.Text, textItem2.Text, textItem3.Text };
        }
    }
}
