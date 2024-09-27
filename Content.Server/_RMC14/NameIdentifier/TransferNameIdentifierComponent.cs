namespace Content.Server._RMC14.NameIdentifier;

[RegisterComponent]
public sealed partial class TransferNameIdentifierComponent : Component
{
    [DataField]
    public string FullIdentifier = string.Empty;

    [DataField]
    public int Identifier;

    [DataField]
    public string Group = string.Empty;
}
