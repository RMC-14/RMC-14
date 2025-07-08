using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.ClawSharpness;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoClawsSystem))]
public sealed partial class XenoClawsComponent : Component
{
    [DataField, AutoNetworkedField]
    public XenoClawType ClawType = XenoClawType.Normal;
}

[Flags]
[Serializable, NetSerializable]
public enum XenoClawType
{
    Normal,
    Sharp,
    VerySharp,
    ImpossiblySharp //For receiver component on things that should be unbreakable
}
