using System;
using System.Collections.Generic;
using System.Text;

namespace RMA.RhiExec.Model
{
  public class RhinoInstallerException : System.Exception
  {
    public RhinoInstallerException() { }
    public RhinoInstallerException(string message) : base(message) { }
    public RhinoInstallerException(string message, Exception innerException) : base(message, innerException) { }
  }

  class PackageAuthoringException : RhinoInstallerException
  {
    public PackageAuthoringException() { }
    public PackageAuthoringException(string message) : base(message) { }
    public PackageAuthoringException(string message, Exception innerException) : base(message, innerException) { }
  }
  class PackageNotCompatibleException : PackageAuthoringException
  {
    public PackageNotCompatibleException() { }
    public PackageNotCompatibleException(string message) : base(message) { }
    public PackageNotCompatibleException(string message, Exception innerException) : base(message, innerException) { }
  }


  class GuidMismatchException : PackageAuthoringException
  {
    public GuidMismatchException() { }
    public GuidMismatchException(string message) : base(message) { }
    public GuidMismatchException(string message, Exception innerException) : base(message, innerException) { }
  }

  class ManifestFileNotFoundException : RhinoInstallerException 
  { 
    public ManifestFileNotFoundException() { }
    public ManifestFileNotFoundException(string message) : base(message) { }
    public ManifestFileNotFoundException(string message, Exception innerException) : base(message, innerException) { }
  }

  class InvalidOperatingSystemException : RhinoInstallerException
  {
    public InvalidOperatingSystemException() { }
    public InvalidOperatingSystemException(string message) : base(message) { }
    public InvalidOperatingSystemException(string message, Exception innerException) : base(message, innerException) { }
  }

  class UnsupportedPlatformException : RhinoInstallerException
  {
    public UnsupportedPlatformException() { }
    public UnsupportedPlatformException(string message) : base(message) { }
    public UnsupportedPlatformException(string message, Exception innerException) : base(message, innerException) { }
  }

  class RhinoVersionNotSupportedException : RhinoInstallerException
  {
    public RhinoVersionNotSupportedException() { }
    public RhinoVersionNotSupportedException(string message) : base(message) { }
    public RhinoVersionNotSupportedException(string message, Exception innerException) : base(message, innerException) { }
  }

  class InstallException : RhinoInstallerException
  {
    public InstallException() { }
    public InstallException(string message) : base(message) { }
    public InstallException(string message, Exception innerException) : base(message, innerException) { }
  }

  class InitException : RhinoInstallerException
  {
    public InitException() { }
    public InitException(string message) : base(message) { }
    public InitException(string message, Exception innerException) : base(message, innerException) { }
  }

  class PackageNotFoundException : InitException
  {
    public PackageNotFoundException() { }
    public PackageNotFoundException(string message) : base(message) { }
    public PackageNotFoundException(string message, Exception innerException) : base(message, innerException) { }
  }

  class ManifestException : InitException
  {
    public ManifestException() { }
    public ManifestException(string message) : base(message) { }
    public ManifestException(string message, Exception innerException) : base(message, innerException) { }
  }

  class ManfiestUnsupportedException : ManifestException
  {
    public ManfiestUnsupportedException() { }
    public ManfiestUnsupportedException(string message) : base(message) { }
    public ManfiestUnsupportedException(string message, Exception innerException) : base(message, innerException) { }
  }
}
