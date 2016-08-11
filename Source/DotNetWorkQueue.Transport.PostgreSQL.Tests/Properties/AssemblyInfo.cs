using System.Reflection;

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
    [assembly: AssemblyConfiguration("Release")]
#endif

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("DotNetWorkQueue.Transport.PostgreSQL.Tests")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyProduct("DotNetWorkQueue.Transport.PostgreSQL.Tests")]

