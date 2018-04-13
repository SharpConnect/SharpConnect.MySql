namespace TestMySqlParser
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tlstrpRefreshDBs = new System.Windows.Forms.ToolStripButton();
            this.tlstrpGenCsCode = new System.Windows.Forms.ToolStripButton();
            this.button1 = new System.Windows.Forms.Button();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // treeView1
            // 
            this.treeView1.Location = new System.Drawing.Point(0, 28);
            this.treeView1.Name = "treeView1";
            this.treeView1.Size = new System.Drawing.Size(333, 566);
            this.treeView1.TabIndex = 5;
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(339, 28);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBox1.Size = new System.Drawing.Size(677, 566);
            this.textBox1.TabIndex = 7;
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tlstrpRefreshDBs,
            this.tlstrpGenCsCode});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1041, 25);
            this.toolStrip1.TabIndex = 8;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tlstrpRefreshDBs
            // 
            this.tlstrpRefreshDBs.Image = ((System.Drawing.Image)(resources.GetObject("tlstrpRefreshDBs.Image")));
            this.tlstrpRefreshDBs.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tlstrpRefreshDBs.Name = "tlstrpRefreshDBs";
            this.tlstrpRefreshDBs.Size = new System.Drawing.Size(86, 22);
            this.tlstrpRefreshDBs.Text = "RefreshDBs";
            this.tlstrpRefreshDBs.Click += new System.EventHandler(this.tlstrpRefreshDBs_Click);
            // 
            // tlstrpGenCsCode
            // 
            this.tlstrpGenCsCode.Image = ((System.Drawing.Image)(resources.GetObject("tlstrpGenCsCode.Image")));
            this.tlstrpGenCsCode.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tlstrpGenCsCode.Name = "tlstrpGenCsCode";
            this.tlstrpGenCsCode.Size = new System.Drawing.Size(89, 22);
            this.tlstrpGenCsCode.Text = "GenCsCode";
            this.tlstrpGenCsCode.Click += new System.EventHandler(this.tlstrpGenCsCode_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(12, 615);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(124, 49);
            this.button1.TabIndex = 9;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1041, 810);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.treeView1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tlstrpRefreshDBs;
        private System.Windows.Forms.ToolStripButton tlstrpGenCsCode;
        private System.Windows.Forms.Button button1;
    }
}

