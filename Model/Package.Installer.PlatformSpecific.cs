using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using RMA.RhiExec.Engine;
using System.Globalization;

namespace RMA.RhiExec.Model
{
  class PackageInstallerPlatformSpecific : PackageInstallerBase
  {
    protected PackageManifest m_manifest;
    protected PackageContentType m_content_type;
    protected string m_install_folder;
    protected PackageInstallRoot m_install_root = PackageInstallRoot.CurrentUserRoamingProfile;

    public PackageInstallerPlatformSpecific(PackageContentType contentType, string installFolder, PackageInstallRoot installRoot)
    {
      m_content_type = contentType;
      m_install_folder = installFolder;
      m_install_root = installRoot;
    }

    public override Guid ID
    {
      get { return m_manifest.ID; }
    }

    public override Version PackageVersion
    {
      get { return m_manifest.VersionNumber; }
    }

    public override string Title
    {
      get { return m_manifest.Title; }
    }

    public override string PackagePath
    {
      get { return m_manifest.Path; }
    }

    public override PackageContentType ContentType
    {
      get
      {
        return m_content_type;
      }
    }

    public override PackageInstallRoot InstallRoot
    {
      get { return m_install_root; }
    }

    public override string InstallFolder(string root)
    {
      string dir = Path.Combine(root, m_install_folder);

      dir = dir.Replace("$(PACKAGE_LANG)", m_manifest.Locale.Name);
      dir = dir.Replace("$(PACKAGE_TITLE)", RemoveInvalidPathChars(Title));
      dir = dir.Replace("$(PACKAGE_ID)", RemoveInvalidPathChars(this.ID.ToString().ToUpperInvariant()));
      dir = dir.Replace("$(PACKAGE_VERSION)", m_manifest.VersionNumber.ToString());

      if (dir.Contains("$("))
      {
        throw new Exception("Unexpected macro in path: " + dir);
      }

      return dir;
    }

    public override RMA.RhiExec.Engine.PackageInstallState GetInstallState(Package package)
    {
      PackageManifest manifestInstalled = new PackageManifest(m_content_type);
      if (!LoadManifest(this.InstallFolder(InstallerEngine.InstallRoot(this.InstallRoot)), m_content_type, out manifestInstalled))
        return PackageInstallState.NotInstalled;

      PackageManifest manifestNew = new PackageManifest(m_content_type);
      if (!LoadManifest(PackagePath, m_content_type, out manifestNew))
        throw new ManifestFileNotFoundException(PackagePath);

      if (manifestInstalled.VersionNumber > manifestNew.VersionNumber)
        return PackageInstallState.NewerVersionInstalledCurrentUser;
      else if (manifestInstalled.VersionNumber == manifestNew.VersionNumber)
        return PackageInstallState.SameVersionInstalledCurrentUser;
      else if (manifestInstalled.VersionNumber < manifestNew.VersionNumber)
        return PackageInstallState.OlderVersionInstalledCurrentUser;
      else
        return PackageInstallState.NotInstalled;
    }

    public override bool IsCompatible(RhinoInfo rhino)
    {
      foreach (RhinoPlatform platform in m_manifest.SupportedPlatforms)
      {
        if (platform == rhino.RhinoPlatform)
        {
          ReportProgress("Package (" + this.PackagePath + ") is compatible with Rhino: " + rhino.RhinoVersion.ToString(), LogLevel.Info);
          return true;
        }
        else
        {
          ReportProgress("Package (" + this.PackagePath + ") NOT compatible with Rhino: " + rhino.RhinoVersion.ToString(), LogLevel.Debug);
        }
      }
      return false;
    }

    protected static bool LoadManifest(string PackageFolder, PackageContentType contentType, out PackageManifest manifest)
    {
      manifest = new PackageManifest(contentType);
      if (!Directory.Exists(PackageFolder))
        return false;

      string[] files = Directory.GetFiles(PackageFolder, PackageManifestName);
      if (files.Length == 0)
      {
        ReportProgress("LoadManifest() did not find " + PackageManifestName + " for package at: " + PackageFolder, LogLevel.Debug);
        return false;
      }

      manifest.ReadXml(files[0]);
      if (!manifest.IsValid())
      {
        ReportProgress(string.Format("{0}::LoadManifest() {1} not valid.", contentType.ToString(), PackageManifestName), LogLevel.Debug);
        return false;
      }

      manifest.Path = PackageFolder;
      ReportProgress(string.Format("{0}::LoadManifest() {1} recognized.", contentType.ToString(), PackageManifestName), LogLevel.Debug);

      return true;
    }

    protected bool LoadManifest(string PackageFolder)
    {
      return LoadManifest(PackageFolder, m_content_type, out m_manifest);
    }

    public override bool ContainsRecognizedPayload(Package package)
    {
      PackageFileKey key = package.FindSingleFile(PackageManifestName);
      if (key != null)
      {
        string manifestFullPath = package.GetFullPath(key);
        if (File.Exists(manifestFullPath))
        {
          return LoadManifest(Path.GetDirectoryName(manifestFullPath));
        }
        ReportProgress(string.Format("Recognized payload as {0} Package", m_content_type.ToString()), LogLevel.Info);
        return true;
      }

      ReportProgress(string.Format("Package not recognized as {0} Package", m_content_type.ToString()), LogLevel.Info);
      return false;
    }

    public override bool Initialize(Package package)
    {
      PackageFileKey key = package.FindSingleFile(PackageManifestName);
      string manifest_path = package.GetFullPath(key);

      if (!LoadManifest(Path.GetDirectoryName(manifest_path)))
      {
        ReportProgress("Initialize() failed for package at " + package.PackagePath, LogLevel.Debug);
        return false;
      }

      return true;
    }

    public override string Describe()
    {
      return string.Format("{0} Package", m_content_type.ToString());
    }
  }
}
