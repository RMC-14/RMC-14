namespace Content.Server._RMC14.NamedItems;

[RegisterComponent]
[Access(typeof(RMCNamedItemSystem))]
public sealed partial class RMCNamedItemComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Name = string.Empty;
}
