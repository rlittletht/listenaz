namespace listest
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.m_pbTestEvent = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // m_pbTestEvent
            // 
            this.m_pbTestEvent.Location = new System.Drawing.Point(713, 12);
            this.m_pbTestEvent.Name = "m_pbTestEvent";
            this.m_pbTestEvent.Size = new System.Drawing.Size(75, 23);
            this.m_pbTestEvent.TabIndex = 0;
            this.m_pbTestEvent.Text = "Test Event";
            this.m_pbTestEvent.UseVisualStyleBackColor = true;
            this.m_pbTestEvent.Click += new System.EventHandler(this.m_pbTestEvent_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.m_pbTestEvent);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button m_pbTestEvent;
    }
}

