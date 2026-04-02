using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.HiveTeam;

/// <summary>
/// Added to xenos that belong to a hive team, carrying their 1-based team number for the HUD overlay.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HiveTeamMemberComponent : Component
{
    /// <summary>1-based team number (1–4).</summary>
    [DataField, AutoNetworkedField]
    public int TeamNumber;
}
