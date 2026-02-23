using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.IntegrationTests.Shared
{
    public static class OsDetectionHelper
    {
        public static bool IsRunningOnServer(ILogger logger)
        {
            if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                // Not on Windows, so definitely not a Windows Server edition
                return false;
            }

            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (key != null)
                    {
                        object installationType = key.GetValue("InstallationType");
                        if (installationType != null && installationType.ToString().Contains("Server"))
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., insufficient permissions to read registry)
                logger?.LogError($"Error reading registry: {ex.Message}");
            }

            return false;
        }
    }
}
