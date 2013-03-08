using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using Microsoft.Win32;
using RMA.RhiExec.Engine;

namespace RMA.RhiExec.Model
{
  class PackageInstallerPlugin : PackageInstallerBase
  {
    private Guid m_id = Guid.Empty;
    protected Package m_package;
    private InstallerUser m_user = InstallerUser.CurrentUser;
    Dictionary<PackageFileKey, PluginInfo> m_plugin_lookup = new Dictionary<PackageFileKey, PluginInfo>();

    #region Package Members

    public override Guid ID
    {
      get { return m_id; }
    }

    public override Version PackageVersion
    {
      get
      {
        Version largest_version = new Version("0.0.0.0");
        foreach (PluginInfo plugin in m_plugin_lookup.Values)
        {
          int cf = plugin.VersionNumber.CompareTo(largest_version);
          if (cf > 0)
            largest_version = plugin.VersionNumber;
        }
        return largest_version;
      }
    }

    public override string Title
    {
      get
      {
        foreach (PluginInfo pii in m_plugin_lookup.Values)
        {
          return pii.Title;
        }
        return null;
      }
    }

    public override string PackagePath
    {
      get
      {
        return m_package.PackagePath;
      }
    }

    public override PackageContentType ContentType
    {
      get {
        return PackageContentType.Plugin;
      }
    }

    public override PackageInstallRoot InstallRoot
    {
      get { return PackageInstallRoot.CurrentUserRoamingProfile; }
    }

    public override string InstallFolder(string rootFolder)
    {
      return PluginInstallFolder(Path.Combine(rootFolder, "Plug-ins"));
    }

    public override PackageInstallState GetInstallState(Package package)
    {
      PackageInstallState accumulated_state = PackageInstallState.NotInstalled;

      foreach (PluginInfo pii in m_plugin_lookup.Values)
      {
        pii.InstallState = GetPluginInstallState(pii);
        if (pii.InstallState > accumulated_state)
          accumulated_state = pii.InstallState;

        pii.WriteXml();
      }

      return accumulated_state;
    }

    public override bool IsCompatible(RhinoInfo rhino)
    {
      foreach (PluginInfo pii in m_plugin_lookup.Values)
      {
        if (pii.IsCompatible(rhino))
          return true;
      }
      return false;
    }

    public override string Describe()
    {
      StringBuilder sb = new StringBuilder();
      sb.Append("PluginPackage:\n");
      sb.Append("Title: ").Append(Title).Append("\n");
      sb.Append("ID: ").Append(ID.ToString()).Append("\n");
      sb.Append("Version: ").Append(PackageVersion).Append("\n");
      sb.Append("PackagePath: ").Append(PackagePath).Append("\n");
      return sb.ToString();
    }

    public override bool ContainsRecognizedPayload(Package package)
    {
      Collection<PackageFileKey> rhp_files = ListRhpFiles(package);
      if (rhp_files.Count > 0)
        return true;

      return false;
    }

    public override bool Initialize(Package package)
    {
      m_package = package;
      // See if at least one compatible Rhino is installed.

      Guid last_plugin_id = Guid.Empty;
      Collection<PackageFileKey> rhp_files = ListRhpFiles(package);

      bool foundOneValidRhino = false;

      foreach (PackageFileKey rhp in rhp_files)
      {
        string rhp_full_path = package.GetFullPath(rhp);
        InstallerPhase rc = ExecutePluginInspector(rhp_full_path);

        if (rc != InstallerPhase.Success)
          continue;

        foundOneValidRhino = true;
        PluginInfo pii = new PluginInfo();
        pii.PluginPath = rhp_full_path;
        pii.ReadXml();
        m_plugin_lookup.Add(rhp, pii);

        if (last_plugin_id != Guid.Empty && pii.ID != last_plugin_id)
        {
          ReportProgress("Plug-in GUID mismatch: " + pii.ID + " != " + last_plugin_id, LogLevel.Error);
          throw new GuidMismatchException("All plug-ins in an installer must have the same GUID. Two different GUIDs were found.");
        }

        last_plugin_id = pii.ID;
      }

      if (!foundOneValidRhino)
      {
        ReportProgress("Plug-in inspection failed", LogLevel.Error);
        throw new PackageNotCompatibleException(package.PackagePath);
      }



      m_id = last_plugin_id;
      return true;
    }

    public override bool AfterInstall(Package package, RhinoInfo[] RhinoList, InstallerUser InstallAsUser)
    {
      string packagePath = package.PackagePath;
      m_user = InstallAsUser;
      bool bSuccess = true;

      // find newest plug-in compatible with each Rhino on the system, and then register it.
      ReportProgress("PluginPackage.Install starting for package at '" + packagePath + "'", LogLevel.Info);

      foreach (RhinoInfo rhino in RhinoList)
      {
        PluginInfo plugin_to_install = null;
        foreach (PackageFileKey plugin_key in ListRhpFiles(package))
        {
          PluginInfo plugin;
          if (!m_plugin_lookup.TryGetValue(plugin_key, out plugin))
          {
			// Rather than throwing an exception, log this warning, and continue on.
			// fixes http://dev.mcneel.com/bugtrack/?q=105121
            ReportProgress("m_plugin_list doesn't contain PluginInfo for " + plugin_key.Key, LogLevel.Warning);
            continue;
          }

          // Set the path to the RHP file based on
          // where the package says it currently lives
          plugin.PluginPath = package.GetFullPath(plugin_key);

          if (plugin.IsCompatible(rhino))
          {
            if (plugin.CompareTo(plugin_to_install) > 0)
              plugin_to_install = plugin;
          }
        }

        if (plugin_to_install == null)
          continue;

        if (!RegisterPlugin(plugin_to_install, rhino))
          bSuccess = false;
      }

      ReportProgress("PluginPackage.Install ending", LogLevel.Info);

      return bSuccess;
    }


    #endregion

    #region Static Methods
    public static InstallerPhase InspectPlugin(string PathToPlugin)
    {
      // This should only be called from a separate process.
      PluginInfo info = new PluginInfo();
      info.ContentType = PackageContentType.Plugin;
      info.InspectPlugin(PathToPlugin);
      if (!info.IsValid())
        return InstallerPhase.InspctFailed;

      info.PluginPath = PathToPlugin;
      info.WriteXml();
      return InstallerPhase.Success;
    }
    #endregion

    #region Protected Methods
    protected string PluginInstallFolder(string root)
    {
      // remove invalid characters from string
      string plugin_name_folder = Path.Combine(root, RemoveInvalidPathChars(Title) + " {" + RemoveInvalidPathChars(this.ID.ToString().ToUpperInvariant()) + "}");
      string plugin_version_folder = Path.Combine(plugin_name_folder, this.PackageVersion.ToString());
      return plugin_version_folder;
    }

    #endregion

    #region Private Methods

    private static Collection<PackageFileKey> ListRhpFiles(Package package)
    {
      Regex rx = new Regex(".*rhp$", RegexOptions.IgnoreCase);
      Collection<PackageFileKey> files = package.FindFiles(rx);
      ReportProgress(string.Format(CultureInfo.InvariantCulture, "Found {0} plug-ins", files.Count), LogLevel.Info);
      return files;
    }

    private PackageInstallState GetPluginInstallState(PluginInfo plugin_info)
    {
      return GetPackageInstallState(plugin_info.VersionNumber);
    }

    private static InstallerPhase ExecutePluginInspector(string PluginPath)
    {
      ReportProgress("Executing 32-bit Plug-in Inspector for '" + PluginPath + "'", LogLevel.Debug);
      InstallerPhase rc = Program.ExecuteChildProcess(OSPlatform.x86, "/INSPECTPLUGIN \"" + PluginPath + "\"", false);
      switch (rc)
      {
        case InstallerPhase.Success:
        case InstallerPhase.InspctPkgNotCompatible:
          break;
        default:
          if (IntPtr.Size == 8)
          {
            ReportProgress("Executing 64-bit Plug-in Inspector for '" + PluginPath + "'", LogLevel.Debug);
            rc = Program.ExecuteChildProcess(OSPlatform.x64, "/INSPECTPLUGIN \"" + PluginPath + "\"", false);
          }
          break;
      }
      return rc;
    }

    private bool RegisterPlugin(PluginInfo plugin_info, RhinoInfo rhino_info)
    {
      ReportProgress("Registering plugin: " + plugin_info.PluginPath, LogLevel.Info);
      RegistryKey PluginsRegKey = null;
      if (m_user == InstallerUser.AllUsers)
      {
        ReportProgress("Registering for all users", LogLevel.Debug);
        PluginsRegKey = rhino_info.OpenAllUsersPluginKey();
      }
      else if (m_user == InstallerUser.CurrentUser)
      {
        ReportProgress("Registering for current user", LogLevel.Debug);
        PluginsRegKey = rhino_info.OpenCurrentUserPluginKey();
      }
      else
      {
        throw new RhinoInstallerException(string.Format(CultureInfo.InvariantCulture, "Unexpected user '{0}' encountered in RegisterPlugin.", m_user));
      }

      if (PluginsRegKey == null)
      {
        ReportProgress("Plug-in registration failed: RegKey == null", LogLevel.Error);
        return false;
      }

      // 2012-04-03, Brian Gillespie.
      // Plug-ins installed by the RHI file should always be re-loaded by Rhino;
      // Older versions of this code would check for existing plug-in registrations
      // and would set the {GUID}\Plug-in\Name and {GUID}\Plug-in\FileName keys instead.
      // But the old behavior fails to update the plug-in commands, or cause new .RUI files
      // to be loaded by Rhino.
      ReportProgress("Creating new plug-in registration", LogLevel.Debug);
      // Doesn't exist; Create it and register the plug-in
      RegistryKey ThisPluginGuidKey = PluginsRegKey.CreateSubKey(plugin_info.ID.ToString().ToUpperInvariant());
      if (ThisPluginGuidKey == null)
        return false;

      // Create Name key
      ReportProgress("Setting Name to '" + plugin_info.Title + "'", LogLevel.Debug);
      ThisPluginGuidKey.SetValue("Name", plugin_info.Title);
      ReportProgress("Setting FileName to '" + plugin_info.PluginPath + "'", LogLevel.Debug);
      ThisPluginGuidKey.SetValue("FileName", plugin_info.PluginPath);

      // Delete load mode:
      object loadMode = ThisPluginGuidKey.GetValue("LoadMode");
      if (loadMode != null)
        ThisPluginGuidKey.DeleteValue("LoadMode");

      return true;
    }

    #endregion
  }
}
