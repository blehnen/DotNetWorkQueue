// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetWorkQueue.Dashboard.Ui.E2E.Tests.Fixtures
{
    /// <summary>
    /// Launches <c>DotNetWorkQueue.Dashboard.Ui.dll</c> as a child process bound
    /// to a random loopback port, so Playwright can drive a real browser against
    /// the production-faithful Kestrel host (not TestServer). Config overrides
    /// propagate via environment variables using ASP.NET Core's
    /// <c>Key:Subkey</c> → <c>Key__Subkey</c> convention.
    /// </summary>
    public sealed partial class DashboardSubprocess : IDisposable
    {
        [GeneratedRegex(@"Now listening on:\s*(http://127\.0\.0\.1:\d+)")]
        private static partial Regex ListeningOn();

        private readonly Process _process;
        public string RootUrl { get; }

        private DashboardSubprocess(Process process, string rootUrl)
        {
            _process = process;
            RootUrl = rootUrl;
        }

        public static async Task<DashboardSubprocess> StartAsync(
            IDictionary<string, string?> configOverrides,
            TimeSpan? startupTimeout = null)
        {
            var dllPath = LocateDashboardDll();
            var workDir = Path.GetDirectoryName(dllPath)!;

            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"\"{dllPath}\"",
                WorkingDirectory = workDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            psi.Environment["ASPNETCORE_URLS"] = "http://127.0.0.1:0";
            psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Testing";
            // ASP.NET Core reads env vars with "__" as the section separator.
            foreach (var kv in configOverrides)
            {
                psi.Environment[kv.Key.Replace(":", "__")] = kv.Value ?? "";
            }

            var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start dotnet.");

            var urlSource = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data == null) return;
                var match = ListeningOn().Match(e.Data);
                if (match.Success)
                    urlSource.TrySetResult(match.Groups[1].Value);
            };
            process.ErrorDataReceived += (_, _) => { /* swallow — stderr is noisy on startup */ };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var timeout = startupTimeout ?? TimeSpan.FromSeconds(30);
            using var cts = new CancellationTokenSource(timeout);
            cts.Token.Register(() => urlSource.TrySetException(
                new TimeoutException($"Dashboard subprocess did not report a listening URL within {timeout}.")));

            string rootUrl;
            try
            {
                rootUrl = await urlSource.Task;
            }
            catch
            {
                try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { /* best effort */ }
                process.Dispose();
                throw;
            }

            return new DashboardSubprocess(process, rootUrl);
        }

        public void Dispose()
        {
            try
            {
                if (!_process.HasExited)
                    _process.Kill(entireProcessTree: true);
            }
            catch { /* best effort */ }
            _process.Dispose();
        }

        private static string LocateDashboardDll()
        {
            // The test bin output has Dashboard.Ui.dll copied in by the project reference.
            var testBin = AppContext.BaseDirectory;
            var candidate = Path.Combine(testBin, "DotNetWorkQueue.Dashboard.Ui.dll");
            if (File.Exists(candidate)) return candidate;
            throw new FileNotFoundException(
                $"DotNetWorkQueue.Dashboard.Ui.dll not found adjacent to test bin at: {candidate}");
        }
    }
}
