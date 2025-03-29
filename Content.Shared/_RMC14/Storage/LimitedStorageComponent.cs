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
        [DataField]
        public int Count = 1;

        [DataField]
        public EntityWhitelist? Blacklist = new();

        [DataField(required: true)]
        public EntityWhitelist? Whitelist = new();

        [DataField(required: true)]
        public LocId Popup;
    }
}
