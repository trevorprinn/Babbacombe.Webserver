using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Babbacombe.Webserver {

    /// <summary>
    /// An HttpServer that can create different session types for different apps 
    /// requested by clients.
    /// </summary>
    public class HttpAppServer : HttpServer {
        private HttpApplicationDef[] _appDefs;

        public HttpAppServer(IEnumerable<HttpApplicationDef> appDefs)
            : base(appDefs.Select(d => d.Prefix)) {
            _appDefs = appDefs.ToArray();
        }

        private HttpApplicationDef getAppDef(Uri url) {
            string appName = url.Segments.Length > 1 ? url.Segments[1].TrimEnd('/') : null;
            var def = _appDefs.SingleOrDefault(d => d.AppName == appName && d.Port == url.Port);
            if (def == null) throw new HttpServerException("Couldn't identify application for url '{0}'", url);
            return def;
        }

        protected override HttpSession CreateSession(Uri url) {
            var appDef = getAppDef(url);
            var session = (HttpSession)Activator.CreateInstance(appDef.SessionType);
            session.BaseFolder = appDef.BaseFolder;
            return session;
        }

        /// <summary>
        /// A cached session is retrieved if the session id matches and it is of the same
        /// type as the requested app.
        /// </summary>
        protected override Func<HttpSession, string, Uri, bool> MatchesSession {
            get {
                return (sess, sid, url) => {
                    if (sess.SessionId != sid) return false;
                    return getAppDef(url).SessionType.Equals(sess.GetType());
                };
            }
        }

        protected internal override string GetFilenameFromRequest(Uri url) {
            var appDef = getAppDef(url);
            if (appDef.AppName == null) return base.GetFilenameFromRequest(url);
            var unwantedSegs = appDef.AppName == null ? 1 : 2;
            if (url.Segments.Length < unwantedSegs + 1) return null;
            return string.Concat(url.Segments.Skip(unwantedSegs));
        }
    }

    /// <summary>
    /// A set of application definitions is passed to the HttpAppServer when it is created.
    /// </summary>
    public class HttpApplicationDef {
        /// <summary>
        /// The name of the application. Defaults to no application (handles urls that don't contain an application).
        /// An application can have more than one entry if it listens on more than one port.
        /// </summary>
        public string AppName { get; private set; }

        /// <summary>
        /// The type of HttpSession to be created for this application. Defaults to HttpSession.
        /// </summary>
        public Type SessionType { get; private set; }

        /// <summary>
        /// The port this application listens on. Defaults to 80.
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// The BaseFolder used for this application. Defaults to the HttpAppServer BaseFolder.
        /// </summary>
        public string BaseFolder { get; private set; }

        public HttpApplicationDef(string appName = null, Type sessionType = null, int port = 80, string baseFolder = null) {
            AppName = appName;
            SessionType = sessionType ?? typeof(HttpSession);
            Port = port;
            BaseFolder = baseFolder;
        }

        public string Prefix {
            get { return string.Format("http://+:{0}/{1}/", Port, AppName ?? "*"); }
        }
    }
}
