using MudBlazor;

namespace OneSwiss.Server.Models;

public class LogTemplate : DatabaseObject
{
    [Label("Имя")] public string Name { get; set; } = string.Empty;

    [Label("Контент logcfg.xml")] public string Content { get; set; } = string.Empty;

    public virtual List<TechLogSeance> Seances { get; set; } = new();

    public static Guid ServerMonitoringId => new("dc610b92-6851-4f13-8cb4-78d457cb74c1");

    public static string ServerMonitoringTemplate =>
        """
        <log history="1">
            <event>
                <eq property="Name" value="EXCP"/>
            </event>
            <event>
                <eq property="Name" value="CONN"/>
            </event>
            <event>
                <eq property="Name" value="PROC"/>
            </event>
            <event>
                <eq property="Name" value="ADMIN"/>
            </event>
            <event>
                <eq property="Name" value="SESN"/>
            </event>
            <event>
                <eq property="Name" value="CLSTR"/>
            </event>
            <property name="all"/>
        </log>
        """;

    public static Guid WaitingsOnManagedLocksId => new("35eb7ecf-1280-4c82-9513-2c5bfd2a7dda");

    public static string WaitingsOnManagedLocksTemplate =>
        """
        <log history="1">
            <event>
                <eq property="name" value="SDBL"/>
                <eq property="Func1" value="CommitTransaction"/>
                <eq property="Func1" value="RollbackTransaction"/>
            </event>
            <event>
                <eq property="name" value="TLOCK"/>
            </event>
            <event>
                <eq property="name" value="TTIMEOUT"/>
            </event>
            <event>
                <eq property="name" value="TDEADLOCK"/>
            </event>
            <property name="all"/>
        </log>
        """;

    public static Guid TimeoutsOnManagedLocksId => new("f869c902-37a2-414c-8002-c46e18362948");

    public static string TimeoutsOnManagedLocksTemplate =>
        """
        <log history="1">
            <event>
                <eq property="name" value="SDBL"/>
                <eq property="Func1" value="CommitTransaction"/>
                <eq property="Func1" value="RollbackTransaction"/>
            </event>
            <event>
                <eq property="name" value="TLOCK"/>
            </event>
            <event>
                <eq property="name" value="TTIMEOUT"/>
            </event>
            <property name="all"/>
        </log>
        """;

    public static Guid DeadlocksOnManagedLocksId => new("d4f5551a-995a-480c-8487-e30d96605c1a");

    public static string CallsScallTemplate =>
        """
        <log history="1">
            <event>
                <eq property="name" value="SCALL"/>
            </event>
            <event>
                <eq property="name" value="CALL"/>
            </event>
            <property name="all"/>
        </log>
        """;

    public static Guid CallScallsId => new("f20908d6-2c2c-4a7b-82e4-4c62b6a8f99a");

    public static string DeadlocksOnManagedLocksTemplate =>
        """
        <log history="1">
            <event>
                <eq property="name" value="SDBL"/>
                <eq property="Func1" value="CommitTransaction"/>
                <eq property="Func1" value="RollbackTransaction"/>
            </event>
            <event>
                <eq property="name" value="TLOCK"/>
            </event>
            <event>
                <eq property="name" value="TTIMEOUT"/>
            </event>
            <event>
                <eq property="name" value="TDEADLOCK"/>
            </event>
            <property name="all"/>
        </log>
        """;

    public static Guid FullId => new("54695318-0ca1-4c95-896f-731872fb1c0e");

    public static string FullTemplate =>
        """
        <log history="4">
            <event>
                <ne property="name" value=""/>
            </event>
            <property name="all"/>
        </log>
        """;

    public static Guid VrsId => new("3090a9ec-dc9c-4a81-b3e4-70d26ff4f929");

    public static string VrsTemplate =>
        """
        <log history="4">
            <event>
                <eq property="name" value="VRSREQUEST"/>
            </event>
            <event>
                <eq property="name" value="VRSRESPONSE"/>
            </event>
            <property name="all"/>
        </log>
        """;

    public static Guid[] BuiltInTemplatesIds =>
    [
        ServerMonitoringId,
        WaitingsOnManagedLocksId,
        TimeoutsOnManagedLocksId,
        DeadlocksOnManagedLocksId,
        CallScallsId,
        VrsId,
        FullId
    ];

    public override bool Equals(object? obj)
    {
        return obj is LogTemplate log &&
               Id.Equals(log.Id);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id);
    }
}