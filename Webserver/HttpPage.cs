#region Licence
/*
    Babbacombe.Webserver
    https://github.com/trevorprinn/Babbacombe.Webserver
    Copyright © 2014 Babbacombe Computers Ltd.

    This library is free software; you can redistribute it and/or
    modify it under the terms of the GNU Lesser General Public
    License as published by the Free Software Foundation; either
    version 2.1 of the License, or (at your option) any later version.

    This library is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
    Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public
    License along with this library; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301
    USA
 */
#endregion
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Babbacombe.Webserver {
    
    /// <summary>
    /// Manages a template page to be sent back in a response from a request handler. The base class is a thin
    /// wrapper around an XDocument with some methods to make it easy to change tagged elements or their values.
    /// </summary>
    /// <remarks>
    /// This class assumes that all the markup is in lower case.
    /// </remarks>
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
        /// Creates an HttpPage from an existing XDocument.HttpPage.Create can
        /// instead be used to load and initialise a page.
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
            return Create(doc, session, pageType, null);
        }

        /// <summary>
        /// Used when creating a page to be sent directly and set up using OnPreparePage
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="session"></param>
        /// <param name="pageType"></param>
        /// <returns></returns>
        internal static HttpPage Create(XDocument doc, HttpSession session, Type pageType, string defaultTagName) {
            var page = (HttpPage)Activator.CreateInstance(pageType, new object[] { doc, session });
            if (defaultTagName != null) page.DefaultTagName = defaultTagName;
            return page;
        }

        /// <summary>
        /// Called automatically when an HttpPage derivative is detected and created by the Webserver. This should
        /// be overridden to set up the page's contents.
        /// </summary>
        internal protected virtual void OnPreparePage() { }

        /// <summary>
        /// Sets the response's content type to xhtml and sets up the page to be the response.
        /// </summary>
        public void Send(HttpSession.RequestData requestData = null) {
            if (requestData == null) requestData = Session.GetRequestData();
            requestData.Context.Response.ContentType = "application/xhtml+xml";
            requestData.Response = ToString();
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
        /// Replaces the values of all the elements that have a tag matching a particular value. For input
        /// elements, the value attribute is set.
        /// </summary>
        /// <param name="tag">The value to search for.</param>
        /// <param name="text"></param>
        /// <param name="tagname">The tag (attribute name) to search for. Defaults to DefaultTagName, normally "id".</param>
        public void ReplaceValue(string tag, string text, string tagname = null) {
            foreach (var element in GetTaggedElements(tag, tagname)) {
                if (element.Name.LocalName == "input") {
                    element.SetAttributeValue("value", text);
                } else {
                    element.Value = text;
                }
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

        /// <summary>
        /// Replaces an attribute value for all the elements that have a tag matching a particular value.
        /// </summary>
        /// <param name="tag">The value to search for.</param>
        /// <param name="attribute">The attribute to add or replace.</param>
        /// <param name="text"></param>
        /// <param name="tagname">The tag (attribute name) to search for. Defaults to DefaultTagName, normally "id".</param>
        public void ReplaceAttribute(string tag, string attribute, string text, string tagname = null) {
            foreach (var element in GetTaggedElements(tag, tagname)) {
                element.SetAttributeValue(attribute, text);
            }
        }

        /// <summary>
        /// Gets or sets the Page title.
        /// </summary>
        public string Title {
            get {
                var ns = Root.GetDefaultNamespace();
                var title = ((IEnumerable<XElement>)Root.XPathEvaluate("head/title")).FirstOrDefault();
                if (title != null) return title.Value;
                var head = Root.Element(ns + "head");
                if (head == null) return null;
                title = head.Element(ns + "title");
                if (title == null) return null;
                return title.Value;
            }
            set {
                var ns = Root.GetDefaultNamespace();
                var head = Root.Element(ns + "head");
                if (head == null) Root.AddFirst(head = new XElement(ns + "head"));
                head.SetElementValue(ns + "title", value);
            }
        }

        /// <summary>
        /// Replaces items in the page with the values of properties in the model.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="tagname"></param>
        public void ApplyModel(object model, string tagname = null) {
            if (tagname == null) tagname = DefaultTagName;
            // Loop through the non-array properties that have a Get.
            foreach (var prop in model.GetType().GetProperties().Where(p => p.CanRead)) {
                // Default the item type to Value unless the property returns an XElement type.
                var itemType = typeof(XElement).IsAssignableFrom(prop.PropertyType)
                    ? PageModelItemTypes.Element : PageModelItemTypes.Value;

                // Check the property to see if the item type has been overridden by a PageModelItemType attribute.
                var typeAttr = prop.GetCustomAttribute<PageModelItemTypeAttribute>();
                string tag = prop.Name;
                string attribute = prop.Name;
                if (typeAttr != null) {
                    itemType = typeAttr.ItemType;
                    tag = typeAttr.Tag ?? tag;
                    attribute = typeAttr.Attribute ?? attribute;
                }

                object propValue = prop.GetValue(model);

                switch (itemType) {
                    case PageModelItemTypes.Value:
                        ReplaceValue(tag, propValue == null ? null : propValue.ToString(), tagname);
                        break;
                    case PageModelItemTypes.Attribute:
                        ReplaceAttribute(tag, attribute, propValue == null ? null : propValue.ToString(), tagname);
                        break;
                    case PageModelItemTypes.Element:
                        ReplaceElement(tag, (XElement)propValue, tagname);
                        break;
                    case PageModelItemTypes.Table:
                        loadTable(tag, (IEnumerable)propValue, tagname);
                        break;
                    default:
                        break;
                }
            }
        }

        private void loadTable(string tag, IEnumerable rowsData, string tagname) {
            // Find the table element
            var table = GetTaggedElements(tag, tagname).Where(e => e.Name.LocalName == "table").FirstOrDefault();
            if (table == null) return;
            
            // Get the names of the properties that contain the header and data values
            string headerNames = (string)table.Attribute("rowheader");
            string[] headerName = string.IsNullOrWhiteSpace(headerNames) ? new string[0] : headerNames.Split(',');
            string dataNames = (string)table.Attribute("rowdata");
            string[] dataName = string.IsNullOrWhiteSpace(dataNames) ? new string[0] : dataNames.Split(',');

            // Clear the non-html attributes
            table.SetAttributeValue("rowheader", null);
            table.SetAttributeValue("rowdata", null);

            // If there's no data, do nothing more
            if (rowsData == null) return;

            foreach (object rowData in rowsData) {
                // Ignore any rows that aren't objects.
                if (rowData.GetType().IsValueType) continue;
                var row = new XElement(Root.GetDefaultNamespace() + "tr");
                foreach (var h in headerName) row.Add(createTableData(rowData, h, true));
                foreach (var d in dataName) row.Add(createTableData(rowData, d, false));
                table.Add(row);
            }
        }

        private XElement createTableData(object row, string propName, bool isHeader) {
            string value = null;
            var prop = row.GetType().GetProperty(propName);
            if (prop != null) value = prop.GetValue(row).ToString();
            var data = new XElement(Root.GetDefaultNamespace() + (isHeader ? "th" : "td"), value);

            string className = null;
            var classAttr = prop != null ? prop.GetCustomAttribute<TableDataItemClassAttribute>() : null;
            if (classAttr != null) className = classAttr.ClassName;
            // Check for a property with the same name but suffixed with Class. This will override the attribute.
            var classProp = row.GetType().GetProperty(propName + "Class");
            if (classProp != null) {
                var classPropValue = classProp.GetValue(row);
                className = classPropValue != null ? classPropValue.ToString() : null;
            }
            if (className != null) data.SetAttributeValue("class", className);
            return data;
        }
    }

    public class HttpErrorPage : HttpPage {
        public HttpErrorPage(HttpSession session, int status, string error, string description = null, string details = null) 
            : base(getDoc(), session) {
                Title = error;
                ReplaceValue("error", error);
                ReplaceValue("status", status.ToString());
                ReplaceValue("description", string.IsNullOrEmpty(description) ? error : description);
                if (session.Server.RespondWithExceptionDetails) ReplaceValue("details", details);
                session.Context.Response.StatusCode = status;
                session.Context.Response.StatusDescription = error;
        }

        public HttpErrorPage(HttpSession session, string details) : this(session, 500, "Internal Server Error", null, details) { }

        private static XDocument getDoc() {
            using (var s = new System.IO.StreamReader(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Babbacombe.Webserver.DefaultErrorPage.html"))) {
                return XDocument.Parse(s.ReadToEnd());
            }
        }
    }

    /// <summary>
    /// Defines how a model property value is used in HttpPage.ApplyModel
    /// </summary>
    public enum PageModelItemTypes {
        Value, Attribute, Element, Table, None
    }

    /// <summary>
    /// Defines how a property value is used in HttpPage.ApplyModel.
    /// The ItemType default is Value except for XElement types, where it is Element.
    /// None causes the property to be ignored when applying the model.
    /// The tag name (normally the property name) can be overridden. The attribute name
    /// (by default also the property name) should be overridden if the type is Attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class PageModelItemTypeAttribute : Attribute {
        public PageModelItemTypes ItemType { get; private set; }
        public string Tag { get; private set; }
        public string Attribute { get; private set; }

        private PageModelItemTypeAttribute() { }

        public PageModelItemTypeAttribute(PageModelItemTypes itemType, string tag = null, string attribute = null) {
            ItemType = itemType;
            Tag = tag;
            Attribute = attribute;
        }
    }

    /// <summary>
    /// Can be used to set the class for a th or td cell by property
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class TableDataItemClassAttribute : Attribute {
        public string ClassName { get; set; }
        public TableDataItemClassAttribute(string className) {
            ClassName = className;
        }
    }
}

