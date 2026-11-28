namespace Content.Shared._RMC14.Requisitions.Components;

[RegisterComponent]
public sealed partial class RequisitionsRequestPaperComponent : Component
{
    public EntityUid Account;
    public int Number;
    public int MaxItems;
    public int ContentsLength;
    public int ReasonLength;
}
