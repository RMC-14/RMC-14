using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;

namespace Content.Shared._RMC14.Xenonids.Inhands;

public sealed class XenoInhandsSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoInhandsComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<XenoInhandsComponent, DidEquipHandEvent>(OnXenoSpritePickedUp);
        SubscribeLocalEvent<XenoInhandsComponent, DidUnequipHandEvent>(OnXenoSpriteDropped);
    }

    private void OnComponentStartup(Entity<XenoInhandsComponent> xeno, ref ComponentStartup component)
    {
        if (!TryComp<HandsComponent>(xeno, out var handsComponent))
            return;

        var hands = (xeno.Owner, handsComponent);
        foreach (var handId in _hands.EnumerateHands(hands))
        {
            if (_hands.TryGetHand(hands, handId, out var h) && h is Hand { } hand)
            {
                _hands.TryGetHeldItem(hands, handId, out var heldItem);
                UpdateHand(xeno, heldItem, hand, heldItem != null);
            }
        }
    }

    public void OnXenoSpritePickedUp(Entity<XenoInhandsComponent> xeno, ref DidEquipHandEvent args)
    {
        UpdateHand(args.User, args.Equipped, args.Hand, true);
    }

    public void OnXenoSpriteDropped(Entity<XenoInhandsComponent> xeno, ref DidUnequipHandEvent args)
    {
        UpdateHand(args.User, args.Unequipped, args.Hand, false);
    }

    private void UpdateHand(EntityUid user, EntityUid? item, Hand hand, bool equipped)
    {
        if (!HasComp<XenoInhandsComponent>(user))
            return;

        if (hand.Location == HandLocation.Middle)
            return;

        var held = string.Empty;
        if (equipped)
        {
            if (TryComp<XenoInhandSpriteComponent>(item, out var inhandSprite))
            {
                held = inhandSprite.StateName ?? string.Empty;
            }
        }

        _appearance.SetData(user,
        hand.Location == HandLocation.Left ? XenoInhandVisuals.LeftHand : XenoInhandVisuals.RightHand,
        held);
    }
}
