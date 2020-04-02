namespace CpanelDdnsProvider.Whm {
    class WhmResponseMetaData {
        public int version { get; set; }
        public string reason { get; set; }
        public int result { get; set; }
        public string command { get; set; }
    }
}