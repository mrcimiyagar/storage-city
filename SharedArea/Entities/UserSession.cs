namespace SharedArea.Entities
{
    public class UserSession : Session
    {
        public long? UserId { get; set; }
        public virtual User User { get; set; }

        public UserSession()
        {
            this.Type = "UserSession";
        }
    }
}