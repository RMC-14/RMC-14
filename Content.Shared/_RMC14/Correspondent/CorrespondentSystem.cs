using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._RMC14.Correspondant;

public sealed class CorrespondentSystem : EntitySystem
{

    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CorrespondentComponent, SelfBeforeGunShotEvent>(BeforeGunShotEvent);
        SubscribeLocalEvent<CorrespondentComponent, CorrespondentShookEvent>(OnShakeAttempt);
    }


    private void BeforeGunShotEvent(Entity<CorrespondentComponent> ent, ref SelfBeforeGunShotEvent args)
    {
        Logger.Debug("Correspondent shoot attempted");
        if (args.Gun.Comp.CorrespondentProof)
            return;

        _stun.TryStun(ent.Owner, ent.Comp.GunShootFailStunTime, true);
        _stun.TryKnockdown(ent.Owner, ent.Comp.GunShootFailStunTime, true);
        _audio.PlayPvs(ent.Comp.GunShootFailSound, ent);

        _popup.PopupEntity(Loc.GetString("correspondent-gun-clumsy"), ent, ent);
        args.Cancel();
    }

    private void OnShakeAttempt(Entity<CorrespondentComponent> ent, ref CorrespondentShookEvent args)
    {
        Logger.Debug("Correspondent shake attempt");
        _stun.TryStun(args.Correspondent , ent.Comp.ShakeFailStunTime, true);
        _stun.TryKnockdown(args.Correspondent, ent.Comp.ShakeFailStunTime, true);
        _popup.PopupEntity(Loc.GetString("correspondent-shake-failed"), ent.Owner, ent.Owner);
    }
}
