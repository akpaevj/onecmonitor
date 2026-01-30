using OneSwiss.V8.Updater;

var info = await UpdateApi.GetConfigurationbAndPlatformUpdateInfo("Enterprise20", "2.5.22.134", "8.3.27.1786");

var configUpdate = info!.AdditionalReleaseUpdate?.Count > 0 ? info.AdditionalReleaseUpdate.First().ConfigurationUpdate : info.ConfigurationUpdateResponse;

var update = await UpdateApi.GetConfigurationbAndPlatformUpdate("", "", configUpdate!, info.PlatformUpdateResponse);
var a = 1;
