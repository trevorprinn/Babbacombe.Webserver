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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Babbacombe.Webserver {

    /// <summary>
    /// Base class for request handlers, objects whose methods can be called directly and
    /// automatically from the query items in a request url. These methods are not those of
    /// the base class, but of implementations of it.
    /// </summary>
    public abstract class HttpRequestHandler {

        /// <summary>
        /// Gets the session that has created the handler.
        /// </summary>
        protected internal HttpSession Session { get; internal set; }
        
        /// <summary>
        /// Get the folder containing template pages for this handler. Defaults to the 
        /// session's BaseFolder.
        /// </summary>
        protected virtual string TemplateFolder { get { return Session.BaseFolder; } }

        /// <summary>
        /// Loads a template document to be processed and sent to the server.
        /// </summary>
        /// <param name="templateName"></param>
        /// <param name="pageType"></param>
        /// <returns></returns>
        /// <remarks>
        /// The default implementation is just a wrapper around HttpPage.Create that
        /// looks for the template file in the TemplateFolder.
        /// </remarks>
        protected virtual HttpPage CreatePage(string templateName, Type pageType = null) {
            return HttpPage.Create(Path.Combine(TemplateFolder, templateName), Session, pageType);
        }

        /// <summary>
        /// True if the request is a POST.
        /// </summary>
        protected bool IsPost {
            get { return Session.Context.Request.HttpMethod == "POST"; }
        }

        /// <summary>
        /// A convenient way of obtaining the request's input stream.
        /// </summary>
        /// <returns></returns>
        protected Stream GetPostStream() {
            return Session.Context.Request.InputStream;
        }

        /// <summary>
        /// A convenient way of reading the request's input stream into a string.
        /// </summary>
        /// <param name="enc">If null, defaults to UTF8.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Gets the posted submit data as a set of QueryItems.
        /// </summary>
        /// <returns></returns>
        protected QueryItems GetPostedItems() {
            return QueryItems.Get(ReadPostStream());
        }

        protected QueryItems QueryItems {
            get { return Session.QueryItems; }
        }

    }
}