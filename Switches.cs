using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace BlockAmazon {
  internal class Switches {

    public bool ShowHelp { get; private set; } = false;
    public bool Verbose { get; private set; } = false;
    public bool WriteEventLog { get; private set; } = false;

    public static string HelpMessage { get {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(String.Format("Usage: {0}.exe [/V] [/E] [/?]", Assembly.GetExecutingAssembly().GetName().Name));
        sb.AppendLine("  /V Show verbose output (including list of blocked IP's)");
        sb.AppendLine("  /E Log an event in the Application event log of Windows containing the program output");
        sb.AppendLine("  /? Show this help message (and don't emit a success/failure indicator)");
        return sb.ToString();
    } }

    public static Switches Parse(string[] args) {
      Switches switches = new Switches();
      if (Array.IndexOf<string>(args, "/?") >= 0) throw new ShowHelpException(); // overrides everything else
      if (args.Length > 2) throw new SwitchParseException("Wrong number of parameters supplied");
      foreach (string arg in args) {
        ParseArg(arg, switches);
      }
      return switches;
    }

    public static void ParseArg(string arg, Switches switches) {
      switch (arg.ToUpperInvariant()) {
        case "/V": switches.Verbose = true; break;
        case "/E": switches.WriteEventLog = true; break;
        default: throw new SwitchParseException(String.Format("Invalid parameter: {0}", arg));
      }
    }

    private Switches() { }

  }
}
