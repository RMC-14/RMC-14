using Content.Shared._RMC14.Xenonids.Egg;
using Content.Shared._RMC14.Xenonids.Rest;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Inhands;

public sealed class XenoInhandsSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<XenoInhandsComponent, DidEquipHandEvent>(OnXenoSpritePickedUp);
        SubscribeLocalEvent<XenoInhandsComponent, DidUnequipHandEvent>(OnXenoSpriteDropped);

        SubscribeLocalEvent<XenoInhandsComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<XenoInhandsComponent, XenoRestEvent>(OnVisualsRest);
        SubscribeLocalEvent<XenoInhandsComponent, KnockedDownEvent>(OnVisualsKnockedDown);
        SubscribeLocalEvent<XenoInhandsComponent, StatusEffectEndedEvent>(OnVisualsStatusEffectEnded);
        SubscribeLocalEvent<XenoInhandsComponent, XenoOvipositorChangedEvent>(OnVisualsOvipositor);
    }

    public void OnXenoSpritePickedUp(Entity<XenoInhandsComponent> xeno, ref DidEquipHandEvent args)
    {
        UpdateHand(args.User, args.Hand);
    }

    public void OnXenoSpriteDropped(Entity<XenoInhandsComponent> xeno, ref DidUnequipHandEvent args)
    {
        UpdateHand(args.User, args.Hand);
    }
    private void UpdateHand(EntityUid user, Hand hand)
    {
        if (!TryComp<XenoInhandsComponent>(user, out var inhands))
            return;

        if (hand.Location == HandLocation.Middle)
            return;

        string held = string.Empty;

        if (hand.HeldEntity != null)
        {
            if (TryComp<XenoInhandSpriteComponent>(hand.HeldEntity, out var inhandSprite))
            {
                held = inhandSprite.StateName ?? string.Empty;
            }
        }

        _appearance.SetData(user,
        hand.Location == HandLocation.Left ? XenoInhandVisuals.Left : XenoInhandVisuals.Right,
        held);
    }

    private void OnMobStateChanged(Entity<XenoInhandsComponent> xeno, ref MobStateChangedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        _appearance.SetData(xeno, XenoInhandVisuals.Downed, args.NewMobState != MobState.Alive);
    }

    private void OnVisualsRest(Entity<XenoInhandsComponent> xeno, ref XenoRestEvent args)
    {
        if (_timing.ApplyingState)
            return;

        _appearance.SetData(xeno, XenoInhandVisuals.Resting, args.Resting);
    }

    private void OnVisualsKnockedDown(Entity<XenoInhandsComponent> xeno, ref KnockedDownEvent args)
    {
        if (_timing.ApplyingState)
            return;

        _appearance.SetData(xeno, XenoInhandVisuals.Downed, true);
    }

    private void OnVisualsOvipositor(Entity<XenoInhandsComponent> xeno, ref XenoOvipositorChangedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        _appearance.SetData(xeno, XenoInhandVisuals.Ovi, args.Attached);
    }

    private void OnVisualsStatusEffectEnded(Entity<XenoInhandsComponent> xeno, ref StatusEffectEndedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (args.Key == "KnockedDown")
            _appearance.SetData(xeno, XenoInhandVisuals.Downed, false);
    }
}
