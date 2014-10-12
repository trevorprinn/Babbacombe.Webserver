﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Babbacombe.Webserver {
    public class HttpServer : IDisposable {
        private HttpListener _listener;
        private Type _sessionType;
        
        /// <summary>
        /// Whether exceptions in background threads are thrown. Defaults to false.
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

        public HttpServer(IEnumerable<string> prefixes) {
            if (!HttpListener.IsSupported) {
                throw new HttpServerException("HttpListener is not supported");
            }
            _sessionType = typeof(HttpSession);

            var prefs = prefixes != null ? prefixes.ToList() : new List<string>();
            if (prefs.Count == 0) prefs.Add("http://+:80/");

            _listener = new HttpListener();
            foreach (var p in prefs) _listener.Prefixes.Add(p);
        }

        public HttpServer(int port = 80)
            : this(new string[] { string.Format("http://+:{0}/", port) }) { }

        public Type SessionType {
            get { return _sessionType; }
            set {
                if (value == null) throw new ArgumentNullException();
                if (!(value.Equals(typeof(HttpSession)) || value.IsSubclassOf(typeof(HttpSession)))) throw new ArgumentException(string.Format("{0} is not derived from Babbacombe.Webserver.HttpSession", value.FullName));
                if (_listener.IsListening) throw new HttpServerException("Can't change session type while the server is running.");
                _sessionType = value;
            }
        }

        public IEnumerable<string> Prefixes { get { return _listener.Prefixes; } }

        public bool Running { get { return _listener.IsListening; } }

        public void Start() {
            if (Running) return;
            _listener.Start();
            ThreadPool.QueueUserWorkItem(new WaitCallback(run));
        }

        public void Stop() {
            _listener.Stop();
        }

        private void run(object o) {
            System.Diagnostics.Trace.WriteLine("Webserver started");
            while (Running) {
                ThreadPool.QueueUserWorkItem((c) => {
                    try {
                        var context = (HttpListenerContext)c;
                        try {
                            var session = Activator.CreateInstance(_sessionType) as HttpSession;
                            session.Context = context;
                            session.Response = null;
                            session.Respond();
                            if (session.Response != null) {
                                byte[] buf = Encoding.UTF8.GetBytes(session.Response);
                                context.Response.ContentLength64 = buf.Length;
                                context.Response.OutputStream.Write(buf, 0, buf.Length);
                            }
                        } catch (Exception ex) {
                            OnException(ex);
                        } finally {
                            context.Response.OutputStream.Close();
                        }
                    } catch (Exception ex) {
                        OnException(ex);
                    }
                }, _listener.GetContext());
            }
        }

        protected void OnException(Exception ex) {
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
            if (_listener != null) {
                _listener.Close();
                _listener = null;
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
