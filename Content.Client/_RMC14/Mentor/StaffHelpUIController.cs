using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Content.Client.Administration.Systems;
using Content.Client.Stylesheets;
using Content.Client.UserInterface.Systems.Bwoink;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Mentor;
using Content.Shared.Input;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Configuration;
using Robust.Shared.Input.Binding;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Client._RMC14.Mentor;

public sealed class StaffHelpUIController : UIController, IOnSystemChanged<BwoinkSystem>
{
    [Dependency] private readonly AHelpUIController _aHelp = default!;
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [UISystemDependency] private readonly AudioSystem? _audio = default!;

    private readonly Dictionary<NetUserId, List<MentorMessage>> _messages = new();
    private readonly Dictionary<NetUserId, string> _destinationNames = new();
    private readonly Dictionary<NetUserId, (List<string> People, CancellationTokenSource Cancel)> _peopleTyping = new();

    public bool IsMentor { get; private set; }
    private bool _canReMentor;
    private StaffHelpWindow? _staffHelpWindow;
    private MentorHelpWindow? _mentorHelpWindow;
    private MentorWindow? _mentorWindow;
    private string? _mHelpSound;
    private bool _unread;
    private (TimeSpan Timestamp, bool Typing) _lastTypingUpdateSent;

    public event Action? MentorStatusUpdated;

    public override void Initialize()
    {
        _net.RegisterNetMessage<MentorStatusMsg>(OnMentorStatus);
        _net.RegisterNetMessage<MentorMessagesReceivedMsg>(OnMentorHelpReceived);
        _net.RegisterNetMessage<MentorSendMessageMsg>();
        _net.RegisterNetMessage<MentorHelpMsg>();
        _net.RegisterNetMessage<DeMentorMsg>();
        _net.RegisterNetMessage<ReMentorMsg>();
        _net.RegisterNetMessage<MentorHelpClientTypingUpdated>();
        _net.RegisterNetMessage<MentorHelpTypingUpdated>(OnMentorHelpTypingUpdated);
        _config.OnValueChanged(RMCCVars.RMCMentorHelpSound, v => _mHelpSound = v, true);
    }

    private void OnMentorStatus(MentorStatusMsg msg)
    {
        IsMentor = msg.IsMentor;
        _canReMentor = msg.CanReMentor;

        if (IsMentor)
        {
            if (_mentorHelpWindow is { IsOpen: true })
            {
                _mentorHelpWindow.Close();
                OpenMentorOrHelpWindow();
            }
        }
        else
        {
            if (_mentorWindow is { IsOpen: true })
            {
                _mentorWindow.Close();
                OpenMentorOrHelpWindow();
            }
        }

        MentorStatusUpdated?.Invoke();
    }

    private void OnMentorHelpReceived(MentorMessagesReceivedMsg msg)
    {
        var other = false;
        foreach (var message in msg.Messages)
        {
            if (message.Author != _player.LocalUser)
                other = true;

            if (IsMentor &&
                _mentorWindow is not { IsOpen: true })
            {
                _unread = true;
                _aHelp.UnreadAHelpReceived();
            }

            _destinationNames.TryAdd(message.Destination, message.DestinationName);
            _messages.GetOrNew(message.Destination).Add(message);
            if (_mentorWindow is { IsOpen: true })
            {
                MentorAddPlayerButton(message.Destination);

                if (_mentorWindow.SelectedPlayer == message.Destination)
                {
                    _mentorWindow.Messages.AddMessage(CreateMessageLabel(message));
                    _mentorWindow.Messages.ScrollToBottom();
                }

                continue;
            }

            if (_mentorHelpWindow is { IsOpen: true } &&
                _player.LocalUser == message.Destination)
            {
                _mentorHelpWindow.Messages.AddMessage(CreateMessageLabel(message));
                _mentorHelpWindow.Messages.ScrollToBottom();
            }
        }

        if (other)
        {
            _audio?.PlayGlobal(_mHelpSound, Filter.Local(), false);
            _clyde.RequestWindowAttention();

            if (!IsMentor)
            {
                if (OpenWindow(ref _mentorHelpWindow,
                        CreateMentorHelpWindow,
                        () => _mentorHelpWindow = null,
                        window => window.Chat,
                        _ => _player.LocalUser))
                {
                    _mentorHelpWindow.OpenCentered();
                }
            }
        }
    }

    private void OnMentorHelpTypingUpdated(MentorHelpTypingUpdated message)
    {
        var id = new NetUserId(message.Destination);
        var author = message.Author;
        if (!message.Typing)
        {
            if (_peopleTyping.TryGetValue(id, out var oldTyping))
                oldTyping.People.Remove(author);

            UpdateTypingIndicator();
            return;
        }

        if (!_peopleTyping.TryGetValue(id, out var typing))
        {
            typing = (new List<string>(), new CancellationTokenSource());
            _peopleTyping[id] = typing;
        }

        if (typing.People.Contains(author))
            return;

        typing.People.Add(author);
        typing.Cancel.Cancel();
        typing.Cancel = new CancellationTokenSource();
        Timer.Spawn(TimeSpan.FromSeconds(10),
            () =>
            {
                if (_peopleTyping.TryGetValue(id, out typing))
                    typing.People.Remove(author);

                UpdateTypingIndicator();
            },
            typing.Cancel.Token);

        UpdateTypingIndicator();
    }

    public void OnSystemLoaded(BwoinkSystem system)
    {
        CommandBinds.Builder
            .BindBefore(ContentKeyFunctions.OpenAHelp,
                InputCmdHandler.FromDelegate(_ => ToggleWindow()),
                typeof(AHelpUIController))
            .Register<StaffHelpUIController>();
    }

    public void OnSystemUnloaded(BwoinkSystem system)
    {
        CommandBinds.Unregister<StaffHelpUIController>();
    }

    public void ToggleWindow()
    {
        if (_staffHelpWindow != null)
        {
            _staffHelpWindow.Close();
            _staffHelpWindow = null;
            SetAHelpButtonPressed(false);
            return;
        }

        SetAHelpButtonPressed(true);
        _staffHelpWindow = new StaffHelpWindow();
        _staffHelpWindow.OnClose += () => _staffHelpWindow = null;
        _staffHelpWindow.OpenCentered();
        UIManager.ClickSound();

        if (_unread)
            _staffHelpWindow.MentorHelpButton.AddStyleClass(StyleNano.StyleClassButtonColorRed);

        _staffHelpWindow.AdminHelpButton.OnPressed += _ =>
        {
            _aHelp.Open();
            _staffHelpWindow.Close();
            SetAHelpButtonPressed(false);
        };

        _staffHelpWindow.MentorHelpButton.OnPressed += _ =>
        {
            OpenMentorOrHelpWindow();
            _staffHelpWindow.Close();
        };
    }

    private void OpenMentorOrHelpWindow()
    {
        SetAHelpButtonPressed(false);
        _unread = false;
        if (IsMentor)
        {
            if (OpenWindow(ref _mentorWindow,
                    CreateMentorWindow,
                    () => _mentorWindow = null,
                    window => window.Chat,
                    window => window.SelectedPlayer))
            {
                foreach (var destination in _messages.Keys)
                {
                    MentorAddPlayerButton(destination);
                }

                _mentorWindow.OpenCentered();
            }
        }
        else
        {
            if (OpenWindow(ref _mentorHelpWindow,
                    CreateMentorHelpWindow,
                    () => _mentorHelpWindow = null,
                    window => window.Chat,
                    _ => _player.LocalUser))
            {
                _mentorHelpWindow.OpenCentered();
            }
        }
    }

    private MentorHelpWindow CreateMentorHelpWindow()
    {
        var window = new MentorHelpWindow();
        window.ReMentorButton.OnPressed += _ => _net.ClientSendMessage(new ReMentorMsg());
        window.ReMentorButton.Visible = _canReMentor;
        window.Chat.OnTextEntered += args =>
        {
            window.Chat.Clear();
            if (string.IsNullOrWhiteSpace(args.Text))
                return;

            var msg = new MentorHelpMsg() { Message = args.Text };
            _net.ClientSendMessage(msg);
        };

        if (_player.LocalUser is { } local && _messages.TryGetValue(local, out var messages))
        {
            foreach (var message in messages)
            {
                window.Messages.AddMessage(CreateMessageLabel(message));
                window.Messages.ScrollToBottom();
            }
        }

        return window;
    }

    private MentorWindow CreateMentorWindow()
    {
        var window = new MentorWindow();
        window.DeMentorButton.OnPressed += _ => _net.ClientSendMessage(new DeMentorMsg());
        window.Chat.OnTextEntered += args =>
        {
            var msg = new MentorSendMessageMsg { Message = args.Text, To = window.SelectedPlayer };
            _net.ClientSendMessage(msg);
            window.Chat.Clear();
        };

        return window;
    }

    private void MentorAddPlayerButton(NetUserId player)
    {
        if (_mentorWindow == null)
            return;

        if (_mentorWindow.PlayerDict.TryGetValue(player, out var button))
        {
            button.SetPositionFirst();
            return;
        }

        var playerName = player.ToString();
        if (_destinationNames.TryGetValue(player, out var destinationName))
            playerName = destinationName;

        var playerButton = new Button
        {
            Text = playerName,
            StyleClasses = { "OpenBoth" },
        };
        playerButton.OnPressed += _ =>
        {
            if (_mentorWindow is not { IsOpen: true })
                return;

            _mentorWindow.SelectedPlayer = player;
            _mentorWindow.Messages.Clear();
            _mentorWindow.Chat.Editable = true;
            UpdateTypingIndicator();
            if (!_messages.TryGetValue(player, out var authorMessages))
                return;

            foreach (var message in authorMessages)
            {
                _mentorWindow.Messages.AddMessage(CreateMessageLabel(message));
                _mentorWindow.Messages.ScrollToBottom();
            }
        };

        _mentorWindow.Players.AddChild(playerButton);
        playerButton.SetPositionFirst();
        _mentorWindow.PlayerDict[player] = playerButton;
    }

    private bool OpenWindow<T>(
        [NotNullWhen(true)] ref T? window,
        Func<T> create,
        Action onClose,
        Func<T, LineEdit> edit,
        Func<T, NetUserId?> selectedPlayer
    ) where T : DefaultWindow
    {
        if (window != null)
            return true;

        window = create();
        window.OnClose += onClose;

        var windowCopy = window;
        edit(window).OnTextChanged += args =>
        {
            var destination = selectedPlayer(windowCopy);
            if (destination == null)
                return;

            var typing = args.Text.Length > 0;
            if (_lastTypingUpdateSent.Typing == typing &&
                _lastTypingUpdateSent.Timestamp + TimeSpan.FromSeconds(1) > _timing.RealTime)
            {
                return;
            }

            _lastTypingUpdateSent = (_timing.RealTime, typing);
            _net.ClientSendMessage(new MentorHelpClientTypingUpdated
            {
                Destination = destination.Value,
                Typing = typing,
            });
        };

        return true;
    }

    private FormattedMessage CreateMessageLabel(MentorMessage message)
    {
        var author = message.AuthorName;
        if (message.Author != message.Destination)
        {
            if (message.IsAdmin)
                author = $"[bold][color=red]{author}[/color][/bold]";
            else if (message.IsMentor)
                author = $"[bold][color=orange]{author}[/color][/bold]";
        }

        var text = $"{message.Time:HH:mm} {author}: {FormattedMessage.EscapeText(message.Text)}";
        return FormattedMessage.FromMarkupPermissive(text);
    }

    private void SetAHelpButtonPressed(bool pressed)
    {
        if (_aHelp.GameAHelpButton != null)
            _aHelp.GameAHelpButton.Pressed = pressed;

        if (_aHelp.GameAHelpButton != null)
            _aHelp.GameAHelpButton.Pressed = pressed;
    }

    private void UpdateTypingIndicator()
    {
        if (_mentorWindow is not { IsOpen: true })
            return;

        var players = string.Empty;
        var count = 0;
        if (_peopleTyping.TryGetValue(_mentorWindow.SelectedPlayer, out var typing))
        {
            players = string.Join(", ", typing.People);
            count = typing.People.Count;
        }

        var msg = new FormattedMessage();
        msg.PushColor(Color.LightGray);
        var text = count == 0
            ? string.Empty
            : Loc.GetString("bwoink-system-typing-indicator",
                ("players", players),
                ("count", count));

        msg.AddText(text);
        msg.Pop();

        _mentorWindow.TypingIndicator.SetMessage(msg);
    }
}
