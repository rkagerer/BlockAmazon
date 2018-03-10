using System;
using System.IO;
using System.Reflection;

namespace BlockAmazon {
  internal static class EmbeddedResources {

    // See also:
    // - http://blogs.msdn.com/b/microsoft_press/archive/2010/02/03/jeffrey-richter-excerpt-2-from-clr-via-c-third-edition.aspx
    // - http://research.microsoft.com/en-us/people/mbarnett/ILMerge.aspx
    // - https://github.com/Fody/Costura
    // - http://stackoverflow.com/questions/189549/embedding-dlls-in-a-compiled-executable

    // Call this method before any functions occur in your program that utilize typenames from embedded DLL's.
    // If you encounter problems, try marking such functions with the attribute:
    // [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    // to ensure they don't get inlined into their parent and prematurely trigger assembly resolution before this hook
    // is in place.  Note: Prior to .NET 4.0, references are resolved as soon as their containing function is invoked,
    // thus this line needs to be called OUTSIDE of (and before) any functions that declare variables of types exported
    // from embedded DLL's.  After .NET 4.0 resolution is deferred until the line of code referencing the type is hit.
    internal static void Initialize() {
      AppDomain.CurrentDomain.AssemblyResolve += (sender, args) => {
        String resourceName = Assembly.GetExecutingAssembly().EntryPoint.DeclaringType.Namespace + "." +
           new AssemblyName(args.Name).Name + ".dll";
        using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)) {
          Byte[] assemblyData = new Byte[stream.Length];
          stream.Read(assemblyData, 0, assemblyData.Length);
          return Assembly.Load(assemblyData);
        }
      };
    }

    public static string GetText(string name) {
      string[] names = Assembly.GetExecutingAssembly().GetManifestResourceNames();
      for (int i = 0; i < names.Length; i++) {
        if (names[i].Replace('\\', '.') == name) {
          using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(names[i])) {
            using (StreamReader reader = new StreamReader(stream)) {
              string result = reader.ReadToEnd();
            }

          }
        }
      }
      throw new ArgumentOutOfRangeException("Resource " + name + " not found.");
    }
  }
}
