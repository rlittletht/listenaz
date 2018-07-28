namespace listest
{
    partial class listest
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
            this.m_reHook = new System.Windows.Forms.RichTextBox();
            this.label1 = new System.Windows.Forms.Label();
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
            // m_reHook
            // 
            this.m_reHook.Location = new System.Drawing.Point(12, 204);
            this.m_reHook.Name = "m_reHook";
            this.m_reHook.Size = new System.Drawing.Size(775, 236);
            this.m_reHook.TabIndex = 1;
            this.m_reHook.Text = "";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 188);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(80, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Hooked Output";
            // 
            // listest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.m_reHook);
            this.Controls.Add(this.m_pbTestEvent);
            this.Name = "listest";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button m_pbTestEvent;
        private System.Windows.Forms.RichTextBox m_reHook;
        private System.Windows.Forms.Label label1;
    }
}

