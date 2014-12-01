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
            this.listMessages = new Babbacombe.WebSampleApp.ListenerListBox();
            this.SuspendLayout();
            // 
            // listMessages
            // 
            this.listMessages.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listMessages.FormattingEnabled = true;
            this.listMessages.IntegralHeight = false;
            this.listMessages.Location = new System.Drawing.Point(0, 0);
            this.listMessages.Name = "listMessages";
            this.listMessages.Size = new System.Drawing.Size(284, 262);
            this.listMessages.TabIndex = 0;
            // 
            // FormServer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 262);
            this.Controls.Add(this.listMessages);
            this.Name = "FormServer";
            this.Text = "Web Server";
            this.ResumeLayout(false);

        }

        #endregion

        private ListenerListBox listMessages;

    }
}

