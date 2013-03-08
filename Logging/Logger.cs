using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Globalization;

namespace RMA.RhiExec
{
  public static class Logger
  {
    private static string m_filename;
    private static LogLevel m_log_level = LogLevel.Info;

    public static void Initialize(string fileName, LogLevel level)
    {
      m_filename = fileName;
      m_log_level = level;
    }

    public static string LogFile
    {
      get
      {
        return GetLogFile();
      }
      set
      {
        SetLogFile(value);
      }
    }

    private static void SetLogFile(string filename)
    {
      if (m_filename != null)
      {
        if (File.Exists(m_filename))
        {
          File.Move(m_filename, filename);
        }
      }
      m_filename = filename;
    }

    private static string GetLogFile()
    {
      return m_filename;
    }

    public static void SetLogLevel(LogLevel level)
    {
      m_log_level = level;
    }

    public static void WriteLine(string line)
    {
      if (string.IsNullOrEmpty(m_filename))
        return;
      System.Diagnostics.Trace.WriteLine(line);

      string dir = Path.GetDirectoryName(m_filename);

      if (!Directory.Exists(dir))
        Directory.CreateDirectory(dir);
      for (int tries = 0; tries < 3; tries++)
      {
        try
        {
          TextWriter w = new StreamWriter(m_filename, true);
          w.WriteLine(line);
          w.Flush();
          w.Close();
          w.Dispose();
        }
        catch (System.IO.IOException)
        {
          System.Threading.Thread.Sleep(200);
          continue;
        }
        break;
      }
    }

    public static void Log(LogLevel level, string message)
    {
      if (level > m_log_level)
        return;

      // if message is multi-line, indent each line with tabs to make it easier to read.
      string msg = message.Replace("\r\n", "\n");
      msg = msg.Replace("\r", "\n");
      msg = msg.Replace("\n", "\r\n\t\t\t\t");
      int pid = System.Diagnostics.Process.GetCurrentProcess().Id;
      string line = string.Format(CultureInfo.InvariantCulture, "{0}\t{1}\t{2}\t{3}", DateTime.Now.ToString(CultureInfo.InvariantCulture), pid, level.ToString(), msg);
      WriteLine(line);
    }

    public static void Log(LogLevel level, Exception ex)
    {
      if (level >= m_log_level)
        return;

      StringBuilder sb = new StringBuilder();
      sb.Append("Exception: " + ex.GetType()).Append("\n");
      sb.Append("Message: " + ex.Message).Append("\n");
      sb.Append("Source: " + ex.Source).Append("\n");
      sb.Append("StackTrace: " + ex.StackTrace).Append("\n");

      Exception inner = ex.InnerException;
      while (inner != null)
      {
        sb.Append("\nException: " + ex.GetType()).Append("\n");
        sb.Append("Inner Exception: " + ex.Message).Append("\n");
        sb.Append("Source: " + ex.Source).Append("\n");
        sb.Append("StackTrace: " + ex.StackTrace).Append("\n");

        inner = inner.InnerException;
      }

      Log(level, sb.ToString());
    }

    public static void PurgeOldLogs()
    {
      string[] files = Directory.GetFiles(Path.GetDirectoryName(m_filename));
      foreach (string file in files)
      {
        DateTime last_modified = File.GetLastWriteTime(file);
        TimeSpan modified_ago = DateTime.Now - last_modified;
        if (modified_ago.TotalDays > 30.0)
          File.Delete(file);
      }
    }
  }

  public enum LogLevel
  {
    None,
    Error,
    Warning,
    Info,
    Debug,
  }
}
