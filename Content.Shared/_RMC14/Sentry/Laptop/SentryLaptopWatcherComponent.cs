using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Sentry.Laptop;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSentryLaptopSystem))]
public sealed partial class SentryLaptopWatcherComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Laptop;

    [DataField, AutoNetworkedField]
    public NetEntity? CurrentSentry;
}
