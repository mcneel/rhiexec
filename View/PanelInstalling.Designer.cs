namespace RMA.RhiExec.View
{
  partial class PanelInstalling
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

    #region Component Designer generated code

    /// <summary> 
    /// Required method for Designer support - do not modify 
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
      this.m_progress_bar = new System.Windows.Forms.ProgressBar();
      this.label1 = new System.Windows.Forms.Label();
      this.SuspendLayout();
      // 
      // m_progress_bar
      // 
      this.m_progress_bar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.m_progress_bar.Location = new System.Drawing.Point(3, 62);
      this.m_progress_bar.Name = "m_progress_bar";
      this.m_progress_bar.Size = new System.Drawing.Size(284, 23);
      this.m_progress_bar.TabIndex = 0;
      // 
      // label1
      // 
      this.label1.AutoSize = true;
      this.label1.Location = new System.Drawing.Point(4, 43);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(57, 13);
      this.label1.TabIndex = 1;
      this.label1.Text = "Installing...";
      // 
      // PanelInstalling
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.Controls.Add(this.label1);
      this.Controls.Add(this.m_progress_bar);
      this.Name = "PanelInstalling";
      this.Size = new System.Drawing.Size(290, 183);
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.ProgressBar m_progress_bar;
    private System.Windows.Forms.Label label1;
  }
}
