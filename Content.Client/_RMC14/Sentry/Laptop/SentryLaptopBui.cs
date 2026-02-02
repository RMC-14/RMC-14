using System.Linq;
using System.Numerics;
using Content.Client.Eye;
using Content.Client.Message;
using Content.Client.Popups;
using Content.Shared._RMC14.Sentry;
using Content.Shared._RMC14.Sentry.Laptop;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Sentry.Laptop;

[UsedImplicitly]
public sealed class SentryLaptopBui : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entities = default!;

    private SentryLaptopWindow? _window;
    private readonly Dictionary<NetEntity, SentryCard> _sentryCards = new();
    private readonly Dictionary<NetEntity, bool> _iffExpanded = new();
    private readonly Dictionary<NetEntity, SentryInfo> _currentInfos = new();
    private readonly Dictionary<string, bool> _globalFactionSelections = new();
    private string _searchText = "";
    private EntityUid? _currentCameraTarget;
    private EntityUid? _cameraEntity;
    private Vector2i? _savedWindowSize;

    public SentryLaptopBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _window = new SentryLaptopWindow();
        _window.OnClose += Close;

        SetupWindowControls();

        if (State is SentryLaptopBuiState state)
            UpdateState(state);

        _window.OpenCentered();
    }

    private void SetupWindowControls()
    {
        if (_window == null)
            return;

        _window.RefreshButton.Button.OnPressed += _ => Refresh();
        _window.UnlinkAllButton.Button.OnPressed += _ => SendMessage(new SentryLaptopUnlinkAllBuiMsg());
        _window.GlobalIFFButton.Button.OnPressed += _ => ToggleGlobalIFFPanel();
        _window.SearchBar.OnTextChanged += args => OnSearchTextChanged(args.Text);
        _window.CloseCameraButton.Button.OnPressed += _ => CloseCamera();
        _window.GlobalApplyIFFButton.Button.OnPressed += _ => ApplyGlobalIFF();

        _window.GlobalResetTargetingButton.Button.OnPressed += _ =>
        {
            _globalFactionSelections.Clear();
            SetupGlobalIFFControls();
            SendMessage(new SentryLaptopGlobalResetTargetingBuiMsg());
        };

        _window.GlobalPowerOnButton.Button.OnPressed += _ =>
        {
            SendMessage(new SentryLaptopGlobalTogglePowerBuiMsg(true));
        };

        _window.GlobalPowerOffButton.Button.OnPressed += _ =>
        {
            SendMessage(new SentryLaptopGlobalTogglePowerBuiMsg(false));
        };
    }

    private void SetupGlobalIFFControls()
    {
        if (_window == null)
            return;

        _window.GlobalFactionContainer.DisposeAllChildren();

        if (State is not SentryLaptopBuiState state || state.AllFactions.Count == 0)
            return;

        var missing = _globalFactionSelections.Keys.Except(state.AllFactions).ToList();
        foreach (var key in missing)
            _globalFactionSelections.Remove(key);

        var selectAllContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Margin = new Thickness(2, 1, 2, 5),
            HorizontalExpand = true
        };

        var selectAllButton = new SentryButton
        {
            Text = "Select All",
            HorizontalExpand = true,
            Margin = new Thickness(0, 0, 2, 0),
            BackgroundColor = Color.FromHex("#1A3D5C"),
            BorderColor = Color.FromHex("#88C7FA"),
            BorderThickness = new Thickness(1)
        };

        var deselectAllButton = new SentryButton
        {
            Text = "Deselect All",
            HorizontalExpand = true,
            Margin = new Thickness(2, 0, 0, 0),
            BackgroundColor = Color.FromHex("#5C1A1A"),
            BorderColor = Color.FromHex("#A42625"),
            BorderThickness = new Thickness(1)
        };

        selectAllButton.Button.OnPressed += _ =>
        {
            foreach (var faction in state.AllFactions)
            {
                _globalFactionSelections[faction] = true;
            }

            SetupGlobalIFFControls();
        };

        deselectAllButton.Button.OnPressed += _ =>
        {
            foreach (var faction in state.AllFactions)
            {
                _globalFactionSelections[faction] = false;
            }

            SetupGlobalIFFControls();
        };

        selectAllContainer.AddChild(selectAllButton);
        selectAllContainer.AddChild(deselectAllButton);
        _window.GlobalFactionContainer.AddChild(selectAllContainer);

        foreach (var faction in state.AllFactions)
        {
            var checkboxContainer = CreateGlobalFactionCheckbox(faction, state);
            _window.GlobalFactionContainer.AddChild(checkboxContainer);
        }
    }

    private BoxContainer CreateGlobalFactionCheckbox(string faction, SentryLaptopBuiState state)
    {
        var container = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Margin = new Thickness(2, 1),
            HorizontalExpand = true,
            MinHeight = 22
        };

        var isSelected = _globalFactionSelections.TryGetValue(faction, out var selected)
            ? selected
            : IsFactionSelectedGlobally(faction, state);

        _globalFactionSelections[faction] = isSelected;

        var checkbox = new CheckBox
        {
            Pressed = isSelected,
            VerticalAlignment = Control.VAlignment.Center
        };

        var displayName = state.FactionNames.GetValueOrDefault(faction, faction);
        var label = new Label
        {
            Text = displayName,
            VerticalAlignment = Control.VAlignment.Center,
            HorizontalExpand = true,
            Margin = new Thickness(5, 0, 0, 0),
            FontColorOverride = GetFactionColor(faction)
        };

        checkbox.OnToggled += args =>
        {
            _globalFactionSelections[faction] = args.Pressed;
        };

        container.AddChild(checkbox);
        container.AddChild(label);

        return container;
    }

    private bool IsFactionSelectedGlobally(string faction, SentryLaptopBuiState state)
    {
        if (state.Sentries.Count == 0)
            return false;

        return state.Sentries.All(s => IsFactionSelected(s.FriendlyFactions, faction, state.AllFactions));
    }

    private bool IsFactionSelected(HashSet<string> friendly, string faction, List<string> allFactions)
    {
        if (faction == "Humanoid")
        {
            var nonXeno = allFactions.Where(f => f != "RMCXeno" && f != "Humanoid").ToList();
            return nonXeno.All(friendly.Contains);
        }

        return friendly.Contains(faction);
    }

    private void ApplyGlobalIFF()
    {
        if (State is not SentryLaptopBuiState state || state.AllFactions.Count == 0)
            return;

        var selected = new List<string>();
        foreach (var faction in state.AllFactions)
        {
            if (_globalFactionSelections.TryGetValue(faction, out var targeted) && targeted)
                selected.Add(faction);
        }

        SendMessage(new SentryLaptopGlobalSetFactionsBuiMsg(selected));
    }

    private void ToggleGlobalIFFPanel()
    {
        if (_window == null)
            return;

        _window.GlobalIFFPanel.Visible = !_window.GlobalIFFPanel.Visible;

        if (_window.GlobalIFFPanel.Visible)
        {
            SetupGlobalIFFControls();
        }
    }

    private void OnSearchTextChanged(string text)
    {
        _searchText = text.ToLower();
        FilterSentryCards();
    }

    private void FilterSentryCards()
    {
        if (string.IsNullOrWhiteSpace(_searchText))
        {
            foreach (var card in _sentryCards.Values)
            {
                card.Visible = true;
            }
            return;
        }

        foreach (var (netEntity, card) in _sentryCards)
        {
            var name = card.SentryName.GetMessage()?.ToLower() ?? string.Empty;
            var location = card.LocationLabel.Text?.ToLower() ?? string.Empty;
            card.Visible = name.Contains(_searchText) || location.Contains(_searchText);
        }
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is SentryLaptopBuiState laptopState)
            UpdateDisplay(laptopState);
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);

        if (message is SentryAlertEvent alert)
            HandleAlert(alert);
    }

    private void HandleAlert(SentryAlertEvent alert)
    {
        if (_window == null)
            return;

        var popup = EntMan.System<PopupSystem>();
        var type = alert.AlertType switch
        {
            SentryAlertType.CriticalHealth => PopupType.LargeCaution,
            SentryAlertType.Damaged => PopupType.MediumCaution,
            SentryAlertType.TargetAcquired => PopupType.MediumCaution,
            SentryAlertType.LowAmmo => PopupType.Medium,
            _ => PopupType.Medium
        };
        popup.PopupEntity(alert.Message, Owner, type);
    }

    private void UpdateDisplay(SentryLaptopBuiState state)
    {
        if (_window == null)
            return;

        var currentIds = state.Sentries.Select(s => s.Id).ToHashSet();
        foreach (var id in _currentInfos.Keys.Where(id => !currentIds.Contains(id)).ToList())
            _currentInfos.Remove(id);
        foreach (var sentry in state.Sentries)
            _currentInfos[sentry.Id] = sentry;

        UpdateHeader(state);
        UpdateStatusLabel(state);
        SetupGlobalIFFControls();
        UpdateSentryCards(state);
        FilterSentryCards();
    }

    private void UpdateHeader(SentryLaptopBuiState state)
    {
        if (_entities.TryGetNetEntity(Owner, out var netEntity) &&
            _entities.TryGetEntity(netEntity, out var laptopEntity) &&
            _entities.TryGetComponent<SentryLaptopComponent>(laptopEntity.Value, out var laptop))
        {
            var sentryCount = state.Sentries.Count;
            var maxSentries = laptop.MaxLinkedSentries;

            _window!.LaptopHeader.SetMarkupPermissive($"[color=#88C7FA][font size=16][bold]SENTRY NETWORK - {sentryCount}/{maxSentries} LINKED[/bold][/font][/color]");
        }
    }

    private void UpdateStatusLabel(SentryLaptopBuiState state)
    {
        int maxSentries = 99;

        if (_entities.TryGetNetEntity(Owner, out var netEntity) &&
            _entities.TryGetEntity(netEntity, out var laptopEnt) &&
            _entities.TryGetComponent<SentryLaptopComponent>(laptopEnt.Value, out var laptop))
        {
            maxSentries = laptop.MaxLinkedSentries;
        }

        if (state.Sentries.Count == 0)
        {
            _window!.StatusLabel.Text = "Status: No sentries linked";
            _window.StatusLabel.FontColorOverride = Color.FromHex("#A42625");
            return;
        }

        var onlineCount = state.Sentries.Count(s => s.Mode == SentryMode.On);
        _window!.StatusLabel.Text = $"Status: {onlineCount}/{state.Sentries.Count} Online";
        _window.StatusLabel.FontColorOverride = onlineCount > 0 ? Color.FromHex("#229132") : Color.FromHex("#CED22B");
    }

    private void UpdateSentryCards(SentryLaptopBuiState state)
    {
        var currentIds = state.Sentries.Select(s => s.Id).ToHashSet();

        var toRemove = _sentryCards.Keys.Where(id => !currentIds.Contains(id)).ToList();
        foreach (var id in toRemove)
        {
            _sentryCards[id].Orphan();
            _sentryCards.Remove(id);
            _iffExpanded.Remove(id);
        }

        foreach (var sentry in state.Sentries)
        {
            if (_sentryCards.TryGetValue(sentry.Id, out var existingCard))
            {
                UpdateSentryCard(existingCard, sentry);
            }
            else
            {
                var card = CreateSentryCard(sentry);
                _sentryCards[sentry.Id] = card;
                _window!.SentryListContainer.AddChild(card);
            }
        }
    }

    private void UpdateSentryCard(SentryCard card, SentryInfo info)
    {
        PopulateSentryBasicInfo(card, info);
        PopulateSentryHealthInfo(card, info);
        PopulateSentryAmmoInfo(card, info);
        PopulateSentryStatusInfo(card, info);
        UpdateFactionControls(card, info);
    }

    private SentryCard CreateSentryCard(SentryInfo info)
    {
        var card = new SentryCard();

        PopulateSentryBasicInfo(card, info);
        PopulateSentryHealthInfo(card, info);
        PopulateSentryAmmoInfo(card, info);
        PopulateSentryStatusInfo(card, info);
        PopulateSentryFactionControls(card, info);
        SetupSentryCardButtons(card, info);

        card.NameLineEdit.OnTextEntered += args =>
        {
            SendMessage(new SentryLaptopSetNameBuiMsg(info.Id, args.Text));
        };

        return card;
    }

    private void PopulateSentryBasicInfo(SentryCard card, SentryInfo info)
    {
        card.SentryName.SetMarkupPermissive($"[color=#88C7FA][bold]{info.Name}[/bold][/color]");

        card.SentryStatus.SetMarkupPermissive(info.Mode switch
        {
            SentryMode.On => "[color=#229132][ONLINE][/color]",
            SentryMode.Off => "[color=#CED22B][OFFLINE][/color]",
            _ => "[color=#A42625][OFFLINE][/color]"
        });

        card.LocationLabel.Text = info.Location;

        if (!string.IsNullOrWhiteSpace(info.CustomName))
        {
            card.NameLineEdit.Text = info.CustomName;
        }
    }

    private void PopulateSentryHealthInfo(SentryCard card, SentryInfo info)
    {
        var healthPercent = info.MaxHealth > 0 ? info.Health / info.MaxHealth : 0;
        card.HealthBar.MinValue = 0;
        card.HealthBar.MaxValue = 1;
        card.HealthBar.Value = healthPercent;
        card.HealthBar.ForegroundStyleBoxOverride = new StyleBoxFlat
        {
            BackgroundColor = GetHealthColor(healthPercent)
        };
        card.HealthLabel.Text = $"{(int)info.Health}/{(int)info.MaxHealth}";
        card.HealthLabel.FontColorOverride = healthPercent > 0.25f ? Color.FromHex("#88C7FA") : Color.FromHex("#A42625");
    }

    private void PopulateSentryAmmoInfo(SentryCard card, SentryInfo info)
    {
        var ammoPercent = info.MaxAmmo > 0 ? (float)info.Ammo / info.MaxAmmo : 0;
        card.AmmoBar.MinValue = 0;
        card.AmmoBar.MaxValue = 1;
        card.AmmoBar.Value = ammoPercent;
        card.AmmoBar.ForegroundStyleBoxOverride = new StyleBoxFlat
        {
            BackgroundColor = GetHealthColor(ammoPercent)
        };
        card.AmmoLabel.Text = $"{info.Ammo}/{info.MaxAmmo}";
        card.AmmoLabel.FontColorOverride = ammoPercent > 0.25f ? Color.FromHex("#88C7FA") : Color.FromHex("#A42625");
    }

    private void PopulateSentryStatusInfo(SentryCard card, SentryInfo info)
    {
        card.ModeLabel.SetMarkupPermissive(info.Mode switch
        {
            SentryMode.On => "[color=#229132]Active[/color]",
            SentryMode.Off => "[color=#CED22B]Standby[/color]",
            _ => "[color=#A42625]Packed[/color]"
        });

        if (info.Target != null && _entities.TryGetEntity(info.Target.Value, out var targetEnt))
        {
            var name = _entities.GetComponent<MetaDataComponent>(targetEnt.Value).EntityName;
            card.TargetLabel.SetMarkupPermissive($"[color=#A42625]{name}[/color]");
        }
        else
        {
            card.TargetLabel.SetMarkupPermissive("[color=#B0B0B0]None[/color]");
        }
    }

    private void PopulateSentryFactionControls(SentryCard card, SentryInfo info)
    {
        card.FactionContainer.DisposeAllChildren();

        if (State is not SentryLaptopBuiState state)
            return;

        foreach (var faction in state.AllFactions)
        {
            var checkboxContainer = CreateFactionCheckbox(faction, info, state);
            card.FactionContainer.AddChild(checkboxContainer);
        }

        card.IFFExpandButton.OnPressed += _ => ToggleIFFExpanded(card, info.Id);

        if (_iffExpanded.TryGetValue(info.Id, out var isExpanded))
        {
            card.IFFPanel.Visible = isExpanded;
            card.IFFExpandButton.Text = isExpanded ? "▼" : "►";
        }
    }

    private void UpdateFactionControls(SentryCard card, SentryInfo info)
    {
        if (State is not SentryLaptopBuiState state)
            return;

        var existingCheckboxes = new Dictionary<string, CheckBox>();

        foreach (var child in card.FactionContainer.Children)
        {
            if (child is BoxContainer container && container.Children.FirstOrDefault() is CheckBox checkbox)
            {
                var label = container.Children.Skip(1).FirstOrDefault() as Label;
                if (label != null)
                {
                    var faction = state.AllFactions.FirstOrDefault(f =>
                        state.FactionNames.GetValueOrDefault(f, f) == label.Text);
                    if (faction != null)
                    {
                        existingCheckboxes[faction] = checkbox;
                    }
                }
            }
        }

        foreach (var faction in state.AllFactions)
        {
            if (existingCheckboxes.TryGetValue(faction, out var checkbox))
            {
                checkbox.Pressed = IsFactionSelected(info.FriendlyFactions, faction, state.AllFactions);

                if (checkbox.Parent is BoxContainer container)
                {
                    var label = container.Children.OfType<Label>().FirstOrDefault();
                    if (label != null)
                        label.FontColorOverride = GetFactionLabelColor(faction, info);
                }
            }
        }
    }

    private void ToggleIFFExpanded(SentryCard card, NetEntity sentryId)
    {
        var isExpanded = _iffExpanded.TryGetValue(sentryId, out var expanded) && expanded;
        isExpanded = !isExpanded;
        _iffExpanded[sentryId] = isExpanded;

        card.IFFPanel.Visible = isExpanded;
        card.IFFExpandButton.Text = isExpanded ? "▼" : "►";
    }

    private BoxContainer CreateFactionCheckbox(string faction, SentryInfo info, SentryLaptopBuiState state)
    {
        var container = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Margin = new Thickness(2, 1),
            HorizontalExpand = true,
            MinHeight = 22,
            Name = faction
        };

        var checkbox = new CheckBox
        {
            Pressed = IsFactionSelected(info.FriendlyFactions, faction, state.AllFactions),
            VerticalAlignment = Control.VAlignment.Center
        };

        var displayName = state.FactionNames.GetValueOrDefault(faction, faction);
        var label = new Label
        {
            Text = displayName,
            VerticalAlignment = Control.VAlignment.Center,
            HorizontalExpand = true,
            Margin = new Thickness(5, 0, 0, 0),
            FontColorOverride = GetFactionLabelColor(faction, info)
        };

        checkbox.OnToggled += args =>
        {
            ApplyLocalFactionChange(info.Id, faction, args.Pressed, container);
            SendMessage(new SentryLaptopToggleFactionBuiMsg(info.Id, faction, args.Pressed));
        };

        container.AddChild(checkbox);
        container.AddChild(label);

        return container;
    }

    private void ApplyLocalFactionChange(NetEntity sentryId, string faction, bool targeted, BoxContainer? container)
    {
        if (!_currentInfos.TryGetValue(sentryId, out var info))
            return;

        if (targeted)
            info.FriendlyFactions.Add(faction);
        else
            info.FriendlyFactions.Remove(faction);

        if (container != null && container.Parent is SentryCard card && _currentInfos.TryGetValue(sentryId, out var updated))
            UpdateFactionControls(card, updated);
    }

    private Color GetFactionLabelColor(string faction, SentryInfo info)
    {
        if (info.HumanoidAdded != null && info.HumanoidAdded.Contains(faction))
            return Color.FromHex("#B180FF");

        return GetFactionColor(faction);
    }

    private void SetupSentryCardButtons(SentryCard card, SentryInfo info)
    {
        card.ResetTargetingButton.Button.OnPressed += _ =>
        {
            SendMessage(new SentryLaptopResetTargetingBuiMsg(info.Id));
        };

        card.TogglePowerButton.Button.OnPressed += _ =>
        {
            SendMessage(new SentryLaptopTogglePowerBuiMsg(info.Id));
        };

        card.ViewButton.Button.OnPressed += _ => ViewSentryCamera(info.Id);
        card.UnlinkButton.Button.OnPressed += _ => SendMessage(new SentryLaptopUnlinkBuiMsg(info.Id));
    }

    private void ViewSentryCamera(NetEntity sentryNetEntity)
    {
        SendMessage(new SentryLaptopViewCameraBuiMsg(sentryNetEntity));

        if (!_entities.TryGetEntity(sentryNetEntity, out var sentryEntity))
            return;

        if (_window == null)
            return;

        if (_entities.TryGetComponent<SentryComponent>(sentryEntity.Value, out var sentryComp) &&
            sentryComp.Mode == SentryMode.Item)
            return;

        if (!_entities.TryGetComponent<TransformComponent>(sentryEntity.Value, out var sentryXform))
            return;

        var eyeLerping = EntMan.System<EyeLerpingSystem>();
        var eyeSystem = EntMan.System<SharedEyeSystem>();

        if (_currentCameraTarget is { } oldCamera)
        {
            eyeLerping.RemoveEye(oldCamera);
            if (_cameraEntity != null)
            {
                _entities.DeleteEntity(_cameraEntity.Value);
                _cameraEntity = null;
            }
        }

        var sentryInfo = State is SentryLaptopBuiState buiState
            ? buiState.Sentries.FirstOrDefault(s => s.Id == sentryNetEntity)
            : null;

        float visionRadius = sentryInfo?.VisionRadius ?? 5.0f;

        var mapUid = sentryXform.MapUid ?? EntityUid.Invalid;
        if (mapUid == EntityUid.Invalid)
            return;

        _cameraEntity = _entities.SpawnEntity(null, new EntityCoordinates(mapUid, sentryXform.WorldPosition));

        if (!_entities.TryGetComponent<TransformComponent>(_cameraEntity.Value, out var camXform))
            return;

        camXform.LocalRotation = sentryXform.LocalRotation;

        var eye = _entities.AddComponent<EyeComponent>(_cameraEntity.Value);

        const float tileSize = 32f;
        const float viewportSize = 600f;
        float desiredTilesAcross = visionRadius * 2f;
        float zoom = (desiredTilesAcross * tileSize) / viewportSize;
        if (zoom < 0.5f) zoom = 0.5f;
        if (zoom > 4f) zoom = 4f;

        eyeSystem.SetZoom(_cameraEntity.Value, new Vector2(zoom, zoom), eye);
        eyeSystem.SetRotation(_cameraEntity.Value, sentryXform.LocalRotation, eye);

        _currentCameraTarget = _cameraEntity;
        eyeLerping.AddEye(_cameraEntity.Value);

        _window.CameraPanel.Visible = true;
        _window.CameraViewport.Eye = eye.Eye;

        var sentryName = "Unknown";
        if (_entities.TryGetComponent<MetaDataComponent>(sentryEntity.Value, out var meta))
            sentryName = meta.EntityName;

        var fovText = sentryInfo?.MaxDeviation is >= 180 ? "360°" : $"{(int)((sentryInfo?.MaxDeviation ?? 75f) * 2)}°";
        _window.CameraTitle.SetMarkupPermissive($"[color=#88C7FA][bold]CAMERA: {sentryName} (Range: {visionRadius:F1} tiles, FOV: {fovText})[/bold][/color]");

        if (_savedWindowSize == null)
            _savedWindowSize = new Vector2i((int)_window.Size.X, (int)_window.Size.Y);
        UpdateWindowSize(true);
    }

    private void CloseCamera()
    {
        SendMessage(new SentryLaptopCloseCameraBuiMsg());

        if (_window == null)
            return;

        _window.CameraPanel.Visible = false;

        UpdateWindowSize(false);

        if (_currentCameraTarget is { } camera)
        {
            var eyeLerping = EntMan.System<EyeLerpingSystem>();
            eyeLerping.RemoveEye(camera);

            if (_cameraEntity != null)
            {
                _entities.DeleteEntity(_cameraEntity.Value);
                _cameraEntity = null;
            }

            _currentCameraTarget = null;
        }

        if (_window != null && _savedWindowSize is { } saved)
        {
            var min = _window.MinSize;
            var target = new Vector2i((int)Math.Max(saved.X, min.X), (int)Math.Max(saved.Y, min.Y));
            _window.SetSize = target;
            _savedWindowSize = null;
        }
    }

    private Color GetHealthColor(float percent)
    {
        return percent > 0.5f ? Color.FromHex("#229132") :
               percent > 0.25f ? Color.FromHex("#CED22B") :
               Color.FromHex("#A42625");
    }

    private Color GetFactionColor(string faction)
    {
        return faction switch
        {
            "UNMC" or "RoyalMarines" => Color.FromHex("#A42625"),
            "RMCXeno" or "CLF" => Color.FromHex("#229132"),
            _ => Color.FromHex("#88C7FA")
        };
    }

    private void Refresh()
    {
        if (State is SentryLaptopBuiState state)
            UpdateDisplay(state);
    }

    private void UpdateWindowSize(bool cameraOpen)
    {
        if (_window == null)
            return;

        var targetMin = cameraOpen ? new Vector2i(1500, 700) : new Vector2i(800, 700);
        _window.MinSize = targetMin;

        var current = _window.Size;
        _window.SetSize = new Vector2i(
            Math.Max((int)current.X, targetMin.X),
            Math.Max((int)current.Y, targetMin.Y)
        );
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            if (_currentCameraTarget is { } camera)
            {
                var eyeLerping = EntMan.System<EyeLerpingSystem>();
                eyeLerping.RemoveEye(camera);

                if (_cameraEntity != null)
                {
                    _entities.DeleteEntity(_cameraEntity.Value);
                    _cameraEntity = null;
                }
            }

            _window?.Dispose();
        }
    }
}
