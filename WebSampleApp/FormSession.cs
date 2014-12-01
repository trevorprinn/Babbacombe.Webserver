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
        private string _lastUrl;
        private string _lastMethod;
        private int _lastUrlCount;

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
            if (url.ToString() == _lastUrl && _lastMethod == method) {
                var ix = listUrls.TopIndex;
                listUrls.Items.RemoveAt(listUrls.Items.Count - 1);
                listUrls.Items.Add(string.Format("{0} {1} {2}", url, method, ++_lastUrlCount));
                listUrls.TopIndex = ix;
                return;
            }

            // Autoscroll the list box if it is showing the last url.
            int visibleCount = listUrls.ClientSize.Height / listUrls.ItemHeight;
            bool autoScroll = listUrls.TopIndex + visibleCount >= listUrls.Items.Count;
            listUrls.Items.Add(string.Format("{0} {1}", url, method));
            if (autoScroll) listUrls.TopIndex++;
            _lastUrl = url.ToString();
            _lastMethod = method;
            _lastUrlCount = 1;
        }

        public IEnumerable<string> GetServerValues() {
            if (InvokeRequired) {
                return (IEnumerable<string>)Invoke(new Func<IEnumerable<string>>(GetServerValues));
            }
            return new string[] { textItem1.Text, textItem2.Text, textItem3.Text };
        }
    }
}
