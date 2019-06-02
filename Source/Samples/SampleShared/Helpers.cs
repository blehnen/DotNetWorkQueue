using System.Collections.Specialized;
namespace SampleShared
{
    public static class Helpers
    {
        public static string ReadSetting(this NameValueCollection collection, string key)
        {
            return collection[key] ?? string.Empty;
        }
    }
}
