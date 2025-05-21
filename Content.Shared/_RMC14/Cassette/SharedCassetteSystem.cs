using Content.Shared._RMC14.Hands;
using Content.Shared.Actions;
using Content.Shared.Clothing;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Cassette;

public abstract class SharedCassetteSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CassettePlayerComponent, GetItemActionsEvent>(OnPlayerGetItemActions);
        SubscribeLocalEvent<CassettePlayerComponent, CassettePlayPauseActionEvent>(OnPlayerPlayPause);
        SubscribeLocalEvent<CassettePlayerComponent, CassetteNextActionEvent>(OnPlayerNext);
        SubscribeLocalEvent<CassettePlayerComponent, CassetteRestartActionEvent>(OnPlayerRestart);
        SubscribeLocalEvent<CassettePlayerComponent, InteractUsingEvent>(OnPlayerInteractUsing);
        SubscribeLocalEvent<CassettePlayerComponent, RMCStorageEjectHandItemEvent>(OnPlayerEjectHand);
        SubscribeLocalEvent<CassettePlayerComponent, GetEquipmentVisualsEvent>(OnPlayerGetEquipmentVisuals, after: [typeof(ClothingSystem)]);
        SubscribeLocalEvent<CassettePlayerComponent, GotUnequippedEvent>(OnPlayerUnequipped);
        SubscribeLocalEvent<CassettePlayerComponent, ExaminedEvent>(OnPlayerExamined);
        SubscribeLocalEvent<CassettePlayerComponent, AfterAutoHandleStateEvent>(OnPlayerState);

        SubscribeLocalEvent<CassetteTapeComponent, ExaminedEvent>(OnTapeExamined);
        SubscribeLocalEvent<CassetteTapeComponent, UseInHandEvent>(OnPlayerUseInHand);
        SubscribeLocalEvent<CassetteTapeComponent, GetVerbsEvent<AlternativeVerb>>(OnTapeGetVerbsAlternative);
    }

    private void OnPlayerGetItemActions(Entity<CassettePlayerComponent> ent, ref GetItemActionsEvent args)
    {
        if (_net.IsClient)
            return;

        if (args.InHands || args.SlotFlags is not { } slots)
            return;

        if (!ent.Comp.Slots.HasFlag(slots))
            return;

        args.AddAction(ref ent.Comp.PlayPauseAction, ent.Comp.PlayPauseActionId);
        args.AddAction(ref ent.Comp.NextAction, ent.Comp.NextActionId);
        args.AddAction(ref ent.Comp.RestartAction, ent.Comp.RestartActionId);
        Dirty(ent);
    }

    private void OnPlayerPlayPause(Entity<CassettePlayerComponent> ent, ref CassettePlayPauseActionEvent args)
    {
        var total = GetTotalSongs(ent);
        var tape = GetTape(ent);

        if (tape is { Comp.CustomTrack: not null })
            total++;

        switch (ent.Comp.State)
        {
            case AudioState.Stopped:
            {
                PlaySong(ent, args.Performer);
                var msg = Loc.GetString("rmc-cassette-playing",
                    ("player", ent),
                    ("current", ent.Comp.Tape + 1),
                    ("total", total));
                _popup.PopupClient(msg, ent, args.Performer);
                break;
            }
            case AudioState.Playing:
            {
                if (_net.IsServer)
                    _audio.SetState(ent.Comp.AudioStream, AudioState.Paused);
                else if (tape is { Comp.Custom: true })
                    _audio.SetState(ent.Comp.CustomAudioStream, AudioState.Paused);

                _popup.PopupClient(Loc.GetString("rmc-cassette-pause", ("player", ent)), ent, args.Performer);
                ent.Comp.State = AudioState.Paused;
                break;
            }
            case AudioState.Paused:
            {
                if (_net.IsServer)
                    _audio.SetState(ent.Comp.AudioStream, AudioState.Playing);
                else if (tape is { Comp.Custom: true })
                    _audio.SetState(ent.Comp.CustomAudioStream, AudioState.Playing);

                var msg = Loc.GetString("rmc-cassette-resume",
                    ("player", ent),
                    ("current", ent.Comp.Tape + 1),
                    ("total", total));
                _popup.PopupClient(msg, ent, args.Performer);
                ent.Comp.State = AudioState.Playing;
                break;
            }
        }

        _audio.PlayLocal(ent.Comp.PlayPauseSound, ent, args.Performer);
    }

    private void OnPlayerNext(Entity<CassettePlayerComponent> ent, ref CassetteNextActionEvent args)
    {
        PlaySong(ent, args.Performer, ent.Comp.Tape + 1);
        _popup.PopupClient(Loc.GetString("rmc-cassette-change"), ent, args.Performer);
    }

    private void OnPlayerRestart(Entity<CassettePlayerComponent> ent, ref CassetteRestartActionEvent args)
    {
        PlaySong(ent, args.Performer);
        _popup.PopupClient(Loc.GetString("rmc-cassette-restart"), ent, args.Performer);
    }

    private void PlaySong(Entity<CassettePlayerComponent> player, EntityUid actor, int? tape = null)
    {
        if (!_container.TryGetContainer(player, player.Comp.ContainerId, out var container) ||
            !container.ContainedEntities.TryFirstOrNull(out var tapeId) ||
            !TryComp(tapeId, out CassetteTapeComponent? tapeComp))
        {
            return;
        }

        _audio.Stop(player.Comp.AudioStream);
        if (tapeComp.Custom)
        {
            _audio.Stop(player.Comp.CustomAudioStream);
            if (PlayCustomTrack(player, (tapeId.Value, tapeComp)) is { } custom)
                player.Comp.CustomAudioStream = custom;

            player.Comp.Tape = 0;
        }
        else if (_net.IsServer)
        {
            tape ??= player.Comp.Tape;
            if (tape < 0 || tape >= tapeComp.Songs.Count)
                tape = 0;

            var song = tapeComp.Songs[tape.Value];
            player.Comp.AudioStream = _audio.PlayGlobal(song, actor, player.Comp.AudioParams)?.Entity;
            player.Comp.Tape = tape.Value;
        }

        player.Comp.State = AudioState.Playing;
        Dirty(player);
        _item.VisualsChanged(player);
    }

    private void OnPlayerInteractUsing(Entity<CassettePlayerComponent> ent, ref InteractUsingEvent args)
    {
        if (!HasComp<CassetteTapeComponent>(args.Used))
            return;

        var slot = _container.EnsureContainer<ContainerSlot>(ent, ent.Comp.ContainerId);
        var contained = slot.ContainedEntity;
        if (contained != null)
            _container.Remove(contained.Value, slot);

        _container.Insert(args.Used, slot);

        if (contained != null)
            _hands.TryPickupAnyHand(args.User, contained.Value);

        _audio.PlayLocal(ent.Comp.InsertEjectSound, ent, args.User);
    }

    private void OnPlayerEjectHand(Entity<CassettePlayerComponent> ent, ref RMCStorageEjectHandItemEvent args)
    {
        if (!_hands.IsHolding(args.User, ent) ||
            !_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container) ||
            !container.ContainedEntities.TryFirstOrNull(out var first) ||
            !_container.Remove(first.Value, container))
        {
            return;
        }

        args.Handled = true;
        _hands.TryPickupAnyHand(args.User, first.Value);
        _audio.PlayLocal(ent.Comp.InsertEjectSound, ent, args.User);
    }

    private void OnPlayerGetEquipmentVisuals(Entity<CassettePlayerComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        args.Layers.Add(("cassette", new PrototypeLayerData
        {
            RsiPath = ent.Comp.WornSprite.RsiPath.ToString(),
            State = ent.Comp.WornSprite.RsiState,
        }));

        if (ent.Comp.State == AudioState.Playing)
        {
            args.Layers.Add(("cassette_music", new PrototypeLayerData
            {
                RsiPath = ent.Comp.MusicSprite.RsiPath.ToString(),
                State = ent.Comp.MusicSprite.RsiState,
            }));
        }
    }

    private void OnPlayerUnequipped(Entity<CassettePlayerComponent> ent, ref GotUnequippedEvent args)
    {
        _audio.Stop(ent.Comp.AudioStream);
        _audio.Stop(ent.Comp.CustomAudioStream);
        ent.Comp.State = AudioState.Stopped;
        Dirty(ent);
    }

    private void OnPlayerExamined(Entity<CassettePlayerComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(CassettePlayerComponent)))
        {
            if (TryGetTape(ent, out var tape))
                args.PushMarkup(Loc.GetString("rmc-cassette-player-examine-tape", ("tape", tape)));
            else
                args.PushMarkup(Loc.GetString("rmc-cassette-player-examine-none"));
        }
    }

    private void OnPlayerState(Entity<CassettePlayerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        _item.VisualsChanged(ent);
    }

    private void OnTapeExamined(Entity<CassetteTapeComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(CassetteTapeComponent)))
        {
            if (ent.Comp.Custom)
                args.PushMarkup(Loc.GetString("rmc-cassette-tape-custom"));
            else
                args.PushMarkup(Loc.GetString("rmc-cassette-tape-examine", ("total", ent.Comp.Songs.Count)));
        }
    }

    protected virtual void OnPlayerUseInHand(Entity<CassetteTapeComponent> tape, ref UseInHandEvent args)
    {
        if (!tape.Comp.Custom)
            return;

        args.Handled = true;
        ChooseCustomTrack(tape);
    }

    private void OnTapeGetVerbsAlternative(Entity<CassetteTapeComponent> tape, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!tape.Comp.Custom)
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("rmc-cassette-tape-custom-choose"),
            Act = () =>
            {
                ChooseCustomTrack(tape);
            },
        });
    }

    private bool TryGetTape(Entity<CassettePlayerComponent> player, out Entity<CassetteTapeComponent> tape)
    {
        tape = default;
        if (!_container.TryGetContainer(player, player.Comp.ContainerId, out var container) ||
            !container.ContainedEntities.TryFirstOrNull(out var first) ||
            !TryComp(first, out CassetteTapeComponent? tapeComp))
        {
            return false;
        }

        tape = (first.Value, tapeComp);
        return true;
    }

    private Entity<CassetteTapeComponent>? GetTape(Entity<CassettePlayerComponent> player)
    {
        return TryGetTape(player, out var tape) ? tape : null;
    }

    private int GetTotalSongs(Entity<CassettePlayerComponent> player)
    {
        return !TryGetTape(player, out var tape)
            ? 0
            : tape.Comp.Songs.Count;
    }

    protected virtual EntityUid? PlayCustomTrack(Entity<CassettePlayerComponent> player, Entity<CassetteTapeComponent> tape)
    {
        return null;
    }

    protected virtual void ChooseCustomTrack(Entity<CassetteTapeComponent> tape)
    {
    }
}
