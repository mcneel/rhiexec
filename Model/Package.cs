using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using ICSharpCode.SharpZipLib.Zip;
using System.Xml;

namespace RMA.RhiExec.Model
{
  /// <summary>
  /// This class encapsulates the details of dealing with the package storage
  /// itself. At this point, the package is not an abstract class because
  /// we have only one representation of a package: a ZIP archive.
  /// </summary>
  class Package
  {
    string m_package_path;
    string m_destination_folder;
    private Collection<PackageFileKey> m_files;
    private int m_files_unzipped; //= 0
    private List<string> m_backup_files = new List<string>();
    
    public Package(string PackageFilePath)
    {
      m_package_path = PackageFilePath;
    }

    #region Public Methods
    public string PackagePath
    {
      get
      {
        return m_package_path;
      }
    }

    public string DestinationFolder
    {
      get { return m_destination_folder; }
      set { m_destination_folder = value; }
    }

    public bool ContainsFileNamed(string fileName)
    {
      foreach (PackageFileKey file in Files)
      {
        if (file.Key.ToUpperInvariant() == fileName.ToUpperInvariant())
          return true;
      }
      return false;
    }

    public PackageFileKey FindSingleFile(string pattern)
    {
      Collection<PackageFileKey> keys = FindFiles(pattern);
      if (keys.Count > 0)
        return keys[0];

      return null;
    }

    public Collection<PackageFileKey> FindFiles(string pattern)
    {
      Regex rx = new Regex(pattern, RegexOptions.IgnoreCase);
      return FindFiles(rx);
    }

    public Collection<PackageFileKey> FindFiles(Regex expression)
    {
      Collection<PackageFileKey> matches = new Collection<PackageFileKey>();

      foreach (PackageFileKey key in Files)
      {
        if (expression.IsMatch(key.Key))
          matches.Add(key);
      }

      return matches;
    }

    public Collection<PackageFileKey> Files
    {
      get
      {
        if (m_package_path == null)
          return null;

        if (m_files == null)
        {
          m_files = new Collection<PackageFileKey>();

          using (ZipFile f = OpenZipFile(m_package_path))
          {
            foreach (ZipEntry entry in f)
            {
              m_files.Add(new PackageFileKey(entry.Name));
            }
            f.Close();
          }
          
        }
        return m_files;
      }
    }

    public string GetFullPath(PackageFileKey fileName)
    {
      // Extract to temporary directory
      if (fileName == null || string.IsNullOrEmpty(fileName.Key))
        return null;

      string fullpath = Path.Combine(DestinationFolder, fileName.Key.Replace(@"/", @"\"));
      if (File.Exists(fullpath))
        return fullpath;

      fullpath = ExtractFile(fileName, DestinationFolder);
      return fullpath;
    }

    public bool Install(string destination_folder, PackageInstallerBase SelectedInstaller)
    {
      m_destination_folder = destination_folder;
      Engine.InstallerEngine.ReportProgress(LogLevel.Info, "Installing package contents to: " + destination_folder);
      m_files_unzipped = 0;
      var installedFiles = new List<string>();

      bool installSucceeded = true;
      using (ZipFile f = OpenZipFile(m_package_path))
      {
        ZipEntry packageEntry = null;
        foreach (ZipEntry entry in f)
        {
          if (0 == string.Compare("package.xml", entry.Name, true))
          {
            packageEntry = entry;
            continue;
          }

          entry.Flags = (int)GeneralBitFlags.UnicodeText;
          string msg = "";
          string destFile = Path.Combine(destination_folder, entry.Name);
          int percentComplete = (int)(100 * (double)m_files_unzipped++ / (double)Files.Count);
          installedFiles.Add(entry.Name);
          if (File.Exists(destFile))
          {
            if (!SelectedInstaller.ShouldReplaceFile(destFile))
            {
              msg = string.Format("  keeping local file '{0}'", destFile);
              continue;
            }
          }
          msg = string.Format("  installing '{0}'", destFile);
          if (entry.IsFile)
            if (!ExtractFile(f, entry, destFile))
              installSucceeded = false;
          Engine.InstallerEngine.ReportProgress(LogLevel.Debug, msg, percentComplete);
        }

        // Install package.xml
        if (packageEntry != null && installSucceeded )
        {
          string packageXmlPath = Path.Combine(destination_folder, packageEntry.Name);
          if (!ExtractFile(f, packageEntry, packageXmlPath))
            installSucceeded = false;

          // Add installed files to package.xml for future removal.
          var doc = new XmlDocument();
          doc.Load(packageXmlPath);
          var installDirElement = doc.CreateElement("InstallFolder");
          installDirElement.InnerText = destination_folder;
          doc.DocumentElement.AppendChild(installDirElement);
          var installedFilesElement = doc.CreateElement("InstalledFiles");
          if (doc.DocumentElement != null)
          {
            doc.DocumentElement.AppendChild(installedFilesElement);
            foreach (var file in installedFiles)
            {
              var fileElement = doc.CreateElement("File");
              fileElement.InnerText = file.Replace("/", "\\");
              installedFilesElement.AppendChild(fileElement);
            }
          }
          doc.Save(packageXmlPath);
        }

        f.Close();
      }

      // If install failed, try to rollback from the backup files.
      if (installSucceeded)
      {
        DeleteBackupFiles();
      }
      else
      {
        Logger.Log(LogLevel.Info, "Installation failed; restoring backup files");
        RestoreBackupFiles();
      }

      return installSucceeded;
    }

    #endregion

    private ZipFile OpenZipFile(string filename)
    {
      // Try several times to open a zip file, waiting a bit longer each time, just in case a virus scanner or indexer 
      // has it opened exclusively. If we can't open it after three tries and 2 seconds, rethrow the exception.
      // http://dev.mcneel.com/bugtrack/?q=68427
      int numberOfTries = 0;
      int maxTries = 3;


      ZipFile zipFile = null;
      while (numberOfTries < maxTries + 1)
      {
        try
        {
          zipFile = new ZipFile(filename);
          break;
        }
        catch (System.IO.IOException ex)
        {
          if (numberOfTries >= maxTries)
            throw new System.IO.IOException(string.Format("Tried to open '{0}' ({1}) times, but failed", filename, maxTries), ex);

          // wait and try again.
          numberOfTries++;
          System.Threading.Thread.Sleep(500 * numberOfTries);
          continue;
        }
      }

      if (zipFile == null)
        throw new RhinoInstallerException(string.Format("OpenZipFile('{0}') failed", filename));

      return zipFile;
    }

    private bool BackupFile(string destinationFile)
    {
      string backupFile = destinationFile + ".dozadu"; // slovak for "backup", that is "your truck".
      try
      {
        if (File.Exists(destinationFile))
        {
          if (File.Exists(backupFile))
            File.Delete(backupFile);

          File.Move(destinationFile, backupFile);
          m_backup_files.Add(backupFile);
        }
        return true;
      }
      catch
      {
        return false; // file backup failed... therefore installation will fail
      }
    }

    private void RestoreBackupFiles()
    {
      foreach (string backupFile in m_backup_files)
      {
        // delete original
        if (!backupFile.EndsWith(".dozadu"))
          continue;

        string originalFile = backupFile.Substring(0, backupFile.Length - ".dozadu".Length);
        try
        {
          File.Delete(originalFile);
          File.Move(backupFile, originalFile);
        }
        catch (Exception ex)
        {
          Logger.Log(LogLevel.Warning, ex);
        }
      }
    }

    private void DeleteBackupFiles()
    {
      foreach (string backupFile in m_backup_files)
      {
        if (!File.Exists(backupFile))
          continue;

        try
        {
          File.Delete(backupFile);
        }
// ReSharper disable EmptyGeneralCatchClause
        catch (Exception)
// ReSharper restore EmptyGeneralCatchClause
        {
          // We don't care about this.
        }
      }
    }

    private bool ExtractFile(ZipFile f, ZipEntry entry, string destinationFile)
    {
      if (entry.IsFile)
      {
        using (Stream s = f.GetInputStream(entry))
        {
          byte[] data = new byte[4096];

          if (!BackupFile(destinationFile))
            return false;

          try
          {
            string dir = Path.GetDirectoryName(destinationFile);
            if (!Directory.Exists(dir))
              Directory.CreateDirectory(dir);

            FileStream fs = new FileStream(destinationFile, FileMode.Create);

            BinaryWriter bw = new BinaryWriter(fs);

            int size = s.Read(data, 0, data.Length);
            while (size > 0)
            {
              bw.Write(data, 0, size);
              size = s.Read(data, 0, data.Length);
            }
            bw.Flush();
            bw.Close();

            // Close can be ommitted as the using statement will do it automatically
            // but leaving it here reminds you that is should be done.
            s.Close();

            // Make sure created and modified dates match
            DateTime modifiedDate = File.GetLastWriteTime(destinationFile);
            File.SetCreationTime(destinationFile, modifiedDate);
            return true;
          }
          catch (Exception ex)
          {
            Logger.Log(LogLevel.Error, ex);
            return false;
          }
        }
      }
      return false;
    }

    private string ExtractFile(PackageFileKey fileName, string folder)
    {
      using (ZipFile f = OpenZipFile(m_package_path))
      {
        ZipEntry entry = f.GetEntry(fileName.Key);
        if (entry == null)
        {
          Engine.InstallerEngine.ReportProgress(LogLevel.Info, "File not found in package: " + fileName);
          return null;
        }

        string fullpath = Path.Combine(folder, fileName.Key.Replace("/", "\\"));
        if (ExtractFile(f, entry, fullpath))
          return fullpath;

        return null;
      }
    }
  }

  class PackageFileKey
  {
    string m_key;
    public string Key
    {
      get { return m_key; }
      set { m_key = value; }
    }

    public PackageFileKey(string keyName)
    {
      m_key = keyName;
    }
  }
}
