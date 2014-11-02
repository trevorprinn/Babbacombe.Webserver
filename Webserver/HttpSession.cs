﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace Babbacombe.Webserver {

    /// <summary>
    /// A session object that manages either a single request from a client, or all the requests
    /// from a single client if its SessionId has been set (normally done by setting the servers
    /// TrackSessions property to true).
    /// </summary>
    public class HttpSession {
        // The current context. NB This changes with each request that is received.
        private HttpListenerContext _context;
        
        /// <summary>
        /// Gets the server that has created this session.
        /// </summary>
        protected internal HttpServer Server { get; internal set; }

        // The value of a cookie used to identify the same session across requests.
        private string _sessionId;

        /// <summary>
        /// The BaseFolder used as the root for file requests. Defaults to the server's BaseFolder property at the
        /// time the session is created.
        /// </summary>
        protected internal string BaseFolder { get; set; }

        /// <summary>
        /// The items in the Query string in the request url.
        /// </summary>
        protected internal QueryItem[] QueryItems { get; private set; }

        // A cache of the request handler objects created by this session.
        private List<HttpRequestHandler> _handlers = new List<HttpRequestHandler>();

        // The last time this session was used. Used to expire sessions.
        internal DateTime LastAccessed { get; set; }

        /// <summary>
        /// Gets the session id used to identify the client across calls. See CreateSessionId for more info.
        /// </summary>
        protected internal string SessionId {
            get { return _sessionId; }
            internal set {
                _sessionId = value;
                Context.Response.Cookies.Add(new Cookie("BabSession", value));
            }
        }

        /// <summary>
        /// Gets the context (the base Request and Response) currently being processed by the session.
        /// </summary>
        public HttpListenerContext Context {
            get { return _context; }
            internal set {
                _context = value;
                QueryItems = QueryItem.GetItems(Context.Request.Url).ToArray();
            }
        }

        /// <summary>
        /// Responds to requests. The base version runs a method on a request handler if there is one defined in the Request url.
        /// If not, it sends a file if there is one in the Request url.
        /// </summary>
        /// <remarks>
        /// This is generally enough for the session to operate, but an implementation can do something completely
        /// different if it wants to.
        /// </remarks>
        protected internal virtual void Respond() {
            if (RequestHasMethod()) {
                RunMethod();
                return;
            }

            string fname = Server.GetFilenameFromRequest(Context.Request.Url);
            if (fname == null) {
                fname = getDefaultFilename();
                if (fname == null) {
                    Context.Response.StatusCode = 404;
                    Context.Response.StatusDescription = "File not found";
                    Response = "<html><body>404 - File not found</body></html>";
                    return;
                }
            } else {
                fname = ConstructFilename(fname);
            }
            Context.Response.StatusCode = 200;
            SendFile(fname);
        }

        /// <summary>
        /// Construct a full filename using the BaseFolder and a relative File Path, which
        /// may have been passed in the Request url.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public string ConstructFilename(string filepath) {
            // If received in the Request url the file path may start with a / that isn't wanted.
            filepath = filepath.TrimStart('/');

            // Change / to \ if necessary
            if (Path.DirectorySeparatorChar != '/') filepath = filepath.Replace('/', Path.DirectorySeparatorChar);

            return Path.Combine(BaseFolder, filepath);            
        }

        private string getDefaultFilename() {
            foreach (string name in Server.DefaultFilenames) {
                var fname = ConstructFilename(name);
                if (File.Exists(fname)) return fname;
            }
            return null;
        }

        /// <summary>
        /// The response to send back to the client after the Repond method has run. If this is left null, the server will assume that
        /// the session has handled the response in some other way.
        /// </summary>
        /// <remarks>
        /// The server always sets this to null before calling the Respond method.
        /// </remarks>
        public string Response { get; set; }

        /// <summary>
        /// A convenient way to send an XML document as the response.
        /// </summary>
        /// <param name="doc"></param>
        public void SetXmlResponse(XDocument doc) {
            Response = doc.ToString();
        }

        /// <summary>
        /// A convenient way to send part of an XML document as the response.
        /// </summary>
        /// <param name="data"></param>
        public void SetXmlResponse(XElement data) {
            Response = data.ToString();
        }

        /// <summary>
        /// Given a body element, wraps it in a completely basic header, to
        /// send as the response.
        /// </summary>
        /// <param name="body"></param>
        public void SetXmlBody(XElement body) {
            var html = new XElement("html", body);
            SetXmlResponse(html);
        }

        /// <summary>
        /// Sends a file as the response.
        /// </summary>
        /// <param name="filename">A full filename.</param>
        /// <remarks>
        /// Sends the file directly to the output stream (doesn't use the Response property, which
        /// should be left null).
        /// </remarks>
        public void SendFile(string filename) {
            if (!File.Exists(filename)) {
                Context.Response.StatusCode = 404;
                Context.Response.StatusDescription = "File not found";
                Response = "<html><body>404 - File not found</body></html>";
                return;
            }
            using (var f = new FileStream(filename, FileMode.Open, FileAccess.Read)) {
                f.CopyTo(Context.Response.OutputStream);
            }
        }

        /// <summary>
        /// Creates a new session id, and puts it into a cookie in the response, to allow the client
        /// to be tracked. Normally handled automatically if the server's TrackSessions property is true,
        /// but can be called by a session if TrackSessions is false.
        /// </summary>
        /// <remarks>
        /// Does nothing if the session already has an id.
        /// </remarks>
        protected internal void CreateSessionId() {
            if (SessionId != null) return;
            SessionId = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// True if the query items in the request url include items defining a request handler class and method
        /// to be called.
        /// </summary>
        /// <returns></returns>
        protected bool RequestHasMethod() {
            var names = QueryItems.Select(i => i.Name);
            return names.Contains(ClassParameter) && names.Contains(MethodParameter);
        }

        /// <summary>
        /// Runs the request handler method defined in the request url. Assumes that RequestHasMethod is true. Normally
        /// called automatically by the base Respond method.
        /// </summary>
        protected void RunMethod() {
            string className = string.Format("{0}.{1}", HandlerNamespace, QueryItems.Single(i => i.Name == ClassParameter).Value);
            var handler = _handlers.SingleOrDefault(h => h.GetType().FullName == className);
            if (handler == null) {
                var type = HandlerAssembly.GetType(className);
                handler = (HttpRequestHandler)Activator.CreateInstance(type);
                _handlers.Add(handler);
            }
            handler.Session = this;
            var method = handler.GetType().GetMethod(QueryItems.Single(i => i.Name == MethodParameter).Value);
            method.Invoke(handler, null);
        }

        /// <summary>
        /// Gets the Assembly containing the HttpRequestHandler classes used by the session. By default, handlers are
        /// in the same assembly as the HttpSession derivative. This can be overridden to specify a different assembly.
        /// </summary>
        protected virtual Assembly HandlerAssembly {
            get { return GetType().Assembly; }
        }

        /// <summary>
        /// Gets the namespace containing the HttpRequestHandler classes used by the session. By default, handlers are
        /// in the same namespace as the HttpSession derivative. This can be overridden to specify a different assembly.
        /// </summary>
        protected virtual string HandlerNamespace {
            get { return GetType().Namespace; }
        }

        /// <summary>
        /// Gets the name of the item in the query url that specifies a request handler class. Defaults to "c" but can
        /// be overridden.
        /// </summary>
        protected virtual string ClassParameter {
            get { return "c"; }
        }

        /// <summary>
        /// Gets the name of the item in the query url that specifies a request handler method. Defaults to "m" but can
        /// be overridden.
        /// </summary>
        protected virtual string MethodParameter {
            get { return "m"; }
        }

        /// <summary>
        /// How long this session is kept idle before expiring it. Defaults to the Server's SessionsExpiryTime.
        /// </summary>
        protected internal virtual TimeSpan ExpiryTime {
            get { return Server.SessionsExpiryTime; }
        }

        internal DateTime ExpiresAt {
            get { return LastAccessed.Add(ExpiryTime); }
        }
    }

    /// <summary>
    /// A name and value passed either in the request url (see HttpSession.QueryItems
    /// or HttpRequestHandler.GetArg) or in posted submit data
    /// (see HttpRequestHandler.GetPostedItems).
    /// </summary>
    public class QueryItem {
        public string Name { get; private set; }
        public string Value { get; private set; }

        internal static IEnumerable<QueryItem> GetItems(Uri url) {
            return GetItems(url.Query);
        }

        internal static IEnumerable<QueryItem> GetItems(string query) {
            List<QueryItem> items = new List<QueryItem>();
            var qs = HttpUtility.ParseQueryString(query);
            for (int i = 0; i < qs.Count; i++) {
                var n = qs.AllKeys[i];
                var v = qs.GetValues(i).FirstOrDefault();
                if (n == null) n = v;
                if (n == null) continue;
                items.Add(new QueryItem { Name = n, Value = v });
            }
            return items;
        }
    }

}
