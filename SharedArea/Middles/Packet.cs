
using System.Collections.Generic;
using SharedArea.Entities;

namespace SharedArea.Middles
{
    public class Packet
    {
        public string Status { get; set; }
        public string Email { get; set; }
        public string VerifyCode { get; set; }
        public Session Session { get; set; }
        public User User { get; set; }
        public UserSecret UserSecret { get; set; }
        public Bot Bot { get; set; }
        public List<Bot> Bots { get; set; }
        public string SearchQuery { get; set; }
        public List<User> Users { get; set; }
        public List<Session> Sessions { get; set; }
        public List<File> Files { get; set; }
        public File File { get; set; }
        public BotCreation BotCreation { get; set; }
        public List<BotCreation> BotCreations { get; set; }
        public ModuleRequest ModuleRequest { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string StreamCode { get; set; }
        public long? Offset { get; set; }
        public string RawJson { get; set; }
        public long? MessageSeenCount { get; set; }
        public bool? BatchData { get; set; }
        public bool? FetchNext { get; set; }
        public string ControlId { get; set; }
        public string Text { get; set; }
        public string ErrorMessage { get; set; }
        public App App { get; set; }
        public Storage Storage { get; set; }
    }
}