using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace BlockAmazon {
  internal static class Logger {

    public enum Severities : int {
      Information = 0,
      Warning = 1,
      Error = 2
    }

    static StringBuilder sb = new StringBuilder();

    //private static Severities severity = Severities.Information;
    //public static Severities Severity {
    //  get { return severity; }
    //  set { if (value > severity) severity = value; } // do not allow downgrading of warning level
    //}

    //public static EventLogEntryType SeverityAsEventLogEntryType { get {
    //  switch (severity) {
    //      case Severities.Information: return EventLogEntryType.Information;
    //      case Severities.Warning: return EventLogEntryType.Warning;
    //      default: return EventLogEntryType.Error;
    //    }
    //} }

    public static void Write() {
      Write("");
    }

    public static void Write(string message) {
      Console.Write(message);
      sb.Append(message);
    }

    public static void WriteLine() {
      WriteLine("");
    }

    public static void WriteLine(string message) {
      Console.WriteLine(message);
      sb.AppendLine(message);
    }

    // Call to retrieve buffered text and reset the buffer
    public static string Pop() {
      string text = sb.ToString();
      Clear();
      return text;
    }

    public static void Clear() {
      //severity = Severities.Information;
      sb = new StringBuilder();
    }

  }
}
