using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Storage;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCStorageSystem))]
public sealed partial class LimitedStorageComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<Limit> Limits = new();

    [DataDefinition]
    [Serializable, NetSerializable]
    public partial struct Limit()
    {
        public int Count = 1;

        [DataField(required: true)]
        public EntityWhitelist Whitelist;

        [DataField(required: true)]
        public LocId Popup;
    }
}
