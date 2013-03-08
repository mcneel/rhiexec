using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Xml;
using System.IO;
using RMA.RhiExec.Engine;

namespace RMA.RhiExec.Model
{
  class PackageInstallerTutorial : PackageInstallerPlatformSpecific
  {
    public PackageInstallerTutorial()
      : base(PackageContentType.Tutorial, 
             @"Tutorials\$(PACKAGE_LANG)\$(PACKAGE_TITLE) {$(PACKAGE_ID)}\$(PACKAGE_VERSION)",
             PackageInstallRoot.CurrentUserLocalProfile)
    {
    }

    public override bool BeforeInstall(Package package, RhinoInfo[] RhinoList, InstallerUser installAsUser)
    {
      // Delete tutorials from roaming profile, if they exist.
      string RoamingProfileTutorialFolder = InstallerEngine.InstallRoot(PackageInstallRoot.CurrentUserRoamingProfile);
      RoamingProfileTutorialFolder = Path.Combine(RoamingProfileTutorialFolder, "Tutorials");
      if (Directory.Exists(RoamingProfileTutorialFolder))
      {
        try
        {
          Directory.Delete(RoamingProfileTutorialFolder, true);
        }
// ReSharper disable EmptyGeneralCatchClause
        catch
// ReSharper restore EmptyGeneralCatchClause
        {
          // We don't care if the delete doesn't succeed.
        }
      }
      return true;
    }

    public override RMA.RhiExec.Engine.PackageInstallState GetInstallState(Package package)
    {
      return GetPackageInstallState(this.m_manifest.VersionNumber);
    }


  }
}
