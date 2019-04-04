using SharedArea.Middles;

namespace SharedArea.Utils
{
    public static class AuthExtracter
    {
        public const string AK = "Authorization";
        public const string AppKeyHeader = "ApiKey";
        
        public static ReqAuth Extract(string header)
        {
            if (header == null) return null;
            var parts = header.Split(" ");
            if (parts.Length == 2)
            {
                return new ReqAuth()
                {
                    SessionId = long.Parse(parts[0]),
                    Token = parts[1]
                };
            }
            return null;
        }
    }
}