using OneSwiss.V8.Extensions;
using OneSwiss.V8.Platform;
using OneSwiss.V8.Platform.RemoteAdministration;
using OneSwiss.V8.Platform.Services;

namespace OneSwiss.Tests;

public class V8
{
    [Test]
    public void ArgsParserWindowsTest()
    {
        const string argsStr = """
                            "C:\Program Files\1cv8\8.3.27.1644\bin\ragent.exe" -srvc -agent -regport 1541 -port 1540 -range 1560:1591 -d "E:\\srvinfo" -debug
                            """;
        var args = ArgsParser.ParsePairs(argsStr);

        Assert.That(args.ItemsCount, Is.EqualTo(8));
        
        Assert.Multiple(() =>
        {
            Assert.That(args.ItemByIndexAsValue(0)!.Value, Is.EqualTo(@"C:\Program Files\1cv8\8.3.27.1644\bin\ragent.exe"));

            Assert.That(args.HasOption("srvc"), Is.True);
            Assert.That(args.HasOption("agent"), Is.True);
            
            Assert.That(args.HasParameter("regport", out var value), Is.True);
            Assert.That(value, Is.EqualTo("1541"));
            
            Assert.That(args.HasParameter("port", out value), Is.True);
            Assert.That(value, Is.EqualTo("1540"));
            
            Assert.That(args.HasParameter("range", out value), Is.True);
            Assert.That(value, Is.EqualTo("1560:1591"));
            
            Assert.That(args.HasParameter("d", out value), Is.True);
            Assert.That(value, Is.EqualTo(@"E:\\srvinfo"));
            
            Assert.That(args.HasOption("debug"), Is.True);
        });
    }
    
    [Test]
    public void ArgsParserWindows2Test()
    {
        const string argsStr = """
                               "D:\Program Files\1cv8\8.3.25.1560\bin\ragent.exe" -srvc -agent -regport 2541 -port 2540 -range 2560:2590 -d "D:\srvinfo2541"  -debug -http -DebugServerPort 2550
                               """;
        var args = ArgsParser.ParsePairs(argsStr);

        Assert.That(args.ItemsCount, Is.EqualTo(10));
        
        Assert.Multiple(() =>
        {
            Assert.That(args.ItemByIndexAsValue(0)!.Value, Is.EqualTo(@"D:\Program Files\1cv8\8.3.25.1560\bin\ragent.exe"));

            Assert.That(args.HasOption("srvc"), Is.True);
            Assert.That(args.HasOption("agent"), Is.True);
            
            Assert.That(args.HasParameter("regport", out var value), Is.True);
            Assert.That(value, Is.EqualTo("2541"));
            
            Assert.That(args.HasParameter("port", out value), Is.True);
            Assert.That(value, Is.EqualTo("2540"));
            
            Assert.That(args.HasParameter("range", out value), Is.True);
            Assert.That(value, Is.EqualTo("2560:2590"));
            
            Assert.That(args.HasParameter("d", out value), Is.True);
            Assert.That(value, Is.EqualTo(@"D:\srvinfo2541"));
            
            Assert.That(args.HasOption("debug"), Is.True);
            Assert.That(args.HasOption("http"), Is.True);
            
            Assert.That(args.HasParameter("DebugServerPort", out value), Is.True);
            Assert.That(value, Is.EqualTo("2550"));
        });
    }
    
    [Test]
    public void FillRasFromWindowsServiceArgsTest()
    {
        const string args = @"C:\Program Files\1cv8\8.3.27.1644\bin\ras.exe cluster --service --port=1600 localhost:1740";
        var parsed = ArgsParser.ParsePairs(args);

        var ras = new RasService();
        V8Services.FillRasFromArgs(ras, parsed);
        Assert.Multiple(() =>
        {
            Assert.That(ras.Port, Is.EqualTo(1600));
            Assert.That(ras.RagentHost, Is.EqualTo("localhost"));
            Assert.That(ras.RagentPort, Is.EqualTo(1740));
        });
    }
    
    [Test]
    public void FillRagentFromWindowsServiceArgsTest()
    {
        const string args = """
                            "C:\Program Files\1cv8\8.3.27.1644\bin\ragent.exe" -srvc -agent -regport 1741 -port 1740 -range 1760:1791 -d "E:\\srvinfo" -debug -http
                            """;
        var parsed = ArgsParser.ParsePairs(args);

        var ras = new RagentService();
        V8Services.FillRagentFromArgs(ras, parsed);
        Assert.Multiple(() =>
        {
            Assert.That(ras.RegPort, Is.EqualTo(1741));
            Assert.That(ras.Port, Is.EqualTo(1740));
            Assert.That(ras.ClusterCatalog, Is.EqualTo(@"E:\\srvinfo/reg_1741"));
            Assert.That(ras.DebugType, Is.EqualTo(RagentDebugType.Http));
        });
    }
    
    [Test]
    public void FillCrServerFromWindowsServiceArgsTest()
    {
        const string args = """
                            "C:\Program Files\1cv8\8.3.27.1644\bin\crserver.exe" -srvc -port 1644 -d D:\\1CRepository
                            """;
        var parsed = ArgsParser.ParsePairs(args);

        var ras = new CrServer();
        V8Services.FillCrServerFromArgs(ras, parsed);
        Assert.Multiple(() =>
        {
            Assert.That(ras.Directory, Is.EqualTo(@"D:\\1CRepository"));
            Assert.That(ras.Port, Is.EqualTo(1644));
        });
    }
    
    [Test]
    public void OutputToOutputItemsTest()
    {
        const string output = """
                              cluster                                   : f3199b6b-0b61-4bc2-98f6-9604fa4d42e6
                              host                                      : W228
                              port                                      : 1541
                              name                                      : "Локальный кластер"
                              expiration-timeout                        : 600
                              lifetime-limit                            : 0
                              max-memory-size                           : 0
                              max-memory-time-limit                     : 0
                              security-level                            : 0
                              session-fault-tolerance-level             : 0
                              load-balancing-mode                       : performance
                              errors-count-threshold                    : 0
                              kill-problem-processes                    : 0
                              kill-by-memory-with-dump                  : 0
                              allow-access-right-audit-events-recording : 0
                              restart-schedule                          : 
                              """;

        var items = Rac.OutputToOutputItems(output);
        
        Assert.That(items, Has.Count.EqualTo(1));
        
        var outputItem = items[0];
        
        Assert.Multiple(() =>
        {
            Assert.That(outputItem["cluster"], Is.EqualTo("f3199b6b-0b61-4bc2-98f6-9604fa4d42e6"));
            Assert.That(outputItem["host"], Is.EqualTo("W228"));
            Assert.That(outputItem["port"], Is.EqualTo("1541"));
            Assert.That(outputItem["name"], Is.EqualTo("\"Локальный кластер\""));
            Assert.That(outputItem["expiration-timeout"], Is.EqualTo("600"));
            Assert.That(outputItem["lifetime-limit"], Is.EqualTo("0"));
            Assert.That(outputItem["max-memory-size"], Is.EqualTo("0"));
            Assert.That(outputItem["max-memory-time-limit"], Is.EqualTo("0"));
            Assert.That(outputItem["security-level"], Is.EqualTo("0"));
            Assert.That(outputItem["session-fault-tolerance-level"], Is.EqualTo("0"));
            Assert.That(outputItem["load-balancing-mode"], Is.EqualTo("performance"));
            Assert.That(outputItem["errors-count-threshold"], Is.EqualTo("0"));
            Assert.That(outputItem["kill-problem-processes"], Is.EqualTo("0"));
            Assert.That(outputItem["kill-by-memory-with-dump"], Is.EqualTo("0"));
            Assert.That(outputItem["allow-access-right-audit-events-recording"], Is.EqualTo("0"));
            Assert.That(outputItem["restart-schedule"], Is.EqualTo(string.Empty));
        });
    }

    [Test]
    public void CreateRacObjectFromOutputTest()
    {
        const string output = """
                              cluster                                   : f3199b6b-0b61-4bc2-98f6-9604fa4d42e6
                              host                                      : W228
                              port                                      : 1541
                              name                                      : "Локальный кластер"
                              expiration-timeout                        : 600
                              lifetime-limit                            : 100
                              max-memory-size                           : 200
                              max-memory-time-limit                     : 300
                              security-level                            : 2
                              session-fault-tolerance-level             : 0
                              load-balancing-mode                       : memory
                              errors-count-threshold                    : 400
                              kill-problem-processes                    : 1
                              kill-by-memory-with-dump                  : 1
                              allow-access-right-audit-events-recording : 1
                              restart-schedule                          : 
                              """;
        
        var clusters = Rac.OutputToOutputItems(output).ToRacObjects<V8Cluster>();
        
        Assert.That(clusters, Has.Count.EqualTo(1));
        
        var cluster = clusters[0];
        
        Assert.Multiple(() =>
        {
            Assert.That(cluster.Id, Is.EqualTo("f3199b6b-0b61-4bc2-98f6-9604fa4d42e6"));
            Assert.That(cluster.Host, Is.EqualTo("W228"));
            Assert.That(cluster.Port, Is.EqualTo(1541));
            Assert.That(cluster.Name, Is.EqualTo("Локальный кластер"));
            Assert.That(cluster.ExpirationTimeout, Is.EqualTo(600));
            Assert.That(cluster.LifetimeLimit, Is.EqualTo(100));
            Assert.That(cluster.MaxMemorySize, Is.EqualTo(200));
            Assert.That(cluster.MaxMemoryTimeLimit, Is.EqualTo(300));
            Assert.That(cluster.SecurityLevel, Is.EqualTo(V8SecurityLevel.Enabled));
            Assert.That(cluster.SessionFaultToleranceLevel, Is.EqualTo(0));
            Assert.That(cluster.LoadBalancingMode, Is.EqualTo(V8ClusterLoadBalancingMode.Memory));
            Assert.That(cluster.ErrorCountThreshold, Is.EqualTo(400));
            Assert.That(cluster.KillProblemProcesses, Is.EqualTo(true));
            Assert.That(cluster.KillByMemoryWithDump, Is.EqualTo(true));
            Assert.That(cluster.AllowAccessRightAuditEventsRecording, Is.EqualTo(true));
            Assert.That(cluster.RestartSchedule, Is.EqualTo(string.Empty));
        });
    }
}