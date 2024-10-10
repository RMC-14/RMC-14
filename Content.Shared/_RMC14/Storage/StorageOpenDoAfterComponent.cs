using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Storage;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StorageOpenDoAfterComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public bool SkipInHand = false;

	[DataField, AutoNetworkedField]
	public bool SkipOnGround = false;
}
