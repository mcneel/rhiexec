using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace RMA.RhiExec.Model
{
  public static class XmlHelper
  {
    public static void AppendElement(XmlNode parent, string elementName, string innerText)
    {
      XmlDocument doc = parent.OwnerDocument;
      if (doc == null)
        return;

      XmlElement e = doc.CreateElement(elementName);
      e.InnerText = innerText;
      parent.AppendChild(e);
    }

    public static string SelectSingleNodeInnerText(XmlNode parent, string tagXPath)
    {
      return SelectSingleNodeInnerText(parent, tagXPath, "(unknown file location)");
    }
    public static string SelectSingleNodeInnerText(XmlNode parent, string tagXPath, string xmlPath)
    {
      XmlNode node = null;
      node = parent.SelectSingleNode(tagXPath);
      if (node != null)
        return node.InnerText;
      else
        throw new ManifestException("\"" + tagXPath + "\" tag not found in: " + xmlPath);
    }

  }
}
