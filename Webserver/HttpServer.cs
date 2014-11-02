using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Babbacombe.Webserver {

    /// <summary>
    /// The main webserver class. This manages listening at the port and handling HttpSession objects.
    /// </summary>
    public class HttpServer : IDisposable {
        
        /// <summary>
        /// The underlying .net listener object, created when the Server is created. There is not normally any need to access this,
        /// but an implementation of HttpServer could use it directly to add authorization, for example.
        /// </summary>
        protected HttpListener Listener { get; private set; }
        private Type _sessionType;

        /// <summary>
        /// The base folder for accessing files (html, scripts etc). Defaults to the location of the
        /// application using the Server. If another location is required, it should be set before
        /// the server is started.
        /// </summary>
        public string BaseFolder { get; set; }
        
        /// <summary>
        /// Whether exceptions in background threads are thrown. Defaults to true (ie, they are not thrown).
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
        /// Controls whether the same session object is used across requests from the same client.
        /// Defaults to False. Can be overridden within a session.
        /// </summary>
        public bool TrackSessions { get; set; }

        // The list of sessions being tracked.
        private List<HttpSession> _cachedSessions = new List<HttpSession>();

        /// <summary>
        /// How long a session is kept unused before being expired. Defaults to 1 hour.
        /// </summary>
        public TimeSpan SessionsExpiryTime { get; set; }

        /// <summary>
        /// How often to check for expired sessions. Defaults to 5 minutes.
        /// </summary>
        public TimeSpan SessionsExpiryInterval { get; set; }

        /// <summary>
        /// Default filenames that the server will look for (in BaseFolder) if no class/method or
        /// filename is specified in the request url.
        /// </summary>
        /// <remarks>
        /// Defaults to index.htm, Index.htm, index.html, Index.html
        /// </remarks>
        public List<string> DefaultFilenames { get; private set; }

        /// <summary>
        /// If true, exceptions will be reported to the client with full details. Defaults to False (just sends internal server error).
        /// </summary>
        public bool RespondWithExceptionDetails { get; set; }

        /// <summary>
        /// Creates a server that listens on a set of prefixes, as defined for HttpListener.
        /// </summary>
        /// <param name="prefixes"></param>
        protected HttpServer(IEnumerable<string> prefixes) {
            if (!HttpListener.IsSupported) {
                throw new HttpServerException("HttpListener is not supported");
            }
            _sessionType = typeof(HttpSession);

            var prefs = prefixes != null ? prefixes.ToList() : new List<string>();
            if (prefs.Count == 0) prefs.Add("http://+:80/");

            Listener = new HttpListener();
            foreach (var p in prefs) Listener.Prefixes.Add(p);

            BaseFolder = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            SessionsExpiryTime = new TimeSpan(1, 0, 0);
            SessionsExpiryInterval = new TimeSpan(0, 5, 0);

            DefaultFilenames = new List<string>(new string[] { "index.htm", "Index.htm", "index.html", "Index.html" });

            TrapExceptions = true;
        }

        /// <summary>
        /// Creates a server (with no app specified) that listens on a port. If no port is specified,
        /// it listens on Port 80.
        /// </summary>
        /// <param name="port"></param>
        public HttpServer(int port = 80)
            : this(new string[] { string.Format("http://+:{0}/", port) }) { }

        /// <summary>
        /// The class that will be created whenever a session is started. Defaults to
        /// the built in <see cref="Babbacombe.Webserver.HttpSession"/>.
        /// </summary>
        public Type SessionType {
            get { return _sessionType; }
            set {
                if (value == null) throw new ArgumentNullException();
                if (!(value.Equals(typeof(HttpSession)) || value.IsSubclassOf(typeof(HttpSession)))) throw new ArgumentException(string.Format("{0} is not derived from Babbacombe.Webserver.HttpSession", value.FullName));
                if (Listener.IsListening) throw new HttpServerException("Can't change session type while the server is running.");
                _sessionType = value;
            }
        }

        /// <summary>
        /// Gets the prefixes the listener is listening on.
        /// </summary>
        public IEnumerable<string> Prefixes { get { return Listener.Prefixes; } }

        /// <summary>
        /// True if the server has been started, and is not current stopped.
        /// </summary>
        public bool Running { get { return Listener.IsListening; } }

        /// <summary>
        /// Starts the server listening.
        /// </summary>
        public void Start() {
            if (Running) return;
            Listener.Start();
            ThreadPool.QueueUserWorkItem(new WaitCallback(run));
        }

        /// <summary>
        /// Stops the server listening, but does not dispose of it. It can be restarted.
        /// </summary>
        /// <param name="clearSessions">
        /// Whether to clear away existing sessions. Defaults to true.
        /// </param>
        public void Stop(bool clearSessions = true) {
            Listener.Stop();
            if (clearSessions) {
                lock (_cachedSessions) {
                    foreach (var s in _cachedSessions.OfType<IDisposable>()) {
                        s.Dispose();
                    }
                    _cachedSessions = new List<HttpSession>();
                }
            }
        }

        private void run(object o) {
            System.Diagnostics.Debug.WriteLine("Webserver started");
            using (var expiryTimer = new System.Timers.Timer()) {
                expiryTimer.Interval = SessionsExpiryInterval.TotalMilliseconds;
                expiryTimer.Elapsed += expiryTimer_Elapsed;

                while (Running) {
                    try {
                        ThreadPool.QueueUserWorkItem((c) => {
                            try {
                                handleRequest((HttpListenerContext)c);
                            } catch (Exception ex) {
                                OnException(ex);
                            }
                        }, Listener.GetContext());
                    } catch (HttpListenerException ex) {
                        // When the listener is stopped, an exception occurs on GetContext.
                        if (Running) OnException(ex);
                    }
                }
            }
            System.Diagnostics.Debug.WriteLine("Webserver stopped");
        }

        private void handleRequest(HttpListenerContext context) {
            try {
                var session = getSession(context);
                // Reset the time used to calculate expiry.
                session.LastAccessed = DateTime.UtcNow;
                // Clear out any output from previous requests.
                session.Response = null;
                // Default the return type.
                context.Response.ContentType = "text/html";
                // Get the response from the session.
                try {
                    session.Respond();
                } catch (Exception ex) {
                    session.OnRespondException(ex is HttpRespondException ? (HttpRespondException)ex : new HttpRespondException(ex));
                    OnException(ex);
                }

                // If this session is to be saved, add it to the cache if it's not already there.
                lock (_cachedSessions) {
                    if (session.SessionId != null && !_cachedSessions.Contains(session)) _cachedSessions.Add(session);
                }

                // If the session has put the text of a page into the Response property, send it.
                // If this is null, it's assumed the session has sent the response some other way.
                if (session.Response != null) {
                    byte[] buf = Encoding.UTF8.GetBytes(session.Response);
                    context.Response.ContentLength64 = buf.Length;
                    context.Response.OutputStream.Write(buf, 0, buf.Length);
                }
                // Reset the expiry timer again in case the response took a hideously long time.
                session.LastAccessed = DateTime.UtcNow;

                // If the session is not being saved, and it is of a type which has been made
                // disposable, dispose of it.
                if (session.SessionId == null && session is IDisposable) {
                    ((IDisposable)session).Dispose();
                }
            } catch (Exception ex) {
                OnException(ex);
            } finally {
                context.Response.OutputStream.Close();
                context.Response.Close();
            }
        }

        // Called every so often to expire sessions that haven't been accessed for a while.
        void expiryTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
            lock (_cachedSessions) {
                var now = DateTime.UtcNow;
                var expired = _cachedSessions.Where(s => s.ExpiresAt < now).ToList();
                foreach (var s in expired) {
                    // An implementation of HttpSession could have been made disposable
                    var disp = s as IDisposable;
                    if (disp != null) disp.Dispose();
                    _cachedSessions.Remove(s);
                }
            }
        }

        private HttpSession getSession(HttpListenerContext context) {
            HttpSession session;
            // See if the request contains our session id cookie.
            var sessionId = identifySession(context.Request);
            lock (_cachedSessions) {
                session = sessionId != null ? _cachedSessions.SingleOrDefault(s => MatchesSession(s, sessionId, context.Request.Url)) : null;
            }
            if (session == null) {
                // Create a session of the required type.
                session = CreateSession(context.Request.Url);
                session.Context = context;
                session.Server = this;
                if (session.BaseFolder == null) session.BaseFolder = BaseFolder;
                if (sessionId != null) {
                    // The session has expired. Start a new one with the old cookie.
                    session.SessionId = sessionId;
                } else if (TrackSessions) {
                    // Create a cookie if required.
                    session.CreateSessionId();
                }
            } else {
                session.Context = context;
            }
            return session;
        }

        /// <summary>
        /// A function that identifies a session within the cache given a cached session,
        /// the SessionId (identifying the client) and the request url.
        /// </summary>
        protected virtual Func<HttpSession, string, Uri, bool> MatchesSession {
            get { return (sess, sid, url) => sess.SessionId == sid; }
        }

        /// <summary>
        /// Creates a session of the type required.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        protected virtual HttpSession CreateSession(Uri url) {
            return (HttpSession)Activator.CreateInstance(_sessionType);
        }

        protected internal virtual string GetFilenameFromRequest(Uri url) {
            if (string.IsNullOrWhiteSpace(url.AbsolutePath) || url.AbsolutePath == "/") return null;
            return url.AbsolutePath;
        }

        private string identifySession(HttpListenerRequest request) {
            // Look for a cookie called BabSession in the request. It identifies the client and
            // there should be a session object already existing for it.
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
