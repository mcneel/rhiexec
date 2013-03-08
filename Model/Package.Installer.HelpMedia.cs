using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using RMA.RhiExec.Engine;
using System.Globalization;

namespace RMA.RhiExec.Model
{
  class PackageInstallerHelpMedia : PackageInstallerPlatformSpecific
  {
    public PackageInstallerHelpMedia()
      : base(PackageContentType.HelpMedia, 
             @"Localization\HelpMedia", 
             PackageInstallRoot.CurrentUserLocalProfile)
    {
    }

    public override bool BeforeInstall(Package package, RhinoInfo[] RhinoList, InstallerUser installAsUser)
    {
      string oldDir = Path.Combine(InstallerEngine.CurrentUserRoamingProfileRoot, @"Localization\HelpMedia");
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
