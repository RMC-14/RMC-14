using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Paratoxin;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CatalyticTailStabComponent : Component
{
    [DataField, AutoNetworkedField]
    public DamageSpecifier DamagePerStack = new();

    [DataField, AutoNetworkedField]
    public int MinStacksToBuff = 10;

    [DataField, AutoNetworkedField]
    public float ProportialStacksToRemoveMultiplier = 0.5f;

    [DataField, AutoNetworkedField]
    public float SpeedMultiplier = 2;

    [DataField, AutoNetworkedField]
    public int ArmorGain = 25;

    [DataField, AutoNetworkedField]
    public TimeSpan BuffDuration = TimeSpan.FromSeconds(3.5);

    [DataField]
    public Color BuffColor = Color.FromHex("#FF7E4A");
}
