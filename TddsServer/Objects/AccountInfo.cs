namespace TddsServer.Objects {
    public class AccountInfo {
        public string UserName { get; set; } = "";
        public string Password { get; set; } = "";
        public DateTime LastLoginTime { get; set; } = DateTime.Now;
    }
}
