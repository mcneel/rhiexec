using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.IO;
using RMA.RhiExec.Engine;

namespace RMA.RhiExec.Model
{
  class Options
  {
    public bool SilentInstall; // = false;
    public string LogFilePath = "";
    public InstallerUser User = InstallerUser.CurrentUser;
    public string InstallerFile = "";
    public LogLevel LogLevel = LogLevel.Debug;
    public string InspectRhinoPath; // = null;
    public string InspectPluginPath; // = null;
    public string InspectWorkingDirectory; // = null;
    public bool EnableErrorReporting = true;
    
    // command line args used by the RhinoInitializer class
    public bool InstallPackagesForRhino; // = false;
    public CultureInfo Locale = CultureInfo.CurrentCulture;
    public int RhinoSdkVersionNumber; // = 0;
    public string PackageFolder; // = null;


    public void SetOptions(string[] args)
    {
      CommandLineParser cmdline = new CommandLineParser(args);

      if (cmdline.GetBoolValue("ADMIN"))
        User = InstallerUser.AllUsers;

      SilentInstall = cmdline.GetBoolValue("SILENT");
      LogFilePath = cmdline.GetStringValue("LOGFILE");
      InstallPackagesForRhino = cmdline.GetBoolValue("INSTALLPACKAGESFORRHINO");
      EnableErrorReporting = !cmdline.GetBoolValue("NOERRORREPORTS");
      
      RhinoSdkVersionNumber = cmdline.GetIntValue("RHINOSDKVERSIONNUMBER");
      InspectRhinoPath = cmdline.GetStringValue("INSPECTRHINO");
      InspectPluginPath = cmdline.GetStringValue("INSPECTPLUGIN");
      PackageFolder = cmdline.GetStringValue("PACKAGEFOLDER");
      InspectWorkingDirectory = cmdline.GetStringValue("INSPECTWORKINGDIR");
      try
      {
        string sLogLevel = cmdline.GetStringValue("LOGLEVEL");
        if (!string.IsNullOrEmpty(sLogLevel))
        {
          sLogLevel = sLogLevel.ToLowerInvariant();
          sLogLevel = sLogLevel[0].ToString().ToUpperInvariant() + sLogLevel.Substring(1);
          LogLevel = (LogLevel)Enum.Parse(LogLevel.GetType(), sLogLevel);
        }
      }
      catch (ArgumentException)
      {
        LogLevel = LogLevel.Debug;
      }


      if (cmdline.ContainsKey("LOCALE"))
      {
        try
        {
          // see if an integer value was passed in as Locale argument
          int nLocale = cmdline.GetIntValue("LOCALE", -1);
          if (nLocale != -1)
            Locale = new CultureInfo(nLocale);
          else
            Locale = new CultureInfo(cmdline.GetStringValue("LOCALE"));
        }
        catch (System.ArgumentException)
        {
          Engine.InstallerEngine.ReportProgress(LogLevel.Error, "Options::SetOptions() encountered invalid Locale: " + cmdline["LOCALE"]);
        }
      }

      foreach (string arg in args)
      {
        if (arg.EndsWith(".rhi", StringComparison.OrdinalIgnoreCase))
          InstallerFile = arg;
      }

      string logfilename;
      if (InstallPackagesForRhino)
        logfilename = "InstallPackagesForRhino";
      else if (!string.IsNullOrEmpty(InspectRhinoPath))
        logfilename = "InspectRhino";
      else if (!string.IsNullOrEmpty(InspectPluginPath))
        logfilename = "InspectPlugin";
      else
        logfilename = Path.GetFileNameWithoutExtension(InstallerFile);

      logfilename += " (" + DateTime.Now.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture) + ").log";
      if (String.IsNullOrEmpty(LogFilePath))
      {
        if (User == InstallerUser.AllUsers)
          LogFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"McNeel\Rhinoceros\5.0\logs\" + logfilename);
        else if (User == InstallerUser.CurrentUser)
          LogFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"McNeel\Rhinoceros\5.0\logs\" + logfilename);
      }
    }
  }
}
