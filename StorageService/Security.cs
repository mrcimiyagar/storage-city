using System;
using System.Text;

namespace StorageService
{
    public class Security
    {
        private const string KeySource = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        public static string MakeKey64() => MakeKey(64);

        public static string MakeKey8() => MakeKey(8);

        private static string MakeKey(int length)
        {
            var result = new StringBuilder();
            var rnd = new Random();
            for (var counter = 0; counter < length; counter++)
                result.Append(KeySource[rnd.Next(KeySource.Length - 1)]);
            return result.ToString();
        }
    }
}