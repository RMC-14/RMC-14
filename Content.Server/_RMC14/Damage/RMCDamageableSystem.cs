using System.Diagnostics.CodeAnalysis;
using Content.Server.Chat.Systems;
using Content.Server.Destructible;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Xenonids.Charge;
using Content.Shared.Chat.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Damage;

public sealed class RMCDamageableSystem : SharedRMCDamageableSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DestructibleSystem _destructible = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<XenoToggleChargingDamageComponent, MapInitEvent>(OnXenoToggleChargingDamageMapInit);
    }

    private void OnXenoToggleChargingDamageMapInit(Entity<XenoToggleChargingDamageComponent> ent, ref MapInitEvent args)
    {
        if (!TryGetDestroyedAt(ent, out var destroyed))
            return;

        ent.Comp.DestroyDamage = destroyed.Value;
        Dirty(ent);
    }

    protected override void DoEmote(EntityUid ent, ProtoId<EmotePrototype> emote)
    {
        _chat.TryEmoteWithoutChat(ent, emote);
    }

    public override bool TryGetDestroyedAt(EntityUid destructible, [NotNullWhen(true)] out FixedPoint2? destroyed)
    {
        return _destructible.TryGetDestroyedAt(destructible, out destroyed);
    }
}
