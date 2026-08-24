using BenchmarkDotNet.Running;

namespace DotNetWorkQueue.Benchmarks
{
    /// <summary>
    /// Entry point. Run with <c>dotnet run -c Release --project Source/DotNetWorkQueue.Benchmarks</c>
    /// and pick a suite, or pass <c>--filter *</c> to run everything.
    /// </summary>
    /// <remarks>
    /// Benchmarks must be run against a Release build; BenchmarkDotNet will refuse otherwise, and
    /// a Debug measurement of this library is meaningless.
    /// </remarks>
    public static class Program
    {
        public static void Main(string[] args)
        {
            //--selftest runs one measurement directly, outside BenchmarkDotNet, so a suspicious
            //benchmark number can be checked against a plain loop in the same process and with the
            //same dependency graph.
            WarnIfAssembliesAreOnASlowMount();
            if (args.Length > 0 && args[0] == "--selftest") { SelfTest(); return; }
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }

        /// <summary>
        /// Refuses to let a run pass silently when the assemblies live on a Windows drive mounted
        /// into WSL. .NET memory-maps assemblies, and page faults against drvfs/9p are slow enough
        /// to impose a uniform floor on everything in the process: measured here, the identical
        /// binaries reported 19,798 us per pooled connection open from /mnt/f and 569 us from
        /// /tmp - a 35x error that looks entirely plausible if you are not checking for it.
        /// </summary>
        private static void WarnIfAssembliesAreOnASlowMount()
        {
            if (!System.OperatingSystem.IsLinux()) return;

            var location = typeof(Program).Assembly.Location;
            if (!location.StartsWith("/mnt/", System.StringComparison.Ordinal)) return;

            var previous = System.Console.ForegroundColor;
            System.Console.ForegroundColor = System.ConsoleColor.Red;
            System.Console.WriteLine();
            System.Console.WriteLine("=========================== INVALID MEASUREMENT ENVIRONMENT ===========================");
            System.Console.WriteLine($" Assemblies are loading from {location}");
            System.Console.WriteLine();
            System.Console.WriteLine(" That is a Windows drive mounted into WSL. .NET memory-maps assemblies, and page faults");
            System.Console.WriteLine(" against that filesystem impose a uniform floor on every operation in the process -");
            System.Console.WriteLine(" measured at roughly 35x for SQLite connection work. Any numbers from this run are");
            System.Console.WriteLine(" wrong, and wrong in a way that still looks plausible.");
            System.Console.WriteLine();
            System.Console.WriteLine(" Copy the build output to a native path and run it from there:");
            System.Console.WriteLine("   cp -r bin/Release/net10.0/. /tmp/bench/ && cd /tmp/bench \\");
            System.Console.WriteLine("     && dotnet ./DotNetWorkQueue.Benchmarks.dll --job short --filter '*' --inProcess");
            System.Console.WriteLine("=======================================================================================");
            System.Console.WriteLine();
            System.Console.ForegroundColor = previous;
        }

        private static void SelfTest()
        {
            //Verbatim copy of the standalone loop that reports ~0.6 us/op elsewhere, so any
            //remaining difference is the environment rather than the code.
            var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "pooltest-" + System.Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(dir);

            double Time(string label, string path, bool pooling, bool holdOne, int n)
            {
                var cs = $"Data Source={path};Version=3;Synchronous=NORMAL;" + (pooling ? "Pooling=True;" : "");
                using (var seed = new System.Data.SQLite.SQLiteConnection(cs))
                {
                    seed.Open();
                    using var cmd = seed.CreateCommand();
                    cmd.CommandText = "PRAGMA journal_mode=WAL;CREATE TABLE IF NOT EXISTS t(id INTEGER PRIMARY KEY, b TEXT);";
                    cmd.ExecuteNonQuery();
                }
                System.Data.SQLite.SQLiteConnection held = null;
                if (holdOne) { held = new System.Data.SQLite.SQLiteConnection(cs); held.Open(); }

                for (var i = 0; i < 20; i++) { using var c = new System.Data.SQLite.SQLiteConnection(cs); c.Open(); }
                var sw = System.Diagnostics.Stopwatch.StartNew();
                for (var i = 0; i < n; i++) { using var c = new System.Data.SQLite.SQLiteConnection(cs); c.Open(); }
                sw.Stop();
                held?.Dispose();
                System.Data.SQLite.SQLiteConnection.ClearAllPools();
                var us = sw.Elapsed.TotalMilliseconds * 1000 / n;
                System.Console.WriteLine($"  {label,-52} {us,10:F1} us/open-close");
                return us;
            }

            System.Console.WriteLine("open+close a connection, no work done on it:\n");
            Time("pooling ON,  no other connection held", System.IO.Path.Combine(dir, "a.db"), true, false, 2000);
            Time("pooling ON,  one connection held open", System.IO.Path.Combine(dir, "b.db"), true, true, 2000);
            Time("pooling OFF, no other connection held", System.IO.Path.Combine(dir, "c.db"), false, false, 200);
            Time("pooling OFF, one connection held open", System.IO.Path.Combine(dir, "d.db"), false, true, 200);
            try { System.IO.Directory.Delete(dir, true); } catch { }
        }
    }
}
