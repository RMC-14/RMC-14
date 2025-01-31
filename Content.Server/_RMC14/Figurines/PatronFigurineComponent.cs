namespace Content.Server._RMC14.Figurines;

[RegisterComponent]
[Access(typeof(FigurineSystem))]
public sealed partial class PatronFigurineComponent : Component
{
    [DataField(required: true)]
    public string Id;
}
