namespace OneSwiss.V8.Updater
{
    public class UpdateResponse
    {
        public string? ErrorName { get; set; }
        public string? ErrorMessage { get; set; }
        public List<UpdateData>? ConfigurationUpdateDataList { get; set; }
        public string? PlatformDistributionUrl { get; set; }
        public object? AdditionalParameters { get; set; }
    }

    public class UpdateData
    {
        public string? TemplatePath { get; set; }
        public bool ExecuteUpdateProcess { get; set; }
        public string? UpdateFileUrl { get; set; }
        public string? UpdateFileName { get; set; }
        public string? UpdateFileFormat { get; set; }
        public long Size { get; set; }
        public string? HashSum { get; set; }
    }
}
