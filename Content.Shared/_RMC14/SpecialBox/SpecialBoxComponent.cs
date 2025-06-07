using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Content.Shared._RMC14.Storage;
using Content.Shared.Storage;
using System;

namespace Content.Shared._RMC14.SpecialBox;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpecialBoxComponent : Component
{
    public delegate void StateChangedDelegate(EntityUid uid, SpecialBoxComponent component);
    public event StateChangedDelegate? StateChanged;

    private SpecialBoxState _state = SpecialBoxState.Closed;

    [DataField, AutoNetworkedField]
    public SpecialBoxState State
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

public enum SpecialBoxState
{
    Closed,
    Opening,
    Open
}

public enum SpecialBoxVisualLayers
{
    Base
}
