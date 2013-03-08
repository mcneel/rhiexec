using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using System.Windows.Forms;
using System.ComponentModel;
using System.Threading;
using RMA.RhiExec.Model;
using RMA.RhiExec.View;


namespace RMA.RhiExec.Engine
{
  static class InstallerEngine
  {
    #region Static Members - be careful about thread safety!
    static int m_main_thread_id = 0;
    static InstallerState m_state = new InstallerState();
    static string InstanceGuid = Guid.NewGuid().ToString();
    static string m_temp_folder; // = null;
    static InstallerDialog m_user_interface;
    static List<PackageInstallerBase> m_package_installers = new List<PackageInstallerBase>();
    static int m_selected_installer_index = -1;
    static Package m_package;
    static PackageInstallerBase SelectedInstaller
    {
      get
      {
        if (m_selected_installer_index > -1 && m_selected_installer_index < m_package_installers.Count)
          return m_package_installers[m_selected_installer_index];
        
        return null;
      }
    }
    #endregion

    static InstallerEngine()
    {
      m_package_installers.Add(new PackageInstallerTutorial());
      m_package_installers.Add(new PackageInstallerLocalization());
      m_package_installers.Add(new PackageInstallerHelpMedia());
      m_package_installers.Add(new PackageInstallerHelp());
      m_package_installers.Add(new PackageInstallerUI());
      m_package_installers.Add(new PackageInstallerPythonPlugin());
      m_package_installers.Add(new PackageInstallerPlugin());

      m_main_thread_id = Thread.CurrentThread.ManagedThreadId;
      m_init_thread = new BackgroundWorker(); //ok
      m_init_thread.WorkerReportsProgress = true;//ok
      m_init_thread.WorkerSupportsCancellation = true;//ok
      m_init_thread.ProgressChanged += m_worker_ProgressChanged;//ok
      m_init_thread.RunWorkerCompleted += m_worker_InitComplete;//ok
      m_init_thread.DoWork += m_worker_Init;//ok

      m_install_thread = new BackgroundWorker();
      m_install_thread.WorkerReportsProgress = true;
      m_install_thread.WorkerSupportsCancellation = true;
      m_install_thread.ProgressChanged += m_worker_ProgressChanged;
      m_install_thread.RunWorkerCompleted += m_worker_InstallComplete;
      m_install_thread.DoWork += m_worker_Install;
    }

    /// <summary>
    /// Public methods are called from the Form that implements all UI.
    /// </summary>
    #region Public Methods
    public static void StartEngine(InstallerDialog ui)
    {
      DebugLog("StartEngine starting");
      m_user_interface = ui;
      InitializeAsync(Program.m_options.InstallerFile);
      m_user_interface.ShowInitializationDialog();
      DebugLog("StartEngine ending");
    }
    public static bool InstallAsync()
    {
      if (m_state.CurrentPhase != InstallerPhase.Initialized)
      {
        throw new InstallException("InstallAsync() called without proper initialization");
      }
      m_install_thread.RunWorkerAsync();
      m_user_interface.ShowProgressDialog();
      return false;
    }

    /// <summary>
    /// This gets called from the Form
    /// when the user clicks Next in the Welcome
    /// Dialog box
    /// </summary>
    public static void CancelInstallation()
    {
      DebugLog("CancelInstallation called");
      bool bCleanup = true;

      if (m_init_thread.IsBusy)//ok
      {
        m_init_thread.CancelAsync();//ok
        bCleanup = false;
      }
      if (m_install_thread.IsBusy)
      {
        m_install_thread.CancelAsync();
        bCleanup = false;
      }

      if (bCleanup)
      {
        {
          if (m_state.ApplicationExiting)
            return;

          m_state.CurrentPhase = InstallerPhase.Canceled;
          CleanupAndExit();
        }
      }
    }

    public static void EndInstallation()
    {
      DebugLog("EndInstallation called");
      bool bCleanup = true;

      if (m_init_thread.IsBusy)//ok
      {
        m_init_thread.CancelAsync();//ok
        bCleanup = false;
      }
      if (m_install_thread.IsBusy)
      {
        m_install_thread.CancelAsync();
        bCleanup = false;
      }

      if (bCleanup)
        CleanupAndExit();
    }

    public static InstallerPhase CurrentPhase()
    {
      return m_state.CurrentPhase;
    }

    public static string PackageTitle
    {
      get
      {
        if (SelectedInstaller != null)
          return SelectedInstaller.Title;

        return "";
      }
    }

    public static void DebugLog(string msg)
    {
      ReportProgress(LogLevel.Debug, InstallerPhase.Unknown, msg);
    }

    public static void ReportProgress(LogLevel level, string message)
    {
      ReportProgress(level, InstallerPhase.Unknown, message);
    }

    public static void ReportProgress(LogLevel level, string message, int percentComplete)
    {
      ReportProgress(level, InstallerPhase.Unknown, message, percentComplete);
    }

    public static string InstallRoot(PackageInstallRoot root)
    {
      switch (root)
      {
        case PackageInstallRoot.AllUsers:
          return AllUsersInstallRoot;
        case PackageInstallRoot.CurrentUserLocalProfile:
          return CurrentUserLocalProfileRoot;
        case PackageInstallRoot.CurrentUserRoamingProfile:
          return CurrentUserRoamingProfileRoot;
      }
      return null;
    }

    public static string CurrentUserRoamingProfileRoot
    {
      get
      {
        string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string root = Path.Combine(appdata, @"McNeel\Rhinoceros\5.0");
        return root;
      }
    }

    public static string CurrentUserLocalProfileRoot
    {
      get
      {
        string appdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string root = Path.Combine(appdata, @"McNeel\Rhinoceros\5.0");
        return root;
      }
    }

    public static string AllUsersInstallRoot
    {
      get
      {
        string program_files = Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles);
        string root = Path.Combine(program_files, @"McNeel\Rhinoceros\5.0");
        return root;
      }
    }

    public static string CreateTempFolder()
    {
      if (m_temp_folder != null && Directory.Exists(m_temp_folder))
        return m_temp_folder;

      // Clean up old failed installs:
      //2011-03-09, Brian Gillespie
      // Attempt to fix http://dev.mcneel.com/bugtrack/?q=82609 by saving temp files in Local profile instaed of Roaming profile.
      string TempRoot = Path.Combine(CurrentUserLocalProfileRoot, "temp");
      if (Directory.Exists(TempRoot))
      {
        ReportProgress(LogLevel.Info, InstallerPhase.Unknown, "Deleting previous temporary folders");
        try
        {
          Directory.Delete(TempRoot, true);
        }
        catch
        {
          Logger.Log(LogLevel.Warning, "Unable to delete temporary directory: " + TempRoot);
        }
      }

      m_temp_folder = Path.Combine(TempRoot, InstanceGuid);
      ReportProgress(LogLevel.Info, InstallerPhase.Unknown, "Creating temporary directory: " + m_temp_folder);
      Directory.CreateDirectory(m_temp_folder);
      DirectoryInfo inf = new DirectoryInfo(TempRoot);
      inf.Attributes = inf.Attributes | FileAttributes.Hidden;
      return m_temp_folder;
    }

    public static bool Is64BitProcess()
    {
      if (IntPtr.Size == 8)
        return true;
      return false;
    }
    #endregion

    #region Background Worker
    static BackgroundWorker m_init_thread = null;
    static BackgroundWorker m_install_thread = null;

    static void m_worker_Init(object sender, DoWorkEventArgs e)
    {
      DebugLog("m_worker_Init starting");
      Init((string)e.Argument);
      DebugLog("m_worker_Init ending");
    }

    static void m_worker_Install(object sender, DoWorkEventArgs e)
    {
      DebugLog("m_worker_Install starting");
      Install();
      DebugLog("m_worker_Install ending");
    }

    static void m_worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
    {
      var report = e.UserState as ProgressReport;
      if (report != null)
      {
        string strPhase = report.Phase.ToString();
        if (report.Phase == InstallerPhase.Unknown)
          strPhase = "";

        strPhase = strPhase.PadRight(20, ' ');
        Logger.Log(report.Level, strPhase + "\t" + report.Message);

        if (m_user_interface != null)
          m_user_interface.SetPercentComplete(e.ProgressPercentage);
        
        if (report.Phase != InstallerPhase.Unknown)
          m_state.CurrentPhase = report.Phase;
      }
    }

    static void m_worker_InitComplete(object sender, RunWorkerCompletedEventArgs e)
    {
      DebugLog("m_worker_InitComplete starting");

      if (e.Error != null)
      {
        ReportProgress(LogLevel.Error, InstallerPhase.InitializationFailed, "m_worker_InitComplete exception caught");
        m_state.CurrentPhase = InstallerPhase.InitializationFailed;
        if (e.Error is PackageNotFoundException)
          m_user_interface.ShowErrorDialog("The installer package was not found:\n\n'" + e.Error.Message + "'");
        else if (e.Error is PackageNotCompatibleException)
          m_user_interface.ShowErrorDialog("This package is not compatible with the Rhino Installer Engine.\n\nDEVELOPERS:\nFor information on making your plug-in compatible, visit\nhttp://wiki.mcneel.com/developer/rhinoinstallerengine/overview");
        else
          m_user_interface.ShowErrorDialog(e.Error);

      }
      else if (e.Cancelled)
      {
        DebugLog("m_worker_InitComplete - init cancelled");
        m_state.CurrentPhase = InstallerPhase.Canceled;
        CleanupAndExit();
      }
      else
      {
        switch (m_state.CurrentPhase)
        {
          case InstallerPhase.AlreadyInstalled:
            m_user_interface.ShowAlreadyInstalledDialog();
            break;
          case InstallerPhase.Initialized:
            m_user_interface.ShowWelcomeDialog();
            break;
          case InstallerPhase.InitializationFailed:
            m_user_interface.ShowErrorDialog();
            break;
          case InstallerPhase.InspctPkgNotCompatible:
            m_user_interface.ShowErrorDialog("This package is not compatible with the Rhino Installer Engine.\n\nDEVELOPERS:\nFor information on making your plug-in compatible, visit\nhttp://wiki.mcneel.com/developer/rhinoinstallerengine/overview");
            break;
          case InstallerPhase.Success:
            break;
          default:
            throw new InitException("InitCompleted encountered unexpected state.");
        }
      }

      DebugLog("m_worker_InitComplete ending");
    }

    static void m_worker_InstallComplete(object sender, RunWorkerCompletedEventArgs e)
    {
      DebugLog("m_worker_InstallComplete starting");

      if (e.Error != null)
      {
        ReportProgress(LogLevel.Error, InstallerPhase.InstallFailed, "m_worker_InstallComplete exception caught");
        m_state.CurrentPhase = InstallerPhase.InstallFailed;
        m_user_interface.ShowErrorDialog(e.Error);
      }
      if (e.Cancelled)
      {
        DebugLog("m_worker_InstallComplete cancelled");
        m_state.CurrentPhase = InstallerPhase.Canceled;
        CleanupAndExit();
      }

      InstallerPhase state = m_state.CurrentPhase;
      switch (state)
      {
        case InstallerPhase.Complete:
          m_user_interface.ShowCompleteDialog();
          break;
        case InstallerPhase.InstallFailed:
          m_user_interface.ShowErrorDialog();
          break;
        default:
          throw new InstallException("InitCompleted encountered unexpected state.");
      }
      DebugLog("m_worker_InstallComplete exiting");
    }
    #endregion


    #region Private Methods
    static bool InitializeAsync(string InstallerFilePath)
    {
      m_init_thread.RunWorkerAsync(InstallerFilePath); //ok
      return false;
    }

    /// <summary>
    /// Init is called to initialize the package for installation.
    /// No installation is done, only inspection of the package contents.
    /// 
    /// 1) Extract .rhi package into temp folder
    /// 2) Select appropriate PackageInstaller
    /// 3) Initialize PackageInstaller
    /// 4) Determine if Package is already installed
    /// 5) Detect Rhino installations
    /// 6) Is package compatible with any Rhino installations?
    /// 7) If all above succeeds, Init succeeded.
    ///
    /// All logic for inspection and initialization should be in
    /// the Package code; not the Engine (the Engine should know
    /// nothing about installing a specific package).
    /// </summary>
    /// <param name="InstallerFilePath"></param>
    static void Init(string PackagePath)
    {
      ReportProgress(LogLevel.Info, InstallerPhase.Initializing, "INIT START: " + m_state.InstallerFilePath);

      if (!File.Exists(PackagePath))
      {
        ReportProgress(LogLevel.Error, InstallerPhase.InitializationFailed, "Installer Package not Found: '" + PackagePath + "'");
        return;
      }

      m_package = new Package(PackagePath);
      m_state.TemporaryFolder = CreateTempFolder();
      m_package.DestinationFolder = m_state.TemporaryFolder;

      if (m_init_thread.CancellationPending) return;

      // 2) Select appropriate PackageInstaller
      ReportProgress(LogLevel.Info, InstallerPhase.Initializing, "Selecting PackageInstaller");
      for (int i=0; i<m_package_installers.Count; i++)
      {
        if (m_package_installers[i].ContainsRecognizedPayload(m_package))
        {
          m_selected_installer_index = i;
          break;
        }
      }
      if (m_init_thread.CancellationPending) return;

      // No appropriate PackageInstaller found
      if (SelectedInstaller == null)
      {
        ReportProgress(LogLevel.Info, InstallerPhase.InitializationFailed, "Initialization Failed - Appropriate Package Installer Not Found");
        return;
      }

      // 3) Initialize PackageInstaller
      ReportProgress(LogLevel.Info, InstallerPhase.Initializing, "Initializing PackageInstaller");
      SelectedInstaller.Initialize(m_package);
      if (m_init_thread.CancellationPending) return;

      // 4) Determine if Package is already installed
      PackageInstallState install_state = SelectedInstaller.GetInstallState(m_package);
      if (install_state >= PackageInstallState.SameVersionInstalledCurrentUser)
      {
        ReportProgress(LogLevel.Info, InstallerPhase.AlreadyInstalled, "Initialization Complete");
        return;
      }

      // 5) Detect Rhino installations
      ReportProgress(LogLevel.Info, InstallerPhase.Initializing, "Detecting Rhino");
      DetectInstalledRhino();
      if (m_init_thread.CancellationPending) return;

      // 6) Is package compatible with any Rhino installations?
      foreach (RhinoInfo rhino in m_state.RhinoList)
      {
        if (SelectedInstaller.IsCompatible(rhino))
        {
          ReportProgress(LogLevel.Info, InstallerPhase.Initializing, "Found Compatible Rhino");
          m_state.CompatibleRhinoIsInstalled = RhinoInstallState.Found;
          break;
        }
        if (m_init_thread.CancellationPending) return;
      }

      if (m_state.CompatibleRhinoIsInstalled == RhinoInstallState.Found)
        ReportProgress(LogLevel.Info, InstallerPhase.Initialized, "Initialization Complete");
      else
        ReportProgress(LogLevel.Info, InstallerPhase.InspctPkgNotCompatible, "Initialization Failed - This package is not compatible with any of the Rhino installations on this computer.");
    }

    static void DetectInstalledRhino()
    {
      List<string> InstallPaths = SearchRegistryForRhinoInstallPaths();
      foreach (string path in InstallPaths)
        ReportProgress(LogLevel.Debug, string.Format("Rhino found here: {0}", path));

      string[] rhino_xml_files = Directory.GetFiles(m_state.TemporaryFolder, "__~~RhinoInfo~~__.*.tmp.xml");
      foreach (string rhino_xml_file in rhino_xml_files)
        File.Delete(rhino_xml_file);


      foreach (string rhpath in InstallPaths)
      {
        if (!File.Exists(rhpath))
        {
          // Don't bother spawning an inspection process if rhpath is not found.
          // Fixes http://dev.mcneel.com/bugtrack/?q=68438
          ReportProgress(LogLevel.Info, InstallerPhase.Unknown, "DetectInstalledRhino() cound not find file: " + rhpath);
          continue;
        }

        InstallerPhase rc = Program.ExecuteChildProcess(OSPlatform.x86, "/INSPECTRHINO \"" + rhpath + "\" /INSPECTWORKINGDIR \"" + m_state.TemporaryFolder + "\"", false);
        if (rc != InstallerPhase.Success)
        {
          if (Is64BitProcess())
          {
            Program.ExecuteChildProcess(OSPlatform.x64, "/INSPECTRHINO \"" + rhpath + "\" /INSPECTWORKINGDIR \"" + m_state.TemporaryFolder + "\"", false);
          }
        }
      }

      string[] rhino_info_files = Directory.GetFiles(m_state.TemporaryFolder, "__~~RhinoInfo~~__*.tmp.xml");

      foreach (string rhino_info in rhino_info_files)
      {
        RhinoInfo info = new RhinoInfo();
        info.ReadXml(Path.Combine(m_state.TemporaryFolder, rhino_info));
        m_state.AddRhino(info);
      }
    }

    static void RegDebugLog(string msg)
    {
      ReportProgress(LogLevel.Debug, string.Format("REG: {0}", msg));
    }

    static void SearchSpecificRegKey(List<string> InstallPaths, string keyName, bool b64bit)
    {
      RegistryKey keyToSearch = Registry.LocalMachine.OpenSubKey(keyName);
      if (keyToSearch != null)
      {
        object oPath = keyToSearch.GetValue("InstallPath");
        if (oPath != null)
        {
          string installPath;
          if (b64bit)
            installPath = Path.Combine((string)oPath, "System\\Rhino.exe");
          else
            installPath = Path.Combine((string)oPath, "System\\Rhino4.exe");

          RegDebugLog(string.Format(@"1: HKLM\{0}\InstallPath = {1}", keyName, installPath));
          if (!InstallPaths.Contains(installPath))
            InstallPaths.Add(installPath);
        }
        else
        {
          RegDebugLog(string.Format(@"0: HKLM\{0}\InstallPath (not found)", keyName));
        }
        keyToSearch.Close();
      }
      else
      {
        RegDebugLog(string.Format(@"0: HKLM\{0} (not found)", keyName));
      }
    }

    static List<string> SearchRegistryForRhinoInstallPaths()
    {
      List<string> InstallPaths = new List<string>();

      /*
       * 32-bit and 64-bit search:
       * hklm\software\mcneel\rhinoceros\4.0\*\install
       * hklm\software\mcneel\rhinoceros\5.0\install
       */
      {
        const string keyName = @"Software\McNeel\Rhinoceros\4.0";
        RegistryKey keyRH4 = Registry.LocalMachine.OpenSubKey(keyName);
        if (keyRH4 != null)
        {
          string[] subkeys = keyRH4.GetSubKeyNames();
          foreach (string subkey in subkeys)
            SearchSpecificRegKey(InstallPaths, keyName + @"\" + subkey + @"\Install", false);

          keyRH4.Close();
        }
        else
        {
          RegDebugLog("key not found: HKLM\\" + keyName);
        }

        SearchSpecificRegKey(InstallPaths, @"Software\McNeel\Rhinoceros\5.0\Install", false);
      }

      /*
       * 64-bit only:
       * hklm\software\mcneel\rhinoceros\5.0x64\install
       * hklm\software\wow6432node\mcneel\rhinoceros\4.0\*\install
       * hklm\software\wow6432node\mcneel\rhinoceros\5.0\install
       */

      if (IntPtr.Size == 8)
      {
        const string keyName = @"Software\Wow6432Node\McNeel\Rhinoceros\4.0";
        RegistryKey keyRH4 = Registry.LocalMachine.OpenSubKey(keyName);
        if (keyRH4 != null)
        {
          string[] subkeys = keyRH4.GetSubKeyNames();
          foreach (string subkey in subkeys)
            SearchSpecificRegKey(InstallPaths, keyName + @"\" + subkey + @"\Install", false);

          keyRH4.Close();
        }
        else
        {
          RegDebugLog("key not found: HKLM\\" + keyName);
        }

        SearchSpecificRegKey(InstallPaths, @"Software\Wow6432Node\McNeel\Rhinoceros\5.0\Install", false);

        SearchSpecificRegKey(InstallPaths, @"Software\McNeel\Rhinoceros\5.0x64\Install", true);
      }

      return InstallPaths;
    }

    static void Install()
    {
      // at this point we're ready to install.
      ReportProgress(LogLevel.Info, InstallerPhase.Installing, "");

      if (m_install_thread.CancellationPending)
        return;

      string destination_folder = "";
      if (m_state.User == InstallerUser.CurrentUser)
      {
        // Todo: handle local profile in addition to roaming profile
        if (SelectedInstaller.InstallRoot == PackageInstallRoot.CurrentUserLocalProfile)
          destination_folder = SelectedInstaller.InstallFolder(CurrentUserLocalProfileRoot);
        else if (SelectedInstaller.InstallRoot == PackageInstallRoot.CurrentUserRoamingProfile)
          destination_folder = SelectedInstaller.InstallFolder(CurrentUserRoamingProfileRoot);
      }
      else if (m_state.User == InstallerUser.AllUsers)
      {
        destination_folder = SelectedInstaller.InstallFolder(AllUsersInstallRoot);
      }
      else
      {
        throw new InstallException("Unrecognized install user: " + m_state.User.ToString());
      }

      if (m_install_thread.CancellationPending)
        return;

      m_package.DestinationFolder = destination_folder;

      // Make sure parent directory exists:
      ReportProgress(LogLevel.Info, InstallerPhase.Installing, "Creating package install folder: " + destination_folder);
      Directory.CreateDirectory(Path.GetDirectoryName(destination_folder));

      // Let package installer do what it wants prior to installation
      ReportProgress(LogLevel.Debug, InstallerPhase.Installing, "Calling BeforeInstall()");
      SelectedInstaller.BeforeInstall(m_package, m_state.RhinoList, m_state.User);

      // Extract package to final location
      if (!m_package.Install(destination_folder, SelectedInstaller))
      {
        ReportProgress(LogLevel.Error, InstallerPhase.InstallFailed, "Package installation failed");
        return;
      }

      // Let package installer clean up after installation
      if (SelectedInstaller.AfterInstall(m_package, m_state.RhinoList, m_state.User))
      {
        ReportProgress(LogLevel.Info, InstallerPhase.Cleanup, "Deleting temporary files");
        DeleteTempFiles(destination_folder);
        DeleteOldVersions(destination_folder, 2);
        DeleteTempFolders();
        ReportProgress(LogLevel.Info, InstallerPhase.Complete, "INSTALL END: " + m_state.InstallerFilePath);
      }
      else
      {
        try
        {
          DeleteTempFolders();
          Directory.Delete(destination_folder, true);
          ReportProgress(LogLevel.Error, InstallerPhase.InstallFailed, "AfterInstall failed, deleting install directory: " + destination_folder);
        }
        catch
        {
          // Can't fix it now.
          ReportProgress(LogLevel.Error, InstallerPhase.InstallFailed, "AfterInstall failed");
        }
      }

    }

    static void DeleteTempFolders()
    {
      string TempRoot = Path.Combine(CurrentUserRoamingProfileRoot, "temp");
      if (Directory.Exists(TempRoot))
      {
        ReportProgress(LogLevel.Info, InstallerPhase.Unknown, "Deleting temporary folders");
        try
        {
          Directory.Delete(TempRoot, true);
        }
        catch (IOException)
        {
          // If we can't clean up the temp folders, there's no reason to crash. 
          // I hope the temp folder will get cleaned up next time around.
          // This is for http://dev.mcneel.com/bugtrack/?q=68445
          // Changing "finally" to "catch" fixes http://dev.mcneel.com/bugtrack/?q=68715
        }
      }
    }

    static void DeleteTempFiles(string PackageFolder)
    {
      if (!Directory.Exists(PackageFolder))
        return;

      string[] tmpfiles = Directory.GetFiles(PackageFolder, "__~~RhinoInfo~~__*.xml", SearchOption.TopDirectoryOnly);
      foreach (string tmpfile in tmpfiles)
      {
        try
        {
          File.Delete(tmpfile);
        }
// ReSharper disable EmptyGeneralCatchClause
        catch
// ReSharper restore EmptyGeneralCatchClause
        {
          // no sense whining about it now.
        }
      }
    }

    static void DeleteOldVersions(string folder, int keepCount)
    {
      string[] installed = PackageInstallerBase.GetAllInstalledVersions(Directory.GetParent(folder).FullName);
      for (int i = 0; i < installed.Length - (keepCount + 1); i++)
      {
        try
        {
          Directory.Delete(installed[i], true);
        }
// ReSharper disable EmptyGeneralCatchClause
        catch
// ReSharper restore EmptyGeneralCatchClause
        {
          // No sense crying about it now; maybe we'll delete it next time.
        }
      }
    }

    static void CleanupAndExit()
    {
      if (m_state.ApplicationExiting)
        return;

      m_state.ApplicationExiting = true;

      DebugLog("CleanupAndExit starting");
      InstallerPhase final_state = InstallerPhase.Complete;
      if (m_state.CurrentPhase >= InstallerPhase.InitializationFailed)
        final_state = m_state.CurrentPhase;

      if (m_state.CurrentPhase > InstallerPhase.Complete)
      {
        ReportProgress(LogLevel.Info, InstallerPhase.Cleanup, "Cleaning Up");
        DeleteTempFolders();
        ReportProgress(LogLevel.Info, final_state, "Complete");
      }
      ReportProgress(LogLevel.Info, final_state, "Exiting");

      Logger.PurgeOldLogs();
      DebugLog("CleanupAndExit ending");
      Application.Exit();
    }

    private static void ReportProgress(LogLevel level, InstallerPhase phase, string message, int percentComplete)
    {
      // if we're running in the main thread, just call the function.
      ProgressReport msg = new ProgressReport();
      msg.Phase = phase;
      msg.Level = level;
      msg.Message = message;

      int current_thread_id = Thread.CurrentThread.ManagedThreadId;
      ProgressChangedEventArgs args = new ProgressChangedEventArgs(percentComplete, msg);
      if (current_thread_id == m_main_thread_id)
      {
        m_worker_ProgressChanged(null, args);
      }
      else if (m_init_thread.IsBusy)
      {
        m_init_thread.ReportProgress(percentComplete, msg);
      }
      else if (m_install_thread.IsBusy)
      {
        m_install_thread.ReportProgress(percentComplete, msg);
      }
      else
      {
        throw new RhinoInstallerException("ReportProgress called with unexpected thread context.");
      }
    }

    private static void ReportProgress(LogLevel level, InstallerPhase phase, string message)
    {
      ReportProgress(level, phase, message, 0);
    }

    #endregion
  }

  public enum PackageInstallState
  {
    Unknown,
    NotInstalled,
    OlderVersionInstalledCurrentUser,
    OlderVersionInstalledAllUsers,
    SameVersionInstalledCurrentUser,
    SameVersionInstalledAllUsers,
    NewerVersionInstalledCurrentUser,
    NewerVersionInstalledAllUsers,
  }

  public enum InstallerUser
  {
    CurrentUser,
    AllUsers,
  }

  public enum OSPlatform
  {
    Unknown,
    Any,
    x86,
    x64,
  }

  public enum RhinoPlatform
  {
    Unknown,
    Rhino4_win32,
    Rhino5_win32,
    Rhino5_win64,
  }
}
