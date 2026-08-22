using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace OneSwiss.Agent.Services;

/// <summary>
/// Assigns child processes to a single Windows Job Object configured with
/// JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE. The job's only handle is held by this process, so when this
/// process exits for any reason - including a crash or "taskkill /F" where no cleanup code runs -
/// Windows closes that handle and the OS kills every process assigned to the job. This is the only
/// reliable way to avoid orphaned child processes on Windows, since .NET's Process class does not
/// terminate children when the parent exits.
/// On non-Windows platforms this is a no-op: the process manager (e.g. systemd with the default
/// KillMode=control-group) already kills the whole process tree when the service stops.
/// </summary>
[SupportedOSPlatform("windows")]
public static class JobObjectProcessTracker
{
    private static readonly Lazy<IntPtr> JobHandle = new(CreateJobObjectWithKillOnClose);

    /// <summary>
    /// Assigns the process to the shared job object so it is killed by the OS if this agent
    /// process ever terminates without running its own cleanup code.
    /// </summary>
    public static void Add(Process process)
    {
        if (!AssignProcessToJobObject(JobHandle.Value, process.Handle))
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    private static IntPtr CreateJobObjectWithKillOnClose()
    {
        var jobHandle = CreateJobObject(IntPtr.Zero, null);
        if (jobHandle == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION
            {
                LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
            }
        };

        var infoLength = Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>();
        var infoPtr = Marshal.AllocHGlobal(infoLength);

        try
        {
            Marshal.StructureToPtr(info, infoPtr, false);

            if (!SetInformationJobObject(jobHandle, JobObjectInfoType.ExtendedLimitInformation, infoPtr, (uint)infoLength))
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        finally
        {
            Marshal.FreeHGlobal(infoPtr);
        }

        return jobHandle;
    }

    private const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000;

    private enum JobObjectInfoType
    {
        ExtendedLimitInformation = 9
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public UIntPtr MinimumWorkingSetSize;
        public UIntPtr MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public UIntPtr Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IO_COUNTERS
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
    {
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        public IO_COUNTERS IoInfo;
        public UIntPtr ProcessMemoryLimit;
        public UIntPtr JobMemoryLimit;
        public UIntPtr PeakProcessMemoryUsed;
        public UIntPtr PeakJobMemoryUsed;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string? lpName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetInformationJobObject(
        IntPtr hJob,
        JobObjectInfoType infoType,
        IntPtr lpJobObjectInfo,
        uint cbJobObjectInfoLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);
}
