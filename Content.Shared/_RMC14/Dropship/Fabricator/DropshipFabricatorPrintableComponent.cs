using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Dropship.Fabricator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(DropshipFabricatorSystem))]
public sealed partial class DropshipFabricatorPrintableComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Cost = 50;

    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(10);

    [DataField, AutoNetworkedField]
    public CategoryType Category;

    public enum CategoryType
    {
        Equipment,
        Ammo,
    }
}
