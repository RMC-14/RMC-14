namespace Content.Shared._RMC14.PDT;

[RegisterComponent]
public sealed partial class PDTKitComponent : Component
{
    [DataField]
    public EntityUid? Locator;

    [DataField]
    public EntityUid? Bracelet;
}
