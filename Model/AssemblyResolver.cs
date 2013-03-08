using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Win32;

namespace RMA.RhiExec.Model
{
  ///<summary>
  /// helper class for resolving assemblies when .NET can't find them
  ///</summary>
  static class AssemblyResolver
  {
    static List<Assembly> m_loaded_assemblies = new List<Assembly>();
    static List<string> m_plugin_paths = new List<string>();
    static List<string> m_rhino_paths = new List<string>();
    static ResolveEventHandler m_resolver;

    public static void AddSearchPath(string path, bool rhino)
    {
      // In order to inspect .NET plug-ins, we need to be able to resolve a local
      // reference for RhinoCommon and RhinoDotNet.
      if (m_rhino_paths.Count < 1)
      {
        if (Environment.Is64BitProcess)
        {
          var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\McNeel\Rhinoceros\5.0x64\Install");
          if (key != null)
          {
            var rhinoPath = key.GetValue("Path") as string;
            if (rhinoPath != null)
              m_rhino_paths.Add(rhinoPath);
          }
          key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\McNeel\Rhinoceros\5.0\Install");
          if (key != null)
          {
            var rhinoPath = key.GetValue("Path") as string;
            if (rhinoPath != null)
              m_rhino_paths.Add(rhinoPath);
          }
        }
        else
        {
          var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\McNeel\Rhinoceros\5.0\Install");
          if (key != null)
          {
            var rhinoPath = key.GetValue("Path") as string;
            if (rhinoPath != null)
              m_rhino_paths.Add(rhinoPath);
          }
        }
      }

      if (null == m_resolver)
      {
        m_resolver = new ResolveEventHandler(CurrentDomain_ReflectionOnlyAssemblyResolve);
        AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += m_resolver;
      }

      if (System.IO.File.Exists(path))
        path = System.IO.Path.GetDirectoryName(path);
      if (System.IO.Directory.Exists(path))
      {
        bool add = true;
        List<string> list = m_plugin_paths;
        if (rhino)
          list = m_rhino_paths;
        for (int i = 0; i < list.Count; i++)
        {
          if (list[i].Equals(path, StringComparison.OrdinalIgnoreCase))
          {
            add = false;
            break;
          }
        }
        if (add)
          list.Add(path);
      }
      else
      {
        // Either a file or directory should exist.
        // This is an error
        throw new System.IO.DirectoryNotFoundException(path);
      }
    }

    static Assembly CurrentDomain_ReflectionOnlyAssemblyResolve(object sender, ResolveEventArgs args)
    {
      int index = args.Name.IndexOf(',');
      if (index > -1)
      {
        string dllname = args.Name.Substring(0, index).Trim();
        for (int i = 0; i < m_loaded_assemblies.Count; i++)
        {
          Assembly a = m_loaded_assemblies[i];
          if (a.FullName.StartsWith(dllname, StringComparison.OrdinalIgnoreCase))
            return a;
        }


        dllname += ".dll";
        for (int i = 0; i < m_rhino_paths.Count; i++)
        {
          string path = System.IO.Path.Combine(m_rhino_paths[i], dllname);
          if (System.IO.File.Exists(path))
          {
            Assembly rc = System.Reflection.Assembly.ReflectionOnlyLoadFrom(path);
            if (null != rc)
            {
              m_loaded_assemblies.Add(rc);
              return rc;
            }
          }
        }

        for (int i = 0; i < m_plugin_paths.Count; i++)
        {
          Assembly rc = SearchHelper(m_plugin_paths[i], dllname);
          if (null != rc)
          {
            m_loaded_assemblies.Add(rc);
            return rc;
          }
        }
      }
      return null;
    }

    // helper function for recursively digging through subdirectories
    static Assembly SearchHelper(string path, string filename)
    {
      string fullname = System.IO.Path.Combine(path, filename);
      if (System.IO.File.Exists(fullname))
      {
        Assembly a = Assembly.ReflectionOnlyLoadFrom(fullname);
        return a;
      }

      string[] subdirectories = System.IO.Directory.GetDirectories(path);
      if (null == subdirectories)
        return null;

      for (int i = 0; i < subdirectories.Length; i++)
      {
        Assembly a = SearchHelper(subdirectories[i], filename);
        if (null != a)
          return a;
      }
      return null;
    }
  }
}
