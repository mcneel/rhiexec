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
  class PackageManifest
  {
    public Version VersionNumber = new Version("0.0.0.0");
    public Version ManifestVersion = new Version("1.0.1");
    public PackageContentType ContentType = PackageContentType.Unknown;
    public string Path = "";
    public string Title = "";
    public Guid ID = Guid.Empty;
    public OSPlatform OS = OSPlatform.Unknown;
    public string UpdateUrl = "";
    public CultureInfo Locale = CultureInfo.InvariantCulture;
    public Collection<RhinoPlatform> SupportedPlatforms = new Collection<RhinoPlatform>();

    private PackageContentType m_required_content_type = PackageContentType.Unknown;

    public PackageManifest(PackageContentType contentType)
    {
      m_required_content_type = contentType;
    }

    public void WriteXml(string xmlPath)
    {
      if (!IsValid())
        return;

      XmlDocument doc = new XmlDocument();
      doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));

      XmlElement rhi = doc.CreateElement("RhinoInstaller");
      XmlAttribute attr = doc.CreateAttribute("Version");
      attr.Value = ManifestVersion.ToString();
      rhi.Attributes.Append(attr);
      doc.AppendChild(rhi);

      XmlElement package = doc.CreateElement("Package");
      rhi.AppendChild(package);

      XmlHelper.AppendElement(package, "Version", this.VersionNumber.ToString());
      XmlHelper.AppendElement(package, "ContentType", this.ContentType.ToString());
      XmlHelper.AppendElement(package, "PackagePath", this.Path);
      XmlHelper.AppendElement(package, "Title", this.Title);
      XmlHelper.AppendElement(package, "UpdateUrl", this.UpdateUrl);
      XmlHelper.AppendElement(package, "ID", this.ID.ToString());
      XmlHelper.AppendElement(package, "OS", this.OS.ToString());

      if (Locale != CultureInfo.InvariantCulture)
        XmlHelper.AppendElement(package, "Locale", Locale.TwoLetterISOLanguageName);

      foreach (RhinoPlatform p in SupportedPlatforms)
      {
        XmlHelper.AppendElement(package, "RhinoPlatform", p.ToString());
      }


      WriteToDocument(package);

      doc.Save(xmlPath);
    }

    public void ReadXml(string xmlPath)
    {
      if (!File.Exists(xmlPath))
        throw new ManifestFileNotFoundException();

      XmlDocument doc = new XmlDocument();
      try
      {
        doc.Load(xmlPath);
      }
      catch (System.Xml.XmlException)
      {
        // Code below added to try to diagnose http://dev.mcneel.com/bugtrack/?q=82608
        if (File.Exists(xmlPath))
        {
          string xmlContent = File.ReadAllText(xmlPath);
          Logger.Log(LogLevel.Debug, "Package XML Content:\r\n\r\n" + xmlContent + "\r\n\r\n");
        }
        throw;
      }

      XmlNode node = null;

      node = doc.SelectSingleNode("/RhinoInstaller");
      if (node != null)
      {
        if (node.Attributes != null)
        {
          XmlAttribute verAttr = node.Attributes["Version"];
          if (verAttr != null)
          {
            ManifestVersion = new Version(verAttr.Value);

            ReadXml101(doc);
          }
          else
          {
            throw new ManfiestUnsupportedException("Version attribute of RhinoInstaller tag missing in " + xmlPath);
          }
        }
        else
        {
          throw new ManfiestUnsupportedException("Version attribute of RhinoInstaller tag missing in " + xmlPath);
        }
      }
      else
      {
        throw new ManfiestUnsupportedException("RhinoInstaller tag missing in root of  " + xmlPath);
      }
    }

    private void ReadXml101(XmlDocument doc)
    {
      XmlNode package = doc.SelectSingleNode("/RhinoInstaller/Package");
      if (package == null)
        return;

      this.VersionNumber = new Version(XmlHelper.SelectSingleNodeInnerText(package, "Version"));
      this.Title = XmlHelper.SelectSingleNodeInnerText(package, "Title");

      // GUID
      string sGuid = XmlHelper.SelectSingleNodeInnerText(package, "ID");
      try
      {
        this.ID = new Guid(sGuid);
      }
      catch
      {
        throw new PackageAuthoringException(string.Format("GUID malformed: '{0}'", sGuid));
      }

      this.UpdateUrl = XmlHelper.SelectSingleNodeInnerText(package, "UpdateUrl");
      string os = XmlHelper.SelectSingleNodeInnerText(package, "OS");
      try
      {
        OS = (OSPlatform)Enum.Parse(OS.GetType(), os);
      }
      catch
      {
        throw new InvalidOperatingSystemException(os);
      }

      XmlNode contentType = package.SelectSingleNode("ContentType");
      if (contentType != null)
      {
        this.ContentType = PackageContentType.Unknown;
        PackageContentType t;
        if (Enum.TryParse(contentType.InnerText, out t))
        {
          this.ContentType = t;
        }
      }

      // Platform, if it exists
      XmlNodeList platforms = package.SelectNodes("RhinoPlatform");
      if (platforms != null)
      {
        foreach (XmlNode platform in platforms)
        {
          RhinoPlatform p = RhinoPlatform.Unknown;
          p = (RhinoPlatform) Enum.Parse(p.GetType(), platform.InnerText);
          if (p != RhinoPlatform.Unknown)
            SupportedPlatforms.Add(p);
        }
      }

      // Culture, if it exists
      XmlNode culture = package.SelectSingleNode("Locale");
      if (null != culture)
      {
        if ( 0 != System.String.CompareOrdinal(culture.InnerText.ToUpperInvariant(), "LOCALEINVARIANT")
          && 0 != System.String.CompareOrdinal(culture.InnerText.ToUpperInvariant(), "UNKNOWN") )
        {
          Locale = new CultureInfo(culture.InnerText);
        }
      }

      ReadFromDocument(package);
    }

    /// <summary>
    /// WriteToDocument is called on the derived class once each time the Xml file is saved.
    /// Base classes should write all information inside the element passed into WriteToDocument, 
    /// and make no assumptions about the rest of the XML document structure.
    /// 
    /// The same element will be passed to ReadFromDocument later.
    /// </summary>
    /// <param name="element"></param>
    public virtual void WriteToDocument(XmlNode element)
    {
      return;
    }

    /// <summary>
    /// ReadFromDocument is called on the derived class once each time the Xml file is read.
    /// Base classes should read only from the element passed to ReadFromDocument, and make no assumptions
    /// about the rest of the XML document structure.
    /// 
    /// The same element will be passed to WriteToDocument for writing.
    /// </summary>
    /// <param name="element"></param>
    public virtual void ReadFromDocument(XmlNode element)
    {
      return;
    }


    /// <summary>
    /// Called by the framework to determine of this instance of the manifest is valid.
    /// </summary>
    /// <returns></returns>
    public virtual bool IsValid()
    {
      for (; ; )
      {
        // Must have a title
        if (string.IsNullOrEmpty(Title))
          break;

        // Must have a GUID
        if (ID == Guid.Empty)
          break;

        // Must have valid content type
        if (m_required_content_type == PackageContentType.Unknown)
          break;
        if (m_required_content_type != ContentType)
          break;

        // Must have a valid platform
        if (OS == OSPlatform.Unknown)
          break;

        // Must have a valid version
        if (VersionNumber == null)
          break;

        int cf = string.Compare(VersionNumber.ToString(), "0.0.0.0", StringComparison.Ordinal);
        if (cf == 0)
          break;

        if (SupportedPlatforms.Count == 0)
          break;

        foreach (RhinoPlatform p in SupportedPlatforms)
        {
          if (p == RhinoPlatform.Unknown)
            break;
        }

        return true;
      }

      return false;
    }
  }
}
