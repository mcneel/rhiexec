using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Win32;
using RMA.RhiExec.Model;

namespace RMA.RhiExec.Engine
{
  internal class RhinoInitializer
  {
    CultureInfo m_culture = CultureInfo.GetCultureInfo(1033);
    int m_sdkservicerelease; // = 0;
    string m_package_folder;
    RMA.RhiExec.View.InitializingRhinoDialog m_dlg;

    public RhinoInitializer(CultureInfo culture, int sdkServiceReleaseNumber)
    {
      m_culture = culture;
      m_sdkservicerelease = sdkServiceReleaseNumber;
    }

    public void SetPackageFolder(string packageFolderPath)
    {
      m_package_folder = packageFolderPath;
    }

    InstallerPhase InstallPackage(string package_filename)
    {
      OSPlatform p = OSPlatform.x86;
      if (InstallerEngine.Is64BitProcess())
        p = OSPlatform.x64;

      string quotedFile = package_filename.Trim("\"".ToCharArray());
      Logger.Log(LogLevel.Info, string.Format("Installing Package: '{0}'", package_filename));
      return Program.ExecuteChildProcess(p, "\"" + quotedFile + "\"", true);
    }

    string GetSdkServiceReleaseRegKey()
    {
      return @"Software\McNeel\Rhinoceros\5.0\Install\rhiexec\" + m_culture.Name;
    }

    string GetSdkServiceReleaseRegValueName()
    {
      return "packages installed for";
    }

    private string GetPackageStatusFile()
    {
      return Path.Combine(InstallerEngine.CurrentUserLocalProfileRoot, "install status.txt");
    }

    private int GetLastInstalledSdkServiceReleaseNumber()
    {
      RegistryKey key = Registry.CurrentUser.OpenSubKey(GetSdkServiceReleaseRegKey(), false);
      if (key == null)
        return 0;

      object objValue = key.GetValue(GetSdkServiceReleaseRegValueName());
      key.Close();

      if (objValue == null)
        return 0;

      if (!(objValue is int))
        return 0;

      return (int)objValue;
    }

    private bool ArePackagesCurrentByRegistry()
    {
      while (true)
      {
        int lastInstalledSdkServiceRelease = GetLastInstalledSdkServiceReleaseNumber();

        if (lastInstalledSdkServiceRelease >= m_sdkservicerelease)
          return true;

        break;
      }
        
      return false;
    }

    private void SavePackageSdkServiceRelease_Registry()
    {
      while (true)
      {
        RegistryKey key = Registry.CurrentUser.CreateSubKey(GetSdkServiceReleaseRegKey());
        if (key == null)
          break;

        if (null != key.GetValue(GetSdkServiceReleaseRegValueName()))
          key.DeleteValue(GetSdkServiceReleaseRegValueName());
        key.SetValue(GetSdkServiceReleaseRegValueName(), m_sdkservicerelease, RegistryValueKind.DWord);
        key.Close();
        break;
      }
    }

    bool IsRhiExecRunning()
    {
      System.Diagnostics.Process[] processes = System.Diagnostics.Process.GetProcessesByName("rhiexec");

      if (processes.Length > 1)
        return true;

      return false;
    }

    public int InstallPackages()
    {
      // We don't want two instances of Rhino to be installing packages at the same time!
      if (IsRhiExecRunning())
      {
        return (int)Model.InstallerPhase.AlreadyRunning;
      }

      // One time cleanup to fix http://dev.mcneel.com/bugtrack/?q=105783
      int lastSdkSrNo = GetLastInstalledSdkServiceReleaseNumber();
      if (lastSdkSrNo > 0 && lastSdkSrNo <= 201207195)
      {
        string localProfileLocalization = Path.Combine(InstallerEngine.CurrentUserLocalProfileRoot, "Localization");
        try
        {
          if (Directory.Exists(localProfileLocalization))
            Directory.Delete(localProfileLocalization, true);
        }
// ReSharper disable EmptyGeneralCatchClause
        catch
// ReSharper restore EmptyGeneralCatchClause
        {
          // Don't worry if we can't delete these folders; it's just a little house keeping anyway.
        }
      }


      // See if the m_sdkservicerelease is the same as the 
      // last time this was run. If it is, do nothing.
      if (ArePackagesCurrentByRegistry())
      {
        Logger.Log(LogLevel.Info, string.Format("InstallPackages exiting because RhinoSdkServiceRelease number has not changed: {0}", m_sdkservicerelease));
        return 0;
      }

      // Display modeless window telling user that it's installing packages.
      System.Windows.Forms.Application.EnableVisualStyles();
      System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
      m_dlg = new RMA.RhiExec.View.InitializingRhinoDialog(this);
      System.Windows.Forms.Application.Run(m_dlg);
      
      return 0;
    }

    public void DoInstallPackages()
    {
      Program.m_options.SilentInstall = true;

      // Find the directory with packages to install.
      if (string.IsNullOrEmpty(m_package_folder))
        throw new Model.RhinoInstallerException("Need to detect location of packages folder");

      List<string> packages_to_install = new List<string>();

      // Updated format again to have 'en-us' instead of 'en'
      string localization_pattern = string.Format(CultureInfo.InvariantCulture, "*({0}).rhi", m_culture.Name);
      packages_to_install.AddRange(Directory.GetFiles(m_package_folder, localization_pattern));

      // Packages for all languages
      string all_languages_pattern = string.Format(CultureInfo.InvariantCulture, "*(any).rhi");
      packages_to_install.AddRange(Directory.GetFiles(m_package_folder, all_languages_pattern));

      InstallerPhase result = InstallerPhase.Unknown;
      bool bInstallFailed = false;

      // Install "UI*" first
      for (int i = packages_to_install.Count - 1; i >= 0; i--)
      {
        string pkgName = Path.GetFileName(packages_to_install[i]);
        if (string.IsNullOrEmpty(pkgName))
          continue;

        pkgName = pkgName.ToUpperInvariant();
        if (pkgName.StartsWith("UI"))
        {
          result = InstallPackage(packages_to_install[i]);
          if (result != InstallerPhase.Success && result != InstallerPhase.AlreadyInstalled)
            bInstallFailed = true;

          packages_to_install.RemoveAt(i);
        }
      }

      // Install "localiation*" second
      for (int i = packages_to_install.Count - 1; i >= 0; i--)
      {
        string pkgName = Path.GetFileName(packages_to_install[i]);
        if (string.IsNullOrEmpty(pkgName))
          continue;
        pkgName = pkgName.ToUpperInvariant();
        if (pkgName.StartsWith("LOCALIZATION"))
        {
          result = InstallPackage(packages_to_install[i]);
          if (result != InstallerPhase.Success && result != InstallerPhase.AlreadyInstalled)
            bInstallFailed = true;
          packages_to_install.RemoveAt(i);
        }
      }

      // Run the rest of the packages next.
      foreach (string package in packages_to_install)
      {
        result = InstallPackage(package);
        if (result != InstallerPhase.Success && result != InstallerPhase.AlreadyInstalled)
          bInstallFailed = true;
      }

      // Done!
      if (!bInstallFailed)
        SavePackageSdkServiceRelease_Registry();
    }
  }
}
