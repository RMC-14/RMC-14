using Content.Shared.Flash.Components;
using Content.Shared.StatusEffect;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Flash;

public abstract class SharedFlashSystem : EntitySystem
{
    public ProtoId<StatusEffectPrototype> FlashedKey = "Flashed";

    public virtual void FlashArea(Entity<FlashComponent?> source, EntityUid? user, float range, float duration, float slowTo = 0.8f, bool displayPopup = false, float probability = 1f, SoundSpecifier? sound = null)
    {
    }

    public virtual bool Flash(EntityUid target,
        EntityUid? user,
        EntityUid? used,
        float flashDuration,
        float slowTo = 0.8f,
        bool displayPopup = true,
        bool melee = false,
        TimeSpan? stunDuration = null)
    {
        return false;
    }
}
