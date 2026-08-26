using MessagePack;

namespace OneSwiss.V8.Platform.RemoteAdministration;

[MessagePackObject]
public class V8SecurityProfile
{
    [Key(0)] [RacField("name")] public string Name { get; set; } = string.Empty;

    [Key(1)] [RacField("descr")] public string Descr { get; set; } = string.Empty;

    [Key(2)] [RacField("config")] public bool Config { get; set; }

    [Key(3)] [RacField("priv")] public bool Priv { get; set; }

    [Key(4)] [RacField("full-privileged-mode")] public bool FullPrivilegedMode { get; set; }

    [Key(5)]
    [RacField("privileged-mode-roles")]
    public string PrivilegedModeRoles { get; set; } = string.Empty;

    [Key(6)] [RacField("crypto")] public bool Crypto { get; set; }

    [Key(7)] [RacField("right-extension")] public bool RightExtension { get; set; }

    [Key(8)]
    [RacField("right-extension-definition-roles")]
    public string RightExtensionDefinitionRoles { get; set; } = string.Empty;

    [Key(9)] [RacField("all-modules-extension")] public bool AllModulesExtension { get; set; }

    [Key(10)]
    [RacField("modules-available-for-extension")]
    public string ModulesAvailableForExtension { get; set; } = string.Empty;

    [Key(11)]
    [RacField("modules-not-available-for-extension")]
    public string ModulesNotAvailableForExtension { get; set; } = string.Empty;
}
