using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Dropship.Utility;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class DropshipUtilityComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan ActivateDelay = TimeSpan.FromSeconds(2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? NextActivateAt;

    [DataField, AutoNetworkedField]
    public bool ActivateInTransport;

    [DataField, AutoNetworkedField]
    public SkillWhitelist? Skills;

    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi? UtilityAttachedSprite;

    /// <summary>
    /// Cached target of the weapons terminal
    /// </summary>
    [AutoNetworkedField]
    public EntityUid? Target = null;
}

[Serializable, NetSerializable]
public enum DropshipUtilityVisuals
{
    Sprite,
    State,
}
