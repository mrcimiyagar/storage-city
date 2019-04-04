namespace SharedArea.Utils
{
    public class ConnStringGenerator
    {
        public static string GenerateSpecificConnectionString(string dbName, string username, string password)
        {
            return
                "Host=" +            "localhost" + ";" +
                "Database=" +        dbName + ";" +
                "Username=" +        username + ";" +
                "Password=" +        password + ";" +
                "Timeout=" +         "0" + ";" +
                "Command Timeout=" + "0";
        }
        
        public static string GenerateDefaultConnectionString(string dbName)
        {
            return
                "Host=" +            "localhost" + ";" +
                "Database=" +         dbName + ";" +
                "Username=" +        "postgres" + ";" +
                "Password=" +        "3g5h165tsK65j1s564L69ka5R168kk37sut5ls3Sk2t" + ";" +
                "Timeout=" +         "0" + ";" +
                "Command Timeout=" + "0";
        }
    }
}