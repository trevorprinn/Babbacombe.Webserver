using Babbacombe.Logger;

namespace Babbacombe.WebSampleApp {
    partial class FormServer {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            if (disposing && _server != null) {
                _server.Dispose();
                _server = null;
                LogFile.Log("Server disposed");
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.listenerListBox1 = new Babbacombe.Logger.ListenerListBox();
            this.SuspendLayout();
            // 
            // listenerListBox1
            // 
            this.listenerListBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listenerListBox1.FormattingEnabled = true;
            this.listenerListBox1.IntegralHeight = false;
            this.listenerListBox1.Location = new System.Drawing.Point(0, 0);
            this.listenerListBox1.Name = "listenerListBox1";
            this.listenerListBox1.SelectedItem = null;
            this.listenerListBox1.Size = new System.Drawing.Size(284, 262);
            this.listenerListBox1.TabIndex = 0;
            // 
            // FormServer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 262);
            this.Controls.Add(this.listenerListBox1);
            this.Name = "FormServer";
            this.Text = "Web Server";
            this.ResumeLayout(false);

        }

        #endregion

        private Logger.ListenerListBox listenerListBox1;



    }
}

