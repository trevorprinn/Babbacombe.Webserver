using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Babbacombe.Webserver;

namespace Babbacombe.WebSampleApp.Test {
    class HttpSession : Babbacombe.Webserver.HttpSession, IDisposable {
        private TimeSpan _expiryTime;
        private FormSession _form;

        protected override void OnCreated() {
            base.OnCreated();
            _expiryTime = base.ExpiryTime;
            FormServer.Instance.BeginInvoke(new Action(() => {
                _form = new FormSession(this);
                _form.Text = "Session: " + SessionId;
                _form.Show();
            }));
        }

        protected override TimeSpan ExpiryTime {
            get { return _expiryTime; }
        }

        public int ExpireSecs {
            get { return (int)_expiryTime.TotalSeconds; }
            set { _expiryTime = new TimeSpan(0, 0, value); }
        }

        public void Dispose() {
            if (_form != null) {
                _form.BeginInvoke(new Action(() => {
                    _form.Dispose();
                    _form = null;
                }));
            }
        }
    }
}
