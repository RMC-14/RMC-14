namespace Content.Shared._RMC14.Construction;

[RegisterComponent]
public sealed partial class RMCDeconstructComponent : Component
{
    [DataField]
    public string? VerbText;

    [DataField]
    public string? VerbFailedText;
}
