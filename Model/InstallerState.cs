using System;
using System.Collections.Generic;
using System.Text;
using RMA.RhiExec.Engine;

namespace RMA.RhiExec.Model
{
  class InstallerState
  {
    private object m_lock_obj = new object();
    private string m_installer_file = "";
    private string m_temporary_folder = "";
    private InstallerPhase m_current_phase = InstallerPhase.Start;
    private InstallerUser m_user = InstallerUser.CurrentUser;
    private List<RhinoInfo> m_rhinos = new List<RhinoInfo>();
    private RhinoInstallState m_compatible_rhino_installed = RhinoInstallState.Unknown;
    private bool m_exiting; // = false

    public string InstallerFilePath
    {
      get
      {
        lock (m_lock_obj)
          return m_installer_file;
      }
    }

    public string TemporaryFolder
    {
      get
      {
        lock (m_lock_obj)
          return m_temporary_folder;
      }
      set
      {
        lock (m_lock_obj)
          m_temporary_folder = value;
      }
    }

    public bool ApplicationExiting
    {
      get
      {
        lock (m_lock_obj)
          return m_exiting;
      }
      set
      {
        lock (m_lock_obj)
          m_exiting = value;
      }
    }

    public InstallerPhase CurrentPhase
    {
      get
      {
        lock (m_lock_obj)
          return m_current_phase;
      }
      set
      {
        lock (m_lock_obj)
          m_current_phase = value;
      }
    }

    public InstallerUser User
    {
      get
      {
        lock (m_lock_obj)
          return m_user;
      }
    }

    public void AddRhino(RhinoInfo info)
    {
      lock (m_lock_obj)
        m_rhinos.Add(info);
    }

    public RhinoInfo[] RhinoList
    {
      get
      {
        lock (m_lock_obj)
          return m_rhinos.ToArray();
      }
    }

    public RhinoInstallState CompatibleRhinoIsInstalled
    {
      get
      {
        lock (m_lock_obj)
          return m_compatible_rhino_installed;
      }
      set
      {
        lock (m_lock_obj)
          m_compatible_rhino_installed = value;
      }
    }

    }

  // These enum values are also application return codes.
  // Do not change the number mapped to any of these enum values
  // because it will break compatibility with other versions of
  // rhiexec that may be spawned by this version.
  public enum InstallerPhase
  {
    // Success
    Success = 0,
    Complete = 0,

    // Incomplete
    Unknown = 300,
    Start = 400,
    InitializeDialog = 401,
    Initializing = 402,
    Initialized = 403,
    AlreadyInstalled = 404,
    WelcomeDialog = 405,
    InstallingDialog = 406,
    Installing = 407,
    Registering = 408,
    Cleanup = 409,
    CompleteDialog = 410,
    Canceled = 411,

    // Failure
    InitializationFailed = 500,
    InstallFailed = 501,
    RegistrationFailed = 502,
    Exception = 503,
    PackageNotFound = 504,
    PackageNotSpecified = 505,

    // Inspection Failure
    InspctWrkDirMissing = 520,
    InspctFailed = 521,
    InspctPkgNotCompatible = 522,

    // Already Running
    AlreadyRunning = 550,
  }
}
