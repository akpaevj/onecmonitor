using MessagePack;
using OneScript.Contexts;

namespace OneSwiss.Common.DTO;

[ContextClass("РепозиторийGit", "GirRepository")]
[MessagePackObject]
public class GitRepositoryDto
{
    [ContextProperty("Идентификатор", "Id")]
    [Key(0)]
    public Guid Id { get; set; }

    [ContextProperty("Имя", "Name")]
    [Key(1)]
    public string Name { get; set; }

    [ContextProperty("Адрес", "Address")]
    [Key(2)]
    public string Address { get; set; }

    [ContextProperty("Токен", "Token")]
    [Key(3)]
    public CredentialsDto Token { get; set; }
}