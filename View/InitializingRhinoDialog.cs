using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace RMA.RhiExec.View
{
  internal partial class InitializingRhinoDialog : Form
  {
    RMA.RhiExec.Engine.RhinoInitializer m_initializer = null;
    BackgroundWorker m_worker = new BackgroundWorker();

    public InitializingRhinoDialog(RMA.RhiExec.Engine.RhinoInitializer initializer)
    {
      InitializeComponent();
      m_initializer = initializer;
      m_worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(m_worker_RunWorkerCompleted);
      m_worker.DoWork += new DoWorkEventHandler(m_worker_DoWork);
    }

    void m_worker_DoWork(object sender, DoWorkEventArgs e)
    {
      m_initializer.DoInstallPackages();
    }

    void m_worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
      this.Close();
    }

    public void InitializingRhinoDialog_Shown(object sender, EventArgs e)
    {
      m_worker.RunWorkerAsync();
    }

    private void DialogLoad(object sender, EventArgs e)
    {
      Rhino.UI.Localization.LocalizeForm(this);
    }

  }
}