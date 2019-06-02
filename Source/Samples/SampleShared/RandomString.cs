using System;
namespace SampleShared
{
    public static class RandomString
    {
        private const string AllowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        public static string Create(int messagePayloadLength)
        {
            var stringChars = new char[messagePayloadLength];
            var random = new Random();

            for (var i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = AllowedChars[random.Next(AllowedChars.Length)];
            }

            return new string(stringChars);
        }
    }
}
