using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Soak;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoSoakingDamageComponent : Component
{
    [DataField, AutoNetworkedField]
    public float DamageAccumulated = 0;

    [DataField, AutoNetworkedField]
    public int DamageGoal = 140;

    [DataField, AutoNetworkedField]
    public FixedPoint2 Heal = FixedPoint2.New(75);

    [DataField, AutoNetworkedField]
    public TimeSpan EffectExpiresAt;

    [DataField, AutoNetworkedField]
    public Color SoakColor = Color.FromHex("#421313");

    [DataField, AutoNetworkedField]
    public Color RageColor = Color.FromHex("#AD1313");

    [DataField, AutoNetworkedField]
    public TimeSpan RageDuration = TimeSpan.FromSeconds(3);
}
