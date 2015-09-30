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

    /// <summary>
    /// Session class to manage requests made to the "exercise" app (see the FormServer constructor
    /// for how this is set up).
    /// </summary>
    /// <remarks>
    /// Normally this would be named something like HttpSessionExercise, to avoid confusion, if there
    /// were multiple session classes. This one is called HttpSession to show how the namespace works.
    /// </remarks>
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
            // Create a form to display data about this session.
            FormServer.Instance.Invoke(new Action(() => {
                _form = new FormSession(reqData);
                _form.Text = "Session: " + SessionId;
                _form.Show();
            }));
        }

        protected override void Respond() {
            // Display the url on the server form, then respond as normal by calling request handlers.
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
            // If the index page has been requested, rather than send it as is, put the
            // current values into the fields on it, and send that.
            if (ea.Filename == Path.Combine(BaseFolder, "index.html")) {
                ea.Document = HttpPage.Create(ea.Filename, this, typeof(IndexPage));
            }
            return ea;
        }

        private class IndexPage : HttpPage {
            public IndexPage(XDocument data, HttpSession session) : base(data, session) {
                DefaultTagName = "name";
                ReplaceValue("session", session.SessionId);
                // Put the values entered into the session form into the page.
                var vals = session.GetServerValues().ToArray();
                ReplaceValue("item1", vals[0]);
                ReplaceValue("item2", vals[1]);
                ReplaceValue("item3", vals[2]);
                // Put the values sent from the client into the page.
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

    /// <summary>
    /// Request handler for requests that contain c=Updates.
    /// </summary>
    class Updates : HttpRequestHandler {
        // A convenience to get the exerciser session object.
        private new HttpSession Session { get { return (HttpSession)base.Session; } }

        /// <summary>
        /// This is an Ajax call made once per second from index.html once a second to update
        /// the page with the values entered on the session form. It constructs an Xml element with
        /// the names and values of the items, and sends the element as the response. Javascript
        /// in the page then updates the display of the fields.
        /// </summary>
        public void Get() {
            var response = new XElement("updates");
            var i = 1;
            foreach (var item in Session.GetServerValues()) {
                response.Add(new XElement("update",
                    new XAttribute("name", "item" + (i++).ToString()), new XAttribute("value", item)));
            }
            Session.SetXmlResponse(response);
        }

        /// <summary>
        /// This is used to demonstrate the ObjectBinder. When the Posted method is called from
        /// index.html the posted data is automatically put into the properties of the vm parameter.
        /// </summary>
        public class PostedViewModel {
            public string Get1 { get; set; }
            public string Get2 { get; set; }
            public string Get3 { get; set; }
            public string[] Values { get { return new string[] { Get1, Get2, Get3 }; } }
        }

        /// <summary>
        /// This is a posted submission from index.html containing the client values entered on the page.
        /// </summary>
        /// <param name="vm"></param>
        /// <param name="Get1"></param>
        public void Posted(PostedViewModel vm, int Get1) {
            // Just to demonstrate that a value can be placed directly into a value type parameter.
            Logger.LogFile.Log("Get1 = " + Get1.ToString());
            // Display the values that were sent on the session form
            Session.ShowClientValues(vm.Values);
            Session.Redirect(Session.TopUrl);
        }

        /// <summary>
        /// Called when the Close button on index.html is pressed. Closes this session, and causes the
        /// browser to redirect to the original url.
        /// </summary>
        public void Close() {
            Session.Close();
            Session.Redirect(Session.TopUrl);
        }
    }
}
