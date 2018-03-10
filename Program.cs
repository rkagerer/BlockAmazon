// Default references added by Visual Studio
using System;
using System.Collections.Generic;
using System.Text;

// For this project
using NetFwTypeLib; // for Firewall API; located in %system32%\FirewallAPI.dll
using System.IO; // for FileNotFound exception class
using System.Net; // to download stuff from the web
using System.Runtime.InteropServices; // to marshal Exception.HResult
using System.Globalization; // for int.Parse NumberStyles
using System.Diagnostics; // for Debug output
using System.Reflection; // to retrieve product name & version

namespace BlockAmazon {

  #region Exit Codes and Custom Exceptions

  [Flags]
  enum ExitCodes : ushort { // bit field per purported best practices; ushort so it fits in EventLog ID
    // Informational codes
    Success = 0,
    // Warning codes
    WarningCodesStart = EncounteredRejects, // change this if you insert new warning codes below
    EncounteredRejects = 1, // 2^0
    // Failure codes
    FailureCodesStart = FetchFailedOrEmpty, // change this if you insert new failure codes below
    FetchFailedOrEmpty = 8192, // 2^13
    SwitchParseError = 16384, // 2^14
    UnknownError = 32768 // 2^15
  }

  class CustomException : Exception {

    public CustomException(string message = "", ExitCodes exitFlags = ExitCodes.UnknownError) : base(message) {
      this.ExitFlags = exitFlags;
    }

    public ExitCodes ExitFlags { get; protected set; } = ExitCodes.UnknownError;

    public virtual string FinalMessage(ExitCodes existingExitFlags) {
      StringBuilder sb = new StringBuilder();
      if (Message != "") {
          sb.AppendLine(Message);
      }
      sb.AppendLine();
      sb.Append((ushort)(ExitFlags | existingExitFlags) >= (ushort)ExitCodes.FailureCodesStart ? Program.FailMessage : Program.SuccessMessage);
      return sb.ToString();
    }

  }

  class ShowHelpException : CustomException {
    public ShowHelpException() : base(exitFlags: ExitCodes.Success) { }
    public override string FinalMessage(ExitCodes existingExitFlags) {
      // note this is the only case where we return a success code but don't emit SuccessMessage
      return Switches.HelpMessage;
    }
  }

  class SwitchParseException : CustomException {
    public SwitchParseException(string message) : base(message, exitFlags: ExitCodes.SwitchParseError) { }
    public override string FinalMessage(ExitCodes existingExitFlags) {
      StringBuilder sb = new StringBuilder();
      sb.AppendLine(Message);
      sb.AppendLine(Switches.HelpMessage);
      sb.Append((ushort)(ExitFlags | existingExitFlags) >= (ushort)ExitCodes.FailureCodesStart ? Program.FailMessage : Program.SuccessMessage);
      return sb.ToString();
    }
  }

  class ContentEmptyException : CustomException {
    public ContentEmptyException() : base("Fetched content was empty", exitFlags: ExitCodes.FetchFailedOrEmpty) { }
  }
  #endregion

  class Program {

    public const string SuccessMessage = "Completed successfully";
    public const string FailMessage = "Failed";

    const string Url = "https://ip-ranges.amazonaws.com/ip-ranges.json";
    const string IPv4Prefix = "\"ip_prefix\": \"";
    const string IPv6Prefix = "\"ipv6_prefix\": \"";
    const string IpSuffix = "\",";

    const string FwProgId = "HNetCfg.FwPolicy2";
    const string FwRuleProgId = "HNetCfg.FWRule";
    const int NotFoundHResult = -2147024894; // 0xF80070002 in Hex signed 2's complement
    const string RuleName = "Block Amazon AWS IP ranges";
    const string RuleDesc = "Block Amazon AWS IP ranges."
      + " List of IP's is refreshed nightly from " + Url + ", which Amazon publishes several times a week."
      + " For more information see https://aws.amazon.com/blogs/aws/aws-ip-ranges-json/";

    static ExitCodes ExitCode = ExitCodes.Success;
    static Switches Switches;

    static int Main(string[] args) {
      try {

        ShowVersion();
        Switches = Switches.Parse(args);

        // This line allows DLL's we depend on to be embedded directly into the EXE, and must be called before we use any classes from the
        // Windows Firewall API.  If you need to rebuild the embedded wrapper:
        // 1) Remove the Interop.NetFwTypeLib reference and delete the Interop.NetFwTypeLib.dll embedded resource.
        // 2) Add a reference to %System32%\FirewallAPI.dll, and ensure it's Copy Local property is True (which is the default).
        // 3) Build the project.  This generates the Interop.NetFwTypeLib.dll wrapper in the output directory, beside the EXE.
        // 4) Move Interop.NetFwTypeLib.dll to the project directory (where the .cs files are).
        // 5) Remove the FirewallAPI.dll reference, and instead add a reference to Interop.NetFwTypeLib.dll.
        // 6) In the Solution Explorer window, click the reference and set Copy Local to False.
        // 7) Add the Interop.NetFwTypeLib.dll file to the project and set its type to Embedded Resource.
        EmbeddedResources.Initialize();

        Run();

        // This point reached means success
        Logger.WriteLine();
        Logger.WriteLine(SuccessMessage);
        ExitCode = ExitCode | ExitCodes.Success;

      } catch (Exception ex) {
        // Convert or wrap
        CustomException cex;
        if (ex is CustomException) cex = (CustomException)ex;
        else cex = new CustomException(ex.Message);
        // Handle
        string msg = cex.FinalMessage(ExitCode);
        if (msg != "") {
          Logger.WriteLine();
          Logger.WriteLine(msg);
        }
        ExitCode = ExitCode | cex.ExitFlags;
      } finally {
        Environment.ExitCode = (int)ExitCode; // required in C# 6+, as declaring main as int no longer passes return code back to environment
        if (Switches != null && Switches.WriteEventLog) EventLog.Log(Logger.Pop(), ExitCodeToEventLogEntryType(ExitCode), (ushort)ExitCode);
      }

      return Environment.ExitCode; // for good measure
    }

    private static void ShowVersion() {
      Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
      System.Version version = assembly.GetName().Version;
      Logger.WriteLine(String.Format("{0} v{1}.{2}.{3}",
        assembly.GetName().Name, version.Major, version.Minor, version.Build));
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    static void Run() {
      Logger.WriteLine(String.Format("\r\nFetching {0}", Url));
      string html = "";
      using (WebClient wc = new WebClient()) {
        html = wc.DownloadString(Url);
      }
      if (html.Trim() == "") throw new ContentEmptyException();
      List<string> rejectedAddresses = null;
      List<string> addressList = ParseAddresses(html, out rejectedAddresses);
      Logger.Write(String.Format("\r\nParsed {0} addresses or ranges", addressList.Count)); // message continued below
      if (Switches.Verbose && addressList.Count > 0) {
        Logger.WriteLine(":");
        foreach (string address in addressList) {
          Logger.WriteLine(address);
        }
      } else {
        Logger.WriteLine(); // finish prior .Write()
      }
      if (rejectedAddresses.Count > 0) {
        ExitCode = ExitCode | ExitCodes.EncounteredRejects;
        Logger.WriteLine(String.Format("\r\nWARNING: Rejected {0} addresses or ranges:", rejectedAddresses.Count));
        int listed = 0;
        foreach (string address in rejectedAddresses) {
          Logger.WriteLine(address);
          listed++;
          if (!Switches.Verbose && listed >= 15 && listed < rejectedAddresses.Count) {
            Logger.WriteLine("Listing truncated. Use \"-v\" switch to show all.");
            break;
          }
        }
      }
      
      INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID(FwProgId));
      INetFwRule rule = GetRule(firewallPolicy, RuleName);
      if (rule == null) {
        Logger.WriteLine(String.Format("\r\nCreating new firewall rule \"{0}\"", RuleName));
        rule = (INetFwRule)Activator.CreateInstance(Type.GetTypeFromProgID(FwRuleProgId));
        ConfigureNewRule(rule);
        firewallPolicy.Rules.Add(rule);
      } else {
        Logger.WriteLine(String.Format("\nUpdating remote addresses in firewall rule \"{0}\"", RuleName));
      }
      string addresses = string.Join(",", addressList.ToArray());
      rule.RemoteAddresses = addresses;
    }

    static List<string> ParseAddresses(string html, out List<string> rejectedAddresses) {
      List<string> addresses = new List<string>();
      rejectedAddresses = new List<string>(0);
      SimpleParser p = new SimpleParser(html);
      ParseAddressesHelper(addresses, p, false, rejectedAddresses);
      p.Cursor = 0; // not neccessary if IPV6 ranges are always after the IPV4 ones, but here just in case they become mixed in future
      ParseAddressesHelper(addresses, p, true, rejectedAddresses);
      return addresses;
    }

    static void ParseAddressesHelper(List<string> addresses, SimpleParser p, bool ipv6, List<string> rejectedAddresses) {
      string address;
      while (true) {
        address = p.TrySeekAndExtract("", ipv6 ? IPv6Prefix : IPv4Prefix, IpSuffix).Trim();
        if (address == "") return;
        if (IsValidIpOrSubnet(address, ipv6)) addresses.Add(address); else rejectedAddresses.Add(address);
      }
    }

    // This function may be somewhat naive.
    // Consider migrating to a more robust validation algorithm, maybe use https://github.com/lduchosal/ipnetwork
    static bool IsValidIpOrSubnet(string address, bool ipv6 = false) {
      char delimiter = ipv6 ? ':' : '.';
      if (!address.Contains(delimiter.ToString())) return false;
      // Split into octets (or fields, for IPv6)
      string[] octets = address.Split(delimiter);
      // Validate number of octets / fields
      if (ipv6) {
        if (octets.Length < 3 || octets.Length > 8) return false;
        // Only one "::" occurrence is allowed; shows up as a blank split piece (excluding first and last fields)
        bool encountered_blank = false;
        for (int i = 1; i < octets.Length - 1; i++) {
          if (octets[i].Trim() == "") {
            if (encountered_blank) return false; // this is the second blank
            encountered_blank = true;
          }
        }
        if (octets.Length < 8 && !encountered_blank) return false; // must use :: if shortened notation provided
      } else {
        if (octets.Length != 4) return false;
      }
      // Check if this looks like a range in CIDR notation
      string cidr = "";
      int lastIndex = octets.Length - 1;
      if (octets[lastIndex].Contains("/")) {
        string[] pieces = octets[lastIndex].Split('/');
        if (pieces.Length != 2) return false; // bad notation
        octets[lastIndex] = pieces[0]; // portion left of slash is last octet
        cidr = pieces[1]; // portion right of slash is CIDR suffix (indicates how many leading bits make up the network prefix)
      }
      // Quick sanity check on length of octets / fields
      for (int i = 0; i < octets.Length; i++) {
        if (octets[i].Length > (ipv6 ? 4 : 3)) return false; // up to four hex digits (for ipv6) or three decimal integers (for ipv4) allowed per field
      }
      // Validate octets
      int[] octet_values = new int[octets.Length];
      for (int i = 0; i < octet_values.Length; i++) {
        int value;
        if (octets[i].Trim() == "") {
          octet_values[i] = 0;
        } else {
          if (!int.TryParse(octets[i], ipv6 ? NumberStyles.AllowHexSpecifier : NumberStyles.None, CultureInfo.InvariantCulture, out value)) return false;
          if (value < 0 || value > (ipv6 ? 0xFFFF : 255)) return false;
          octet_values[i] = value;
        }
      }
      // Validate CIDR suffix if present
      int cidr_value;
      if (cidr.Trim() != "") {
        if (!int.TryParse(cidr, NumberStyles.None, CultureInfo.InvariantCulture, out cidr_value)) return false;
        if (cidr_value < 0 || cidr_value > (ipv6 ? 128 : 32)) return false;
      }
      // This point reached means validation passed
      return true;
    }

    static INetFwRule GetRule(INetFwPolicy2 fwpol, string name) {
      INetFwRule rule = null;
      try {
        rule = fwpol.Rules.Item(name);
      } catch (FileNotFoundException ex) {
        int HResult = Marshal.GetHRForException(ex); // required for .NET < 4.5
        // Testing showed the function returns -2147024894 in HRESULT if rule doesn't exist, so that code can be silently ignored. Throw others.
        if (HResult != NotFoundHResult) throw ex;
      }
      return rule;
    }

    static void ConfigureNewRule(INetFwRule rule) {
      rule.Name = RuleName;
      rule.Description = RuleDesc;
      rule.Action = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
      rule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN;
      rule.InterfaceTypes = "All";
      rule.Profiles = (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_ALL; // note this is a bitfield, can combine network profiles you want to block
      rule.Protocol = (int)NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_TCP;
      rule.LocalPorts = "443";
      //rule.RemotePorts = "All"; // probably is already the appropriate value for "All" by default
      rule.EdgeTraversal = false;
      //rule.Grouping = "@firewallapi.dll,-23255"; // not sure what this is; just a sample retrieved from code online
      rule.Enabled = true;
    }

    static EventLogEntryType ExitCodeToEventLogEntryType(ExitCodes ExitCode) {
      // Tailor behavior of this one to my taste.  Windows event log should only have red exclamation marks for major / program errors.
      if ((ExitCode & ExitCodes.FetchFailedOrEmpty) == ExitCodes.FetchFailedOrEmpty && ExitCode <= ExitCodes.FetchFailedOrEmpty) return EventLogEntryType.Warning;

      if (ExitCode >= ExitCodes.FailureCodesStart) return EventLogEntryType.Error;
      if (ExitCode >= ExitCodes.WarningCodesStart) return EventLogEntryType.Warning;
      return EventLogEntryType.Information;
    }

    static bool UnitTests() {
      bool passed = true;
      if (!UnitTest_IsValidIpOrSubnet()) passed = false;
      if (!EventLog.UnitTests()) passed = false;
      Debug.Print(passed ? "All tests passed" : "Failed one or more tests");
      return passed;
    }

    static bool UnitTest_IsValidIpOrSubnet() {
      bool passed = true;
      string[] valid_ipv4 = {
          "172.22.0.21",
          "0.0.0.1",
          "255.255.255.255",
          "2.3.1.5/12",
          "2.3.1.0/32",
        };
      string[] invalid_ipv4 = {
        "256.0.0.1",
        "-5.2.5.1",
        "a.2.5.1",
        "1.5",
        "1.5.3",
        "2.3.1.5/33",
        "2.3.1.5/-1",
        "0x3.1.2.5",
        "&h3.1.2.5",
        "2.3.1.3/0x5",
        "2.3.1.3/&h5",
        "2.3.1.3/1.2345E-02",
      };
      string[] valid_ipv6 = {
        "0:0:0:2:3:3:2:8",
        "240f:80a0:8000::/40",
        "2001:0000:0dea:C1AB:0000:00D0:ABCD:004E",
        "2001:0:eab:DEAD:0:A0:ABCD:4E", // leading 0's can be omitted
        "2001:0:0eab:dEAd:0:a0:abcd:4e", // not case sensitive
        "2001:0:0eab:dead::a0:abcd:4e", // single :: instance
        "::/0",
        "::/1",
        "2000::/3",
        "2001:db8::/29",
        "2001:db8::/121",
        "2001:db8:0000:0000:0000:0000:0000:007f",
        "2001:eab::1/128",
        "2001:eab::/64",
      };
      string[] invalid_ipv6 = {
        "0:0:0:2",
        "2001::eab:dead::a0:abcd:4e", // multiple :: instances
        "-2001::eab:dead::a0:abcd:4e",
        "0x3::eab:dead::a0:abcd:4e",
        ":/0",
        "3:/0",
      };
      if (!UnitTestHelper_IsValidIpOrSubnet(valid_ipv4, false, true)) passed = false;
      if (!UnitTestHelper_IsValidIpOrSubnet(invalid_ipv4, false, false)) passed = false;
      if (!UnitTestHelper_IsValidIpOrSubnet(valid_ipv6, true, true)) passed = false;
      if (!UnitTestHelper_IsValidIpOrSubnet(invalid_ipv6, true, false)) passed = false;
      return passed;
    }

    static bool UnitTestHelper_IsValidIpOrSubnet(string[] values, bool ipv6, bool valid) {
      bool passed = true;
      foreach (string address in values) {
        bool result = IsValidIpOrSubnet(address, ipv6);
        if (result != valid) {
          Debug.Print(String.Format("IsValidIpOrSubnet incorrectly returned {0} on {1}", result, address));
          passed = false;
        }
      }
      return passed;
    }

  }
}