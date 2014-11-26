using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Babbacombe.WebSampleApp.Test {
    partial class FormSession : Form {
        private HttpSession _session;

        public FormSession(HttpSession session) {
            InitializeComponent();

            _session = session;
            numExpiry.Value = _session.ExpireSecs;
        }

        private void numExpiry_ValueChanged(object sender, EventArgs e) {
            _session.ExpireSecs = (int)numExpiry.Value;
        }
    }
}
