namespace RMA.RhiExec.View
{
  partial class PanelWelcome
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
      this.label4 = new System.Windows.Forms.Label();
      this.radioButton2 = new System.Windows.Forms.RadioButton();
      this.radioButton1 = new System.Windows.Forms.RadioButton();
      this.label3 = new System.Windows.Forms.Label();
      this.label2 = new System.Windows.Forms.Label();
      this.SuspendLayout();
      // 
      // label4
      // 
      this.label4.AutoSize = true;
      this.label4.Location = new System.Drawing.Point(-3, 127);
      this.label4.Name = "label4";
      this.label4.Size = new System.Drawing.Size(157, 13);
      this.label4.TabIndex = 18;
      this.label4.Text = "Click Next to begin installation...";
      // 
      // radioButton2
      // 
      this.radioButton2.AutoSize = true;
      this.radioButton2.Enabled = false;
      this.radioButton2.Location = new System.Drawing.Point(19, 75);
      this.radioButton2.Name = "radioButton2";
      this.radioButton2.Size = new System.Drawing.Size(175, 17);
      this.radioButton2.TabIndex = 17;
      this.radioButton2.Text = "Anyone who uses this computer";
      this.radioButton2.UseVisualStyleBackColor = true;
      // 
      // radioButton1
      // 
      this.radioButton1.AutoSize = true;
      this.radioButton1.Checked = true;
      this.radioButton1.Location = new System.Drawing.Point(19, 54);
      this.radioButton1.Name = "radioButton1";
      this.radioButton1.Size = new System.Drawing.Size(62, 17);
      this.radioButton1.TabIndex = 16;
      this.radioButton1.TabStop = true;
      this.radioButton1.Text = "Just Me";
      this.radioButton1.UseVisualStyleBackColor = true;
      // 
      // label3
      // 
      this.label3.AutoSize = true;
      this.label3.Location = new System.Drawing.Point(-3, 36);
      this.label3.Name = "label3";
      this.label3.Size = new System.Drawing.Size(147, 13);
      this.label3.TabIndex = 15;
      this.label3.Text = "Install [PACKAGE_TITLE] for:";
      // 
      // label2
      // 
      this.label2.AutoSize = true;
      this.label2.Location = new System.Drawing.Point(-3, 0);
      this.label2.Name = "label2";
      this.label2.Size = new System.Drawing.Size(274, 13);
      this.label2.TabIndex = 14;
      this.label2.Text = "[PACKAGE_TITLE] is about to be installed for Rhino 5.0.";
      // 
      // PanelWelcome
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.Controls.Add(this.label4);
      this.Controls.Add(this.radioButton2);
      this.Controls.Add(this.radioButton1);
      this.Controls.Add(this.label3);
      this.Controls.Add(this.label2);
      this.Name = "PanelWelcome";
      this.Size = new System.Drawing.Size(265, 163);
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.Label label4;
    private System.Windows.Forms.RadioButton radioButton2;
    private System.Windows.Forms.RadioButton radioButton1;
    private System.Windows.Forms.Label label3;
    private System.Windows.Forms.Label label2;
  }
}
