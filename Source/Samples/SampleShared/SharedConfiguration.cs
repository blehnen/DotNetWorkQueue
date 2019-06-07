using System.Configuration;
using System.Text;

namespace SampleShared
{
    public static class SharedConfiguration
    {
        #region Constructor
        static SharedConfiguration()
        {
            System.Collections.Specialized.NameValueCollection oSettings = ConfigurationManager.AppSettings;
            if (oSettings["EnableTrace"] != null)
            {
                EnableTrace = bool.Parse(oSettings["EnableTrace"]);
            }
            if (oSettings["EnableMetrics"] != null)
            {
                EnableMetrics = bool.Parse(oSettings["EnableMetrics"]);
            }
            if (oSettings["EnableCompression"] != null)
            {
                EnableCompression = bool.Parse(oSettings["EnableCompression"]);
            }
            if (oSettings["EnableEncryption"] != null)
            {
                EnableEncryption = bool.Parse(oSettings["EnableEncryption"]);
            }
            if (oSettings["EnableChaos"] != null)
            {
                EnableChaos = bool.Parse(oSettings["EnableChaos"]);
            }
        }
        #endregion

        #region Public Props
        public static bool EnableTrace { get; }
        public static bool EnableMetrics { get; }
        public static bool EnableCompression { get; }
        public static bool EnableEncryption { get; }
        public static bool EnableChaos { get; }
        #endregion

        public static string AllSettings
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("Tracing:");
                sb.Append(EnableTrace.ToString());
                sb.Append(" Chaos:");
                sb.Append(EnableChaos.ToString());
                sb.Append(" Metrics:");
                sb.Append(EnableMetrics.ToString());
                sb.Append(" Compression:");
                sb.Append(EnableCompression.ToString());
                sb.Append(" Encryption:");
                sb.Append(EnableEncryption.ToString());
                return sb.ToString();
            }
        }
    }
}
