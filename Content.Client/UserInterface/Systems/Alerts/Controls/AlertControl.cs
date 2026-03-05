using System.Numerics;
using Content.Client.Actions.UI;
using Content.Client.Cooldown;
using Content.Shared.Alert;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.Alerts.Controls
{
    public sealed class AlertControl : BaseButton
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        private static readonly SpriteSpecifier.Rsi ResinMarkerCenterGlow =
            new(new ResPath("/Textures/_RMC14/Markers/xeno_markers.rsi"), "center_glow");

        private readonly SpriteSystem _sprite;

        public AlertPrototype Alert { get; }

        /// <summary>
        /// Current cooldown displayed in this slot. Set to null to show no cooldown.
        /// </summary>
        public (TimeSpan Start, TimeSpan End)? Cooldown
        {
            get => _cooldown;
            set
            {
                _cooldown = value;
                if (SuppliedTooltip is ActionAlertTooltip actionAlertTooltip)
                {
                    actionAlertTooltip.Cooldown = value;
                }
            }
        }

        public string? DynamicMessage
        {
            get => _dynamicMessage;
            set
            {
                _dynamicMessage = value;
                if (SuppliedTooltip is ActionAlertTooltip actionAlertTooltip)
                {
                    actionAlertTooltip.DynamicMessage = value;
                }
            }
        }

        private (TimeSpan Start, TimeSpan End)? _cooldown;
        private string? _dynamicMessage;

        private short? _severity;

        private readonly SpriteView _icon;
        private readonly CooldownGraphic _cooldownGraphic;

        private EntityUid _spriteViewEntity;

        /// <summary>
        /// Creates an alert control reflecting the indicated alert + state
        /// </summary>
        /// <param name="alert">alert to display</param>
        /// <param name="severity">severity of alert, null if alert doesn't have severity levels</param>
        public AlertControl(AlertPrototype alert, short? severity)
        {
            // Alerts will handle this.
            MuteSounds = true;

            IoCManager.InjectDependencies(this);
            _sprite = _entityManager.System<SpriteSystem>();
            TooltipSupplier = SupplyTooltip;
            Alert = alert;

            HorizontalAlignment = HAlignment.Left;
            _severity = severity;
            _icon = new SpriteView
            {
                Scale = new Vector2(2, 2),
                MaxSize = new Vector2(64, 64),
                Stretch = SpriteView.StretchMode.None,
                HorizontalAlignment = HAlignment.Left
            };

            SetupIcon();

            Children.Add(_icon);
            _cooldownGraphic = new CooldownGraphic
            {
                MaxSize = new Vector2(64, 64)
            };
            Children.Add(_cooldownGraphic);
        }

        private Control SupplyTooltip(Control? sender)
        {
            var msg = FormattedMessage.FromMarkupOrThrow(Loc.GetString(Alert.Name));
            var desc = FormattedMessage.FromMarkupOrThrow(Loc.GetString(Alert.Description));
            return new ActionAlertTooltip(msg, desc) { Cooldown = Cooldown, DynamicMessage = DynamicMessage };
        }

        /// <summary>
        /// Change the alert severity, changing the displayed icon
        /// </summary>
        public void SetSeverity(short? severity)
        {
            if (_severity == severity)
                return;
            _severity = severity;

            if (!_entityManager.TryGetComponent<SpriteComponent>(_spriteViewEntity, out var sprite))
                return;
            var icon = Alert.GetIcon(_severity);
            if (_sprite.LayerMapTryGet((_spriteViewEntity, sprite), AlertVisualLayers.Base, out var layer, false))
            {
                if (UseResinMarkerDirectionalOverlay())
                    _sprite.LayerSetSprite((_spriteViewEntity, sprite), layer, Alert.Icons[0]);
                else
                    _sprite.LayerSetSprite((_spriteViewEntity, sprite), layer, icon);
            }

            if (UseResinMarkerDirectionalOverlay())
                UpdateResinMarkerDirectionalOverlay((_spriteViewEntity, sprite), icon);

            UpdateIconDirection(icon);
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);
            UserInterfaceManager.GetUIController<AlertsUIController>().UpdateAlertSpriteEntity(_spriteViewEntity, Alert);

            if (!Cooldown.HasValue)
            {
                _cooldownGraphic.Visible = false;
                _cooldownGraphic.Progress = 0;
                return;
            }

            _cooldownGraphic.FromTime(Cooldown.Value.Start, Cooldown.Value.End);
        }

        private void SetupIcon()
        {
            if (!_entityManager.Deleted(_spriteViewEntity))
                _entityManager.QueueDeleteEntity(_spriteViewEntity);

            _spriteViewEntity = _entityManager.Spawn(Alert.AlertViewEntity);
            if (_entityManager.TryGetComponent<SpriteComponent>(_spriteViewEntity, out var sprite))
            {
                var icon = Alert.GetIcon(_severity);
                if (_sprite.LayerMapTryGet((_spriteViewEntity, sprite), AlertVisualLayers.Base, out var layer, false))
                {
                    if (UseResinMarkerDirectionalOverlay())
                        _sprite.LayerSetSprite((_spriteViewEntity, sprite), layer, Alert.Icons[0]);
                    else
                        _sprite.LayerSetSprite((_spriteViewEntity, sprite), layer, icon);
                }

                if (UseResinMarkerDirectionalOverlay())
                    UpdateResinMarkerDirectionalOverlay((_spriteViewEntity, sprite), icon);

                UpdateIconDirection(icon);
            }

            _icon.SetEntity(_spriteViewEntity);
        }

        private void UpdateIconDirection(SpriteSpecifier icon)
        {
            if (icon is not SpriteSpecifier.Rsi rsi)
            {
                _icon.OverrideDirection = null;
                return;
            }

            var state = _sprite.GetState(rsi);
            if (state.RsiDirections == RsiDirectionType.Dir1)
            {
                _icon.OverrideDirection = null;
                return;
            }

            _icon.OverrideDirection = _severity switch
            {
                2 => Direction.South,
                3 => Direction.SouthEast,
                4 => Direction.East,
                5 => Direction.NorthEast,
                6 => Direction.North,
                7 => Direction.NorthWest,
                8 => Direction.West,
                9 => Direction.SouthWest,
                _ => null
            };
        }

        private bool UseResinMarkerDirectionalOverlay()
        {
            return Alert.ID == "ResinMarkerTracker";
        }

        private void UpdateResinMarkerDirectionalOverlay(Entity<SpriteComponent> spriteEnt, SpriteSpecifier icon)
        {
            var ent = spriteEnt.AsNullable();

            if (!_sprite.LayerMapTryGet(ent, AlertVisualLayers.CenterGlow, out var centerGlowLayer, false))
                centerGlowLayer = _sprite.LayerMapReserve(ent, AlertVisualLayers.CenterGlow);

            if (!_sprite.LayerMapTryGet(ent, AlertVisualLayers.Direction, out var directionLayer, false))
                directionLayer = _sprite.LayerMapReserve(ent, AlertVisualLayers.Direction);

            if (!_severity.HasValue || _severity.Value <= 0)
            {
                _sprite.LayerSetVisible(ent, centerGlowLayer, false);
                _sprite.LayerSetVisible(ent, directionLayer, false);
                return;
            }

            _sprite.LayerSetSprite(ent, centerGlowLayer, ResinMarkerCenterGlow);
            _sprite.LayerSetVisible(ent, centerGlowLayer, true);
            _sprite.LayerSetSprite(ent, directionLayer, icon);
            _sprite.LayerSetVisible(ent, directionLayer, true);
        }

        protected override void EnteredTree()
        {
            base.EnteredTree();
            SetupIcon();
        }

        protected override void ExitedTree()
        {
            base.ExitedTree();

            if (!_entityManager.Deleted(_spriteViewEntity))
                _entityManager.QueueDeleteEntity(_spriteViewEntity);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!_entityManager.Deleted(_spriteViewEntity))
                _entityManager.QueueDeleteEntity(_spriteViewEntity);
        }
    }

    public enum AlertVisualLayers : byte
    {
        Base,
        CenterGlow,
        Direction
    }
}
