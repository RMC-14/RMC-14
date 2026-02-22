using System.Globalization;
using Content.Client._RMC14.Medical.HUD;
using Content.Client.Atmos.Rotting;
using Content.Client.Message;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Medical.Defibrillator;
using Content.Shared._RMC14.Medical.HUD;
using Content.Shared._RMC14.Medical.HUD.Components;
using Content.Shared._RMC14.Medical.HUD.Systems;
using Content.Shared._RMC14.Medical.Scanner;
using Content.Shared._RMC14.Medical.Unrevivable;
using Content.Shared._RMC14.Medical.Wounds;
using Content.Shared._RMC14.Xenonids.Parasite;
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
    private readonly RMCUnrevivableSystem _unrevivable;
    private readonly MobStateSystem _mob;
    private readonly RottingSystem _rot;

    private Dictionary<EntProtoId<SkillDefinitionComponent>, int> BloodPackSkill = new() { ["RMCSkillSurgery"] = 1 };
    private Dictionary<EntProtoId<SkillDefinitionComponent>, int> DefibSkill = new() { ["RMCSkillMedical"] = 2 };
    private Dictionary<EntProtoId<SkillDefinitionComponent>, int> LarvaSurgerySkill = new() { ["RMCSkillSurgery"] = 2 };

    public HealthScannerBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _holocardIcons = _entities.System<ShowHolocardIconsSystem>();
        _skills = _entities.System<SkillsSystem>();
        _wounds = _entities.System<SharedWoundsSystem>();
        _unrevivable = _entities.System<RMCUnrevivableSystem>();
        _mob = _entities.System<MobStateSystem>();
        _rot = _entities.System<RottingSystem>();
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
            _window.Title = Loc.GetString("rmc-health-analyzer-title");
        }

        if (_entities.GetEntity(uiState.Target) is not { Valid: true } target)
            return;

        _lastTarget = uiState.Target;

        _window.PatientLabel.Text = Loc.GetString("rmc-health-analyzer-patient", ("name", Identity.Name(target, _entities, _player.LocalEntity)));

        var thresholdsSystem = _entities.System<MobThresholdSystem>();
        if (!_entities.TryGetComponent(target, out DamageableComponent? damageable))
        {
            if (!_window.IsOpen)
                _window.OpenCentered();

            return;
        }

        var ent = new Entity<DamageableComponent>(target, damageable);
        AddGroup(ent, _window.BruteLabel, Color.FromHex("#DF3E3E"), "Brute", Loc.GetString("rmc-health-analyzer-brute"));
        AddGroup(ent, _window.BurnLabel, Color.FromHex("#FFB833"), "Burn", Loc.GetString("rmc-health-analyzer-burn"));
        AddGroup(ent, _window.ToxinLabel, Color.FromHex("#25CA4C"), "Toxin", Loc.GetString("rmc-health-analyzer-toxin"));
        AddGroup(ent, _window.OxygenLabel, Color.FromHex("#2E93DE"), "Airloss", Loc.GetString("rmc-health-analyzer-oxygen"));
        if (damageable.DamagePerGroup["Genetic"] > 0)
        {
            _window.CloneBox.Visible = true;
            AddGroup(ent, _window.CloneLabel, Color.FromHex("#02c9c0"), "Genetic", Loc.GetString("rmc-health-analyzer-clone"));
        }
        else
            _window.CloneBox.Visible = false;

        bool isPermaDead = false;

        if (thresholdsSystem.TryGetIncapThreshold(target, out var threshold))
        {
            var damage = threshold.Value - damageable.TotalDamage;
            _window.HealthBar.MinValue = 0;
            _window.HealthBar.MaxValue = 100;

            if (_mob.IsDead(target) && (_entities.HasComponent<VictimBurstComponent>(target) ||
                _rot.IsRotten(target) || _unrevivable.IsUnrevivable(target) ||
                _entities.HasComponent<RMCDefibrillatorBlockedComponent>(target)))
            {
                isPermaDead = true;
                _window.HealthBar.Value = 100;
                _window.HealthBar.ModulateSelfOverride = Color.Red;
                _window.HealthBarText.Text = Loc.GetString("rmc-health-analyzer-permadead");
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

                _window.HealthBarText.Text = Loc.GetString("rmc-health-analyzer-healthy", ("percent", healthString));
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
                if (!_prototype.TryIndexReagent(reagent.Reagent.Prototype, out ReagentPrototype? prototype))
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

        _window.UnknownReagentsLabel.SetMarkupPermissive(Loc.GetString("rmc-health-analyzer-unknown-reagents"));
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

        _window.AdviceContainer.DisposeAllChildren();
        //Medication Advice
        if (!isPermaDead)
        {
            _window.MedicalAdviceLabel.Visible = true;
            _window.MedicalAdviceSeparator.Visible = true;
            MedicalAdvice(ent, uiState, _window);
            if (_window.AdviceContainer.ChildCount == 0)
            {
                _window.MedicalAdviceLabel.Visible = false;
                _window.MedicalAdviceSeparator.Visible = false;
            }
        }
        else
        {
            _window.MedicalAdviceLabel.Visible = false;
            _window.MedicalAdviceSeparator.Visible = false;
        }


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

    private void AddGroup(Entity<DamageableComponent> damageable, RichTextLabel label, Color color, ProtoId<DamageGroupPrototype> group, string labelStr)
    {
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

    private void MedicalAdvice(Entity<DamageableComponent> target, HealthScannerBuiState uiState, HealthScannerWindow window)
    {
        WoundedComponent? wounds = null;
        _entities.TryGetComponent(target, out wounds);
        bool hasBruteWounds = false;
        bool hasBurnWounds = false;

        if (wounds != null && _wounds.HasUntreated((target, wounds), wounds.BruteWoundGroup))
            hasBruteWounds = true;

        if (wounds != null && _wounds.HasUntreated((target, wounds), wounds.BurnWoundGroup))
            hasBurnWounds = true;

        if (_player.LocalEntity is not { } viewer)
            return;

        //Defibrilation related
        if (_mob.IsDead(target))
        {
            var thresholdsSystem = _entities.System<MobThresholdSystem>();

            if (thresholdsSystem.TryGetDeadThreshold(target, out var deadThreshold))
            {
                if (deadThreshold + 30 < target.Comp.Damage.GetTotal() && uiState.Chemicals != null
                    && !uiState.Chemicals.ContainsReagent("CMEpinephrine", null))
                {
                    AddAdvice(Loc.GetString("rmc-health-analyzer-advice-epinedrine"), window);
                }
                else
                {
                    string defib = String.Empty;
                    if (deadThreshold - 20 <= target.Comp.Damage.GetTotal() &&
                        wounds != null && !hasBruteWounds && !hasBurnWounds)
                        defib = Loc.GetString("rmc-health-analyzer-advice-defib-repeated");
                    else if (deadThreshold > target.Comp.Damage.GetTotal())
                        defib = Loc.GetString("rmc-health-analyzer-advice-defib");

                    if (defib != String.Empty && !_skills.HasAllSkills(viewer, DefibSkill))
                        defib = $"[color=#858585]{defib}[/color]";

                    if (defib != String.Empty)
                        AddAdvice(defib, window);
                }
            }

            AddAdvice(Loc.GetString("rmc-health-analyzer-advice-cpr"), window);
        }

        //Surgery related
        if (_entities.TryGetComponent(target, out HolocardStateComponent? holocardComponent) &&
            holocardComponent.HolocardStatus == HolocardStatus.Xeno)
        {
            string larvaSurgery = Loc.GetString("rmc-health-analyzer-advice-larva-surgery");
            if (!_skills.HasAllSkills(viewer, LarvaSurgerySkill))
                larvaSurgery = $"[color=#858585]{larvaSurgery}[/color]";
            AddAdvice(Loc.GetString("rmc-health-analyzer-advice-larva-surgery"), window);
        }

        //TODO RMC14 more surgery advice

        //Wound related
        if (hasBruteWounds)
            AddAdvice(Loc.GetString("rmc-health-analyzer-advice-brute-wounds"), window);

        if (hasBurnWounds)
            AddAdvice(Loc.GetString("rmc-health-analyzer-advice-burn-wounds"), window);

        //Blood related
        if (uiState.Blood < uiState.MaxBlood)
        {
            var bloodPercent = uiState.Blood / uiState.MaxBlood;

            if (bloodPercent < 0.85)
            {
                string bloodpack = Loc.GetString("rmc-health-analyzer-advice-blood-pack");
                if (!_skills.HasAllSkills(viewer, BloodPackSkill))
                    bloodpack = $"[color=#858585]{bloodpack}[/color]";
                AddAdvice(bloodpack, window);
            }

            if (bloodPercent < 0.9 && uiState.Chemicals != null && !uiState.Chemicals.ContainsReagent("Nutriment", null))
                AddAdvice(Loc.GetString("rmc-health-analyzer-advice-food"), window);
        }

        //TODO RMC14 Pain related medical advice

        //Damage related
        var airloss = target.Comp.DamagePerGroup.GetValueOrDefault("Airloss");
        var brute = target.Comp.DamagePerGroup.GetValueOrDefault("Brute");
        var burn = target.Comp.DamagePerGroup.GetValueOrDefault("Burn");
        var toxin = target.Comp.DamagePerGroup.GetValueOrDefault("Toxin");
        var genetic = target.Comp.DamagePerGroup.GetValueOrDefault("Genetic");

        if (airloss > 0 && !_mob.IsDead(target))
        {
            if (airloss > 10 && _mob.IsCritical(target))
                AddAdvice(Loc.GetString("rmc-health-analyzer-advice-cpr-crit"), window);

            if (airloss > 30 && uiState.Chemicals != null &&
                !uiState.Chemicals.ContainsReagent("CMDexalin", null))
                AddAdvice(Loc.GetString("rmc-health-analyzer-advice-dexalin"), window);
        }

        if (brute > 30 && uiState.Chemicals != null &&
            !uiState.Chemicals.ContainsReagent("CMBicaridine", null) &&
            !_mob.IsDead(target))
            AddAdvice(Loc.GetString("rmc-health-analyzer-advice-bicaridine"), window);

        if (burn > 30 && uiState.Chemicals != null &&
            !uiState.Chemicals.ContainsReagent("CMKelotane", null) &&
            !_mob.IsDead(target))
            AddAdvice(Loc.GetString("rmc-health-analyzer-advice-kelotane"), window);

        if (toxin > 10 && uiState.Chemicals != null &&
            !uiState.Chemicals.ContainsReagent("CMDylovene", null) && !uiState.Chemicals.ContainsReagent("Inaprovaline", null) &&
            !_mob.IsDead(target))
            AddAdvice(Loc.GetString("rmc-health-analyzer-advice-dylovene"), window);

        //TODO RMC14 Clone damage advice
    }

    private void AddAdvice(string text, HealthScannerWindow window)
    {
        var label = new RichTextLabel();
        label.SetMarkupPermissive(text);
        window.AdviceContainer.AddChild(label);
    }
}
