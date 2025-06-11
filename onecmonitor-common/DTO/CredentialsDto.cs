using MessagePack;
using OneScript.Contexts;

namespace OnecMonitor.Common.DTO;

[ContextClass("УчетныеДанные", "Credentials")]
[MessagePackObject]
public class CredentialsDto
{
    [ContextProperty("Пользователь", "User")]
    [Key(0)]
    public string User { get; set; }
    [ContextProperty("Пароль", "Password")]
    [Key(1)]
    public string Password { get; set; }
}