namespace OneSwiss.V8.Updater
{
    public class UpdateRequest
    {
        public string ProgramVersionUin { get; set; }
        public List<string> UpgradeSequence { get; set; } = [];
        public string Login { get; set; }
        public string Password { get; set; }
        public string PlatformDistributionUin { get; set; }
    }
}
