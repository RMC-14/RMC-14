using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Intel.Tech;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(IntelSystem), typeof(TechSystem))]
public sealed partial class TechControlConsoleComponent : Component
{
    [DataField, AutoNetworkedField]
    public IntelTechTree Tree = new();

    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi LockedRsi = new(new ResPath("_RMC14/Interface/tech_64.rsi"), "marine_locked");

    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi UnlockedRsi = new(new ResPath("_RMC14/Interface/tech_64.rsi"), "marine");
}
