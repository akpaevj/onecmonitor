namespace OneSwiss.Agent.Helpers;

public static class IoHelper
{
    public static async Task DeleteFolderInCycle(string path, int triesCount)
    {
        for (var i = 0; i < triesCount; i++)
            try
            {
                Directory.Delete(path, true);
                break;
            }
            catch
            {
                await Task.Delay(20 * 1000);
            }
    }
}