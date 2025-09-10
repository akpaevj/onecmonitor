using System.ComponentModel.DataAnnotations;

namespace OneSwiss.V8.Platform.RemoteAdministration;

public enum V8InfoBaseDbms
{
    [Display(Name = "Microsoft SQL Server")]
    MsSqlServer,
    [Display(Name = "PostgreSQL")] PostgreSql,
    [Display(Name = "IBM DB2")] IbmDb2,
    [Display(Name = "Oracle Database")] OracleDatabase
}