namespace RMA.RhiExec.View
{
  partial class InstallerDialog
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
      System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InstallerDialog));
      this.pictureBox1 = new System.Windows.Forms.PictureBox();
      this.btnNext = new System.Windows.Forms.Button();
      this.btnCancel = new System.Windows.Forms.Button();
      this.label1 = new System.Windows.Forms.Label();
      this.btnBack = new System.Windows.Forms.Button();
      this.m_Panel = new System.Windows.Forms.UserControl();
      ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
      this.SuspendLayout();
      // 
      // pictureBox1
      // 
      resources.ApplyResources(this.pictureBox1, "pictureBox1");
      this.pictureBox1.Name = "pictureBox1";
      this.pictureBox1.TabStop = false;
      // 
      // btnNext
      // 
      resources.ApplyResources(this.btnNext, "btnNext");
      this.btnNext.Name = "btnNext";
      this.btnNext.UseVisualStyleBackColor = true;
      this.btnNext.Click += new System.EventHandler(this.btnNext_Click);
      // 
      // btnCancel
      // 
      resources.ApplyResources(this.btnCancel, "btnCancel");
      this.btnCancel.Name = "btnCancel";
      this.btnCancel.UseVisualStyleBackColor = true;
      this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
      // 
      // label1
      // 
      resources.ApplyResources(this.label1, "label1");
      this.label1.Name = "label1";
      // 
      // btnBack
      // 
      resources.ApplyResources(this.btnBack, "btnBack");
      this.btnBack.Name = "btnBack";
      this.btnBack.UseVisualStyleBackColor = true;
      // 
      // m_Panel
      // 
      resources.ApplyResources(this.m_Panel, "m_Panel");
      this.m_Panel.Name = "m_Panel";
      // 
      // InstallerDialog
      // 
      resources.ApplyResources(this, "$this");
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.BackColor = System.Drawing.SystemColors.Control;
      this.Controls.Add(this.btnBack);
      this.Controls.Add(this.m_Panel);
      this.Controls.Add(this.label1);
      this.Controls.Add(this.btnCancel);
      this.Controls.Add(this.btnNext);
      this.Controls.Add(this.pictureBox1);
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
      this.Name = "InstallerDialog";
      this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.InstallerDialog_FormClosing);
      this.Load += new System.EventHandler(this.InstallerDialog_Load);
      this.Shown += new System.EventHandler(this.InstallerDialog_Shown);
      ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.PictureBox pictureBox1;
    private System.Windows.Forms.Button btnNext;
    private System.Windows.Forms.Button btnCancel;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Button btnBack;
    private System.Windows.Forms.UserControl m_Panel;

  }
}

