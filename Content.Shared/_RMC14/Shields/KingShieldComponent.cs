using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Shields;

[RegisterComponent, NetworkedComponent]
public sealed partial class KingShieldComponent : Component
{
    [DataField]
    public float MaxDamagePercent = 0.1f;

    [DataField]
    public ResPath RsiPath = new("/Textures/_RMC14/Effects/beam.rsi");

    [DataField]
    public string LightningEffectState = "purple_lightning";

    [DataField]
    public float LightningWidth = 0.5f;
}
