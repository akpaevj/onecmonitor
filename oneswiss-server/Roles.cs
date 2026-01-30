using System.Reflection;

namespace OneSwiss.Server;

public static class Roles
{
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
    public const string ReadGitRepositories = "ReadGitRepositories";
    public const string WriteGitRepositories = "WriteGitRepositories";
    public const string ReadEventLog = "ReadEventLog";
    public const string ReadBuildTasks = "ReadBuildTasks";
    public const string ConfigBuildTasks = "ConfigBuildTasks";
    private static readonly List<(string Name, string Description)> _roles;

    static Roles()
    {
        _roles = GetConstants(typeof(Roles)).Select(c =>
        {
            var name = (string)c.GetRawConstantValue()!;
            var description = GetDescription(name);

            return (name, description);
        }).ToList();
    }

    public static IReadOnlyList<(string Name, string Description)> AllRoles => _roles;

    private static string GetDescription(string role)
    {
        return role switch
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
            ReadGitRepositories => "Чтение репозиториев Git",
            WriteGitRepositories => "Запись репозиториев Git",
            ReadEventLog => "Чтение журнала регистрации",
            ReadBuildTasks => "Чтение задач сборки",
            ConfigBuildTasks => "Настройка задач сборки",
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
        };
    }

    private static List<FieldInfo> GetConstants(Type type)
    {
        var fieldInfos = type.GetFields(BindingFlags.Public |
                                        BindingFlags.Static | BindingFlags.FlattenHierarchy);

        return fieldInfos.Where(fi => fi is { IsLiteral: true, IsInitOnly: false }).ToList();
    }
}