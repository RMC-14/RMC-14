using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared.Actions;
using Content.Shared.Audio;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Telephone;

public abstract class SharedTelephoneSystem : EntitySystem
{
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SquadSystem _squad = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private static readonly SoundSpecifier RemotePickupSound = new SoundPathSpecifier("/Audio/_RMC14/Phone/remote_pickup.ogg");
    private static readonly SoundSpecifier RemoteHangupSound = new SoundPathSpecifier("/Audio/_RMC14/Phone/remote_hangup.ogg");
    private static readonly SoundSpecifier BusySound = new SoundPathSpecifier("/Audio/_RMC14/Phone/phone_busy.ogg");

    public override void Initialize()
    {
        SubscribeLocalEvent<RotaryPhoneComponent, MapInitEvent>(OnRotaryPhoneMapInit);
        SubscribeLocalEvent<RotaryPhoneComponent, BeforeActivatableUIOpenEvent>(OnRotaryPhoneBeforeOpen);
        SubscribeLocalEvent<RotaryPhoneComponent, ComponentShutdown>(OnRotaryPhoneTerminating);
        SubscribeLocalEvent<RotaryPhoneComponent, EntityTerminatingEvent>(OnRotaryPhoneTerminating);

        SubscribeLocalEvent<RotaryPhoneDialingComponent, InteractUsingEvent>(OnRotaryPhoneDialingInteractUsing);

        SubscribeLocalEvent<RotaryPhoneReceivingComponent, InteractHandEvent>(OnRotaryPhoneReceivingInteractHand, before: [typeof(ActivatableUISystem)]);
        SubscribeLocalEvent<RotaryPhoneReceivingComponent, InteractUsingEvent>(OnRotaryPhoneReceivingInteractUsing);

        SubscribeLocalEvent<TelephoneComponent, ComponentShutdown>(OnTelephoneTerminating);
        SubscribeLocalEvent<TelephoneComponent, EntityTerminatingEvent>(OnTelephoneTerminating);

        SubscribeLocalEvent<RotaryPhoneBackpackComponent, GetItemActionsEvent>(OnBackpackGetItemActions);
        SubscribeLocalEvent<RotaryPhoneBackpackComponent, TelephoneActionEvent>(OnBackpackTelephoneAction);

        Subs.BuiEvents<RotaryPhoneComponent>(TelephoneUiKey.Key,
            subs =>
            {
                subs.Event<TelephoneCallBuiMsg>(OnTelephoneCallMsg);
            });
    }

    private void OnRotaryPhoneMapInit(Entity<RotaryPhoneComponent> ent, ref MapInitEvent args)
    {
        _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.ContainerId);

        if (!TrySpawnInContainer(ent.Comp.PhoneId, ent, ent.Comp.ContainerId, out var phone))
            return;

        ent.Comp.Phone = phone.Value;
        Dirty(ent);

        if (TryComp(phone, out TelephoneComponent? phoneComp))
        {
            phoneComp.RotaryPhone = ent;
            Dirty(phone.Value, phoneComp);
        }
    }

    private void OnRotaryPhoneBeforeOpen(Entity<RotaryPhoneComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        SendUIState(ent);
    }

    private void OnRotaryPhoneTerminating<T>(Entity<RotaryPhoneComponent> ent, ref T args)
    {
        if (TryComp(ent.Comp.Phone, out TelephoneComponent? phone))
        {
            phone.RotaryPhone = null;
            Dirty(ent.Comp.Phone.Value, phone);
        }
    }

    private void OnRotaryPhoneDialingInteractUsing(Entity<RotaryPhoneDialingComponent> ent, ref InteractUsingEvent args)
    {
        if (HangUpDialing(ent, args.Used, args.User))
            args.Handled = true;
    }

    private void OnRotaryPhoneReceivingInteractHand(Entity<RotaryPhoneReceivingComponent> ent, ref InteractHandEvent args)
    {
        if (_timing.ApplyingState)
            return;

        args.Handled = true;
        PickupReceiving(ent, args.User);
    }

    private void OnRotaryPhoneReceivingInteractUsing(Entity<RotaryPhoneReceivingComponent> ent, ref InteractUsingEvent args)
    {
        if (HangUpReceiving(ent, args.Used, args.User))
            args.Handled = true;
    }

    private void OnTelephoneTerminating<T>(Entity<TelephoneComponent> ent, ref T args)
    {
        if (TryComp(ent.Comp.RotaryPhone, out RotaryPhoneComponent? phone))
        {
            phone.Phone = null;
            Dirty(ent.Comp.RotaryPhone.Value, phone);
        }
    }

    private void OnBackpackGetItemActions(Entity<RotaryPhoneBackpackComponent> ent, ref GetItemActionsEvent args)
    {
        if (args.InHands || (args.SlotFlags & ent.Comp.Slot) == 0)
            return;

        args.AddAction(ref ent.Comp.Action, ent.Comp.ActionId, ent);
    }

    private void OnBackpackTelephoneAction(Entity<RotaryPhoneBackpackComponent> ent, ref TelephoneActionEvent args)
    {
        args.Handled = true;
        if (HasComp<RotaryPhoneDialingComponent>(ent))
            return;

        if (TryComp(ent, out RotaryPhoneReceivingComponent? receiving))
        {
            PickupReceiving((ent, receiving), args.Performer);
            return;
        }

        SendUIState(ent);
        _ui.TryOpenUi(ent.Owner, TelephoneUiKey.Key, args.Performer);
    }

    private void OnTelephoneCallMsg(Entity<RotaryPhoneComponent> ent, ref TelephoneCallBuiMsg args)
    {
        var time = _timing.CurTime;
        if (time < ent.Comp.LastCall + ent.Comp.CallCooldown)
            return;

        _ui.CloseUi(ent.Owner, TelephoneUiKey.Key);

        if (_net.IsClient)
            return;

        if (GetEntity(args.Id) is not { Valid: true } target  ||
            ent.Owner == target ||
            !TryComp(target, out RotaryPhoneComponent? targetRotaryPhone))
        {
            return;
        }

        if (HasComp<RotaryPhoneDialingComponent>(ent))
            return;

        var user = args.Actor;
        if (IsPhoneBusy(target))
        {
            _popup.PopupEntity("That phone is busy!", user, user, PopupType.MediumCaution);
            return;
        }

        if (HasComp<RotaryPhoneBackpackComponent>(target) &&
            !TryGetPhoneBackpackHolder(target, out _))
        {
            _popup.PopupEntity("No transmitters could be located to call!", user, user, PopupType.MediumCaution);
            return;
        }

        ent.Comp.Idle = false;
        ent.Comp.LastCall = time;
        Dirty(ent);

        var dialing = EnsureComp<RotaryPhoneDialingComponent>(ent);
        dialing.Other = target;
        Dirty(ent, dialing);

        var receiving = EnsureComp<RotaryPhoneReceivingComponent>(target);
        receiving.Other = ent;
        Dirty(target, receiving);

        if (_net.IsServer)
        {
            if (ent.Comp.DialingSound is { } dialingSound)
            {
                var selfSound = EnsureComp<AmbientSoundComponent>(ent);
                _ambientSound.SetSound(ent, dialingSound, selfSound);
                _ambientSound.SetRange(ent, 16, selfSound);
                _ambientSound.SetVolume(ent, dialingSound.Params.Volume, selfSound);
            }

            if (ent.Comp.ReceivingSound is { } receivingSound)
            {
                var otherSound = EnsureComp<AmbientSoundComponent>(target);
                _ambientSound.SetSound(target, receivingSound, otherSound);
                _ambientSound.SetRange(target, 16, otherSound);
                _ambientSound.SetVolume(target, receivingSound.Params.Volume, otherSound);
            }
        }

        if (ent.Comp.Phone is { } phone)
            PickupPhone(ent, phone, user);

        UpdateAppearance((ent, ent));
        UpdateAppearance((target, targetRotaryPhone));
    }

    private bool IsPhoneBusy(EntityUid ent)
    {
        return HasComp<RotaryPhoneDialingComponent>(ent) || HasComp<RotaryPhoneReceivingComponent>(ent);
    }

    private void UpdateAppearance(Entity<RotaryPhoneComponent?> phone, bool forceNotRinging = false)
    {
        if (!Resolve(phone, ref phone.Comp, false))
            return;

        var visual = RotaryPhoneVisuals.Base;
        if (!_container.TryGetContainer(phone, phone.Comp.ContainerId, out var container) ||
            container.ContainedEntities.Count == 0)
        {
            visual = RotaryPhoneVisuals.Ear;
        }
        else if (HasComp<RotaryPhoneReceivingComponent>(phone) && !forceNotRinging)
        {
            visual = RotaryPhoneVisuals.Ring;
        }

        _appearance.SetData(phone, RotaryPhoneLayers.Layer, visual);
    }

    protected virtual void PickupPhone(Entity<RotaryPhoneComponent> rotary, EntityUid telephone, EntityUid user)
    {
        if (_container.TryGetContainer(rotary, rotary.Comp.ContainerId, out var container))
            _container.Remove(telephone, container);

        _hands.TryPickupAnyHand(user, telephone);
        EnsureComp<PickedUpPhoneComponent>(telephone);
    }

    private void ReturnPhone(EntityUid rotary, EntityUid telephone, EntityUid? user)
    {
        if (!TryComp(rotary, out RotaryPhoneComponent? rotaryPhone) ||
            rotaryPhone.Phone != telephone ||
            !_container.TryGetContainer(rotary, rotaryPhone.ContainerId, out var container))
        {
            return;
        }

        if (user != null)
            _hands.TryDropIntoContainer(user.Value, telephone, container);
        else
            _container.Insert(telephone, container);
    }

    private void HangUp(EntityUid self, EntityUid other)
    {
        StopSound(self);

        if (!HasComp<RotaryPhoneDialingComponent>(other) &&
            !HasComp<RotaryPhoneReceivingComponent>(other))
        {
            StopSound(other);
            return;
        }

        if (_net.IsServer)
        {
            _ambientSound.SetSound(other, BusySound);
            _ambientSound.SetVolume(other, BusySound.Params.Volume);
        }

        if (!HasPickedUp(other))
            return;

        if (_net.IsServer)
            _audio.PlayPvs(RemoteHangupSound, other);
    }

    private void StopSound(EntityUid ent)
    {
        _ambientSound.SetSound(ent, new SoundPathSpecifier(""));
    }

    protected bool TryGetOtherPhone(EntityUid rotary, out EntityUid other)
    {
        if (TryComp(rotary, out RotaryPhoneDialingComponent? dialing) &&
            TryComp(dialing.Other, out RotaryPhoneComponent? otherRotary) &&
            otherRotary.Phone != null)
        {
            other = otherRotary.Phone.Value;
            return true;
        }
        else if (TryComp(rotary, out RotaryPhoneReceivingComponent? receiving) &&
                 TryComp(receiving.Other, out otherRotary) &&
                 otherRotary.Phone != null)
        {
            other = otherRotary.Phone.Value;
            return true;
        }

        other = default;
        return false;
    }

    private bool IsCorrectPhone(Entity<RotaryPhoneComponent?> rotary, EntityUid phone)
    {
        return Resolve(rotary, ref rotary.Comp, false) && rotary.Comp.Phone == phone;
    }

    private bool HasPickedUp(Entity<RotaryPhoneComponent?, RotaryPhoneReceivingComponent?> receiving)
    {
        return Resolve(receiving, ref receiving.Comp1, ref receiving.Comp2, false) &&
               _container.TryGetContainer(receiving, receiving.Comp1.ContainerId, out var container) &&
               container.ContainedEntities.Count == 0;
    }

    private bool TryGetPhoneBackpackHolder(EntityUid backpack, out EntityUid holder)
    {
        holder = default;
        if (!_container.TryGetContainingContainer((backpack, null, null), out var container))
            return false;

        if (!HasComp<InventoryComponent>(container.Owner))
            return false;

        holder = container.Owner;
        return true;
    }

    private void SendUIState(EntityUid phone)
    {
        if (_net.IsClient)
            return;

        var phones = new List<Phone>();
        var phonesQuery = EntityQueryEnumerator<RotaryPhoneComponent>();
        while (phonesQuery.MoveNext(out var otherId, out var otherComp))
        {
            if (otherId == phone)
                continue;

            var name = GetPhoneName((otherId, otherComp));
            phones.Add(new Phone(GetNetEntity(otherId), otherComp.Category, name));
        }

        _ui.SetUiState(phone, TelephoneUiKey.Key, new TelephoneBuiState(phones));
    }

    private void PickupReceiving(Entity<RotaryPhoneReceivingComponent> receiving, EntityUid user)
    {
        if (TryComp(receiving, out RotaryPhoneComponent? rotaryPhone) &&
            rotaryPhone.Phone is { } phone)
        {
            PickupPhone((receiving, rotaryPhone), phone, user);
        }

        StopSound(receiving);

        if (receiving.Comp.Other is { } other)
        {
            StopSound(other);

            if (_net.IsServer)
                _audio.PlayPvs(RemotePickupSound, other);
        }

        UpdateAppearance((receiving, rotaryPhone));
    }

    protected string GetPhoneName(Entity<RotaryPhoneComponent?> phone)
    {
        var name = Name(phone);
        if (!TryGetPhoneBackpackHolder(phone, out var holder))
            return name;

        name = Name(holder);
        if (TryComp(holder, out JobPrefixComponent? jobPrefix))
            name = $"{jobPrefix.Prefix} {name}";

        if (_squad.TryGetMemberSquad(holder, out var squad))
            name = $"{name} ({Name(squad)})";

        return name;
    }

    private bool HangUpDialing(Entity<RotaryPhoneDialingComponent> ent, EntityUid phone, EntityUid? user)
    {
        if (!IsCorrectPhone(ent.Owner, phone))
            return false;

        RemCompDeferred<RotaryPhoneDialingComponent>(ent);
        ReturnPhone(ent.Owner, phone, user);

        if (ent.Comp.Other is { } other)
        {
            StopSound(other);
            HangUp(ent, other);

            if (!HasPickedUp(other))
            {
                RemCompDeferred<RotaryPhoneReceivingComponent>(other);
                StopSound(other);
            }

            UpdateAppearance(other, true);
        }

        UpdateAppearance(ent.Owner, true);
        return true;
    }

    private bool HangUpReceiving(Entity<RotaryPhoneReceivingComponent> ent, EntityUid used, EntityUid? user)
    {
        if (!IsCorrectPhone(ent.Owner, used))
            return false;

        RemCompDeferred<RotaryPhoneReceivingComponent>(ent);
        ReturnPhone(ent.Owner, used, user);

        if (ent.Comp.Other is { } other)
        {
            HangUp(ent, other);

            if (!HasPickedUp(other))
                RemCompDeferred<RotaryPhoneReceivingComponent>(other);
        }

        UpdateAppearance(ent.Owner, true);
        return true;
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var dialingQuery = EntityQueryEnumerator<RotaryPhoneDialingComponent, RotaryPhoneComponent>();
        while (dialingQuery.MoveNext(out var uid, out var dialing, out var phone))
        {
            if (phone.Idle)
                continue;

            phone.Idle = true;
            Dirty(uid, phone);

            if (dialing.Other is not { } other)
                continue;

            if (!HasComp<RotaryPhoneDialingComponent>(other) ||
                !HasComp<RotaryPhoneReceivingComponent>(other))
            {
                continue;
            }

            if (!HasPickedUp(other) &&
                time > phone.LastCall + phone.DialingIdleDelay &&
                phone.DialingIdleSound is { } sound)
            {
                _ambientSound.SetSound(uid, sound);
                _ambientSound.SetVolume(uid, sound.Params.Volume);
            }
        }

        var pickedUpPhonesQuery = EntityQueryEnumerator<PickedUpPhoneComponent, TelephoneComponent>();
        while (pickedUpPhonesQuery.MoveNext(out var uid, out var pickedUp, out var telephone))
        {
            if (telephone.RotaryPhone is not { } rotary)
                continue;

            void PhoneSnapBackPopup()
            {
                _popup.PopupEntity($"The {Name(uid)} snaps back to the {Name(rotary)}!", uid, PopupType.MediumCaution);
            }

            var phonePosition = _transform.GetMoverCoordinates(uid);
            var rotaryPosition = _transform.GetMoverCoordinates(rotary);
            if (!phonePosition.TryDistance(EntityManager, _transform, rotaryPosition, out var distance) ||
                distance > pickedUp.Range)
            {
                if (TryComp(rotary, out RotaryPhoneDialingComponent? dialing))
                {
                    if (HangUpDialing((rotary, dialing), uid, null))
                        PhoneSnapBackPopup();
                }
                else if (TryComp(rotary, out RotaryPhoneReceivingComponent? receiving))
                {
                    if (HangUpReceiving((rotary, receiving), uid, null))
                        PhoneSnapBackPopup();
                }
            }
        }
    }
}
