using System;
using System.Collections.Generic;
using System.Text;

namespace RMA.RhiExec.Model
{
  class CommandLineParser
  {
    // Dictionary of upper-case keys.
    private Dictionary<string, string> m_name_value_pairs;

    public CommandLineParser(string[] args)
    {
      m_name_value_pairs = new Dictionary<string, string>();
      ProcessArguments(args);
    }

    private void ProcessArguments(string[] args)
    {
      string current_arg;
      for (int i=0; i<args.Length; i++)
      {
        current_arg = args[i];

        if (current_arg.Contains("="))
        {
          // This is a name-value pair
          //   color=white
          string[] pair = current_arg.Split("=".ToCharArray(), 2, StringSplitOptions.RemoveEmptyEntries);
          string name = pair[0].ToUpperInvariant().Trim("/".ToCharArray());
          this[name] = pair[1];

        }
        else if (current_arg.StartsWith("/", StringComparison.OrdinalIgnoreCase) && current_arg.Length > 2)
        {
          // This is also a name-value pair
          //   /color white
          // but not
          //   /x  <== these are just a "switch"; see below
          string name = current_arg.TrimStart("/".ToCharArray()).ToUpperInvariant();
          if (i + 1 < args.Length)
          {
            if (args[i + 1].StartsWith("/", StringComparison.OrdinalIgnoreCase))
            {
              // current arg is just a long-named switch
              this[name] = "";
            }
            else
            {
              // current arg is name/value pair
              this[name] = args[++i];
            }
          }
          else
          {
            // current arg is just a long-named switch
            this[name] = "";
          }
        }
        else if (current_arg.StartsWith("/", StringComparison.Ordinal) && current_arg.Length == 2)
        {
          string name = current_arg.TrimStart("/".ToCharArray());
          this[name] = "";
        }
        else
        {
          // This is just a single parameter
          this[current_arg] = "";
        }
      }
    }

    public bool GetBoolValue(string key)
    {
      return ContainsKey(key);
    }

    public int GetIntValue(string key)
    {
      return GetIntValue(key, 0);
    }

    public int GetIntValue(string key, int defaultValue)
    {
      if (!ContainsKey(key))
        return defaultValue;

      int result = defaultValue;
      if (!int.TryParse(GetStringValue(key), out result))
        result = defaultValue;

      return result;
    }

    public string GetStringValue(string key)
    {
      if (m_name_value_pairs.ContainsKey(key))
        return m_name_value_pairs[key];

      return null;
    }

    public string[] GetKeys()
    {
      List<string> keys = new List<string>();
      foreach (string key in m_name_value_pairs.Keys)
        keys.Add(key);
      return keys.ToArray();
    }

    public string this[string index]
    {
      get
      {
        if (m_name_value_pairs.ContainsKey(index.ToUpperInvariant()))
          return m_name_value_pairs[index.ToUpperInvariant()];
        else if (m_name_value_pairs.ContainsKey(index))
          return m_name_value_pairs[index];
        else
          return null;
      }
      set
      {
        if (!m_name_value_pairs.ContainsKey(index))
        {
          m_name_value_pairs.Add(index, value);
        }
        else
        {
          throw new CommandLineParserException("Skipping duplicate command line parameter: '" + index + "' with value '" + value + "'");
        }
      }
    }

    public bool ContainsKey(string key)
    {
      if (string.IsNullOrEmpty(key))
        return false;

      if (m_name_value_pairs.ContainsKey(key.ToUpperInvariant()))
        return true;

      return false;
    }
  }

  public class CommandLineParserException : Exception
  {
    public CommandLineParserException() {}
    public CommandLineParserException(string message) : base (message) {}
    public CommandLineParserException(string message, Exception innerException) : base (message, innerException) {}
  }
}
