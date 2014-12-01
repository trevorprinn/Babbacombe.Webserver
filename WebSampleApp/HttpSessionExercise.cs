using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

using Babbacombe.Webserver;

namespace Babbacombe.WebSampleApp.Exercise {
    class HttpSession : Babbacombe.Webserver.HttpSession, IDisposable {
        private TimeSpan _expiryTime;
        private FormSession _form;

        protected override void OnCreated() {
            base.OnCreated();
            _expiryTime = base.ExpiryTime;
            FormServer.Instance.Invoke(new Action(() => {
                _form = new FormSession(this);
                _form.Text = "Session: " + SessionId;
                _form.Show();
            }));
        }

        protected override void Respond() {
            _form.AddUrl(Context.Request.Url, Context.Request.HttpMethod);
            base.Respond();
        }

        protected override TimeSpan ExpiryTime {
            get { return _expiryTime; }
        }

        public int ExpireSecs {
            get { return (int)_expiryTime.TotalSeconds; }
            set { _expiryTime = new TimeSpan(0, 0, value); }
        }

        public void Dispose() {
            if (_form != null) {
                _form.Invoke(new Action(() => _form.Close()));
                _form.Dispose();
                _form = null;
            }
        }

        protected override FileRequestedEventArgs OnFileRequested(string filename) {
            var ea = base.OnFileRequested(filename);
            if (ea.Handled) return ea;
            if (ea.Filename == Path.Combine(BaseFolder, "index.html")) {
                ea.Document = HttpPage.Create(ea.Filename, this, typeof(IndexPage));
            }
            return ea;
        }

        private class IndexPage : HttpPage {
            public IndexPage(XDocument data, HttpSession session) : base(data, session) {
                ReplaceValue("session", session.SessionId);
            }
        }
    }
}
