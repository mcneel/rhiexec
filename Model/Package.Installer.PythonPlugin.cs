using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using RMA.RhiExec.Engine;
using Microsoft.Win32;

namespace RMA.RhiExec.Model
{
  class PackageInstallerPythonPlugin : PackageInstallerPlugin
  {
    private PythonPluginInfo m_info;

    private PythonPluginInfo Plugin
    {
      get
      {
        if (m_info == null)
        {
          m_info = new PythonPluginInfo();
          m_info.Initialize(m_package);
          if (!m_info.IsValid())
            m_info = null;
        }
        return m_info;
      }
    }

    private void ClearPluginInfo()
    {
      m_info = null;
      m_package = null;
    }

    public override Guid ID
    {
      get
      {
        if (Plugin == null)
          return Guid.Empty;

        return Plugin.ID;
      }
    }

    public override Version PackageVersion
    {
      get
      {
        if (Plugin == null)
          return new Version();

        return Plugin.Version;
      }
    }

    public override string Title
    {
      get 
      {
        if (Plugin == null)
          return "";

        return Plugin.Title; 
      }
    }

    public override PackageContentType ContentType
    {
      get
      {
        return PackageContentType.Python;
      }
    }

    public override RMA.RhiExec.Engine.PackageInstallState GetInstallState(Package package)
    {
      // See what version this package contains.
      PythonPluginInfo ppi = new PythonPluginInfo();
      if (!ppi.Initialize(package))
        throw new PackageNotCompatibleException(package.PackagePath);

      // Is this installed for All Users?
      ReportProgress("Getting InstallState for " + this.PackagePath, LogLevel.Debug);

      // Is plug-in installed for all users?
      ReportProgress("Checking install state for current user", LogLevel.Debug);
      string folder = InstallerEngine.InstallRoot(this.InstallRoot);

      // AllUsersInstallFolder returns ...\plug-in name\version
      // we want to look in just ...\plug-in name
      folder = Path.GetDirectoryName(folder);

      Version InstalledVersion = GetNewestVersionOfInstalledPackage(folder);
      return CompareVersions(this.PackageVersion, InstalledVersion);
    }

    public override bool IsCompatible(RhinoInfo rhino)
    {
      if (rhino.RhinoVersion.Major == 5)
        return true;

      return false;
    }

    public override bool ContainsRecognizedPayload(Package package)
    {
      if (package.ContainsFileNamed("__plugin__.py"))
      {
        ReportProgress("Recognized payload as Python plugin", LogLevel.Info);
        return true;
      }

      ReportProgress("Package not recognized as Python plugin", LogLevel.Info);
      return false;
    }

    public override bool Initialize(Package package)
    {
      ClearPluginInfo();
      m_package = package;
      
      if (Plugin.IsValid())
      {
        ReportProgress("Package Description:\n" + Describe(), LogLevel.Info);
        return true;
      }
      else
      {
        ReportProgress("Package Description:\n" + Describe(), LogLevel.Info);
        ReportProgress("Package failed to initialize properly: " + package.PackagePath, LogLevel.Error);
        return false;
      }
    }

    public override bool AfterInstall(Package package, RhinoInfo[] RhinoList, RMA.RhiExec.Engine.InstallerUser InstallAsUser)
    {
      ClearPluginInfo();

      m_package = package;
      if (m_package == null)
        return false;

      string PackageFolder = package.PackagePath;
      ReportProgress("PythonPackage.Install() starting for Package at '" + PackageFolder + "'", LogLevel.Info);

      // Find all the *_cmd.py files (in top directory only, not subdirectories) and register them with IronPython
      if (!Plugin.IsValid())
      {
        ReportProgress("Package Description:\n" + Describe(), LogLevel.Info);
        ReportProgress("Package failed to initialize properly: " + PackageFolder, LogLevel.Error);
        return false;
      }

      string[] commands = Plugin.Commands;

      if (commands.Length == 0)
      {
        ReportProgress("No files named *_cmd.py found in " + PackageFolder, LogLevel.Error);
        return false;
      }

      // HKEY_CURRENT_USER\Software\McNeel\Rhinoceros\5.0\Plug-Ins\814d908a-e25c-493d-97e9-ee3861957f49\CommandList
      RegistryKey RegCmd_32 = Registry.CurrentUser.CreateSubKey(@"Software\McNeel\Rhinoceros\5.0\Plug-Ins\814d908a-e25c-493d-97e9-ee3861957f49\CommandList");
      if (RegCmd_32 == null)
        ReportProgress(@"Unable to write to HKCU\Software\McNeel\Rhinoceros\5.0\Plug-Ins\814d908a-e25c-493d-97e9-ee3861957f49\CommandList", LogLevel.Error);

      RegistryKey RegCmd_64 = Registry.CurrentUser.CreateSubKey(@"Software\McNeel\Rhinoceros\5.0x64\Plug-Ins\814d908a-e25c-493d-97e9-ee3861957f49\CommandList");
      if (RegCmd_64 == null)
        ReportProgress(@"Unable to write to HKCU\Software\McNeel\Rhinoceros\5.0\Plug-Ins\814d908a-e25c-493d-97e9-ee3861957f49\CommandList", LogLevel.Error);

      foreach (string cmd in commands)
      {
        // Register this command in IronPython:
        if (RegCmd_32 != null) // 32-bit
          RegCmd_32.SetValue(cmd, "66;" + cmd, RegistryValueKind.String);
        if (RegCmd_64 != null) // 64-bit
          RegCmd_64.SetValue(cmd, "66;" + cmd, RegistryValueKind.String);

        ReportProgress("Registering command: " + cmd, LogLevel.Info);
      }

      return true;
    }

    public override string Describe()
    {
      StringBuilder sb = new StringBuilder();
      sb.Append("Python Plug-in\n");
      sb.Append("Path: ").Append(this.PackagePath).Append("\n");
      sb.Append("IsValid: ").Append(Plugin.IsValid()).Append("\n");
      sb.Append("Title: ").Append(this.Title).Append("\n");
      sb.Append("Version: ").Append(this.PackageVersion).Append("\n");
      sb.Append("ID: ").Append(this.ID).Append("\n");
      sb.Append("Commands:\n");
      if (Plugin.Commands != null)
      {
        foreach (string cmd in Plugin.Commands)
        {
          sb.Append("  ").Append(cmd).Append("\n");
        }
      }
      else
      {
        sb.Append("  (none)").Append("\n");
      }

      return sb.ToString();
    }

    public override string InstallFolder(string rootFolder)
    {
      return PluginInstallFolder(Path.Combine(rootFolder, @"Plug-ins\PythonPlugins"));
    }



  }

  class PythonPluginInfo
  {
    public string PluginPath = "";
    public Guid ID = Guid.Empty;
    public Version Version = new Version("0.0.0.0");
    public string Title = "";
    private List<string> m_commands = new List<string>();
    public string[] Commands
    {
      get {
        if (m_commands.Count == 0)
          return null;
        return m_commands.ToArray(); 
      }
    }

    public bool IsValid()
    {
      if (string.IsNullOrEmpty(PluginPath))
        return false;

      if (ID == Guid.Empty)
        return false;

      if (Version == null)
        return false;

      int cf = string.Compare(Version.ToString(), "0.0.0.0", StringComparison.Ordinal);
      if (cf == 0)
        return false;

      if (string.IsNullOrEmpty(Title))
        return false;

      if (m_commands.Count == 0)
        return false;

      return true;
    }

    public bool Initialize(Package package)
    {
      if (package == null)
      {
        InstallerEngine.ReportProgress(LogLevel.Error, "PythonPluginInfo::Initialize called with null package");
        return false;
      }
      PackageFileKey plugin_py_key = package.FindSingleFile("__plugin__.py");
      string plugin_py = package.GetFullPath(plugin_py_key);
      if (plugin_py == null)
        return false;

      if (!LoadPluginPyFile(plugin_py))
        return false;

      ReadCommandsFromPackage(package);

      InstallerEngine.ReportProgress(LogLevel.Info, "PythonPluginInfo::Initialize failed");
      return this.IsValid();
    }

    private bool LoadPluginPyFile(string plugin_py)
    {
      // id={08F2B7B0-EAEF-4932-A583-E61E68D610A3}
      // version=1.0.7.3
      // update_url=http://apps.mcneel.com/update/mcneel/coolarch.update.xml
      // title=CoolArch
      // author_name=Steve Baer
      // author_email=steve@mcneel.com
      // web_site=http://apps.mcneel.com/steve/coolarc

      if (string.IsNullOrEmpty(plugin_py))
        return false;

      if (!File.Exists(plugin_py))
        return false;

      PluginPath = plugin_py;
      TextReader r = new StreamReader(plugin_py);
      string contents = r.ReadToEnd();

      // Strip python style comments.
      // TODO: This does not correctly handle # characters embedded in strings:
      //    'title="The #1 Plug-in" # My title has a # in it!'
      // becomes
      //    'title="The '

      contents = Regex.Replace(contents, "#.*", "", RegexOptions.Multiline);

      Match m = Regex.Match(contents, @"version\s*=\s*""(\d+\.\d+\.\d+\.\d+)""", RegexOptions.IgnoreCase | RegexOptions.Multiline);
      Version = new Version(m.Groups[1].Value);

      m = Regex.Match(contents, @"title\s*=\s*""(.*)""", RegexOptions.IgnoreCase | RegexOptions.Multiline);
      Title = m.Groups[1].Value;

      m = Regex.Match(contents, @"id\s*=\s*""(.*)""", RegexOptions.IgnoreCase | RegexOptions.Multiline);
      string sID = m.Groups[1].Value;
      try
      {
        ID = new Guid(sID);
      }
      catch
      {
        ID = Guid.Empty;
      }

      return true;

    }

    private void ReadCommandsFromPackage(Package package)
    {
      Regex rxcmd = new Regex(".*_cmd.py", RegexOptions.IgnoreCase);
      Collection<PackageFileKey> command_files = package.FindFiles(rxcmd);
      List<string> filenames = new List<string>();
      foreach (PackageFileKey key in command_files)
        filenames.Add(key.Key);
      GetCommandList(filenames);
    }

    private void GetCommandList(IEnumerable<string> command_files)
    {
      if (command_files == null)
        return;

      foreach (string command_file in command_files)
      {
        string filename = Path.GetFileName(command_file);
        Match m = Regex.Match(filename, "^(.*)_cmd.py$");
        if (m.Success)
        {
          string cmd = m.Groups[1].Value;
          m_commands.Add(cmd);
        }
      }
    }
  }
}
