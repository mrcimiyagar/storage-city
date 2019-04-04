using System.Collections.Generic;

namespace SharedArea.Entities
{
    public class ModuleRequest
    {
        public string EndPointName { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
    }
}