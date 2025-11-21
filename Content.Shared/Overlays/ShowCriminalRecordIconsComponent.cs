using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Overlays;

/// <summary>
///     This component allows you to see criminal record status of mobs.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ShowCriminalRecordIconsComponent : Component
{
	[DataField, AutoNetworkedField]
	public bool Enabled = true;
}
