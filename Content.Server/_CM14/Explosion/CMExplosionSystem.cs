using Content.Server.Explosion.EntitySystems;
using Content.Server.Popups;
using Content.Shared._CM14.CCVar;
using Content.Shared._CM14.Explosion;
using Content.Shared.Humanoid;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Server._CM14.Explosion;

public sealed class CMExplosionSystem : SharedCMExplosionSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly INetConfigurationManager _config = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CMVocalizeTriggerComponent, ActiveTimerTriggerEvent>(OnVocalizeTriggered);
    }

    private void OnVocalizeTriggered(Entity<CMVocalizeTriggerComponent> ent, ref ActiveTimerTriggerEvent args)
    {
        if (args.User is not { } user)
            return;

        var popup = Loc.GetString(ent.Comp.UserPopup, ("used", ent.Owner));
        _popup.PopupEntity(popup, user, user, PopupType.LargeCaution);

        popup = Loc.GetString(ent.Comp.UserPopup, ("user", user), ("used", ent.Owner));
        _popup.PopupEntity(popup, user, Filter.PvsExcept(user), true, ent.Comp.PopupType);

        var gender = CompOrNull<HumanoidAppearanceComponent>(user)?.Sex ?? Sex.Unsexed;
        if (ent.Comp.Sounds.TryGetValue(gender, out var sound))
        {
            foreach (var session in Filter.Pvs(user).Recipients)
            {
                if (session.AttachedEntity is not { } recipient ||
                    !_config.GetClientCVar(session.Channel, CMCVars.CMPlayHumanoidVoicelines))
                {
                    continue;
                }

                _audio.PlayEntity(sound, recipient, user);
            }
        }
    }
}
