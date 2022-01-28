namespace DotNetWorkQueue.IntegrationTests.Shared
{
    internal static class TraceSettings
    {
        public static string TraceName(string testType)
        {
            return $"dotnetworkqueue-{testType}";
        }

        public static string Host => "192.168.0.2";
        public static int Port => 6831;
        public static bool Enabled => true;
    }
}
