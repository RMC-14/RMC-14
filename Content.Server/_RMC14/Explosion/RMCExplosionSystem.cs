using Content.Server.Explosion.EntitySystems;
using Content.Server.Popups;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Voicelines;
using Content.Server.Decals;
using Content.Shared.Humanoid;
using Content.Shared.Popups;
using Content.Shared.Sticky;
using Robust.Server.Audio;
using Robust.Shared.Map;
using Robust.Shared.Player;
using System.Numerics;
using Content.Shared.Decals;
using Robust.Shared.Prototypes;
using System.Linq;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Explosion;

public sealed class RMCExplosionSystem : SharedRMCExplosionSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly HumanoidVoicelinesSystem _humanoidVoicelines = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;
    [Dependency] private readonly DecalSystem _decals = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private string[] _scorchDecals = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CMVocalizeTriggerComponent, ActiveTimerTriggerEvent>(OnVocalizeTriggered);

        SubscribeLocalEvent<RMCExplosiveDeleteWallsComponent, EntityStuckEvent>(OnExplosiveDeleteWallsStuck);

        SubscribeLocalEvent<RMCScorchEffectComponent, CMExplosiveTriggeredEvent>(OnExplosionEffectTriggered);

        CacheDecals();
    }

    private void OnVocalizeTriggered(Entity<CMVocalizeTriggerComponent> ent, ref ActiveTimerTriggerEvent args)
    {
        if (args.User is not { } user)
            return;

        var popup = Loc.GetString(ent.Comp.UserPopup, ("used", ent.Owner));
        _popup.PopupEntity(popup, user, user, PopupType.LargeCaution);

        popup = Loc.GetString(ent.Comp.OthersPopup, ("user", user), ("used", ent.Owner));
        _popup.PopupEntity(popup, user, Filter.PvsExcept(user), true, ent.Comp.PopupType);

        var gender = CompOrNull<HumanoidAppearanceComponent>(user)?.Sex ?? Sex.Unsexed;
        if (!ent.Comp.Sounds.TryGetValue(gender, out var sound))
            return;

        var filter = Filter.Pvs(user).RemoveWhere(s => !_humanoidVoicelines.ShouldPlayVoiceline(user, s));
        if (filter.Count == 0)
            return;

        _audio.PlayEntity(sound, filter, user, true);
    }

    private void OnExplosiveDeleteWallsStuck(Entity<RMCExplosiveDeleteWallsComponent> ent, ref EntityStuckEvent args)
    {
        _trigger.HandleTimerTrigger(ent, args.User, ent.Comp.Delay, ent.Comp.BeepInterval, null, null);
    }

    private void OnExplosionEffectTriggered(Entity<RMCScorchEffectComponent> ent, ref CMExplosiveTriggeredEvent args)
    {
        if (_scorchDecals.Length == 0) return;

        var decalId = _scorchDecals[_random.Next(_scorchDecals.Length)];
        //Decals spawn based on bottom left corner, if bigger decals are used the offset will have to change
        var coords = Transform(ent).Coordinates.Offset(new Vector2(-0.5f, -0.5f));
        _decals.TryAddDecal(decalId, coords, out _, rotation: _random.NextAngle(), cleanable: true);
    }

    public override void QueueExplosion(
        MapCoordinates epicenter,
        string typeId,
        float totalIntensity,
        float slope,
        float maxTileIntensity,
        EntityUid? cause,
        float tileBreakScale = 1f,
        int maxTileBreak = int.MaxValue,
        bool canCreateVacuum = true,
        bool addLog = true)
    {
        _explosion.QueueExplosion(
            epicenter,
            typeId,
            totalIntensity,
            slope,
            maxTileIntensity,
            cause,
            tileBreakScale,
            maxTileBreak,
            canCreateVacuum,
            addLog
        );
    }

    public override void TriggerExplosive(EntityUid uid,
        bool delete = true,
        float? totalIntensity = null,
        float? radius = null,
        EntityUid? user = null)
    {
        _explosion.TriggerExplosive(uid, null, delete, totalIntensity, radius, user);
    }


    private void CacheDecals()
    {
        _scorchDecals = _prototypeManager.EnumeratePrototypes<DecalPrototype>().Where(x => x.Tags.Contains("RMCScorch")).Select(x => x.ID).ToArray();
        if (_scorchDecals.Length == 0)
            Log.Error("Failed to get any decals for RMCScorchEffectComponent. Check that at least one decal has tag RMCScorch.");
    }
}
