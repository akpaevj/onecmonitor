using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using Octokit;
using OneSwiss.Server.Hubs;

namespace OneSwiss.Server.Services;

public class UpdatesChecker(
    UpdatesChecker.State state,
    IHubContext<UpdatesCheckingHub> hub,
    ILogger<UpdatesChecker> logger) : BackgroundService
{
    private const string UpdatesCheckerName = "OneSwiss";
    private const string RepoOwnerName = "akpaevj";
    private const string RepoName = "oneswiss";

    private readonly Version? _currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
            try
            {
                var client = new GitHubClient(new ProductHeaderValue(UpdatesCheckerName));
                var releases = await client.Repository.Release.GetAll(RepoOwnerName, RepoName);
                var latestRelease = releases.OrderByDescending(c => c.CreatedAt).FirstOrDefault();

                if (latestRelease == null)
                    continue;

                var version = latestRelease.TagName.Replace("v", "").Trim();

                if (!Regex.IsMatch(version, @"^\d+\.\d+\.\d+$"))
                    continue;

                version += ".0";

                state.UpdatesAvailable = new Version(version).CompareTo(_currentVersion) < 0;

                if (!state.UpdatesAvailable)
                    continue;

                state.ReleaseInfo = latestRelease;
                await hub.Clients.All.SendAsync("UpdatesAvailable", stoppingToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Ошибка при проверке новых релизов OneSwiss");
            }
            finally
            {
                await Task.Delay(60 * 60 * 1000, stoppingToken);
            }
    }

    public class State
    {
        public bool UpdatesAvailable { get; internal set; }
        public Release? ReleaseInfo { get; internal set; }
    }
}