using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using RMA.RhiExec.Engine;
using System.Globalization;

namespace RMA.RhiExec.Model
{
  class PackageInstallerLocalization : PackageInstallerPlatformSpecific
  {
    public PackageInstallerLocalization()
      : base(PackageContentType.Localization, 
             @"Localization\$(PACKAGE_LANG)", 
             PackageInstallRoot.CurrentUserRoamingProfile)
    {
    }

    public override bool BeforeInstall(Package package, RhinoInfo[] RhinoList, InstallerUser installAsUser)
    {
      // Delete the entire Localization folder if it exists in the Roaming profile.
      // It will be replaced by a Template Files folder, and most localization stuff will move to the local profile.
      //try
      //{
      //  string oldPath = Path.Combine(InstallerEngine.CurrentUserRoamingProfileRoot, @"Localization");
      //  if (Directory.Exists(oldPath))
      //  {
      //    Directory.Delete(oldPath, true);
      //  }
      //}
      //finally
      //{
      //}

      return true;
    }

    public override bool ShouldReplaceFile(string DestinationFilePath)
    {
      string ext = Path.GetExtension(DestinationFilePath);
      if (string.IsNullOrEmpty(ext))
        return true;

      ext = ext.ToUpperInvariant();
      if (ext == ".3DM")
      {
        DateTime created = File.GetCreationTime(DestinationFilePath);
        DateTime modified = File.GetLastWriteTime(DestinationFilePath);

        // Old versions of the Rhino Installer Engine installed files
        // but didn't ensure that the Created and Modified dates match.
        if (created < new DateTime(2011, 03, 15))
          return true;

        if (created > modified)
          return true;

        // 1 tick == 1 / 10,000,000 second
        double diff = Math.Abs(created.Ticks - modified.Ticks);
        double seconds_of_slop = 3.0;
        if (diff > seconds_of_slop * 1.0e7) // created and modified differ by more than seconds_of_slop.
          return false;
      }
      return true;
    }

  }
}
