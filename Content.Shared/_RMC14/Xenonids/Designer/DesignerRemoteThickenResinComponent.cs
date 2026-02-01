namespace Content.Shared._RMC14.Xenonids.Designer;

[RegisterComponent]
public sealed partial class DesignerRemoteThickenResinComponent : Component
{
    [DataField]
    public int PlasmaCost = 60;

    [DataField]
    public float Cooldown = 0.5f;

    [DataField]
    public float DoAfter = 1f;

    [DataField]
    public float Range = 10f;
}
