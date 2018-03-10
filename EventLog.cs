using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Diagnostics;
using System.Reflection;

namespace BlockAmazon {
  internal static class EventLog {

    public static readonly string EVENT_LOG_SOURCE = Assembly.GetExecutingAssembly().GetName().Name;
    public const string EVENT_LOG_NAME = "Application";
    public const ushort UNKNOWN_EVENT_ID = 0;

    private const string TRUNCATION_WARNING = "... (Truncated last {0} characters from message)"; // {0} will be replaced with culture-formatted count
    public const int MAX_ENTRY_LENGTH = 32766 - 1000; // note actual max is 32766, but we reserve space for warning and some extra out of caution

    // Writes output to the event log
    public static void Log(string message, EventLogEntryType type = EventLogEntryType.Information, ushort eventID = UNKNOWN_EVENT_ID, short category = 0, byte[] rawData = null) {
      if (!System.Diagnostics.EventLog.SourceExists(EVENT_LOG_SOURCE)) System.Diagnostics.EventLog.CreateEventSource(EVENT_LOG_SOURCE, EVENT_LOG_NAME);
      System.Diagnostics.EventLog.WriteEntry(EVENT_LOG_SOURCE, Sanitize(message), type, (int)eventID, category, rawData);
    }

    private static string Sanitize(string message) {
      message = SanitizePercentSigns(message);
      message = message.Replace((char)0, '?'); // embedded null characters would prematurely truncate the message
      message = Truncate(message);
      return message;
    }

    // Percent signs can screw up event log.  See https://msdn.microsoft.com/en-us/library/e29k5ebc.aspx
    // This function finds any percent signs that are immediately proceeded by a digit, and replaces the percent sign in question with
    // the text <pct>.
    private static string SanitizePercentSigns(string message) {
      const char FIND_CHAR = '%';
      const string REPLACE_WITH = "<pct>";
      int startAt = 0;
      char nextChar;
      while (true) {
        if (message.Length <= startAt + 1) break; // must be at least one more character after search start position
        int i = message.IndexOf(FIND_CHAR, startAt);
        if (i < startAt) break; // no hit found
        if (message.Length <= i + 1) break; // must be at least one more character after hit
        nextChar = message[i + 1];
        if (nextChar >= '0' && nextChar <= '9') {
          // Replace the % sign with "<pct>"
          message = message.Substring(0, i) + REPLACE_WITH + message.Substring(i + 1, message.Length - (i + 1));
          startAt = i + REPLACE_WITH.Length + 1; // move cursor just past the character to the right of the inserted text
        } else {                                 // (can skip proceeding character because we know it's a digit, not a %)
          startAt = i + 1; // move cursor to character immediately after the %
        }
      }
      return message;
    }

    // Truncates log message if it exceeds Event Log Viewer maximum length of 32766 characters.
    // (Adapted from https://stackoverflow.com/a/25725394)
    private static string Truncate(string message) {
      if (message.Length > MAX_ENTRY_LENGTH) {
        string warning = string.Format(CultureInfo.CurrentCulture, TRUNCATION_WARNING, message.Length - MAX_ENTRY_LENGTH);
        message = message.Substring(0, MAX_ENTRY_LENGTH) + warning;
      }
      return message;
    }

    internal static bool UnitTests() {
      bool passed = true;
      if (!UnitTest_SanitizePercentSigns()) passed = false;
      return passed;
    }

    internal static bool UnitTest_Truncate() {
      bool passed = true;
      Random rng = new Random();
      int seed = rng.Next(1, int.MaxValue);
      rng = new Random(seed);
      StringBuilder sb = new StringBuilder(MAX_ENTRY_LENGTH);
      for (int i = 0; i < MAX_ENTRY_LENGTH; i++) {
        sb.Append(rng.Next(1, 255));
      }
      string vector = sb.ToString();
      if (Truncate(vector) != vector) {
        Debug.Print("Utils.Truncate() incorrectly truncated random test vector generated from seed {0}", seed);
        passed = false;
      }
      sb.Append("A");
      if (Truncate(vector) != vector.Substring(0, MAX_ENTRY_LENGTH) + string.Format(CultureInfo.CurrentCulture, TRUNCATION_WARNING, 1)) {
        Debug.Print("Utils.Truncate() failed to correctly truncate random test vector generated from seed {0} + 'A'", seed);
        passed = false;
      }
      return passed;
    }

    internal static bool UnitTest_SanitizePercentSigns() {
      bool passed = true;
      string[] vectors = {
        "%", "%",
        "%1", "<pct>1",
        "a%1", "a<pct>1",
        "%1b", "<pct>1b",
        "a%1b", "a<pct>1b",
        "a%", "a%",
        "%%", "%%",
        "%1%", "<pct>1%",
        "1%%", "1%%",
        "1%1", "1<pct>1",
        "1%1%", "1<pct>1%",
        "x%9y", "x<pct>9y",
        "a%b", "a%b",
        "100%", "100%",
        "%%0", "%<pct>0",
        "%%0%", "%<pct>0%",
        "a%8%5b", "a<pct>8<pct>5b"
      };
      for (int i = 0; i < vectors.Length; i+=2) {
        string result = SanitizePercentSigns(vectors[i]);
        if (result != vectors[i + 1]) {
          Debug.Print(String.Format("SanitizePercentSigns incorrectly returned {0} on {1}", result, vectors[i]));
          passed = false;
        }
      }
      return passed;
    }

  }
}
