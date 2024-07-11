using Content.Shared._RMC14.Attachable.Components;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared._RMC14.Attachable.Systems;

public sealed class AttachableSilencerSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<AttachableSilencerComponent, GunRefreshModifiersEvent>(OnSilencerRefreshModifiers);
        SubscribeLocalEvent<AttachableSilencerComponent, GunMuzzleFlashAttemptEvent>(OnSilencerMuzzleFlash);
    }

    private void OnSilencerRefreshModifiers(Entity<AttachableSilencerComponent> ent, ref GunRefreshModifiersEvent args)
    {
        args.SoundGunshot = ent.Comp.Sound;
    }

    private void OnSilencerMuzzleFlash(Entity<AttachableSilencerComponent> ent, ref GunMuzzleFlashAttemptEvent args)
    {
        args.Cancelled = true;
    }
}
