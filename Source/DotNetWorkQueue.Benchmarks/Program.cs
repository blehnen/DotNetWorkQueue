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
            if (args.Length > 0 && args[0] == "--selftest") { SelfTest(); return; }
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }

        private static void SelfTest()
        {
            var dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dnwq-selftest-" + System.Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(dir);
            var path = System.IO.Path.Combine(dir, "t.db");
            var cs = $"Data Source={path};Version=3;Synchronous=NORMAL;Pooling=True;";

            using (var seed = new System.Data.SQLite.SQLiteConnection(cs))
            {
                seed.Open();
                using var cmd = seed.CreateCommand();
                cmd.CommandText = "PRAGMA journal_mode=WAL;CREATE TABLE t(id INTEGER PRIMARY KEY);";
                cmd.ExecuteNonQuery();
            }

            for (var i = 0; i < 50; i++) { using var c = new System.Data.SQLite.SQLiteConnection(cs); c.Open(); }
            var sw = System.Diagnostics.Stopwatch.StartNew();
            const int n = 2000;
            for (var i = 0; i < n; i++) { using var c = new System.Data.SQLite.SQLiteConnection(cs); c.Open(); }
            sw.Stop();
            System.Console.WriteLine($"selftest: pooled open+close = {sw.Elapsed.TotalMilliseconds * 1000 / n:F1} us/op  ({n} ops)");
            System.Console.WriteLine($"  System.Data.SQLite: {typeof(System.Data.SQLite.SQLiteConnection).Assembly.GetName().Version} " +
                                     $"at {typeof(System.Data.SQLite.SQLiteConnection).Assembly.Location}");
            System.Data.SQLite.SQLiteConnection.ClearAllPools();
            try { System.IO.Directory.Delete(dir, true); } catch { }
        }
    }
}
