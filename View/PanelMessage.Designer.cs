namespace RMA.RhiExec.View
{
  partial class PanelMessage
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
      this.m_label = new System.Windows.Forms.Label();
      this.SuspendLayout();
      // 
      // m_label
      // 
      this.m_label.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                  | System.Windows.Forms.AnchorStyles.Left)
                  | System.Windows.Forms.AnchorStyles.Right)));
      this.m_label.Location = new System.Drawing.Point(0, 0);
      this.m_label.Name = "m_label";
      this.m_label.Size = new System.Drawing.Size(354, 189);
      this.m_label.TabIndex = 1;
      this.m_label.Text = "An error occurred during installation.";
      // 
      // PanelMessage
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.Controls.Add(this.m_label);
      this.Name = "PanelMessage";
      this.Size = new System.Drawing.Size(354, 189);
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.Label m_label;
  }
}
