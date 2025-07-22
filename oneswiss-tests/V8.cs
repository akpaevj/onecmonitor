using OneSwiss.V8.Platform;
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
}