using System;
using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Sentry.Laptop;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSentryLaptopSystem))]
public sealed partial class SentryLaptopComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool IsOpen;

    [DataField, AutoNetworkedField]
    public bool IsPowered;

    [DataField, AutoNetworkedField]
    public float Range = 20f;

    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> LinkedSentries = new();

    [DataField, AutoNetworkedField]
    public int MaxLinkedSentries = 99;

    [DataField, AutoNetworkedField]
    public Dictionary<EntityUid, string> SentryCustomNames = new();

    [DataField, AutoNetworkedField]
    public List<EntityUid> Watchers = new();

    [DataField, AutoNetworkedField]
    public EntityUid? CurrentCamera;

}

[Serializable, NetSerializable]
public enum SentryLaptopVisuals : byte
{
    State
}

[Serializable, NetSerializable]
public enum SentryLaptopVisualLayers : byte
{
    Base
}

[Serializable, NetSerializable]
public enum SentryLaptopState : byte
{
    Closed,
    Open,
    Active
}

[Serializable, NetSerializable]
public enum SentryLaptopUiKey : byte
{
    Key
}
