using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Babbacombe.Webserver {
    public class HttpServer : IDisposable {
        protected HttpListener Listener { get; private set; }
        private Type _sessionType;
        public string BaseFolder { get; set; }
        
        /// <summary>
        /// Whether exceptions in background threads are thrown. Defaults to false (ie, they are thrown).
        /// </summary>
        /// <remarks>
        /// These exceptions are passed out in Exception events regardless of how this is set.
        /// </remarks>
        public bool TrapExceptions { get; set; }

        public class ExceptionEventArgs : EventArgs {
            public Exception Ex { get; private set; }
            public ExceptionEventArgs(Exception ex) {
                Ex = ex;
            }
        }
        /// <summary>
        /// Raised when an exception occurs within a background thread.
        /// </summary>
        public event EventHandler<ExceptionEventArgs> Exception;

        /// <summary>
        /// Controls whether the same session object is used across requests from the same source.
        /// Defaults to False. Can be overridden within a session.
        /// </summary>
        public bool TrackSessions { get; set; }

        private List<HttpSession> _cachedSessions = new List<HttpSession>();

        public HttpServer(IEnumerable<string> prefixes) {
            if (!HttpListener.IsSupported) {
                throw new HttpServerException("HttpListener is not supported");
            }
            _sessionType = typeof(HttpSession);

            var prefs = prefixes != null ? prefixes.ToList() : new List<string>();
            if (prefs.Count == 0) prefs.Add("http://+:80/");

            Listener = new HttpListener();
            foreach (var p in prefs) Listener.Prefixes.Add(p);

            BaseFolder = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        }

        public HttpServer(int port = 80)
            : this(new string[] { string.Format("http://+:{0}/", port) }) { }

        public Type SessionType {
            get { return _sessionType; }
            set {
                if (value == null) throw new ArgumentNullException();
                if (!(value.Equals(typeof(HttpSession)) || value.IsSubclassOf(typeof(HttpSession)))) throw new ArgumentException(string.Format("{0} is not derived from Babbacombe.Webserver.HttpSession", value.FullName));
                if (Listener.IsListening) throw new HttpServerException("Can't change session type while the server is running.");
                _sessionType = value;
            }
        }

        public IEnumerable<string> Prefixes { get { return Listener.Prefixes; } }

        public bool Running { get { return Listener.IsListening; } }

        public void Start() {
            if (Running) return;
            Listener.Start();
            ThreadPool.QueueUserWorkItem(new WaitCallback(run));
        }

        public void Stop() {
            Listener.Stop();
            _cachedSessions = new List<HttpSession>();
        }

        private void run(object o) {
            System.Diagnostics.Debug.WriteLine("Webserver started");
            while (Running) {
                try {
                    ThreadPool.QueueUserWorkItem((c) => {
                        try {
                            var context = (HttpListenerContext)c;
                            try {
                                var session = getSession(context);
                                session.Context = context;
                                session.Response = null;
                                context.Response.ContentType = "text/html";
                                session.Respond();

                                if (session.SessionId != null && !_cachedSessions.Contains(session)) _cachedSessions.Add(session);

                                if (session.Response != null) {
                                    byte[] buf = Encoding.UTF8.GetBytes(session.Response);
                                    context.Response.ContentLength64 = buf.Length;
                                    context.Response.OutputStream.Write(buf, 0, buf.Length);
                                }
                            } catch (Exception ex) {
                                OnException(ex);
                            } finally {
                                context.Response.OutputStream.Close();
                                context.Response.Close();
                            }
                        } catch (Exception ex) {
                            OnException(ex);
                        }
                    }, Listener.GetContext());
                } catch (HttpListenerException ex) {
                    // When the listener is stopped, an exception occurs on GetContext.
                    if (Running) OnException(ex);
                }
            }
            System.Diagnostics.Debug.WriteLine("Webserver stopped");
        }

        private HttpSession getSession(HttpListenerContext context) {
            HttpSession session;
            var sessionId = identifySession(context.Request);
            session = sessionId != null ? _cachedSessions.FirstOrDefault(s => s.SessionId == sessionId) : null;
            if (session == null) {
                session = Activator.CreateInstance(_sessionType) as HttpSession;
                session.Context = context;
                session.Server = this;
                session.BaseFolder = BaseFolder;
                if (sessionId != null) {
                    session.SessionId = sessionId;
                } else if (TrackSessions) {
                    session.CreateSessionId();
                }
            } else {
                session.Context = context;
            }
            return session;
        }

        private string identifySession(HttpListenerRequest request) {
            var cookie = request.Cookies.Cast<Cookie>().FirstOrDefault(c => c.Name == "BabSession");
            return cookie != null ? cookie.Value : null;
        }

        protected virtual void OnException(Exception ex) {
            if (Exception != null) Exception(this, new ExceptionEventArgs(ex));
            if (!TrapExceptions) {
                if (ex is HttpServerException) {
                    throw ex;
                } else {
                    throw new HttpServerException(ex);
                }
            }
        }

        protected virtual void Dispose(bool disposing) {
            if (Listener != null) {
                if (Running) Listener.Stop();
                Listener.Close();
                Listener = null;
            }
        }

        public void Dispose() {
            Dispose(true);
        }
    }

    [Serializable]
    public class HttpServerException : ApplicationException {
        public HttpServerException(string message) : base(message) { }
        public HttpServerException(string fmt, params object[] args) : this(string.Format(fmt, args)) { }
        public HttpServerException(Exception innerException, string message) : base(message, innerException) { }
        public HttpServerException(Exception innerException, string fmt, params object[] args) : this(innerException, string.Format(fmt, args)) { }
        public HttpServerException(Exception innerException) : this(innerException, "HttpListener exception") { }
    }
}
