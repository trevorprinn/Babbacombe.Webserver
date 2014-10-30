using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Babbacombe.Webserver {
    
    /// <summary>
    /// Manages a template page to be sent back in a response from a request handler. The base class is a thin
    /// wrapper around an XDocument with some methods to make it easy to change tagged elements or their values.
    /// </summary>
    public class HttpPage : XDocument {
        /// <summary>
        /// The session that has created the page.
        /// </summary>
        protected HttpSession Session { get; private set; }
        
        /// <summary>
        /// The default name to use when searching for tags. This defaults to "id".
        /// </summary>
        public string DefaultTagName { get; set; }

        private HttpPage() { }

        /// <summary>
        /// Creates an HttpPage from an existing XDocument. Normally, HttpPage.Create should
        /// instead be used to load an initialise a page.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="session"></param>
        public HttpPage(XDocument data, HttpSession session) : base(data) {
            Session = session;
            DefaultTagName = "id";
        }

        /// <summary>
        /// Creates an HttpPage object from a template file.
        /// </summary>
        /// <param name="templateFile">
        /// The full path of the template file (HttpRequestHandler.CreatePage calls
        /// this but includes the request's TemplatePath).
        /// </param>
        /// <param name="session"></param>
        /// <param name="pageType">
        /// The type of HttpPage that should be created. If null, a base HttpPage is created.
        /// </param>
        /// <returns></returns>
        public static HttpPage Create(string templateFile, HttpSession session, Type pageType = null) {
            if (pageType == null) pageType = typeof(HttpPage);
            var doc = XDocument.Load(templateFile);
            return (HttpPage)Activator.CreateInstance(pageType, new object[] { doc, session });
        }

        /// <summary>
        /// Sets the response's content type to xhtml and sets up the page to be the response.
        /// </summary>
        public void Send() {
            Session.Context.Response.ContentType = "application/xhtml+xml";
            Session.SetXmlResponse(this);
        }

        /// <summary>
        /// Gets a list of the elements that have a tag matching a particular value.
        /// </summary>
        /// <param name="tag">The value to search for.</param>
        /// <param name="tagname">The tag (attribute name) to search for. Defaults to DefaultTagName, normally "id".</param>
        /// <returns></returns>
        protected IEnumerable<XElement> GetTaggedElements(string tag, string tagname = null) {
            if (tagname == null) tagname = DefaultTagName;
            return this.Descendants().Where(e => e.Attributes().Any(a => a.Name == tagname && a.Value == tag));
        }

        /// <summary>
        /// Replaces the values of all the elements that have a tag matching a particular value.
        /// </summary>
        /// <param name="tag">The value to search for.</param>
        /// <param name="text"></param>
        /// <param name="tagname">The tag (attribute name) to search for. Defaults to DefaultTagName, normally "id".</param>
        public void ReplaceValue(string tag, string text, string tagname = null) {
            foreach (var element in GetTaggedElements(tag, tagname)) {
                element.Value = text;
            }
        }

        /// <summary>
        /// Replaces all of the elements that have a tag matching a particular value.
        /// </summary>
        /// <param name="tag">The value to search for.</param>
        /// <param name="replacement"></param>
        /// <param name="tagname">The tag (attribute name) to search for. Defaults to DefaultTagName, normally "id".</param>
        public void ReplaceElement(string tag, XElement replacement, string tagname = null) {
            foreach (var element in GetTaggedElements(tag, tagname)) {
                element.ReplaceWith(replacement);
            }
        }
    }
}
