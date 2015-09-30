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
    class HttpSession : Babbacombe.Webserver.HttpSession {
        private TimeSpan _expiryTime;
        private FormSession _form;

        protected override void OnCreated() {
            base.OnCreated();
            _expiryTime = base.ExpiryTime;
            /* Get the data about this specific thread, so that it can be passed
             * to the form. Trying to retrieve it in the form code will fail because
             * it will be running on a different thread. */
            var reqData = GetRequestData();
            FormServer.Instance.Invoke(new Action(() => {
                _form = new FormSession(reqData);
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

        protected override void Dispose(bool disposing) {
            if (_form != null) {
                _form.Invoke(new Action(() => _form.Close()));
                _form.Dispose();
                _form = null;
            }
            base.Dispose(disposing);
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
                DefaultTagName = "name";
                ReplaceValue("session", session.SessionId);
                var vals = session.GetServerValues().ToArray();
                ReplaceValue("item1", vals[0]);
                ReplaceValue("item2", vals[1]);
                ReplaceValue("item3", vals[2]);
                vals = session.GetClientValues().ToArray();
                ReplaceValue("Get1", vals[0]);
                ReplaceValue("Get2", vals[1]);
                ReplaceValue("Get3", vals[2]);
            }
        }

        public IEnumerable<string> GetServerValues() {
            if (_form == null) return null;
            return _form.GetServerValues();
        }

        public IEnumerable<string> GetClientValues() {
            if (_form == null) return null;
            return _form.GetClientValues();
        }

        public void ShowClientValues(IEnumerable<string> values) {
            if (_form != null) _form.ShowClientValues(values);
        }
    }

    class Updates : HttpRequestHandler {
        private new HttpSession Session { get { return (HttpSession)base.Session; } }

        public void Get() {
            var response = new XElement("updates");
            var i = 1;
            foreach (var item in Session.GetServerValues()) {
                response.Add(new XElement("update",
                    new XAttribute("name", "item" + (i++).ToString()), new XAttribute("value", item)));
            }
            Session.SetXmlResponse(response);
        }

        public class PostedViewModel {
            public string Get1 { get; set; }
            public string Get2 { get; set; }
            public string Get3 { get; set; }
            public string[] Values { get { return new string[] { Get1, Get2, Get3 }; } }
        }

        public void Posted(PostedViewModel vm, int Get1) {
            System.Diagnostics.Trace.WriteLine("Get1 = " + Get1.ToString());
            Session.ShowClientValues(vm.Values);
            var url = Session.Context.Request.Url;
            Session.Redirect(Session.TopUrl);
        }

        public void Close() {
            Session.Close();
            Session.Redirect(Session.TopUrl);
        }
    }
}
