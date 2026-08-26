using MessagePack;
using OneScript.Contexts;
using ScriptEngine.Machine.Contexts;

namespace OneSwiss.Common.DTO;

[ContextClass("УчетныеДанные", "Credentials")]
[MessagePackObject]
public class CredentialsDto : AutoContext<CredentialsDto>
{
    [ContextProperty("Пользователь", "User")]
    [Key(0)]
    public string User { get; set; }

    [ContextProperty("Пароль", "Password")]
    [Key(1)]
    public string Password { get; set; }

    [ContextProperty("ЭтоТокен", "IsToken")]
    [Key(2)]
    public bool IsToken { get; set; }

    [ContextProperty("Токен", "Token")]
    [Key(3)]
    public string Token { get; set; }
}