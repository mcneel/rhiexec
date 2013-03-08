using System;
using System.Collections.Generic;
using System.Text;
using RMA.RhiExec.Model;

namespace RMA.RhiExec
{
  class ProgressReport
  {
    InstallerPhase m_Phase = InstallerPhase.Unknown;
    LogLevel m_Level = LogLevel.Info;
    string m_message = "";

    public InstallerPhase Phase
    {
      get { return m_Phase; }
      set { m_Phase = value; }
    }

    public LogLevel Level
    {
      get { return m_Level; }
      set { m_Level = value; }
    }

    public string Message
    {
      get { return m_message; }
      set { m_message = value; }
    }
  }
}
