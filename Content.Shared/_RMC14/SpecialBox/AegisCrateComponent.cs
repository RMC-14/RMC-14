using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Content.Shared._RMC14.Storage;
using Content.Shared.Storage;
using System;

namespace Content.Shared._RMC14.AegisCrate;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AegisCrateComponent : Component
{
    public delegate void StateChangedDelegate(EntityUid uid, AegisCrateComponent component);
    public event StateChangedDelegate? StateChanged;

    private AegisCrateState _state = AegisCrateState.Closed;

    [DataField, AutoNetworkedField]
    public AegisCrateState State
    {
        get => _state;
        set
        {
            if (_state == value) return;
            _state = value;
            StateChanged?.Invoke(Owner, this);
        }
    }

    [NonSerialized]
    public EntityUid? StorageUid;
}

public enum AegisCrateState
{
    Closed,
    Opening,
    Open
}

public enum AegisCrateVisualLayers
{
    Base
}
