using Content.Shared._RMC14.Dropship.Utility.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Dropship.Utility.Events;

/// <summary>
/// Event applied onto a MedevacStretcher to prepare to send a humanoid entity
/// to a target location. DOES NOT MOVE THE HUNANOID, for that <see cref="SharedMedevacSystem.Update(float)"/>>
/// </summary>
[Serializable, NetSerializable]
public sealed partial class PrepareMedevacEvent : EntityEventArgs
{
    /// <summary>
    /// The medevac point
    /// </summary>
    public NetEntity MedevacEntity;

    public bool ReadyForMedevac = false;

    public PrepareMedevacEvent(NetEntity medevacEntity)
    {
        MedevacEntity = medevacEntity;
    }
}
