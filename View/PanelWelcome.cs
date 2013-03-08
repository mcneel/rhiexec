using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace RMA.RhiExec.View
{
  partial class PanelWelcome : UserControl
  {
    public PanelWelcome()
    {
      InitializeComponent();
    }

    public void SetTitle(string title)
    {
      foreach (Control c in this.Controls)
      {
        Label l = c as Label;
        if (l != null)
        {
          l.Text = l.Text.Replace("[PACKAGE_TITLE]", title);
        }
      }
    }
  }
}
