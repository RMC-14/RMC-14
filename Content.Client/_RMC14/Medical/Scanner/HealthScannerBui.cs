using System.Globalization;
using Content.Client._RMC14.Medical.HUD;
using Content.Client.Message;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Medical.HUD;
using Content.Shared._RMC14.Medical.HUD.Components;
using Content.Shared._RMC14.Medical.HUD.Systems;
using Content.Shared._RMC14.Medical.Scanner;
using Content.Shared._RMC14.Medical.Wounds;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Temperature;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Medical.Scanner;

[UsedImplicitly]
public sealed class HealthScannerBui : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    [ViewVariables]
    private HealthScannerWindow? _window;
    private NetEntity _lastTarget;

    private readonly ShowHolocardIconsSystem _holocardIcons;
    private readonly SkillsSystem _skills;
    private readonly SharedWoundsSystem _wounds;
    private readonly SharedRottingSystem _rot;

    public HealthScannerBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _holocardIcons = _entities.System<ShowHolocardIconsSystem>();
        _skills = _entities.System<SkillsSystem>();
        _wounds = _entities.System<SharedWoundsSystem>();
        _rot = _entities.System<SharedRottingSystem>();
    }

    protected override void Open()
    {
        base.Open();
        if (State is HealthScannerBuiState state)
            UpdateState(state);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is HealthScannerBuiState uiState)
            UpdateState(uiState);
    }

    private void UpdateState(HealthScannerBuiState uiState)
    {
        if (_window == null)
        {
            _window = this.CreateWindow<HealthScannerWindow>();
            _window.Title = "Health Scan";
        }

        if (_entities.GetEntity(uiState.Target) is not { Valid: true } target)
            return;

        _lastTarget = uiState.Target;

        _window.PatientLabel.Text = $"Patient: {Identity.Name(target, _entities, _player.LocalEntity)}";

        var thresholdsSystem = _entities.System<MobThresholdSystem>();
        if (!_entities.TryGetComponent(target, out DamageableComponent? damageable))
        {
            if (!_window.IsOpen)
                _window.OpenCentered();

            return;
        }

        var ent = new Entity<DamageableComponent>(target, damageable);
        AddGroup(ent, _window.BruteLabel, Color.FromHex("#DF3E3E"), "Brute");
        AddGroup(ent, _window.BurnLabel, Color.FromHex("#FFB833"), "Burn");
        AddGroup(ent, _window.ToxinLabel, Color.FromHex("#25CA4C"), "Toxin");
        AddGroup(ent, _window.OxygenLabel, Color.FromHex("#2E93DE"), "Airloss", "Oxygen");

        if (thresholdsSystem.TryGetIncapThreshold(target, out var threshold))
        {
            var damage = threshold.Value - damageable.TotalDamage;
            _window.HealthBar.MinValue = 0;
            _window.HealthBar.MaxValue = 100;

            if (_entities.HasComponent<VictimBurstComponent>(target) || _rot.IsRotten(target))
            {
                _window.HealthBar.Value = 100;
                _window.HealthBar.ModulateSelfOverride = Color.Red;
                _window.HealthBarText.Text = "Permanently deceased";
            }
            else
            {
                _window.HealthBar.ModulateSelfOverride = null;
                //Scale negative values with how close to death we are - if we have a different crit and dead state
                if (damage < 0 && thresholdsSystem.TryGetDeadThreshold(target, out var deadThreshold) &&
                    deadThreshold != threshold)
                    threshold = deadThreshold - threshold;

                var healthValue = damage.Float() / threshold.Value.Float() * 100f;
                _window.HealthBar.Value = healthValue;

                var healthString = MathHelper.CloseTo(healthValue, 100) ? "100%" : $"{healthValue:F2}%";

                _window.HealthBarText.Text = $"{healthString} healthy";
            }
        }

        _window.ChangeHolocardButton.Text = Loc.GetString("ui-health-scanner-holocard-change");
        _window.ChangeHolocardButton.OnPressed += OpenChangeHolocardUI;
        if (_player.LocalEntity is { } viewer &&
            _skills.HasSkill(viewer, HolocardSystem.SkillType, HolocardSystem.MinimumRequiredSkill))
        {
            _window.ChangeHolocardButton.Disabled = false;
            _window.ChangeHolocardButton.ToolTip = "";
        }
        else
        {
            _window.ChangeHolocardButton.Disabled = true;
            _window.ChangeHolocardButton.ToolTip = Loc.GetString("ui-holocard-change-insufficient-skill");
        }

        if (_entities.TryGetComponent(target, out HolocardStateComponent? holocardComponent) &&
            _holocardIcons.TryGetDescription((target, holocardComponent), out var description) &&
            _holocardIcons.TryGetHolocardColor((target, holocardComponent), out var color))
        {
            _window.HolocardDescription.Text = description;
            if (_window.HolocardPanel.PanelOverride is StyleBoxFlat panel)
                panel.BackgroundColor = color.Value;
        }
        else
        {
            _window.HolocardDescription.Text = Loc.GetString("hc-none-description");
            _window.HolocardPanel.ModulateSelfOverride = null;
            if (_window.HolocardPanel.PanelOverride is StyleBoxFlat panel)
                panel.BackgroundColor = Color.Transparent;
        }

        _window.ChemicalsContainer.DisposeAllChildren();

        var anyChemicals = false;
        var anyUnknown = false;
        if (uiState.Chemicals != null)
        {
            foreach (var reagent in uiState.Chemicals.Contents)
            {
                if (!_prototype.TryIndex(reagent.Reagent.Prototype, out ReagentPrototype? prototype))
                    continue;

                if (prototype.Unknown)
                {
                    // TODO RMC14 these shouldn't be setting sent to the client
                    anyUnknown = true;
                    continue;
                }

                var text = $"{reagent.Quantity.Float():F1} {prototype.LocalizedName}";
                if (prototype.Overdose != null && reagent.Quantity > prototype.Overdose)
                    text = $"[bold][color=red]{FormattedMessage.EscapeText(text)} OD[/color][/bold]";

                var label = new RichTextLabel();
                label.SetMarkupPermissive(text);
                _window.ChemicalsContainer.AddChild(label);
                anyChemicals = true;
            }
        }

        _window.UnknownReagentsLabel.SetMarkupPermissive($"[color=white][italic]Unknown reagents detected.[/italic][/color]");
        _window.UnknownChemicalsPanel.Visible = anyUnknown;
        _window.ChemicalContentsLabel.Visible = anyChemicals;
        _window.ChemicalContentsSeparator.Visible = anyChemicals;
        _window.ChemicalsContainer.Visible = anyChemicals;

        _window.BloodTypeLabel.Text = "Blood:";
        var bloodMsg = new FormattedMessage();
        bloodMsg.PushColor(Color.FromHex("#25B732"));

        var percentage = uiState.MaxBlood == 0 ? 100 : uiState.Blood.Float() / uiState.MaxBlood.Float() * 100f;
        var percentageString = MathHelper.CloseTo(percentage, 100) ? "100%" : $"{percentage:F1}%";
        bloodMsg.AddText($"{percentageString}, {uiState.Blood}cl");
        bloodMsg.Pop();
        _window.BloodAmountLabel.SetMessage(bloodMsg);

        if (uiState.Bleeding)
            _window.Bleeding.SetMarkup(" [bold][color=#DF3E3E]\\[Bleeding\\][/color][/bold]");
        else
            _window.Bleeding.SetMessage(string.Empty);

        var temperatureMsg = new FormattedMessage();
        if (uiState.Temperature is { } temperatureKelvin)
        {
            var celsius = TemperatureHelpers.KelvinToCelsius(temperatureKelvin);
            var fahrenheit = TemperatureHelpers.KelvinToFahrenheit(temperatureKelvin);
            temperatureMsg.AddText($"{celsius:F1}ºC ({fahrenheit:F1}ºF)");
        }
        else
        {
            temperatureMsg.AddText("None");
        }

        _window.BodyTemperatureLabel.SetMessage(temperatureMsg);

        if (!_window.IsOpen)
        {
            _window.OpenCentered();
        }
    }

    private void OpenChangeHolocardUI(BaseButton.ButtonEventArgs obj)
    {
        if (_player.LocalEntity is { } viewer)
            SendMessage(new OpenChangeHolocardUIEvent(_entities.GetNetEntity(viewer), _lastTarget));
    }

    private void AddGroup(Entity<DamageableComponent> damageable, RichTextLabel label, Color color, ProtoId<DamageGroupPrototype> group, string? labelStr = null)
    {
        // TODO RMC14 unhardcode this
        labelStr ??= group.Id;
        var msg = new FormattedMessage();
        msg.AddText($"{labelStr}: ");
        msg.PushColor(color);

        var damage = damageable.Comp.DamagePerGroup.GetValueOrDefault(group)
            .Int()
            .ToString(CultureInfo.InvariantCulture);
        if (_wounds.HasUntreated(damageable.Owner, group))
            msg.AddText($"{{{damage}}}");
        else
            msg.AddText($"{damage}");

        msg.Pop();
        label.SetMessage(msg);
    }
}
