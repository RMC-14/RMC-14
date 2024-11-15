using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Dialog;

[Serializable, NetSerializable]
public enum DialogType
{
    Options,
    Input,
    Confirm,
}
