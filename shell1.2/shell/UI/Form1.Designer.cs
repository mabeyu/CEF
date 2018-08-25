namespace shell
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.panel1 = new System.Windows.Forms.Panel();
            this.fileSystemWatcher1 = new System.IO.FileSystemWatcher();
            this.fileSystemWatcher2 = new System.IO.FileSystemWatcher();
            this.fileSystemWatcher3 = new System.IO.FileSystemWatcher();
            this.fileSystemWatcher4 = new System.IO.FileSystemWatcher();
            this.fileSystemWatcher5 = new System.IO.FileSystemWatcher();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            ((System.ComponentModel.ISupportInitialize)(this.fileSystemWatcher1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.fileSystemWatcher2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.fileSystemWatcher3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.fileSystemWatcher4)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.fileSystemWatcher5)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.ForeColor = System.Drawing.Color.Transparent;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(876, 470);
            this.panel1.TabIndex = 0;
            // 
            // fileSystemWatcher1
            // 
            this.fileSystemWatcher1.EnableRaisingEvents = true;
            this.fileSystemWatcher1.Path = "D:\\ETCAD\\制版\\";
            this.fileSystemWatcher1.SynchronizingObject = this;
            this.fileSystemWatcher1.Created += new System.IO.FileSystemEventHandler(this.fileSystemWatcher1_Created);
            // 
            // fileSystemWatcher2
            // 
            this.fileSystemWatcher2.EnableRaisingEvents = true;
            this.fileSystemWatcher2.Path = "D:\\ETCAD\\裁剪";
            this.fileSystemWatcher2.SynchronizingObject = this;
            this.fileSystemWatcher2.Created += new System.IO.FileSystemEventHandler(this.fileSystemWatcher2_Created);
            // 
            // fileSystemWatcher3
            // 
            this.fileSystemWatcher3.EnableRaisingEvents = true;
            this.fileSystemWatcher3.Path = "D:\\ETCAD\\绣花操作";
            this.fileSystemWatcher3.SynchronizingObject = this;
            this.fileSystemWatcher3.Created += new System.IO.FileSystemEventHandler(this.fileSystemWatcher3_Created);
            // 
            // fileSystemWatcher4
            // 
            this.fileSystemWatcher4.EnableRaisingEvents = true;
            this.fileSystemWatcher4.Path = "D:\\ETCAD\\排料";
            this.fileSystemWatcher4.SynchronizingObject = this;
            this.fileSystemWatcher4.Created += new System.IO.FileSystemEventHandler(this.fileSystemWatcher4_Created);
            // 
            // fileSystemWatcher5
            // 
            this.fileSystemWatcher5.EnableRaisingEvents = true;
            this.fileSystemWatcher5.Path = "D:\\ETCAD\\绣花列表";
            this.fileSystemWatcher5.SynchronizingObject = this;
            this.fileSystemWatcher5.Created += new System.IO.FileSystemEventHandler(this.fileSystemWatcher5_Created);
            // 
            // backgroundWorker1
            // 
            this.backgroundWorker1.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker1_DoWork);
            this.backgroundWorker1.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker1_RunWorkerCompleted);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(876, 470);
            this.Controls.Add(this.panel1);
            this.KeyPreview = true;
            this.Name = "Form1";
            this.Text = "浙江大学创新技术研究院 DPS";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.fileSystemWatcher1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.fileSystemWatcher2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.fileSystemWatcher3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.fileSystemWatcher4)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.fileSystemWatcher5)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.IO.FileSystemWatcher fileSystemWatcher1;
        private System.IO.FileSystemWatcher fileSystemWatcher2;
        private System.IO.FileSystemWatcher fileSystemWatcher3;
        private System.IO.FileSystemWatcher fileSystemWatcher4;
        private System.IO.FileSystemWatcher fileSystemWatcher5;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
    }
}

