using System;
using System.Windows.Forms;
using System.IO;
using System.Text;
using System.Diagnostics;
using RMA.RhiExec.Model;
using RMA.RhiExec.Engine;
using RMA.RhiExec.View;
/*
 * Command lines for testing
 * 
 * Launch with RHI file:
 * "D:\dev\rhino\V5Beta1\src4\rhino4\Plug-ins\ColorPickerHCL\bin\Release\ColorPickerHCL.rhi"
 * "C:\Users\brian\Documents\tmp\bgtools.rhi"
 * "D:\dev\rhino\V5Beta1\src4\rhino4\Plug-ins\ColorPickerHCL\bin\Release\ColorPickerHCL_20100211.rhi"
 * "D:\dev\rhino\V5Beta1\src4\RhinoInstallerEngine\tests\Tutorial\Level1_1.0.54.2.rhi"
 * /loglevel=Debug /logfile="C:\Users\brian\AppData\Roaming\McNeel\Rhinoceros\5.0\logs\ (20100427-165734).log" /silent d:\dev\rhino\V5Beta1\src4\rhino4\x64\Debug\Packages\localization_en.rhi
 * 
 * Python:
 * "C:\Users\brian\AppData\Roaming\McNeel\Rhinoceros\5.0\Plug-ins\PythonPlugins\BGTools\dev\BGTools.rhi"
 * 
 * Inspect Rhino:
 * /loglevel=Debug /logfile="C:\Users\brian\AppData\Roaming\McNeel\Rhinoceros\5.0\logs\ (20100210-155807).log" /INSPECTRHINO "C:\Program Files (x86)\Rhinoceros 4.0\System\Rhino4.exe" /INSPECTWORKINGDIR "C:\Users\brian\AppData\Roaming\McNeel\Rhinoceros\5.0\Plug-ins\temp\f6e21a3c-4493-4a35-b1e5-3213f5c294b3"
 * 
 * 
 * Inspect Plugin:
 * /loglevel=Debug /logfile="C:\Users\brian\AppData\Roaming\McNeel\Rhinoceros\5.0\logs\ (20100210-164702).log" /INSPECTPLUGIN "D:\dev\rhino\V5Beta1\src4\rhino4\Plug-ins\ColorPickerHCL\bin\Release\ColorPickerHCL.rhp"
 * /loglevel=Debug /logfile="C:\Users\brian\AppData\Roaming\McNeel\Rhinoceros\5.0\logs\ (20100210-164702).log" /INSPECTPLUGIN "C:\Users\brian\AppData\Roaming\McNeel\Rhinoceros\5.0\temp\4605e121-e49e-4f4a-a0b7-c2567a1a333a\ColorPickerHCL.rhp"
 * /loglevel=Debug /logfile="C:\Users\brian\AppData\Roaming\McNeel\Rhinoceros\5.0\logs\ColorPickerHCL_20100211 (20100305-113444).log" /INSPECTPLUGIN "C:\Users\brian\AppData\Roaming\McNeel\Rhinoceros\5.0\temp\5333be9d-5c28-4942-b16a-2938e01e3871\ColorPickerHCL.rhp"
 * 
 * Install multiple Packages to initialize Rhino:
 * /installpackagesforrhino /silent /locale=1028 /packagefolder="C:\Program Files\Rhinoceros 5.0 Beta (64-bit)\Packages" /rhinosdkversionnumber=201107275
 */

[assembly:CLSCompliant(true)]
namespace RMA.RhiExec
{
  static class Program
  {
    public static Options m_options = new Options();
    public static InstallerDialog m_dlg;
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static int Main(string[] args)
    {
//#if DEBUG
//      StringBuilder sb = new StringBuilder();
//      foreach (string s in args)
//        sb.Append(s).Append("\n");
//      MessageBox.Show("Attach Debugger\n\n"+sb.ToString());
//#endif

      // Initialize Logger
      string logfile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
      logfile = Path.Combine(logfile, @"McNeel\Rhinoceros\5.0\logs\RhinoInstallerLog.log");

      InstallerPhase returnCode = InstallerPhase.Unknown;

      Logger.LogFile = logfile;
      Logger.SetLogLevel(LogLevel.Debug);

      try
      {
        m_options.SetOptions(args);
        Logger.LogFile = m_options.LogFilePath;
        Logger.SetLogLevel(m_options.LogLevel);
        WriteVersionInfoToLog(args);

        while (true)
        {
          if (m_options.InspectRhinoPath != null)
          {
            returnCode = RhinoInfo.InspectRhinoAndWriteXml(m_options.InspectRhinoPath, m_options.InspectWorkingDirectory);
            break;
          }

          if (m_options.InspectPluginPath != null)
          {
            returnCode = PackageInstallerPlugin.InspectPlugin(m_options.InspectPluginPath);
            break;
          }

          if (m_options.Locale != null)
          {
            Rhino.UI.Localization.SetLanguageId(m_options.Locale.LCID);
          }

          if (m_options.InstallPackagesForRhino)
          {
            RhinoInitializer init = new RhinoInitializer(m_options.Locale, m_options.RhinoSdkVersionNumber);
            init.SetPackageFolder(m_options.PackageFolder);
            returnCode = (InstallerPhase)init.InstallPackages();
            break;
          }

          // At this point, we're going to actually execute an .rhi file.
          // Make sure we have one to execute, lest we throw an exception later.
          if (string.IsNullOrEmpty(m_options.InstallerFile))
          {
            Logger.Log(LogLevel.Error, "Package not specified on command line.");
            returnCode = InstallerPhase.PackageNotSpecified;
            break;
          }
          if (!File.Exists(m_options.InstallerFile))
          {
            Logger.Log(LogLevel.Error, string.Format("Package not found: '{0}'", m_options.InstallerFile));
            returnCode = InstallerPhase.PackageNotFound;
            break;
          }

          Application.EnableVisualStyles();
          Application.SetCompatibleTextRenderingDefault(false);

          //var fake = new InitializingRhinoDialog();
          //fake.ShowDialog();

          m_dlg = new InstallerDialog();

          if (m_options.SilentInstall)
          {
            m_dlg.ShowInTaskbar = false;
            m_dlg.Size = new System.Drawing.Size(0, 0);
            m_dlg.StartPosition = FormStartPosition.Manual;
            m_dlg.Location = new System.Drawing.Point(-5000, -5000);
          }
          else
          {
            m_dlg.StartPosition = FormStartPosition.CenterScreen;
          }
          Application.Run(m_dlg);

          returnCode = InstallerEngine.CurrentPhase();

          break;
        }
      }
      catch (PackageNotCompatibleException ex)
      {
        Logger.Log(LogLevel.Error, ex);
        returnCode = InstallerPhase.InspctPkgNotCompatible;
      }
      catch (Exception ex)
      {
        Logger.Log(LogLevel.Error, ex);
        returnCode = InstallerPhase.Exception;
      }

      Logger.Log(LogLevel.Debug, string.Format("FinalExit\tExiting installation with return code {0}", returnCode));
      WriteFooterToLog();

      switch (returnCode)
      {
        case InstallerPhase.Success:
        case InstallerPhase.AlreadyInstalled:
        case InstallerPhase.AlreadyRunning:
        case InstallerPhase.PackageNotSpecified:
          break;
        default:
#if !DEBUG
          UploadErrorReport();
#endif
          break;
      }

      return (int)returnCode;
    }

    public static InstallerPhase ExecuteChildProcess(OSPlatform platform, string arguments, bool uploadErrors)
    {
      Logger.Log(LogLevel.Debug, "ExecuteChildProcess starting: " + platform + ", " + arguments);
      StringBuilder sb = new StringBuilder();
      Process p = new Process();

      string thisDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

#if DEBUG
      if (platform == OSPlatform.x86)
        p.StartInfo.FileName = Path.Combine(thisDir, @"..\..\x86\Debug\rhiexec.exe");
      else if (platform == OSPlatform.x64)
        p.StartInfo.FileName = Path.Combine(thisDir, @"..\..\x64\Debug\rhiexec.exe");
#else
      if (platform == OSPlatform.x86)
        p.StartInfo.FileName = Path.Combine(thisDir, @"..\x86\rhiexec.exe");
      else if (platform == OSPlatform.x64)
        p.StartInfo.FileName = Path.Combine(thisDir, @"..\x64\rhiexec.exe");
#endif

      sb.Append("/loglevel=").Append(m_options.LogLevel.ToString()).Append(" ");
      sb.Append("/logfile=\"").Append(Logger.LogFile).Append("\" ");
      if (m_options.SilentInstall)
      {
        sb.Append("/silent ");
        p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        p.StartInfo.CreateNoWindow = true;
      }
      if (!uploadErrors)
        sb.Append("/noerrorreports ");
      sb.Append(arguments);

      p.StartInfo.Arguments = sb.ToString();
      p.StartInfo.UseShellExecute = false;

      Logger.Log(LogLevel.Debug, "Starting: " + p.StartInfo.FileName + " " + p.StartInfo.Arguments);
      if (!p.Start())
        return InstallerPhase.Unknown;

      // Fix for http://dev.mcneel.com/bugtrack/?q=68426
      // Add more time before cancelling operation
      int timeout = 10 * 60*1000; // 10 minutes
      p.WaitForExit(timeout);

      // Fix for http://dev.mcneel.com/bugtrack/?q=68426
      // Don't reference p.ExitCode property after calling p.Close - it crashes.
      int exitCode = 0;
      if (p.HasExited)
      {
        exitCode = p.ExitCode;
      }
      else
      {
        p.Close();
        Logger.Log(LogLevel.Warning, string.Format("Forcing closed after {0} seconds: {1}", timeout / 1000, p.StartInfo.FileName));
      }

      Logger.Log(LogLevel.Debug, "ExecuteChildProcess complete. Child process returned: " + (InstallerPhase)exitCode);
      return (InstallerPhase)exitCode;
    }

    public static void UploadErrorReport()
    {
      if (!m_options.EnableErrorReporting)
        return;

      // RmaErrorReporting.exe -upload -source=rhiexec -u=file -ud=file -silent"
      string args = string.Format("-upload -source=rhiexec -u=\"{0}\" -silent", m_options.LogFilePath);
      Logger.Log(LogLevel.Info, "Uploading this log file");

      string thisDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
      string RmaErrorReportingPath = Path.Combine(thisDir, @"..\RmaErrorReporting.exe");
#if DEBUG
      RmaErrorReportingPath = @"..\..\..\..\RmaErrorReporting\Debug\RmaErrorReporting.exe";
#endif

      if (File.Exists(RmaErrorReportingPath))
        Process.Start(RmaErrorReportingPath, args);
      else
      {
        Logger.Log(LogLevel.Warning, RmaErrorReportingPath + " not found");
      }
    }

    private static void WriteVersionInfoToLog(string[] args)
    {
      // Apparently sometimes OSInfo can fail; we'd rather write the log file 
      // and continue installing than have this information.
      // Fixes http://dev.mcneel.com/bugtrack/?q=68442
      try
      {
        Logger.Log(LogLevel.Info, string.Format("Start: rhiexec\nversion {0}\n{1}\n{2} {3} {4} {5}",
          System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(),
          InstallerEngine.Is64BitProcess() ? "64-bit" : "32-bit",
          OSInfo.Name, OSInfo.Edition, OSInfo.ServicePack, OSInfo.VersionString));
      }
      catch
      {
        Logger.Log(LogLevel.Warning, "System information could not be loaded");
      }

      try
      {
        StringBuilder msg = new StringBuilder();
        msg.Append("arguments: ");
        foreach (string arg in args)
          msg.Append("\r\n\"").Append(arg).Append("\"");
        Logger.Log(LogLevel.Info, msg.ToString());
      }
      catch
      {
        Logger.Log(LogLevel.Warning, "command line arguments could not be printed");
      }

      Logger.Log(LogLevel.Info, "Logging started: " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture));
    }

    private static void WriteFooterToLog()
    {
      Logger.Log(LogLevel.Info, string.Format("Logging ended: {0}\n", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)));
    }
  }
}