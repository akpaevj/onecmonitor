using System.Reflection;
using System.Runtime.CompilerServices;

namespace OneSwiss.Server;

public static class BuiltInRoles
{
    private static readonly List<(string Name, string Description)> AllRoles;
    public static IReadOnlyList<(string Name, string Description)> Roles => AllRoles;
    
    public const string Administrator = "Administrator";
    public const string ReadClusterItems = "ReadClusters";
    public const string WriteClusterItemsInfo = "WriteClusterInfo";
    public const string ReadSessions = "ReadSessions";
    public const string CloseSessions = "CloseSessions";
    public const string ReadMaintenanceTasks = "ReadMaintenanceTasks";
    public const string WriteMaintenanceTasks = "WriteMaintenanceTasks";
    public const string ReadTechLogSeances = "ReadTechLogSeances";
    public const string WriteTechLogSeances = "WriteTechLogSeances";
    public const string ReadErrorLoggingReports = "ReadErrorLoggingReports";

    private static string GetDescription(string role)
        => role switch
        {
            Administrator => "Администратор",
            ReadClusterItems => "Просмотр элементов кластеров",
            WriteClusterItemsInfo => "Изменение данных элемента кластера",
            ReadSessions => "Просмотр соединений",
            CloseSessions => "Закрытие соединений",
            ReadMaintenanceTasks => "Просмотр задач обслуживания",
            WriteMaintenanceTasks => "Изменение задач обслуживания",
            ReadTechLogSeances => "Чтение сеансов сбора ТЖ",
            WriteTechLogSeances => "Изменение сеансов сбора ТЖ",
            ReadErrorLoggingReports => "Просмотр отчетов об ошибках",
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
        };
    
    static BuiltInRoles()
    {
        AllRoles = GetConstants(typeof(BuiltInRoles)).Select(c =>
        {
            var name = (string)c.GetRawConstantValue()!;
            var description = GetDescription(name);

            return (name, description);
        }).ToList();
    }
    
    private static List<FieldInfo> GetConstants(Type type)
    {
        var fieldInfos = type.GetFields(BindingFlags.Public |
                                        BindingFlags.Static | BindingFlags.FlattenHierarchy);

        return fieldInfos.Where(fi => fi is { IsLiteral: true, IsInitOnly: false }).ToList();
    }
}