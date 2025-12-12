using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Barricade;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DirectionalAttackBlockerComponent : Component
{
    [DataField, AutoNetworkedField]
    public float MinimumBlockChance = 0.3f;

    [DataField, AutoNetworkedField]
    public int MaxHealth;

    [DataField, AutoNetworkedField]
    public bool BlockMarineAttacks;
}
