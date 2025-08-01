using OneScript.Contexts.Enums;

namespace OneSwiss.OneScript.Oscript;

[EnumerationType("ТипОтладкиRagent", "RagentDebugType")]
public enum RagentDebugTypeWrapper
{
    [EnumValue("Отключена", "Disabled")]
    None,
    [EnumValue("TCP")]
    Tcp,
    [EnumValue("HTTP")]
    Http
}