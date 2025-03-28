using Timer = System.Timers.Timer;

namespace OnecMonitor.Agent.Extensions;

public static class TimerExtension
{
    public static void Reset(this Timer timer)
    {
        timer.Stop();
        timer.Start();
    }
}