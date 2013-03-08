namespace RMA.RhiExec.View
{
  partial class InitializingRhinoDialog
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
      System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InitializingRhinoDialog));
      this.m_label_title = new System.Windows.Forms.Label();
      this.m_img_background = new System.Windows.Forms.PictureBox();
      this.m_txt_body = new System.Windows.Forms.Label();
      ((System.ComponentModel.ISupportInitialize)(this.m_img_background)).BeginInit();
      this.SuspendLayout();
      // 
      // m_label_title
      // 
      resources.ApplyResources(this.m_label_title, "m_label_title");
      this.m_label_title.BackColor = System.Drawing.Color.White;
      this.m_label_title.Name = "m_label_title";
      // 
      // m_img_background
      // 
      this.m_img_background.BackColor = System.Drawing.Color.White;
      resources.ApplyResources(this.m_img_background, "m_img_background");
      this.m_img_background.Image = global::RMA.RhiExec.Properties.Resources.InitializingRhino;
      this.m_img_background.InitialImage = global::RMA.RhiExec.Properties.Resources.InitializingRhino;
      this.m_img_background.Name = "m_img_background";
      this.m_img_background.TabStop = false;
      // 
      // m_txt_body
      // 
      this.m_txt_body.BackColor = System.Drawing.Color.White;
      resources.ApplyResources(this.m_txt_body, "m_txt_body");
      this.m_txt_body.Name = "m_txt_body";
      // 
      // InitializingRhinoDialog
      // 
      resources.ApplyResources(this, "$this");
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.BackColor = System.Drawing.Color.White;
      this.ControlBox = false;
      this.Controls.Add(this.m_txt_body);
      this.Controls.Add(this.m_label_title);
      this.Controls.Add(this.m_img_background);
      this.DoubleBuffered = true;
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.Name = "InitializingRhinoDialog";
      this.ShowIcon = false;
      this.ShowInTaskbar = false;
      this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
      this.Load += new System.EventHandler(this.DialogLoad);
      this.Shown += new System.EventHandler(this.InitializingRhinoDialog_Shown);
      ((System.ComponentModel.ISupportInitialize)(this.m_img_background)).EndInit();
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.Label m_label_title;
    private System.Windows.Forms.PictureBox m_img_background;
    private System.Windows.Forms.Label m_txt_body;
  }
}