using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace RMA.RhiExec.View
{
  partial class PanelMessage : UserControl
  {
    public PanelMessage()
    {
      InitializeComponent();
    }

    public void SetMessage(string message)
    {
      m_label.Text = message;
    }
  }
}
