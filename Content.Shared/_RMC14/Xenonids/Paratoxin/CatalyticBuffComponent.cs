using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Paratoxin;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CatalyticBuffComponent : Component
{
    [DataField, AutoNetworkedField]
    public float SpeedMultiplier;

    [DataField, AutoNetworkedField]
    public int Armor;

    [DataField, AutoNetworkedField]
    public TimeSpan ExpiresAt;
}
