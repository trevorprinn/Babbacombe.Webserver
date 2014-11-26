namespace Babbacombe.WebSampleApp.Test {
    partial class FormSession {
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
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.label1 = new System.Windows.Forms.Label();
            this.numExpiry = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.numExpiry)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 7);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(38, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Expiry:";
            // 
            // numExpiry
            // 
            this.numExpiry.Location = new System.Drawing.Point(50, 5);
            this.numExpiry.Maximum = new decimal(new int[] {
            86400,
            0,
            0,
            0});
            this.numExpiry.Name = "numExpiry";
            this.numExpiry.Size = new System.Drawing.Size(84, 20);
            this.numExpiry.TabIndex = 1;
            this.numExpiry.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numExpiry.ThousandsSeparator = true;
            this.numExpiry.ValueChanged += new System.EventHandler(this.numExpiry_ValueChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(140, 7);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(29, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "secs";
            // 
            // FormSession
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(430, 268);
            this.ControlBox = false;
            this.Controls.Add(this.label2);
            this.Controls.Add(this.numExpiry);
            this.Controls.Add(this.label1);
            this.Name = "FormSession";
            this.Text = "Session";
            ((System.ComponentModel.ISupportInitialize)(this.numExpiry)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown numExpiry;
        private System.Windows.Forms.Label label2;
    }
}