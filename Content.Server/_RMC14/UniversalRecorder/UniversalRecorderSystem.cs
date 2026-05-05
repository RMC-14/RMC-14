using System.Text;
using Content.Server.Atmos;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Speech;
using Content.Server.Speech.Components;
using Content.Shared._RMC14.UniversalRecorder;
using Content.Shared.Atmos;
using Content.Shared.Chat;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Explosion;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.Tools.Systems;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Server.GameObjects;

namespace Content.Server._RMC14.UniversalRecorder;

public sealed class UniversalRecorderSystem : EntitySystem
{
    private static readonly AudioParams PlaybackHissAudioParams = AudioParams.Default
        .WithVolume(-8f)
        .WithMaxDistance(7f);

    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<UniversalRecorderComponent, ComponentInit>(OnRecorderInit);
        SubscribeLocalEvent<UniversalRecorderComponent, MapInitEvent>(OnRecorderMapInit);
        SubscribeLocalEvent<UniversalRecorderComponent, ComponentRemove>(OnRecorderRemove);
        SubscribeLocalEvent<UniversalRecorderComponent, ExaminedEvent>(OnRecorderExamined);
        SubscribeLocalEvent<UniversalRecorderComponent, UseInHandEvent>(OnRecorderUseInHand);
        SubscribeLocalEvent<UniversalRecorderComponent, InteractUsingEvent>(OnRecorderInteractUsing);
        SubscribeLocalEvent<UniversalRecorderComponent, GetVerbsEvent<AlternativeVerb>>(OnRecorderGetVerbs);
        SubscribeLocalEvent<UniversalRecorderComponent, UniversalRecorderRecorderActionBuiMsg>(OnRecorderBuiAction);
        SubscribeLocalEvent<UniversalRecorderComponent, ListenEvent>(OnRecorderListen);
        SubscribeLocalEvent<UniversalRecorderComponent, EntInsertedIntoContainerMessage>(OnRecorderTapeInserted);
        SubscribeLocalEvent<UniversalRecorderComponent, EntRemovedFromContainerMessage>(OnRecorderTapeRemoved);
        SubscribeLocalEvent<UniversalRecorderComponent, EntityTerminatingEvent>(OnRecorderTerminating);
        SubscribeLocalEvent<UniversalRecorderComponent, IgnitedEvent>(OnRecorderIgnited);
        SubscribeLocalEvent<UniversalRecorderComponent, TileFireEvent>(OnRecorderTileFire);
        SubscribeLocalEvent<UniversalRecorderComponent, BeforeExplodeEvent>(OnRecorderExploded);

        SubscribeLocalEvent<UniversalRecorderTapeComponent, MapInitEvent>(OnTapeMapInit);
        SubscribeLocalEvent<UniversalRecorderTapeComponent, ExaminedEvent>(OnTapeExamined);
        SubscribeLocalEvent<UniversalRecorderTapeComponent, UseInHandEvent>(OnTapeUseInHand);
        SubscribeLocalEvent<UniversalRecorderTapeComponent, InteractUsingEvent>(OnTapeInteractUsing);
        SubscribeLocalEvent<UniversalRecorderTapeComponent, GetVerbsEvent<AlternativeVerb>>(OnTapeGetVerbs);
        SubscribeLocalEvent<UniversalRecorderTapeComponent, UniversalRecorderTapeActionBuiMsg>(OnTapeBuiAction);
        SubscribeLocalEvent<UniversalRecorderTapeComponent, BoundUserInterfaceCheckRangeEvent>(OnTapeUiRangeCheck);
        SubscribeLocalEvent<UniversalRecorderTapeComponent, UniversalRecorderTapeRespoolDoAfterEvent>(OnTapeRespoolDoAfter);
        SubscribeLocalEvent<UniversalRecorderTapeComponent, IgnitedEvent>(OnTapeIgnited);
        SubscribeLocalEvent<UniversalRecorderTapeComponent, TileFireEvent>(OnTapeTileFire);
        SubscribeLocalEvent<UniversalRecorderTapeComponent, BeforeExplodeEvent>(OnTapeExploded);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<UniversalRecorderComponent, UniversalRecorderRuntimeComponent>();
        while (query.MoveNext(out var uid, out var recorder, out var runtime))
        {
            var ent = (uid, recorder);
            switch (runtime.State)
            {
                case UniversalRecorderState.Recording:
                    UpdateRecording(ent);
                    break;
                case UniversalRecorderState.Playing:
                    UpdatePlayback(ent);
                    break;
            }
        }
    }

    private void OnRecorderInit(Entity<UniversalRecorderComponent> ent, ref ComponentInit args)
    {
        EnsureComp<UniversalRecorderRuntimeComponent>(ent);
        _itemSlots.AddItemSlot(ent, UniversalRecorderComponent.TapeSlotId, ent.Comp.TapeSlot);
    }

    private void OnRecorderMapInit(Entity<UniversalRecorderComponent> ent, ref MapInitEvent args)
    {
        UpdateAppearance(ent);
    }

    private void OnRecorderRemove(Entity<UniversalRecorderComponent> ent, ref ComponentRemove args)
    {
        _itemSlots.RemoveItemSlot(ent, ent.Comp.TapeSlot);
    }

    private void OnTapeMapInit(Entity<UniversalRecorderTapeComponent> ent, ref MapInitEvent args)
    {
        EnsureComp<UniversalRecorderTapeRuntimeComponent>(ent);
        EnsureTapeNames(ent);
        if (_random.Prob(0.5f))
            FlipTape(ent, playSound: false, popupUser: null);
        else
            UpdateTapeAppearance(ent);
    }

    private void OnRecorderExamined(Entity<UniversalRecorderComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(UniversalRecorderComponent)))
        {
            args.PushMarkup(Loc.GetString("rmc-universal-recorder-examine-actions"));

            if (!TryGetTape(ent, out var tape))
            {
                args.PushMarkup(Loc.GetString("rmc-universal-recorder-examine-display", ("value", Loc.GetString("rmc-universal-recorder-readout-empty"))));
                return;
            }

            var tapeRuntime = GetTapeRuntime(tape);
            args.PushMarkup(Loc.GetString("rmc-universal-recorder-examine-tape", ("tape", tape.Owner)));
            args.PushMarkup(Loc.GetString("rmc-universal-recorder-examine-display", ("value", GetReadout(ent, tapeRuntime))));

            if (tapeRuntime.Unspooled)
                args.PushMarkup(Loc.GetString("rmc-universal-recorder-examine-broken"));
        }
    }

    private void OnTapeExamined(Entity<UniversalRecorderTapeComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(UniversalRecorderTapeComponent)))
        {
            var tapeRuntime = GetTapeRuntime(ent);
            args.PushMarkup(Loc.GetString("rmc-universal-recorder-tape-side", ("side", GetSideName(tapeRuntime.Side))));

            if (tapeRuntime.Unspooled)
            {
                args.PushMarkup(Loc.GetString("rmc-universal-recorder-tape-unspooled"));
                return;
            }

            var usedPercent = ent.Comp.MaxCapacity == TimeSpan.Zero
                ? 0
                : (int) MathF.Floor((float) (tapeRuntime.UsedCapacity.TotalSeconds / ent.Comp.MaxCapacity.TotalSeconds * 100f));

            var key = usedPercent switch
            {
                <= 5 => "rmc-universal-recorder-tape-unused",
                <= 25 => "rmc-universal-recorder-tape-bit-used",
                <= 50 => "rmc-universal-recorder-tape-under-half",
                <= 75 => "rmc-universal-recorder-tape-over-half",
                <= 90 => "rmc-universal-recorder-tape-almost-full",
                <= 99 => "rmc-universal-recorder-tape-tiny-bit-left",
                _ => "rmc-universal-recorder-tape-fully-used",
            };

            args.PushMarkup(Loc.GetString(key));
        }
    }

    private void OnRecorderUseInHand(Entity<UniversalRecorderComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (!TryGetTape(ent, out var tape))
        {
            _popup.PopupEntity(Loc.GetString("rmc-universal-recorder-popup-no-tape"), ent.Owner, args.User);
            args.Handled = true;
            return;
        }

        var tapeRuntime = GetTapeRuntime(tape);
        if (tapeRuntime.Unspooled)
        {
            _popup.PopupEntity(Loc.GetString("rmc-universal-recorder-popup-broken"), ent.Owner, args.User);
            args.Handled = true;
            return;
        }

        args.Handled = TryOpenRecorderUi(ent, args.User);
    }

    private void OnRecorderInteractUsing(Entity<UniversalRecorderComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !HasComp<UniversalRecorderTapeComponent>(args.Used))
            return;

        if (TryGetTape(ent, out _))
            return;

        if (!_itemSlots.TryInsertFromHand(ent.Owner, ent.Comp.TapeSlot, args.User, excludeUserAudio: true))
            return;

        _popup.PopupEntity(
            Loc.GetString("rmc-universal-recorder-popup-insert", ("tape", args.Used)),
            ent.Owner,
            args.User);
        args.Handled = true;
    }

    private void OnRecorderGetVerbs(Entity<UniversalRecorderComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !TryGetTape(ent, out var tape))
            return;

        var user = args.User;
        var runtime = GetRecorderRuntime(ent);
        var tapeRuntime = GetTapeRuntime(tape);

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("rmc-universal-recorder-verb-controls"),
            Priority = 3,
            Act = () => TryOpenRecorderUi(ent, user),
        });

        if (runtime.State == UniversalRecorderState.Playing || runtime.State == UniversalRecorderState.Recording)
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString("rmc-universal-recorder-verb-stop"),
                Priority = 2,
                Act = () => Stop(ent),
            });
        }
        else if (!tapeRuntime.Unspooled)
        {
            if (tapeRuntime.Entries.Count > 0)
            {
                args.Verbs.Add(new AlternativeVerb
                {
                    Text = Loc.GetString("rmc-universal-recorder-verb-play"),
                    Priority = 2,
                    Act = () => StartPlayback(ent, user),
                });

                args.Verbs.Add(new AlternativeVerb
                {
                    Text = Loc.GetString("rmc-universal-recorder-verb-print"),
                    Act = () => PrintTranscript(ent, user),
                });
            }

            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString("rmc-universal-recorder-verb-record"),
                Priority = tapeRuntime.Entries.Count == 0 ? 1 : 0,
                Act = () => StartRecording(ent, user),
            });
        }

    }

    private void OnTapeUseInHand(Entity<UniversalRecorderTapeComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (GetTapeRuntime(ent).Unspooled)
        {
            FlipTape(ent, popupUser: args.User);
            args.Handled = true;
            return;
        }

        args.Handled = TryOpenTapeUi(ent, args.User);
    }

    private void OnTapeBuiAction(Entity<UniversalRecorderTapeComponent> ent, ref UniversalRecorderTapeActionBuiMsg args)
    {
        switch (args.Action)
        {
            case UniversalRecorderTapeAction.Flip:
                FlipTape(ent, popupUser: args.Actor);
                break;
            case UniversalRecorderTapeAction.Unwind:
                UnspoolTape(ent, args.Actor, true);
                break;
        }
    }

    private void OnTapeUiRangeCheck(Entity<UniversalRecorderTapeComponent> ent, ref BoundUserInterfaceCheckRangeEvent args)
    {
        if (args.Result == BoundUserInterfaceRangeResult.Fail || args.UiKey is not UniversalRecorderUiKey.Tape)
            return;

        if (!_hands.IsHolding(args.Actor.Owner, ent.Owner))
            args.Result = BoundUserInterfaceRangeResult.Fail;
    }

    private void OnRecorderBuiAction(Entity<UniversalRecorderComponent> ent, ref UniversalRecorderRecorderActionBuiMsg args)
    {
        switch (args.Action)
        {
            case UniversalRecorderRecorderAction.Record:
                StartRecording(ent, args.Actor);
                break;
            case UniversalRecorderRecorderAction.Play:
                StartPlayback(ent, args.Actor);
                break;
            case UniversalRecorderRecorderAction.Stop:
                Stop(ent);
                break;
            case UniversalRecorderRecorderAction.PrintTranscript:
                PrintTranscript(ent, args.Actor);
                break;
            case UniversalRecorderRecorderAction.Eject:
                if (TryGetTape(ent, out var tape) &&
                    _itemSlots.TryEjectToHands(ent, ent.Comp.TapeSlot, args.Actor, excludeUserAudio: true))
                {
                    _popup.PopupEntity(
                        Loc.GetString("rmc-universal-recorder-popup-eject", ("tape", tape.Owner)),
                        ent.Owner,
                        args.Actor);
                }
                break;
        }
    }

    private bool TryOpenRecorderUi(Entity<UniversalRecorderComponent> ent, EntityUid user)
    {
        if (!TryGetTape(ent, out var tape))
        {
            _popup.PopupEntity(Loc.GetString("rmc-universal-recorder-popup-no-tape"), ent.Owner, user);
            return false;
        }

        if (GetTapeRuntime(tape).Unspooled)
        {
            _popup.PopupEntity(Loc.GetString("rmc-universal-recorder-popup-broken"), ent.Owner, user);
            return false;
        }

        var actions = GetRecorderRadialActions(ent);
        if (actions.Count == 0)
            return false;

        _ui.SetUiState(ent.Owner, UniversalRecorderUiKey.Recorder, new UniversalRecorderRecorderBuiState(actions.ToArray()));
        return _ui.TryToggleUi(ent.Owner, UniversalRecorderUiKey.Recorder, user);
    }

    private bool TryOpenTapeUi(Entity<UniversalRecorderTapeComponent> ent, EntityUid user)
    {
        var actions = GetTapeRadialActions(ent);
        if (actions.Count == 0)
            return false;

        _ui.SetUiState(ent.Owner, UniversalRecorderUiKey.Tape, new UniversalRecorderTapeBuiState(actions.ToArray()));
        return _ui.TryToggleUi(ent.Owner, UniversalRecorderUiKey.Tape, user);
    }

    private List<UniversalRecorderRecorderAction> GetRecorderRadialActions(Entity<UniversalRecorderComponent> ent)
    {
        var actions = new List<UniversalRecorderRecorderAction>();
        var runtime = GetRecorderRuntime(ent);

        if (!TryGetTape(ent, out var tape))
            return actions;

        var tapeRuntime = GetTapeRuntime(tape);
        if (tapeRuntime.Unspooled)
            return actions;

        if (runtime.State == UniversalRecorderState.Stopped)
        {
            actions.Add(UniversalRecorderRecorderAction.Record);
            actions.Add(UniversalRecorderRecorderAction.Play);

            if (_timing.CurTime >= runtime.NextPrintAt && tapeRuntime.Entries.Count > 0)
                actions.Add(UniversalRecorderRecorderAction.PrintTranscript);
        }

        if (runtime.State is UniversalRecorderState.Playing or UniversalRecorderState.Recording)
            actions.Add(UniversalRecorderRecorderAction.Stop);

        actions.Add(UniversalRecorderRecorderAction.Eject);
        return actions;
    }

    private List<UniversalRecorderTapeAction> GetTapeRadialActions(Entity<UniversalRecorderTapeComponent> ent)
    {
        var actions = new List<UniversalRecorderTapeAction>
        {
            UniversalRecorderTapeAction.Flip,
        };

        if (!GetTapeRuntime(ent).Unspooled)
            actions.Add(UniversalRecorderTapeAction.Unwind);

        return actions;
    }

    private void OnTapeGetVerbs(Entity<UniversalRecorderTapeComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !TryComp<HandsComponent>(args.User, out var hands) || !_hands.IsHolding((args.User, hands), ent.Owner))
            return;

        var user = args.User;
        if (!GetTapeRuntime(ent).Unspooled)
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString("rmc-universal-recorder-verb-controls"),
                Priority = 3,
                Act = () => TryOpenTapeUi(ent, user),
            });
        }

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("rmc-universal-recorder-tape-verb-flip"),
            Priority = 2,
            Act = () => FlipTape(ent, popupUser: user),
        });

        if (!GetTapeRuntime(ent).Unspooled)
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString("rmc-universal-recorder-tape-verb-unwind"),
                Priority = 1,
                Act = () => UnspoolTape(ent, user, true),
            });
        }
    }

    private void OnTapeInteractUsing(Entity<UniversalRecorderTapeComponent> ent, ref InteractUsingEvent args)
    {
        var runtime = GetTapeRuntime(ent);
        if (args.Handled || !runtime.Unspooled)
            return;

        if (!_tool.HasQuality(args.Used, ent.Comp.ScrewdriverQuality))
            return;

        _popup.PopupEntity(Loc.GetString("rmc-universal-recorder-tape-popup-respool-start"), ent.Owner, args.User);
        args.Handled = _tool.UseTool(args.Used,
            args.User,
            ent.Owner,
            (float) ent.Comp.RespoolTime.TotalSeconds,
            ent.Comp.ScrewdriverQuality,
            new UniversalRecorderTapeRespoolDoAfterEvent());
    }

    private void OnTapeRespoolDoAfter(Entity<UniversalRecorderTapeComponent> ent, ref UniversalRecorderTapeRespoolDoAfterEvent args)
    {
        if (args.Cancelled)
        {
            _popup.PopupEntity(Loc.GetString("rmc-universal-recorder-tape-popup-respool-cancel"), ent.Owner, args.User);
            return;
        }

        RespoolTape(ent);
        _popup.PopupEntity(Loc.GetString("rmc-universal-recorder-tape-popup-respool-finish"), ent.Owner, args.User);
    }

    private void OnRecorderListen(Entity<UniversalRecorderComponent> ent, ref ListenEvent args)
    {
        var runtime = GetRecorderRuntime(ent);
        if (runtime.State != UniversalRecorderState.Recording)
            return;

        if (!TryGetTape(ent, out var tape))
        {
            Stop(ent, suppressStatus: true);
            return;
        }

        var tapeRuntime = GetTapeRuntime(tape);
        if (tapeRuntime.Unspooled)
        {
            Stop(ent, suppressStatus: true);
            return;
        }

        var currentDuration = GetCurrentRecordedDuration(tape.Comp, runtime);
        if (currentDuration >= tape.Comp.MaxCapacity)
        {
            tapeRuntime.UsedCapacity = tape.Comp.MaxCapacity;
            SendRecorderNotice(ent, Loc.GetString("rmc-universal-recorder-popup-full"));
            Stop(ent);
            return;
        }

        var speech = _chat.GetSpeechVerb(args.Source, args.Message);
        var nameEv = new TransformSpeakerNameEvent(args.Source, Name(args.Source));
        RaiseLocalEvent(args.Source, nameEv);
        if (nameEv.SpeechVerb != null && _prototype.TryIndex(nameEv.SpeechVerb, out var overrideSpeech))
            speech = overrideSpeech;

        var speechVerb = Loc.GetString(_random.Pick(speech.SpeechVerbStrings));
        var line = FormatTranscriptLine(currentDuration, nameEv.VoiceName, speechVerb, args.Message);

        tapeRuntime.Entries.Add(new RecorderEntry(
            currentDuration,
            nameEv.VoiceName,
            speechVerb,
            args.Message,
            speech.FontId,
            speech.FontSize,
            speech.Bold,
            line));
        tapeRuntime.UsedCapacity = currentDuration;
    }

    private void OnRecorderTapeInserted(Entity<UniversalRecorderComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        OnRecorderTapeSlotChanged(ent, args.Container.ID);
    }

    private void OnRecorderTapeRemoved(Entity<UniversalRecorderComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        OnRecorderTapeSlotChanged(ent, args.Container.ID);
    }

    private void OnRecorderTapeSlotChanged(Entity<UniversalRecorderComponent> ent, string containerId)
    {
        if (containerId != UniversalRecorderComponent.TapeSlotId)
            return;

        if (!TryGetTape(ent, out _))
            Stop(ent, suppressStatus: true);

        UpdateAppearance(ent);
    }

    private void OnRecorderTerminating(Entity<UniversalRecorderComponent> ent, ref EntityTerminatingEvent args)
    {
        Stop(ent, suppressStatus: true);
    }

    private void OnRecorderIgnited(Entity<UniversalRecorderComponent> ent, ref IgnitedEvent args)
    {
        BreakInsertedTape(ent);
    }

    private void OnRecorderTileFire(Entity<UniversalRecorderComponent> ent, ref TileFireEvent args)
    {
        BreakInsertedTape(ent);
    }

    private void OnRecorderExploded(Entity<UniversalRecorderComponent> ent, ref BeforeExplodeEvent args)
    {
        BreakInsertedTape(ent);
    }

    private void OnTapeIgnited(Entity<UniversalRecorderTapeComponent> ent, ref IgnitedEvent args)
    {
        UnspoolTape(ent);
    }

    private void OnTapeTileFire(Entity<UniversalRecorderTapeComponent> ent, ref TileFireEvent args)
    {
        UnspoolTape(ent);
    }

    private void OnTapeExploded(Entity<UniversalRecorderTapeComponent> ent, ref BeforeExplodeEvent args)
    {
        UnspoolTape(ent);
    }

    private bool StartRecording(Entity<UniversalRecorderComponent> ent, EntityUid user)
    {
        var runtime = GetRecorderRuntime(ent);
        if (runtime.State != UniversalRecorderState.Stopped)
            return false;

        if (!TryGetTape(ent, out var tape))
        {
            _popup.PopupEntity(Loc.GetString("rmc-universal-recorder-popup-no-tape"), ent.Owner, user);
            return false;
        }

        var tapeRuntime = GetTapeRuntime(tape);
        if (tapeRuntime.Unspooled)
        {
            _popup.PopupEntity(Loc.GetString("rmc-universal-recorder-popup-broken"), ent.Owner, user);
            return false;
        }

        if (tapeRuntime.UsedCapacity >= tape.Comp.MaxCapacity)
        {
            SendRecorderNotice(ent, Loc.GetString("rmc-universal-recorder-popup-full"));
            return false;
        }

        runtime.RecordingBaseOffset = tapeRuntime.UsedCapacity;
        runtime.RecordingStartedAt = _timing.CurTime;
        runtime.WarningSent = false;
        runtime.PendingSilenceSeconds = null;
        runtime.WaitingForHissLoop = false;
        runtime.HissLoopStartAt = TimeSpan.Zero;
        runtime.PlaybackStartStream = null;
        runtime.PlaybackIndex = 0;
        runtime.State = UniversalRecorderState.Recording;

        EnsureComp<ActiveListenerComponent>(ent).Range = ent.Comp.ListenRange;

        _audio.PlayPvs(ent.Comp.PlaySound, ent);
        SendRecorderNotice(ent, Loc.GetString("rmc-universal-recorder-popup-recording-start"));
        UpdateAppearance(ent);
        return true;
    }

    private bool StartPlayback(Entity<UniversalRecorderComponent> ent, EntityUid user)
    {
        var runtime = GetRecorderRuntime(ent);
        if (runtime.State != UniversalRecorderState.Stopped)
            return false;

        if (!TryGetTape(ent, out var tape))
        {
            _popup.PopupEntity(Loc.GetString("rmc-universal-recorder-popup-no-tape"), ent.Owner, user);
            return false;
        }

        var tapeRuntime = GetTapeRuntime(tape);
        if (tapeRuntime.Unspooled)
        {
            _popup.PopupEntity(Loc.GetString("rmc-universal-recorder-popup-broken"), ent.Owner, user);
            return false;
        }

        if (tapeRuntime.Entries.Count == 0)
        {
            SendRecorderNotice(ent, Loc.GetString("rmc-universal-recorder-popup-no-data"));
            return false;
        }

        runtime.State = UniversalRecorderState.Playing;
        runtime.PlaybackIndex = 0;
        runtime.PendingSilenceSeconds = null;
        runtime.NextPlaybackAt = _timing.CurTime;
        runtime.WarningSent = false;
        runtime.WaitingForHissLoop = true;
        runtime.HissLoopStartAt = _timing.CurTime + ent.Comp.HissStartDelay;
        runtime.PlaybackStartStream = _audio.PlayPvs(
            ent.Comp.HissStartSound,
            ent,
            PlaybackHissAudioParams)?.Entity;
        runtime.PlaybackStream = null;

        _audio.PlayPvs(ent.Comp.PlaySound, ent);
        SendRecorderNotice(ent, Loc.GetString("rmc-universal-recorder-popup-playback-start"));
        UpdateAppearance(ent);
        return true;
    }

    private void Stop(Entity<UniversalRecorderComponent> ent, bool suppressStatus = false)
    {
        var runtime = GetRecorderRuntime(ent);
        if (runtime.State == UniversalRecorderState.Stopped)
            return;

        if (TryGetTape(ent, out var tape) && runtime.State == UniversalRecorderState.Recording)
            GetTapeRuntime(tape).UsedCapacity = GetCurrentRecordedDuration(tape.Comp, runtime);

        RemCompDeferred<ActiveListenerComponent>(ent);
        runtime.PlaybackStartStream = _audio.Stop(runtime.PlaybackStartStream);
        runtime.PlaybackStream = _audio.Stop(runtime.PlaybackStream);

        var previous = runtime.State;
        runtime.State = UniversalRecorderState.Stopped;
        runtime.WarningSent = false;
        runtime.PendingSilenceSeconds = null;
        runtime.WaitingForHissLoop = false;
        runtime.HissLoopStartAt = TimeSpan.Zero;
        runtime.PlaybackIndex = 0;
        runtime.PlaybackStartStream = null;
        runtime.PlaybackStream = null;

        _audio.PlayPvs(ent.Comp.StopSound, ent);

        if (!suppressStatus)
        {
            var message = previous == UniversalRecorderState.Recording
                ? "rmc-universal-recorder-popup-recording-stop"
                : "rmc-universal-recorder-popup-playback-stop";
            SendRecorderNotice(ent, Loc.GetString(message));
        }

        UpdateAppearance(ent);
    }

    private void UpdateRecording(Entity<UniversalRecorderComponent> ent)
    {
        var runtime = GetRecorderRuntime(ent);
        if (!TryGetTape(ent, out var tape))
        {
            Stop(ent, suppressStatus: true);
            return;
        }

        var tapeRuntime = GetTapeRuntime(tape);
        if (tapeRuntime.Unspooled)
        {
            Stop(ent, suppressStatus: true);
            return;
        }

        var used = GetCurrentRecordedDuration(tape.Comp, runtime);
        tapeRuntime.UsedCapacity = used;

        var remaining = tape.Comp.MaxCapacity - used;
        if (!runtime.WarningSent && remaining <= ent.Comp.WarningThreshold)
        {
            runtime.WarningSent = true;
            SendRecorderNotice(ent,
                Loc.GetString("rmc-universal-recorder-popup-warning",
                    ("seconds", Math.Max(0, (int) remaining.TotalSeconds))));
        }

        if (used < tape.Comp.MaxCapacity)
            return;

        tapeRuntime.UsedCapacity = tape.Comp.MaxCapacity;
        SendRecorderNotice(ent, Loc.GetString("rmc-universal-recorder-popup-full"));
        Stop(ent);
    }

    private void UpdatePlayback(Entity<UniversalRecorderComponent> ent)
    {
        var runtime = GetRecorderRuntime(ent);
        if (_timing.CurTime < runtime.NextPlaybackAt)
            return;

        if (!TryGetTape(ent, out var tape))
        {
            Stop(ent, suppressStatus: true);
            return;
        }

        var tapeRuntime = GetTapeRuntime(tape);
        if (tapeRuntime.Unspooled)
        {
            Stop(ent, suppressStatus: true);
            return;
        }

        UpdatePlaybackHiss(ent, runtime);

        if (runtime.PendingSilenceSeconds is { } silenceSeconds)
        {
            runtime.PendingSilenceSeconds = null;
            SendRecorderNotice(ent, Loc.GetString("rmc-universal-recorder-playback-silence", ("seconds", silenceSeconds)));
            runtime.NextPlaybackAt = _timing.CurTime + TimeSpan.FromSeconds(1);
            return;
        }

        if (runtime.PlaybackIndex >= tapeRuntime.Entries.Count)
        {
            SendRecorderNotice(ent, Loc.GetString("rmc-universal-recorder-popup-end"));
            Stop(ent, suppressStatus: true);
            return;
        }

        var currentIndex = runtime.PlaybackIndex;
        var currentEntry = tapeRuntime.Entries[currentIndex];
        runtime.PlaybackIndex++;

        SendPlaybackSpeech(ent, currentEntry);

        if (runtime.PlaybackIndex >= tapeRuntime.Entries.Count)
        {
            runtime.NextPlaybackAt = _timing.CurTime + TimeSpan.FromSeconds(1);
            return;
        }

        var nextEntry = tapeRuntime.Entries[runtime.PlaybackIndex];
        var delta = nextEntry.Timestamp - currentEntry.Timestamp;
        if (delta > ent.Comp.PlaybackSilenceThreshold)
        {
            runtime.PendingSilenceSeconds = Math.Max(1, (int) Math.Round(delta.TotalSeconds));
            runtime.NextPlaybackAt = _timing.CurTime + TimeSpan.FromSeconds(1);
            return;
        }

        if (delta <= TimeSpan.Zero)
            delta = TimeSpan.FromSeconds(1);

        runtime.NextPlaybackAt = _timing.CurTime + delta;
    }

    private bool PrintTranscript(Entity<UniversalRecorderComponent> ent, EntityUid user)
    {
        var runtime = GetRecorderRuntime(ent);
        if (runtime.State != UniversalRecorderState.Stopped)
            return false;

        if (_timing.CurTime < runtime.NextPrintAt)
        {
            _popup.PopupEntity(Loc.GetString("rmc-universal-recorder-popup-print-cooldown"), ent.Owner, user);
            return false;
        }

        if (!TryGetTape(ent, out var tape))
        {
            _popup.PopupEntity(Loc.GetString("rmc-universal-recorder-popup-no-tape"), ent.Owner, user);
            return false;
        }

        var tapeRuntime = GetTapeRuntime(tape);
        if (tapeRuntime.Entries.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("rmc-universal-recorder-popup-no-data"), ent.Owner, user);
            return false;
        }

        var printed = Spawn(ent.Comp.PrintoutPrototype, Transform(ent).Coordinates);
        if (!TryComp<PaperComponent>(printed, out var paperComp))
        {
            QueueDel(printed);
            return false;
        }

        var text = new StringBuilder();
        text.AppendLine(Loc.GetString("rmc-universal-recorder-transcript-header"));
        text.AppendLine();
        foreach (var entry in tapeRuntime.Entries)
        {
            text.AppendLine(FormattedMessage.EscapeText(entry.TranscriptLine));
        }

        _paper.SetContent((printed, paperComp), text.ToString());
        paperComp.EditingDisabled = true;
        Dirty(printed, paperComp);
        _metaData.SetEntityName(printed, Loc.GetString("rmc-universal-recorder-transcript-name", ("tape", tape.Owner)));
        _hands.TryPickupAnyHand(user, printed);

        runtime.NextPrintAt = _timing.CurTime + ent.Comp.PrintCooldown;
        _audio.PlayPvs(ent.Comp.PrintSound, ent);
        SendRecorderNotice(ent, Loc.GetString("rmc-universal-recorder-popup-print"));
        return true;
    }

    private void BreakInsertedTape(Entity<UniversalRecorderComponent> ent)
    {
        if (!TryGetTape(ent, out var tape))
            return;

        UnspoolTape(tape);
        Stop(ent, suppressStatus: true);
        UpdateAppearance(ent);
    }

    private void FlipTape(Entity<UniversalRecorderTapeComponent> ent, EntityUid? popupUser = null, bool playSound = true)
    {
        var runtime = GetTapeRuntime(ent);
        EnsureTapeNames(ent);

        var currentEntries = runtime.Entries;
        runtime.Entries = runtime.OtherSideEntries;
        runtime.OtherSideEntries = currentEntries;

        var currentCapacity = runtime.UsedCapacity;
        runtime.UsedCapacity = runtime.OtherSideUsedCapacity;
        runtime.OtherSideUsedCapacity = currentCapacity;

        var currentName = Name(ent);
        if (runtime.Side == UniversalRecorderTapeSide.Front)
        {
            runtime.FrontName = currentName;
            runtime.Side = UniversalRecorderTapeSide.Back;
            _metaData.SetEntityName(ent, runtime.BackName ?? currentName);
        }
        else
        {
            runtime.BackName = currentName;
            runtime.Side = UniversalRecorderTapeSide.Front;
            _metaData.SetEntityName(ent, runtime.FrontName ?? currentName);
        }

        if (playSound)
            _audio.PlayPvs(ent.Comp.FlipSound, ent);

        if (popupUser != null)
            _popup.PopupEntity(Loc.GetString("rmc-universal-recorder-tape-popup-flip"), ent.Owner, popupUser.Value);

        UpdateTapeAppearance(ent);
    }

    private void UnspoolTape(Entity<UniversalRecorderTapeComponent> ent, EntityUid? user = null, bool popup = false)
    {
        var runtime = GetTapeRuntime(ent);
        if (runtime.Unspooled)
            return;

        runtime.Unspooled = true;
        UpdateTapeAppearance(ent);

        if (popup && user != null)
            _popup.PopupEntity(Loc.GetString("rmc-universal-recorder-tape-popup-unwind"), ent.Owner, user.Value);
    }

    private void RespoolTape(Entity<UniversalRecorderTapeComponent> ent)
    {
        GetTapeRuntime(ent).Unspooled = false;
        UpdateTapeAppearance(ent);
    }

    private void EnsureTapeNames(Entity<UniversalRecorderTapeComponent> ent)
    {
        var runtime = GetTapeRuntime(ent);
        var currentName = Name(ent);
        runtime.FrontName ??= currentName;
        runtime.BackName ??= currentName;
    }

    private void SendRecorderNotice(Entity<UniversalRecorderComponent> ent, string message)
    {
        _popup.PopupEntity(message, ent.Owner, PopupType.Medium);
    }

    private void SendPlaybackSpeech(Entity<UniversalRecorderComponent> ent, RecorderEntry entry)
    {
        var wrapped = Loc.GetString(
            entry.Bold ? "chat-manager-entity-say-bold-wrap-message" : "chat-manager-entity-say-wrap-message",
            ("entityName", FormattedMessage.EscapeText(entry.SpeakerName)),
            ("verb", entry.SpeechVerb),
            ("fontType", entry.FontId),
            ("fontSize", entry.FontSize),
            ("message", FormattedMessage.EscapeText(entry.Text)));

        var channels = new HashSet<INetChannel>();
        foreach (var recipient in Filter.Pvs(ent.Owner, entityManager: EntityManager).Recipients)
        {
            channels.Add(recipient.Channel);
        }

        if (channels.Count == 0)
            return;

        _chatManager.ChatMessageToMany(
            ChatChannel.Local,
            entry.Text,
            wrapped,
            ent.Owner,
            hideChat: true,
            recordReplay: true,
            channels);
    }

    private TimeSpan GetCurrentRecordedDuration(
        UniversalRecorderTapeComponent tape,
        UniversalRecorderRuntimeComponent runtime)
    {
        var elapsed = _timing.CurTime - runtime.RecordingStartedAt;
        var used = runtime.RecordingBaseOffset + elapsed;
        return used > tape.MaxCapacity ? tape.MaxCapacity : used;
    }

    private string GetReadout(
        Entity<UniversalRecorderComponent> ent,
        UniversalRecorderTapeRuntimeComponent tapeRuntime)
    {
        if (GetRecorderRuntime(ent).State == UniversalRecorderState.Playing)
            return Loc.GetString("rmc-universal-recorder-readout-playing");

        return Loc.GetString("rmc-universal-recorder-readout-time",
            ("duration", FormatDuration(tapeRuntime.UsedCapacity)));
    }

    private string GetSideName(UniversalRecorderTapeSide side)
    {
        var key = side == UniversalRecorderTapeSide.Front
            ? "rmc-universal-recorder-tape-side-front"
            : "rmc-universal-recorder-tape-side-back";

        return Loc.GetString(key);
    }

    private bool TryGetTape(Entity<UniversalRecorderComponent> ent, out Entity<UniversalRecorderTapeComponent> tape)
    {
        tape = default;

        if (!_itemSlots.TryGetSlot(ent, UniversalRecorderComponent.TapeSlotId, out var slot) ||
            slot.Item is not { } item ||
            !TryComp(item, out UniversalRecorderTapeComponent? tapeComp))
        {
            return false;
        }

        tape = (item, tapeComp);
        return true;
    }

    private void UpdateAppearance(Entity<UniversalRecorderComponent> ent)
    {
        var visual = UniversalRecorderVisualState.Empty;
        var runtime = GetRecorderRuntime(ent);

        if (TryGetTape(ent, out _))
        {
            visual = runtime.State switch
            {
                UniversalRecorderState.Recording => UniversalRecorderVisualState.Recording,
                UniversalRecorderState.Playing => UniversalRecorderVisualState.Playing,
                _ => UniversalRecorderVisualState.Idle,
            };
        }

        _appearance.SetData(ent, UniversalRecorderVisuals.State, visual);
        _item.VisualsChanged(ent);
    }

    private void UpdatePlaybackHiss(Entity<UniversalRecorderComponent> ent, UniversalRecorderRuntimeComponent runtime)
    {
        if (!runtime.WaitingForHissLoop ||
            runtime.PlaybackStream != null ||
            _timing.CurTime < runtime.HissLoopStartAt)
        {
            return;
        }

        runtime.WaitingForHissLoop = false;
        runtime.PlaybackStartStream = _audio.Stop(runtime.PlaybackStartStream);
        runtime.PlaybackStream = _audio.PlayPvs(
            ent.Comp.HissLoopSound,
            ent,
            PlaybackHissAudioParams.WithLoop(true))?.Entity;
    }

    private void UpdateTapeAppearance(Entity<UniversalRecorderTapeComponent> ent)
    {
        var runtime = GetTapeRuntime(ent);
        _appearance.SetData(ent, UniversalRecorderTapeVisuals.Side, runtime.Side);
        _appearance.SetData(ent, UniversalRecorderTapeVisuals.Unspooled, runtime.Unspooled);
        _item.VisualsChanged(ent);
    }

    private UniversalRecorderRuntimeComponent GetRecorderRuntime(EntityUid uid)
    {
        return EnsureComp<UniversalRecorderRuntimeComponent>(uid);
    }

    private UniversalRecorderTapeRuntimeComponent GetTapeRuntime(EntityUid uid)
    {
        return EnsureComp<UniversalRecorderTapeRuntimeComponent>(uid);
    }

    private static string FormatTimestamp(TimeSpan timestamp)
    {
        var totalMinutes = (int) timestamp.TotalMinutes;
        return $"[{totalMinutes:00}:{timestamp.Seconds:00}]";
    }

    private static string FormatTranscriptLine(TimeSpan timestamp, string speakerName, string speechVerb, string text)
    {
        return $"{FormatTimestamp(timestamp)} {speakerName} {speechVerb}, \"{text}\"";
    }

    private static string FormatDuration(TimeSpan duration)
    {
        var totalMinutes = (int) duration.TotalMinutes;
        return $"{totalMinutes}m {duration.Seconds}s";
    }
}
