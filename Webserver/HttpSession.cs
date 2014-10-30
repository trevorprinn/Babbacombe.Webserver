using System;
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
    public class HttpSession {

        private HttpListenerContext _context;
        protected internal HttpServer Server { get; internal set; }
        private string _sessionId;
        protected internal string BaseFolder { get; set; }
        protected internal QueryItem[] QueryItems { get; private set; }

        private List<HttpRequestHandler> _handlers = new List<HttpRequestHandler>();

        internal DateTime LastAccessed { get; set; }

        protected internal string SessionId {
            get { return _sessionId; }
            internal set {
                _sessionId = value;
                Context.Response.Cookies.Add(new Cookie("BabSession", value));
            }
        }

        public HttpListenerContext Context {
            get { return _context; }
            internal set {
                _context = value;
                QueryItems = QueryItem.GetItems(Context.Request.Url).ToArray();
            }
        }

        /// <summary>
        /// Responds to requests. The base version runs a method if there is one.
        /// If not, it sends a file if there is one in the Request url.
        /// </summary>
        protected internal virtual void Respond() {
            if (RequestHasMethod()) {
                RunMethod();
                return;
            }

            var url = Context.Request.Url;
            var fname = ConstructFilename(url.AbsolutePath);
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

        public string Response { get; set; }

        public void SetXmlResponse(XDocument doc) {
            Response = doc.ToString();
        }

        public void SetXmlResponse(XElement data) {
            Response = data.ToString();
        }

        public void SetXmlBody(XElement body) {
            var html = new XElement("html", body);
            SetXmlResponse(html);
        }

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

        protected internal void CreateSessionId() {
            if (SessionId != null) return;
            SessionId = Guid.NewGuid().ToString();
        }

        protected bool RequestHasMethod() {
            var names = QueryItems.Select(i => i.Name);
            return names.Contains(ClassParameter) && names.Contains(MethodParameter);
        }

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

        protected virtual Assembly HandlerAssembly {
            get {
                // By default, handlers are in the same assembly as the HttpSession derivative.
                return GetType().Assembly;
            }
        }

        protected virtual string HandlerNamespace {
            get {
                // By default, handlers are in the same assembly as the HttpSession derivative.
                return GetType().Namespace;
            }
        }

        protected virtual string ClassParameter {
            get { return "c"; }
        }

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
