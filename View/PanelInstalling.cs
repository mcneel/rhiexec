using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace RMA.RhiExec.View
{
  partial class PanelInstalling : UserControl
  {
    public PanelInstalling()
    {
      InitializeComponent();
      m_progress_bar.Step = 10;
      m_progress_bar.Minimum = 0;
      m_progress_bar.Maximum = 100;
    }

    public void setProgress(int percentComplete)
    {
      m_progress_bar.Value = percentComplete;
    }
  }
}
