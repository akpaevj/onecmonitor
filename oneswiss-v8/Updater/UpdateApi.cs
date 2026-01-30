using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace OneSwiss.V8.Updater
{
    public class UpdateApi
    {
        private const string UpdateApiUrl = "https://update-api.1c.ru";

        private static HttpClient GetHttpClient()
            => new()
            {
                BaseAddress = new Uri(UpdateApiUrl)
            };

        public static async Task<UpdateInfoResponse?> GetConfigurationbAndPlatformUpdateInfo(string programName, string versionNumber, string platformVersion)
            => await GetUpdateInfo(programName, versionNumber, "NewConfigurationAndOrPlatform", platformVersion);

        public static async Task<UpdateInfoResponse?> GetProgramOrRedactionUpdateInfo(string programName, string versionNumber, string platformVersion)
            => await GetUpdateInfo(programName, versionNumber, "NewProgramOrRedaction", platformVersion);

        public static async Task<UpdateResponse?> GetConfigurationbAndPlatformUpdate(string login, string password, ConfigurationUpdate configurationUpdate, PlatformUpdate? platformUpdate = null)
           => await GetUpdate(configurationUpdate!.ProgramVersionUin!, configurationUpdate.UpgradeSequence?.ToArray() ?? [], login, password, platformUpdate?.DistributionUin ?? "");

        private static async Task<UpdateResponse?> GetUpdate(string programVersionUin, string[] upgradeSequence, string login, string password, string platformDistributionUin)
        {
            using var httpClient = GetHttpClient();

            var response = await httpClient.PostAsJsonAsync("/update-platform/programs/update/", new UpdateRequest
            {
                ProgramVersionUin = programVersionUin,
                UpgradeSequence = [.. upgradeSequence],
                Login = login,
                Password = password,
                PlatformDistributionUin = platformDistributionUin
            });

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<UpdateResponse>();
        }

        private static async Task<UpdateInfoResponse?> GetUpdateInfo(string programName, string versionNumber, string updateType, string platformVersion)
        {
            using var httpClient = GetHttpClient();

            var response = await httpClient.PostAsJsonAsync("/update-platform/programs/update/info", new UpdateInfoRequest
            {
                ProgramName = programName,
                VersionNumber = versionNumber,
                UpdateType = updateType,
                PlatformVersion = platformVersion
            });

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<UpdateInfoResponse>();
        }
    }
}
