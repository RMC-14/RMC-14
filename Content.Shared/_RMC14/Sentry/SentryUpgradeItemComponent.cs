using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Sentry;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SentrySystem))]
public sealed partial class SentryUpgradeItemComponent : Component;
