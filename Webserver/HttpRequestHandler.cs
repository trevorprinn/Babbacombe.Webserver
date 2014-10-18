using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Babbacombe.Webserver {
    public abstract class HttpRequestHandler {
        protected internal HttpSession Session { get; internal set; }

        protected string GetArg(string name) {
            return Session.QueryItems.SingleOrDefault(i => i.Name == name).Value;
        }
    }
}
