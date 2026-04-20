using Content.Server.Chat.Systems;
using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Mobs.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Xenonids.Emote;

public sealed class XenoEmoteSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedRMCEmoteSystem _emote = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoComponent, EmoteEvent>(OnXenoEmote);
        SubscribeLocalEvent<XenoComponent, ComponentStartup>(OnXenoStartup);
        SubscribeLocalEvent<CauseEmoteOnProjectileHitComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    private void OnXenoStartup(Entity<XenoComponent> xeno, ref ComponentStartup args)
    {
        if (xeno.Comp.EmoteSounds == null)
            return;

        _proto.TryIndex(xeno.Comp.EmoteSounds, out xeno.Comp.Sounds);
    }

    private void OnXenoEmote(Entity<XenoComponent> xeno, ref EmoteEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = _chat.TryPlayEmoteSound(xeno, xeno.Comp.Sounds, args.Emote);
    }

    private void OnProjectileHit(Entity<CauseEmoteOnProjectileHitComponent> ent, ref ProjectileHitEvent args)
    {
        var comp = ent.Comp;

        var target = args.Target;

        if (_mobState.IsIncapacitated(target))
            return;

        if (!_whitelist.CheckBoth(target, comp.Blacklist, comp.Whitelist))
            return;

        var chance =  Math.Clamp((5f + MathF.Floor((float)args.Damage.GetTotal() / 4f)) / 100f, 0, 1);

        if (_random.Prob(chance))
        {
            var emote = _random.Prob(comp.EmoteChance) ? comp.Emote : comp.RareEmote;
            _emote.TryEmoteWithChat(target, emote, cooldown: comp.EmoteCooldown);
        }
    }
}
