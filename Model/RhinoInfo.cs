using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Text;
using System.Xml;
using Microsoft.Win32;
using RMA.RhiExec.Engine;

namespace RMA.RhiExec.Model
{
  class RhinoInfo
  {
    private static readonly Version NullVersion = new Version("0.0.0.0");
    public Version RhinoVersion = NullVersion;
    public string RhinoSdkVersion = "";
    public string Edition = "";
    public string RhinoSdkServiceRelease = "";
    public string RhinoDotNetVersion = "";
    public string RhinoCommonVersion = "";
    public string RhinoExePath;
    public OSPlatform OS = OSPlatform.Unknown;


    public void Inspect(string PathToRhino)
    {
      Logger.Log(LogLevel.Debug, "InspectRhino\tConstructing new RhinoInfo: " + PathToRhino);
      RhinoExePath = PathToRhino;
      if (IntPtr.Size == 4)
        OS = OSPlatform.x86;
      else if (IntPtr.Size == 8)
        OS = OSPlatform.x64;

      AssemblyResolver.AddSearchPath(RhinoExePath, true);
      GetRhinoDotNetVersion();
      GetRhinoCommonVersion();
      GetCppSdkVersions();
    }

    public bool IsValid()
    {
      if (RhinoVersion == NullVersion)
        return false;

      if (string.IsNullOrEmpty(RhinoSdkVersion))
        return false;

      if (string.IsNullOrEmpty(RhinoSdkServiceRelease))
        return false;

      if (string.IsNullOrEmpty(RhinoDotNetVersion))
        return false;

      if (string.IsNullOrEmpty(RhinoExePath))
        return false;

      if (OS == OSPlatform.Any || OS == OSPlatform.Unknown)
        return false;

      return true;
    }

    public RhinoPlatform RhinoPlatform
    {
      get
      {

        if (OS == OSPlatform.x64)
          return RhinoPlatform.Rhino5_win64;

        if (OS == OSPlatform.x86)
        {
          if (RhinoVersion.Major == 4)
            return RhinoPlatform.Rhino4_win32;
          else if (RhinoVersion.Major == 5)
            return RhinoPlatform.Rhino5_win32;
          else
            return RhinoPlatform.Unknown;
        }

        return RhinoPlatform.Unknown;
      }
    }

    public bool ReadXml(string XmlPath)
    {
      Logger.Log(LogLevel.Debug, "InspectRhino\tReadXml: " + XmlPath);

      XmlDocument doc = new XmlDocument();
      doc.Load(XmlPath);

      XmlNode node = doc.SelectSingleNode("/RhinoExeInfo");
      if (node == null)
        return false;

      if (node.Attributes == null)
        return false;

      XmlAttribute verAttr = node.Attributes["Version"];
      if (verAttr == null)
        return false;

      string version = verAttr.Value;
      if (version != "1.0.1")
        return false;

      XmlNode exeinfo = doc.SelectSingleNode("/RhinoExeInfo");

      RhinoVersion = new Version(XmlHelper.SelectSingleNodeInnerText(exeinfo, "RhinoVersion", XmlPath));
      RhinoSdkVersion = XmlHelper.SelectSingleNodeInnerText(exeinfo, "RhinoSdkVersion", XmlPath);
      Edition = XmlHelper.SelectSingleNodeInnerText(exeinfo, "Edition", XmlPath);
      RhinoSdkServiceRelease = XmlHelper.SelectSingleNodeInnerText(exeinfo, "RhinoSdkServiceRelease", XmlPath);
      RhinoDotNetVersion = XmlHelper.SelectSingleNodeInnerText(exeinfo, "RhinoDotNetVersion", XmlPath);
      RhinoCommonVersion = XmlHelper.SelectSingleNodeInnerText(exeinfo, "RhinoCommonVersion", XmlPath);
      RhinoExePath = XmlHelper.SelectSingleNodeInnerText(exeinfo, "RhinoExePath", XmlPath);

      string platform = XmlHelper.SelectSingleNodeInnerText(exeinfo, "OSPlatform", XmlPath);

      try
      {
        OS = (OSPlatform)Enum.Parse(OS.GetType(), platform);
      }
      catch
      {
        throw new InvalidOperatingSystemException("Unsupported OS Platform: " + platform);
      }

      return true;
    }

    public void WriteXml(string XmlPath)
    {
      Logger.Log(LogLevel.Debug, "InspectRhino\tWriteXml: " + XmlPath);

      XmlDocument doc = new XmlDocument();
      doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));

      XmlElement rhi = doc.CreateElement("RhinoExeInfo");
      XmlAttribute attr = doc.CreateAttribute("Version");
      attr.Value = "1.0.1";
      rhi.Attributes.Append(attr);
      doc.AppendChild(rhi);

      XmlHelper.AppendElement(rhi, "RhinoVersion", this.RhinoVersion.ToString());
      XmlHelper.AppendElement(rhi, "Edition", this.Edition);
      XmlHelper.AppendElement(rhi, "RhinoExePath", this.RhinoExePath);
      XmlHelper.AppendElement(rhi, "RhinoSdkVersion", this.RhinoSdkVersion);
      XmlHelper.AppendElement(rhi, "RhinoSdkServiceRelease", this.RhinoSdkServiceRelease);
      XmlHelper.AppendElement(rhi, "RhinoDotNetVersion", this.RhinoDotNetVersion);
      XmlHelper.AppendElement(rhi, "RhinoCommonVersion", this.RhinoCommonVersion);
      XmlHelper.AppendElement(rhi, "OSPlatform", this.OS.ToString());

      doc.Save(XmlPath);
    }

    public string GetXmlFileName()
    {
      string filename = "";
      filename += "__~~RhinoInfo~~__";
      filename += RhinoVersion;
      filename += ".tmp.xml";
      return filename;
    }

    private void GetCppSdkVersions()
    {
      Logger.Log(LogLevel.Debug, "InspectRhino\tGetCppSdkVersions starting");
      if (!System.IO.File.Exists(RhinoExePath))
      {
        Logger.Log(LogLevel.Warning, "InspectRhino\tRhino not found: " + RhinoExePath);
        return;
      }

      System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(RhinoExePath);
      RhinoVersion = new Version(fvi.FileMajorPart, fvi.FileMinorPart, fvi.FileBuildPart, fvi.FilePrivatePart);
      Logger.Log(LogLevel.Info, "InspectRhino\tRhino Version: " + RhinoVersion);
      Edition = fvi.SpecialBuild;
      if (fvi.FileMajorPart != 4 && fvi.FileMajorPart != 5)
      {
        Logger.Log(LogLevel.Warning, "InspectRhino\tUnexpected Rhino Version: " + RhinoVersion);
        return;
      }


      IntPtr hRhino = UnsafeNativeMethods.LoadLibraryEx(RhinoExePath, IntPtr.Zero, UnsafeNativeMethods.DONT_RESOLVE_DLL_REFERENCES);
      if (IntPtr.Zero == hRhino)
      {
        Logger.Log(LogLevel.Warning, "InspectRhino\tLoadLibraryEx failed: " + RhinoExePath);
        return;
      }

      // Rhino 5 (post 22 Jan 2010) includes "C" style exported functions for getting the
      // sdk version/service release.
      IntPtr funcPtrSdkVersion = UnsafeNativeMethods.GetProcAddress(hRhino, "RhinoSdkVersion");
      IntPtr funcPtrSdkServiceRelease = UnsafeNativeMethods.GetProcAddress(hRhino, "RhinoSdkServiceRelease");
      if (IntPtr.Zero != funcPtrSdkVersion && IntPtr.Zero != funcPtrSdkServiceRelease)
      {
        UnsafeNativeMethods.GetIntegerInvoke GetVersionInt;
        GetVersionInt = (UnsafeNativeMethods.GetIntegerInvoke)Marshal.GetDelegateForFunctionPointer(funcPtrSdkVersion, typeof(UnsafeNativeMethods.GetIntegerInvoke));
        RhinoSdkVersion = GetVersionInt().ToString(CultureInfo.InvariantCulture);
        Logger.Log(LogLevel.Info, "InspectRhino\tRhinoSdkVersion: " + RhinoSdkVersion);
        GetVersionInt = (UnsafeNativeMethods.GetIntegerInvoke)Marshal.GetDelegateForFunctionPointer(funcPtrSdkServiceRelease, typeof(UnsafeNativeMethods.GetIntegerInvoke));
        RhinoSdkServiceRelease = GetVersionInt().ToString(CultureInfo.InvariantCulture);

        Logger.Log(LogLevel.Info, "InspectRhino\tRhinoSdkServiceRelease: " + RhinoSdkServiceRelease);
        return; //got em, we're done
      }

      // If we couldn't find the exported functions, then we're dealing with a Rhino4
      switch (RhinoVersion.ToString())
      {
        case "4.0.2006.1206":
          RhinoSdkVersion = "200612060";
          RhinoSdkServiceRelease = "200612060";
          //RhinoDotNetVersion = "4.0.61206.0";
          break;

        case "4.0.2007.118":
          RhinoSdkVersion = "200612060";
          RhinoSdkServiceRelease = "200612060";
          //RhinoDotNetVersion = "4.0.61206.0";
          break;

        case "4.0.2007.1017 ":
          RhinoSdkVersion = "200612060";
          RhinoSdkServiceRelease = "200710174";
          //RhinoDotNetVersion = "4.0.61206.1";
          break;

        case "4.0.2008.206 -err":
          RhinoSdkVersion = "200612060";
          RhinoSdkServiceRelease = "200710174";
          //RhinoDotNetVersion = "4.0.61206.1";
          break;

        case "4.0.2008.222 ":
          RhinoSdkVersion = "200612060";
          RhinoSdkServiceRelease = "200802224";
          //RhinoDotNetVersion = "4.0.61206.3";
          break;

        case "4.0.2008.602 ":
          RhinoSdkVersion = "200612060";
          RhinoSdkServiceRelease = "200806024";
          //RhinoDotNetVersion = "4.0.61206.4";
          break;

        case "4.0.2008.715 ":
          RhinoSdkVersion = "200612060";
          RhinoSdkServiceRelease = "200807154";
          //RhinoDotNetVersion = "4.0.61206.4";
          break;

        case "4.0.2008.718 ":
          RhinoSdkVersion = "200612060";
          RhinoSdkServiceRelease = "200807184";
          //RhinoDotNetVersion = "4.0.61206.5";
          break;

        case "4.0.2008.807 ":
          RhinoSdkVersion = "200612060";
          RhinoSdkServiceRelease = "200808074";
          //RhinoDotNetVersion = "4.0.61206.5";
          break;

        case "4.0.2008.827 ":
          RhinoSdkVersion = "200612060";
          RhinoSdkServiceRelease = "200808274";
          //RhinoDotNetVersion = "4.0.61206.5";
          break;

        case "4.0.2008.1215 ":
          RhinoSdkVersion = "200612060";
          RhinoSdkServiceRelease = "200811034";
          //RhinoDotNetVersion = "4.0.61206.8";
          break;

        case "4.0.2009.108 ":
          RhinoSdkVersion = "200612060";
          RhinoSdkServiceRelease = "200901084";
          //RhinoDotNetVersion = "4.0.61206.9";
          break;

        case "4.0.2009.116 ":
          RhinoSdkVersion = "200612060";
          RhinoSdkServiceRelease = "200901144";
          //RhinoDotNetVersion = "4.0.61206.9";
          break;

        case "4.0.2009.121 ":
          RhinoSdkVersion = "200612060";
          RhinoSdkServiceRelease = "200901214";
          //RhinoDotNetVersion = "4.0.61206.9";
          break;

        case "4.0.2009.126 ":
          RhinoSdkVersion = "200612060";
          RhinoSdkServiceRelease = "200901214";
          //RhinoDotNetVersion = "4.0.61206.9";
          break;

        case "4.0.2009.226 ":
          RhinoSdkVersion = "200612060";
          RhinoSdkServiceRelease = "200902264";
          //RhinoDotNetVersion = "4.0.61206.10";
          break;

        case "4.0.2009.519 ":
          RhinoSdkVersion = "200612060";
          RhinoSdkServiceRelease = "200905084";
          //RhinoDotNetVersion = "4.0.61206.11";
          break;

        case "4.0.2009.528 ":
          RhinoSdkVersion = "200612060";
          RhinoSdkServiceRelease = "200905084";
          //RhinoDotNetVersion = "4.0.61206.11";
          break;

        case "4.0.2009.624 ":
          RhinoSdkVersion = "200612060";
          RhinoSdkServiceRelease = "200905084";
          //RhinoDotNetVersion = "4.0.61206.11";
          break;

        case "4.0.2009.709 ":
          RhinoSdkVersion = "200612060";
          RhinoSdkServiceRelease = "200905084";
          //RhinoDotNetVersion = "4.0.61206.11";
          break;

        case "4.0.2009.802 ":
          RhinoSdkVersion = "200612060";
          RhinoSdkServiceRelease = "200905084";
          //RhinoDotNetVersion = "4.0.61206.12";
          break;

        case "4.0.2009.813 ":
          RhinoSdkVersion = "200612060";
          RhinoSdkServiceRelease = "200905084";
          //RhinoDotNetVersion = "4.0.61206.12";
          break;

        case "4.0.2009.922 ":
          RhinoSdkVersion = "200612060";
          RhinoSdkServiceRelease = "200905084";
          //RhinoDotNetVersion = "4.0.61206.13";
          break;

        case "4.0.2009.1027":
          RhinoSdkVersion = "200612060";
          RhinoSdkServiceRelease = "200905084";
          //RhinoDotNetVersion = "4.0.61206.13";
          break;

        case "4.0.2009.1030":
          RhinoSdkVersion = "200612060";
          RhinoSdkServiceRelease = "200905084";
          //RhinoDotNetVersion = "4.0.61206.13";
          break;

        case "4.0.2009.1130":
          RhinoSdkVersion = "200612060";
          RhinoSdkServiceRelease = "200905084";
          //RhinoDotNetVersion = "4.0.61206.13";
          break;

        case "4.0.2009.1214":
          RhinoSdkVersion = "200612060";
          RhinoSdkServiceRelease = "200912014";
          //RhinoDotNetVersion = "4.0.61206.13";
          break;

        case "4.0.2010.401":
          RhinoSdkVersion = "200612060";
          RhinoSdkServiceRelease = "201003104";
          //RhinoDotNetVersion = "4.0.61206.13";
          break;
      }
      Logger.Log(LogLevel.Info, "InspectRhino\tRhinoSdkVersion: " + RhinoSdkVersion);
      Logger.Log(LogLevel.Info, "InspectRhino\tRhinoSdkServiceRelease: " + RhinoSdkServiceRelease);
    } 

    private void GetRhinoDotNetVersion()
    {
      // look in the same directory as the rhino executable for Rhino_DotNet.dll
      string path = System.IO.Path.GetDirectoryName(RhinoExePath);
      path = System.IO.Path.Combine(path, "Rhino_DotNet.dll");
      Logger.Log(LogLevel.Debug, "InspectRhino\tGetRhinoDotNetVersion");

      if (System.IO.File.Exists(path))
      {
        try
        {
          Logger.Log(LogLevel.Debug, "InspectRhino\nLoading Rhino_DotNet.dll");
          AssemblyName assy = AssemblyName.GetAssemblyName(path);
          RhinoDotNetVersion = assy.Version.ToString();
          Logger.Log(LogLevel.Debug, "InspectRhino\nRhino_DotNet.dll version: " + RhinoDotNetVersion);
        }
        catch (Exception ex)
        {
          Logger.Log(LogLevel.Error, ex);
        }
      }
      else
      {
        Logger.Log(LogLevel.Debug, "InspectRhino\nRhino_DotNet.dll not found.");
      }
    }

    private void GetRhinoCommonVersion()
    {
      // look in the same directory as the rhino executable for RhinoCommon.dll
      Logger.Log(LogLevel.Debug, "InspectRhino\tGetRhinoCommonVersion");
      string path = System.IO.Path.GetDirectoryName(RhinoExePath);
      path = System.IO.Path.Combine(path, "RhinoCommon.dll");
      if (System.IO.File.Exists(path))
      {
        try
        {
          Logger.Log(LogLevel.Debug, "InspectRhino\nLoading RhinoCommon.dll");
          AssemblyName assy = AssemblyName.GetAssemblyName(path);
          RhinoCommonVersion = assy.Version.ToString();
          Logger.Log(LogLevel.Debug, "InspectRhino\nRhinoCommon.dll version: " + RhinoCommonVersion);
        }
        catch (Exception ex)
        {
          Logger.Log(LogLevel.Error, ex);
        }
      }
      else
      {
        Logger.Log(LogLevel.Debug, "InspectRhino\nRhinoCommon.dll not found.");
      }
    }

    private string BuildDate
    {
      get
      {
        if (RhinoVersion == NullVersion)
          throw new RhinoInstallerException("Rhino Version Number is null.");

        string version = RhinoVersion.ToString();
        string[] version_parts = version.Split(".".ToCharArray());
        if (version_parts.Length != 4)
          throw new RhinoInstallerException("Rhino Version Number improperly formatted: " + RhinoVersion);

        StringBuilder builddate = new StringBuilder();
        builddate.Append(version_parts[2]).Append("-"); // year
        if (version_parts[3].Length == 3)
        {
          builddate.Append("0").Append(version_parts[3].Substring(0, 1)).Append("-"); // month
          builddate.Append(version_parts[3].Substring(1, 2)); // day
        }
        else if (version_parts[3].Length == 4)
        {
          builddate.Append(version_parts[3].Substring(0, 2)).Append("-"); // month
          builddate.Append(version_parts[3].Substring(2, 2)); // day
        }
        else
        {
          throw new RhinoInstallerException("Rhino Version Number improperly formatted: " + RhinoVersion);
        }

        return builddate.ToString();
      }
    }

    public RegistryKey OpenCurrentUserPluginKey()
    {
      return OpenPluginKey(Registry.CurrentUser);
    }

    public RegistryKey OpenAllUsersPluginKey()
    {
      return OpenPluginKey(Registry.LocalMachine);
    }

    private RegistryKey OpenPluginKey(RegistryKey RegRoot)
    {
      if (this.RhinoVersion.Major == 4 && this.RhinoVersion.Minor == 0)
      {
        if (InstallerEngine.Is64BitProcess())
        {
          // Installer running in 64-bit process
          if (this.OS == OSPlatform.x86)
          {
            if (RegRoot == Registry.LocalMachine)
              return RegRoot.CreateSubKey(string.Format(CultureInfo.InvariantCulture, @"Software\Wow6432Node\McNeel\Rhinoceros\4.0\{0}\Plug-ins", this.BuildDate));
            else
              return RegRoot.CreateSubKey(string.Format(CultureInfo.InvariantCulture, @"Software\McNeel\Rhinoceros\4.0\{0}\Plug-ins", this.BuildDate));
          }
        }
        else
        {
          // Installer running in 32-bit process
          if (this.OS == OSPlatform.x86)
            return RegRoot.CreateSubKey(string.Format(CultureInfo.InvariantCulture, @"Software\McNeel\Rhinoceros\4.0\{0}\Plug-ins", this.BuildDate));
        }
        throw new UnsupportedPlatformException(this.OS.ToString());
      }
      else if (this.RhinoVersion.Major == 5)
      {
        if (InstallerEngine.Is64BitProcess())
        {
          // Installer running in 64-bit process
          if (this.OS == OSPlatform.x86)
          {
            if (RegRoot == Registry.LocalMachine)
              return RegRoot.CreateSubKey(@"Software\Wow6432Node\McNeel\Rhinoceros\5.0\Plug-ins");
            else
              return RegRoot.CreateSubKey(@"Software\McNeel\Rhinoceros\5.0\Plug-ins");
          }
          else if (this.OS == OSPlatform.x64)
            return RegRoot.CreateSubKey(@"Software\McNeel\Rhinoceros\5.0x64\Plug-ins");
        }
        else
        {
          // Installer running in 32-bit process
          if (this.OS == OSPlatform.x86)
            return RegRoot.CreateSubKey(@"Software\McNeel\Rhinoceros\5.0\Plug-ins");
        }
        throw new UnsupportedPlatformException(this.OS.ToString());
      }
      else
      {
        throw new RhinoVersionNotSupportedException("OpenCurrentUserPluginKey doesn't support Rhino version " + RhinoVersion);
      }
    }

    public static InstallerPhase InspectRhinoAndWriteXml(string PathToRhino, string OutputDirectory)
    {
      Logger.Log(LogLevel.Debug, "InspectRhino\tBeginning inspection of '" + PathToRhino + "'");

      // This should only be called from a separate process.
      if (string.IsNullOrEmpty(OutputDirectory))
        return InstallerPhase.InspctWrkDirMissing;

      RhinoInfo info = new RhinoInfo();
      info.Inspect(PathToRhino);
      if (!info.IsValid())
        return InstallerPhase.InspctFailed;

      string xmlname = info.GetXmlFileName();
      Directory.CreateDirectory(OutputDirectory);
      string path = Path.Combine(OutputDirectory, xmlname);
      info.WriteXml(path);
      return InstallerPhase.Success;
    }



  }
}
