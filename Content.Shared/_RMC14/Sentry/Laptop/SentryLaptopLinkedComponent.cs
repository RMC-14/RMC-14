using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Sentry.Laptop;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSentryLaptopSystem))]
public sealed partial class SentryLaptopLinkedComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? LinkedLaptop;
}
