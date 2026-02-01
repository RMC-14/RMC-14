namespace Content.Shared._RMC14.Xenonids.Designer;

[RegisterComponent]
public sealed partial class DesignerDeleteDesignNodeComponent : Component
{
    [DataField]
    public int PlasmaCost = 25;

    [DataField]
    public bool OnlyOwnNodes = true;
}
