# BlockAmazon

After encountering a spike in unknown traffic coming from Amazon to one of our servers, I wrote this program as a quick and dirty "sledgehammer" to automatically block all Amazon AWS IP's in Windows Firewall.

### Details

The authoritative list of all AWS IP ranges is published by Amazon at [https://ip-ranges.amazonaws.com/ip-ranges.json](https://ip-ranges.amazonaws.com/ip-ranges.json).  The list can be updated several times a week, and is described in more detail in this [blog post](https://aws.amazon.com/blogs/aws/aws-ip-ranges-json/).

The program downloads and parses the list, and creates (or updates) a block rule in Windows Firewall.

The program is a console application (distributed as a self-contained, single-file EXE) which can easily be configured as a Scheduled Task (e.g. to run nightly).  It has minimal requirements and runs on .NET 2.0 (which is included out of the box on every version of Windows since XP).

Anticipating that it might be invoked from a diverse set of tools, the program emits success/failure indicators in several forms:

- It emits a consistent "Completed Successfully" or "Failed" message as its final line of output (with the exception of a few rare cases such as when the /? switch is invoked to show the syntax guide).
- A detailed _%ERRORLEVEL%_ code is set when run from the command line. Best practices are followed by ensuring error code values increase with increasing severity.
- The full program output can be logged to the Application event log, with sensible icons (Information / Warning / Failed) and distinct (filterable) error codes corresponding to different categories of failure.

Despite all the options, the program is incredibly simple to start using.  Just run:

    BlockAmazon.exe

To see documentation, run `BlockAmazon /?`:

    BlockAmazon v1.0.2
    
    Usage: BlockAmazon.exe [/V] [/E] [/?]
      /V Show verbose output (including list of blocked IP's)
      /E Log an event in the Application event log of Windows containing the program output
      /? Show this help message (and don't emit a success/failure indicator)

If you set this up as a scheduled, the account it runs under requires rights to create (first time it's run) and modify (subsequent runs) rules in Windows Firewall.  For the non-security-paranoid, I find it easiest to simply configure the task as follows:

![alt text](https://i.imgur.com/DwzPmnH.png")

### Limitations

This is a quick-and-dirty prototype that has not yet been extensively tested in production.

Several of the initial settings for the Windows Firewall rule are hard-coded (e.g. TCP protocol, local port 443).  These are trivial to modify if you're able to compile the code from source.  If not, you can manually change the settings for the rule after its created (the program checks to see if a rule by the name "Block Amazon AWS IP ranges" already exists, and if so all it does is update the Remote IP address list on the Scope tab.