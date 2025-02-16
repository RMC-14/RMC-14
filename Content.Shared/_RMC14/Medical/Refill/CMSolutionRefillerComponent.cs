﻿using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Medical.Refill;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(CMRefillableSolutionSystem))]
public sealed partial class CMSolutionRefillerComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public HashSet<ProtoId<ReagentPrototype>> Reagents = new();

    [DataField(required: true), AutoNetworkedField]
    public EntityWhitelist Whitelist = new();

    [DataField, AutoNetworkedField]
    public FixedPoint2 Current;

    [DataField, AutoNetworkedField]
    public FixedPoint2 Max;

    [DataField, AutoNetworkedField]
    public FixedPoint2 Recharge;

    [DataField, AutoNetworkedField]
    public TimeSpan RechargeCooldown;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan RechargeAt;
}

[ByRefEvent]
public readonly record struct RefilledSolutionEvent();
