using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace RMA.RhiExec.Model
{
  static class UnsafeNativeMethods
  {
    public delegate int GetIntegerInvoke();
    public delegate Guid GetGuidInvoke();

    public delegate IntPtr GetStringInvoke();

    //public const uint LOAD_LIBRARY_AS_DATAFILE = 0x00000002;
    public const uint DONT_RESOLVE_DLL_REFERENCES = 0x00000001;
    //public const uint LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008;
    //public const uint LOAD_IGNORE_CODE_AUTHZ_LEVEL = 0x00000010;
    //public const uint LOAD_LIBRARY_AS_DATAFILE_EXCLUSIVE = 0x00000040;

    [DllImport("kernel32", CharSet = CharSet.Unicode)]
    public static extern IntPtr LoadLibraryEx(string filename, IntPtr hFile, uint flags);

    [DllImport("kernel32" /*, CharSet = CharSet.Unicode */)]
    public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    [DllImport("kernel32" /*, CharSet = CharSet.Unicode */)]
    public static extern uint SetErrorMode(uint uMode);

    public const uint SEM_FAILCRITICALERRORS = 0x01;
  }
}
