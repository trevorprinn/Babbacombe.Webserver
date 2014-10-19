using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Babbacombe.Webserver {
    public class HttpPage : XElement {
        protected HttpSession Session { get; private set; }
        public string TagName { get; set; }

        private HttpPage() : base("temp") { }

        public HttpPage(XElement data, HttpSession session) : base(data) {
            Session = session;
        }

        public static HttpPage Create(string templateFile, HttpSession session, Type pageType = null) {
            if (pageType == null) pageType = typeof(HttpPage);
            var doc = XDocument.Load(templateFile);
            return (HttpPage)Activator.CreateInstance(pageType, new object[] { doc.Root, session });
        }

        public void Send() {
            Session.SetXmlResponse(this);
        }

        protected IEnumerable<XElement> GetTaggedElements(string tag) {
            return this.Descendants().Where(e => e.Attributes().Any(a => a.Name == "Tag" && a.Value == tag));
        }

        public void ReplaceValue(string tag, string text) {
            foreach (var element in GetTaggedElements(tag)) {
                element.Value = text;
            }
        }

        public void ReplaceElement(string tag, XElement replacement) {
            foreach (var element in GetTaggedElements(tag)) {
                element.ReplaceWith(replacement);
            }
        }
    }
}
