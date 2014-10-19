using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Babbacombe.Webserver {
    public abstract class HttpRequestHandler {
        protected internal HttpSession Session { get; internal set; }
        
        protected string GetArg(string name) {
            return Session.QueryItems.SingleOrDefault(i => i.Name == name).Value;
        }

        protected virtual string TemplateFolder { get { return Session.BaseFolder; } }

        protected virtual HttpPage CreatePage(string templateName, Type pageType = null) {
            return HttpPage.Create(Path.Combine(TemplateFolder, templateName), Session, pageType);
        }

        protected bool IsPost {
            get { return Session.Context.Request.HttpMethod == "POST"; }
        }

        protected Stream GetPostStream() {
            return Session.Context.Request.InputStream;
        }

        protected string ReadPostStream(Encoding enc = null) {
            if (enc == null) enc = Encoding.UTF8;
            using (var s = GetPostStream()) {
                StringBuilder data = new StringBuilder();
                var buf = new byte[10240];
                int count;
                do {
                    count = s.Read(buf, 0, buf.Length);
                    data.Append(enc.GetString(buf, 0, count));
                } while (count > 0);
                return data.ToString();
            }
        }

    }
}