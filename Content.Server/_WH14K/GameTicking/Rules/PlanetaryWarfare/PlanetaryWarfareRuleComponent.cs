

namespace Content.Server._WH14K.GameTicking.Rules;

[RegisterComponent, Access(typeof(PlanetaryWarfareRuleSystem))]
public sealed partial class PlanetaryWarfareRuleComponent : Component
{
    [DataField]
    public WinTypePW WinTypePW = WinTypePW.Neutral;
}

public enum WinTypePW : byte
{
    AllCommandDead,
    WarpShtormSummoned,
    Neutral,
    AllAltarExploded
}
