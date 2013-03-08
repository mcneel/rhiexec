using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using RMA.RhiExec.Engine;
using System.Globalization;

namespace RMA.RhiExec.Model
{
  class PackageInstallerUI : PackageInstallerPlatformSpecific
  {
    public PackageInstallerUI()
      : base(PackageContentType.UserInterface, @"UI", PackageInstallRoot.CurrentUserRoamingProfile)
    {
    }

    public override bool BeforeInstall(Package package, RhinoInfo[] RhinoList, InstallerUser installAsUser)
    {
      if (Directory.Exists(package.DestinationFolder))
      {
        string[] files = Directory.GetFiles(package.DestinationFolder, "*.tb");
        foreach (string file in files)
        {
          try
          {
            File.Delete(file);
          }
          catch (System.UnauthorizedAccessException)
          {
            // Fixes http://dev.mcneel.com/bugtrack/?q=68713
            // Unable to delete .tb file; we'll try again next time.
          }
        }
      }

      return true;
    }

    public override bool ShouldReplaceFile(string DestinationFilePath)
    {
      string ext = Path.GetExtension(DestinationFilePath);
      if (string.IsNullOrEmpty(ext))
        return true;

      ext = ext.ToUpperInvariant();
      if (ext == ".RUI")
      {
        // Fixing http://dev.mcneel.com/bugtrack/?q=101288
        // if the creation date of the RUI file is older than 9/6/2012, replace it
        // otherwise don't.
        DateTime created = File.GetCreationTime(DestinationFilePath);
        if (created < new DateTime(2012, 9, 7))
        {
          string bakFileName = Path.Combine(Path.GetDirectoryName(DestinationFilePath), "default (old 1).rui");
          try
          {
            if (File.Exists(DestinationFilePath))
              File.Copy(DestinationFilePath, bakFileName, true);
          }
          catch {  /* Can't overwrite backup file. Sigh */ }
          return true;
        }

        return false;
      }

      return true;
    }

  }
}
