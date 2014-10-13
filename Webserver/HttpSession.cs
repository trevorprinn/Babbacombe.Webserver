using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Babbacombe.Webserver {
    public class HttpSession {

        protected internal HttpListenerContext Context { get; internal set; }

        protected internal virtual void Respond() {
            var url = Context.Request.Url;
            var fname = url.AbsolutePath;
            fname = fname.TrimStart('/');
            SendFile(Path.Combine(".", fname));
        }

        protected internal string Response { get; set; }

        protected void SetXmlResponse(XElement data) {
            Response = data.ToString();
        }

        protected void SetXmlBody(XElement body) {
            var html = new XElement("html", body);
            SetXmlResponse(html);
        }

        protected void SendFile(string filename) {
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
    }
}
