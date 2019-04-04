using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace ApiGateway.DbContexts
{
    public static class StreamRepo
    {
        public static Dictionary<string, Stream> FileStreams { get; set; } = new Dictionary<string, Stream>();
        public static Dictionary<string, object> FileStreamLocks { get; set; } = new Dictionary<string, object>();
        public static Dictionary<string, object> FileTransferDoneLocks { get; set; } = new Dictionary<string, object>();
    }
}