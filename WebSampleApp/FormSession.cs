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
        private HttpSession.RequestData _reqData;
        private string _lastUrl;
        private string _lastMethod;
        private int _lastUrlCount;

        private HttpSession Session { get { return (HttpSession)_reqData.Session; } }

        public FormSession(HttpSession.RequestData reqData) {
            InitializeComponent();

            _reqData = reqData;
            numExpiry.Value = Session.ExpireSecs;
            labelHost.Text = reqData.Context.Request.UserHostName;
        }

        private void numExpiry_ValueChanged(object sender, EventArgs e) {
            Session.ExpireSecs = (int)numExpiry.Value;
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

        public IEnumerable<string> GetClientValues() {
            if (InvokeRequired) {
                return (IEnumerable<string>)Invoke(new Func<IEnumerable<string>>(GetClientValues));
            }
            return new string[] { textClient1.Text, textClient2.Text, textClient3.Text };
        }

        public void ShowClientValues(IEnumerable<string> vals) {
            if (InvokeRequired) {
                BeginInvoke(new Action<IEnumerable<string>>(ShowClientValues), vals);
                return;
            }
            var values = vals.ToArray();
            if (values.Length > 0) textClient1.Text = values[0];
            if (values.Length > 1) textClient2.Text = values[1];
            if (values.Length > 2) textClient3.Text = values[2];
        }
    }
}
