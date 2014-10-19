using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Babbacombe.Webserver {
    public class HttpPage : XDocument {
        protected HttpSession Session { get; private set; }
        public string TagName { get; set; }
        public string DefaultTagName = "id";

        public HttpPage() : base() { }

        public HttpPage(XDocument data, HttpSession session) : base(data) {
            Session = session;
        }

        public static HttpPage Create(string templateFile, HttpSession session, Type pageType = null) {
            if (pageType == null) pageType = typeof(HttpPage);
            var doc = XDocument.Load(templateFile);
            return (HttpPage)Activator.CreateInstance(pageType, new object[] { doc, session });
        }

        public void Send() {
            Session.Context.Response.ContentType = "application/xhtml+xml";
            Session.SetXmlResponse(this);
        }

        protected IEnumerable<XElement> GetTaggedElements(string tag, string tagname = null) {
            if (tagname == null) tagname = DefaultTagName;
            return this.Descendants().Where(e => e.Attributes().Any(a => a.Name == tagname && a.Value == tag));
        }

        public void ReplaceValue(string tag, string text, string tagname = null) {
            foreach (var element in GetTaggedElements(tag)) {
                element.Value = text;
            }
        }

        public void ReplaceElement(string tag, XElement replacement, string tagname = null) {
            foreach (var element in GetTaggedElements(tag)) {
                element.ReplaceWith(replacement);
            }
        }
    }
}
