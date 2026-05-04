using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.ClawSharpness;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoClawsSystem))]
public sealed partial class XenoClawsComponent : Component, IComponentDebug
{
    [DataField, AutoNetworkedField]
    public XenoClawType ClawType = XenoClawType.Normal;

    public string GetDebugString()
    {
        return $"""
            ClawType: {ClawType}
            """;
    }
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
