using System;
using System.Collections.Generic;
using System.Text;

namespace OneSwiss.V8.Updater
{

    public class UpdateInfoResponse
    {
        public string? ErrorName { get; set; }
        public string? ErrorMessage { get; set; }
        public ConfigurationUpdate? ConfigurationUpdateResponse { get; set; }
        public PlatformUpdate? PlatformUpdateResponse { get; set; }
        public object? AdditionalParameters { get; set; }
        public string? CurrentReleaseSupportEndDate { get; set; }
        public List<AdditionalReleaseUpdate>? AdditionalReleaseUpdate { get; set; }
        public object? CurrentReleasePlatformUpdate { get; set; }
    }

    public class AdditionalReleaseUpdate
    {
        public ConfigurationUpdate? ConfigurationUpdate { get; set; }
        public PlatformUpdate? PlatformUpdate { get; set; }
    }

    public class ConfigurationUpdate
    {
        public string? ConfigurationVersion { get; set; }
        public long Size { get; set; }
        public string? PlatformVersion { get; set; }
        public string? UpdateInfoUrl { get; set; }
        public string? HowToUpdateInfoUrl { get; set; }
        public List<string>? UpgradeSequence { get; set; }
        public string? ProgramVersionUin { get; set; }
        public string? SupportEndDate { get; set; }
    }

    public class PlatformUpdate
    {
        public string? PlatformVersion { get; set; }
        public string? TransitionInfoUrl { get; set; }
        public string? ReleaseUrl { get; set; }
        public string? DistributionUin { get; set; }
        public long Size { get; set; }
        public bool Recommended { get; set; }
    }
}
