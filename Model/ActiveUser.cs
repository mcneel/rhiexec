using System;

namespace RMA.RhiExec.Model
{
  // Commented out because we are not using this at the moment. Once we have some sort
  // of install button, then we will need to know if the current user can perform an install
  /// <summary>
  /// The following is some serious voodoo code to me. I need to get a better understanding of everthing that
  /// is going on here before shipping.
  /// </summary>
  public static class ActiveUser
  {
    public static bool CanInstall
    {
      get
      {
        return IsAdministrator || CanElevateToAdministrator;
      }
    }

    public static bool IsAdministrator
    {
      get
      {
        Initialize();
        System.Security.Principal.WindowsPrincipal p = (System.Security.Principal.WindowsPrincipal)System.Threading.Thread.CurrentPrincipal;
        return p.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
      }
    }

    public static bool CanElevateToAdministrator
    {
      get
      {
        Initialize();
        if (IsAdministrator)
          return false;

        // We can only elevate if we are running on Vista (or later) and the UAC is enabled
        if (Environment.OSVersion.Version.Major < 6)
        {
          // pre-vista: good for you
          return false;
        }

        bool bUacEnabled = false;
        Microsoft.Win32.RegistryKey keyUAC = null;
        try
        {
          string strKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System";
          keyUAC = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(strKey);
          if (keyUAC != null)
          {
            int v = (int) keyUAC.GetValue("EnableLUA");
            if (v == 1)
              bUacEnabled = true;
          }
        }
        finally
        {
          if (keyUAC != null)
          {
            keyUAC.Close();
            keyUAC = null;
          }
        }
        return bUacEnabled;
      }
    }

    private static void Initialize()
    {
      if (!m_bInitialized)
      {
        AppDomain currentDomain = AppDomain.CurrentDomain;
        currentDomain.SetPrincipalPolicy(System.Security.Principal.PrincipalPolicy.WindowsPrincipal);
      }
      m_bInitialized = true;
    }

    private static bool m_bInitialized; // = false by default
  }
}
