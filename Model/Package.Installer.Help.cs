using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RMA.RhiExec.Engine;

namespace RMA.RhiExec.Model
{
  class PackageInstallerHelp : PackageInstallerPlatformSpecific
  {
    public PackageInstallerHelp()
      : base(PackageContentType.Help, 
             @"Localization\$(PACKAGE_LANG)\Help", 
             PackageInstallRoot.CurrentUserLocalProfile)
    {
    }

    public override bool BeforeInstall(Package package, RhinoInfo[] RhinoList, InstallerUser installAsUser)
    {
      string oldDir = Path.Combine(InstallerEngine.CurrentUserRoamingProfileRoot, string.Format(@"Localization\{0}\Help", m_manifest.Locale.TwoLetterISOLanguageName));
      if (Directory.Exists(oldDir))
      {
        try
        {
          Directory.Delete(oldDir, true);
        }
// ReSharper disable EmptyGeneralCatchClause
        catch
// ReSharper restore EmptyGeneralCatchClause
        {
          // We don't care if it fails; it'll clean up more next time.
        }
      }
      return true;
    }
  }
}