#region Licence
/*
    Babbacombe.Webserver
    https://github.com/trevorprinn/Babbacombe.Webserver
    Copyright © 2014 Babbacombe Computers Ltd.

    This library is free software; you can redistribute it and/or
    modify it under the terms of the GNU Lesser General Public
    License as published by the Free Software Foundation; either
    version 2.1 of the License, or (at your option) any later version.

    This library is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
    Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public
    License along with this library; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301
    USA
 */
#endregion
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace Babbacombe.Webserver {

    /// <summary>
    /// A session object that manages either a single request from a client, or all the requests
    /// from a single client if its SessionId has been set (normally done by setting the servers
    /// TrackSessions property to true).
    /// </summary>
    public class HttpSession : IDisposable {

        /// <summary>
        /// Contains data specific to one request/thread.
        /// </summary>
        public sealed class RequestData {
            public HttpSession Session { get; private set; }
            internal int ThreadId { get; private set; }
            public HttpListenerContext Context { get; private set; }
            public QueryItems QueryItems { get; private set; }
            public string Response { get; set; }
            public Uri TopUrl {
                get {
                    var u = Context.Request.Url;
                    return new UriBuilder(u.Scheme, u.Host, u.Port, Session.Server is HttpAppServer ? u.Segments[1] : null).Uri;
                }
            }

            internal RequestData(HttpSession session, HttpListenerContext context) {
                ThreadId = Thread.CurrentThread.ManagedThreadId;
                Session = session;
                Context = context;
                QueryItems = QueryItems.Get(Context.Request.Url);
            }
        }

        // Data for each of the current requests.
        private List<RequestData> _requestData = new List<RequestData>();

        /// <summary>
        /// Obtains the request specific data so that it can be used in a thread other
        /// than the one the request is being processed on.
        /// </summary>
        /// <returns></returns>
        public RequestData GetRequestData() {
            var data = _requestData.SingleOrDefault(d => d.ThreadId == Thread.CurrentThread.ManagedThreadId);
            if (data == null) throw new HttpSessionThreadException();
            return data;
        }

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
        /// The items in the Query string in the request url. This property is thread specific.
        /// </summary>
        public QueryItems QueryItems {
            get { return GetRequestData().QueryItems; }
        }

        // A cache of the request handler objects created by this session.
        private List<HttpRequestHandler> _handlers = new List<HttpRequestHandler>();

        // The last time this session was used. Used to expire sessions.
        internal DateTime LastAccessed { get; set; }

        /// <summary>
        /// True if Close() has been called during this request (or any concurrent request).
        /// </summary>
        public bool Closing { get; private set; }

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
        /// Gets the context (the base Request and Response) currently being processed by the session. This
        /// property is thread specific.
        /// </summary>
        public HttpListenerContext Context {
            get { return GetRequestData().Context; }
            internal set {
                _requestData.Add(new RequestData(this, value));
            }
        }

        internal void RequestComplete() {
            // Remove the request data for this thread.
            var data = _requestData.SingleOrDefault(d => d.ThreadId == Thread.CurrentThread.ManagedThreadId);
            if (data != null) _requestData.Remove(data);
        }

        /// <summary>
        /// Raised after the session has been created and populated by the Server.
        /// </summary>
        public event EventHandler Created;

        protected internal virtual void OnCreated() {
            if (Created != null) Created(this, EventArgs.Empty);
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
        /// the session has handled the response in some other way. This property is thread specific.
        /// </summary>
        public string Response {
            get { return GetRequestData().Response; }
            set { GetRequestData().Response = value; }
        }

        /// <summary>
        /// A convenient way to send an XML document as the response. Can be called only on the request's
        /// original thread.
        /// </summary>
        /// <param name="doc"></param>
        public void SetXmlResponse(XDocument doc) {
            Response = doc.ToString();
        }

        /// <summary>
        /// A convenient way to send part of an XML document as the response. Can be called only on the request's
        /// original thread.
        /// </summary>
        /// <param name="data"></param>
        public void SetXmlResponse(XElement data) {
            Response = data.ToString();
        }

        /// <summary>
        /// Given a body element, wraps it in a completely basic header, to
        /// send as the response. Can be called only on the request's original thread.
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
        /// <param name="requestData">Should be set if not running on the request's original thread.</param>
        /// <remarks>
        /// Sends the file directly to the output stream (doesn't use the Response property, which
        /// should be left null).
        /// </remarks>
        public void SendFile(string filename, RequestData requestData = null) {
            if (requestData == null) requestData = GetRequestData();

            var details = OnFileRequested(filename);

            if (details.Contents != null || details.Document != null) {
                requestData.Response = details.Contents != null ? details.Contents : details.Document.ToString();
                return;
            }
            
            if (details.Stream != null) {
                details.Stream.CopyTo(requestData.Context.Response.OutputStream);
                if (!details.LeaveStream) details.Stream.Dispose();
                return;
            }

            if (details.Filename == null || !File.Exists(details.Filename)) {
                new HttpErrorPage(this, 404, "File not found").Send();
                return;
            }
            using (var f = new FileStream(details.Filename, FileMode.Open, FileAccess.Read)) {
                f.CopyTo(requestData.Context.Response.OutputStream);
            }
        }

        public class FileRequestedEventArgs : EventArgs {
            public string Filename { get; set; }
            public string Contents { get; set; }
            public Stream Stream { get; set; }
            public bool LeaveStream { get; set; }
            public XDocument Document { get; set; }
            public bool Handled { get { return Contents != null || Stream != null || Document != null; } }
        }
        public event EventHandler<FileRequestedEventArgs> FileRequested;

        protected virtual FileRequestedEventArgs OnFileRequested(string filename) {
            var ea = new FileRequestedEventArgs { Filename = filename };
            if (FileRequested != null) FileRequested(this, ea);
            return ea;
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
        /// to be called. Should be called only on the request's original thread.
        /// </summary>
        /// <returns></returns>
        protected bool RequestHasMethod() {
            return QueryItems.Contains(ClassParameter) && QueryItems.Contains(MethodParameter);
        }

        /// <summary>
        /// Runs the request handler method defined in the request url. Assumes that RequestHasMethod is true. Normally
        /// called automatically by the base Respond method.
        /// </summary>
        protected void RunMethod() {
            string className = string.Format("{0}.{1}", HandlerNamespace, QueryItems[ClassParameter]);
            var handler = _handlers.SingleOrDefault(h => h.GetType().FullName == className);
            if (handler == null) {
                try {
                    var type = HandlerAssembly.GetType(className);
                    handler = (HttpRequestHandler)Activator.CreateInstance(type);
                } catch (Exception ex) {
                    throw new HttpUnknownHandlerException(className, ex);
                }
                _handlers.Add(handler);
            }
            handler.Session = this;
            string methodName = null;
            methodName = QueryItems[MethodParameter];
            var method = handler.GetType().GetMethod(methodName);
            if (method == null) throw new HttpUnknownMethodException(className, methodName);
            try {
                method.Invoke(handler, null);
            } catch (Exception ex) {
                throw new HttpHandlerMethodException(className, methodName, ex);
            }
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

        internal bool InUse {
            get { return _requestData.Any(); }
        }

        /// <summary>
        /// Returns the top url of the web app. This property is thread specific.
        /// </summary>
        public Uri TopUrl {
            get {
                var u = Context.Request.Url;
                return new UriBuilder(u.Scheme, u.Host, u.Port, Server is HttpAppServer ? u.Segments[1] : null).Uri;
            }
        }

        /// <summary>
        /// Redirects the client to the given url.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="requestData">Should be set if not running on the request's original thread.</param>
        public void Redirect(string url, RequestData requestData = null) {
            Redirect(new Uri(url));
        }

        /// <summary>
        /// Redirects the client to the given url.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="requestData">Should be set if not running on the request's original thread.</param>
        public void Redirect(Uri url, RequestData requestData = null) {
            if (requestData == null) requestData = GetRequestData();
            requestData.Context.Response.Headers.Set("Location", url.ToString());
            requestData.Context.Response.StatusCode = 303;
        }

        /// <summary>
        /// Called if an untrapped exception occurs within a Respond method. Can only be called on the
        /// request's original thread.
        /// </summary>
        /// <param name="ex"></param>
        protected internal virtual void OnRespondException(HttpRespondException ex) {
            var errPage = new HttpErrorPage(this, ex.ToString());
            errPage.Send();
        }

        /// <summary>
        /// The base HttpSession.Dispose does nothing.
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            // Dispose of any handlers that have had IDisposable added to them.
            foreach (var h in _handlers.OfType<IDisposable>()) h.Dispose();
            _handlers.Clear();
        }

        /// <summary>
        /// Closes the session at the end of processing the current request.
        /// </summary>
        public void Close() {
            Closing = true;
        }
    }

    [Serializable]
    public class HttpRespondException : ApplicationException {
        public HttpRespondException(string message, Exception ex = null) : base(message, ex) { }
        public HttpRespondException(Exception ex) : base("An exception has occurred within the session's Respond method", ex) { }
    }

    [Serializable]
    public class HttpUnknownHandlerException : HttpRespondException {
        public HttpUnknownHandlerException(string handler, Exception ex)
            : base(string.Format("Unable to create the requested handler '{0}'", handler), ex) { }
    }

    [Serializable]
    public class HttpUnknownMethodException : HttpRespondException {
        public HttpUnknownMethodException(string handler, string method)
            : base(string.Format("Unable to run the requested method '{0}.{1}'", handler, method)) { }
    }

    [Serializable]
    public class HttpHandlerMethodException : HttpRespondException {
        public HttpHandlerMethodException(string handler, string method, Exception ex)
            : base(string.Format("Exception within the requested method '{0}.{1}'", handler, method), ex) { }
    }

    [Serializable]
    public class HttpSessionThreadException : ApplicationException {
        public HttpSessionThreadException() { }
        public override string Message {
            get { return "Attempted to access Request specific data on an unknown thread"; }
        }
    }

    public class QueryItems : IEnumerable<QueryItem> {
        private QueryItem[] _items;

        private QueryItems() { }

        internal static QueryItems Get(Uri url) {
            return Get(url.Query);
        }

        internal static QueryItems Get(string query) {
            List<QueryItem> items = new List<QueryItem>();
            var qs = HttpUtility.ParseQueryString(query);
            for (int i = 0; i < qs.Count; i++) {
                var n = qs.AllKeys[i];
                var v = qs.GetValues(i).FirstOrDefault();
                if (n == null) n = v;
                if (n == null) continue;
                items.Add(new QueryItem { Name = n, Value = v });
            }
            return new QueryItems { _items = items.ToArray() };
        }

        public string this[string name] {
            get {
                var item = _items.FirstOrDefault(i => i.Name == name);
                return item == null ? null : item.Value;
            }
        }

        public IEnumerator<QueryItem> GetEnumerator() {
            foreach (var item in _items) yield return item;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return _items.GetEnumerator();
        }

        public bool Contains(string name) { return _items.Any(i => i.Name == name); }
    }

    /// <summary>
    /// A name and value passed either in the request url (see HttpSession.QueryItems
    /// or HttpRequestHandler.GetArg) or in posted submit data
    /// (see HttpRequestHandler.GetPostedItems).
    /// </summary>
    public class QueryItem {
        public string Name { get; internal set; }
        public string Value { get; internal set; }
    }
}
