using System.Collections.Generic;

namespace PacketRouter
{
    public static class LockRepo
    {
        public static Dictionary<string, object> RequestLocks { get; set; } = new Dictionary<string, object>();
    }
}