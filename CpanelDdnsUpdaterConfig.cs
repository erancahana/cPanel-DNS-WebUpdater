namespace CpanelDdnsProvider {
    public class CpanelDdnsUpdaterConfig {
        public string WhmHost { get; set; }
        public int WhmPort { get; set; } = 2087;
        public string WhmUsername { get; set; }
        public string WhmSecurityToken { get; set; }
        public string RequestAuthCode { get; set; }
    }
}