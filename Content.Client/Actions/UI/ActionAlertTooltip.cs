using Content.Client.Stylesheets;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.Actions.UI
{
    /// <summary>
    /// Tooltip for actions or alerts because they are very similar.
    /// </summary>
    public sealed class ActionAlertTooltip : PanelContainer
    {
        private const float TooltipTextMaxWidth = 350;

        private readonly RichTextLabel _cooldownLabel;
        private readonly RichTextLabel _dynamicMessageLabel;
        private readonly IGameTiming _gameTiming;

        /// <summary>
        /// Current cooldown displayed in this tooltip. Set to null to show no cooldown.
        /// </summary>
        public (TimeSpan Start, TimeSpan End)? Cooldown { get; set; }
        public string? DynamicMessage { get; set; }

        public ActionAlertTooltip(FormattedMessage name, FormattedMessage? desc, string? requires = null, FormattedMessage? charges = null)
        {
            _gameTiming = IoCManager.Resolve<IGameTiming>();

            SetOnlyStyleClass(StyleNano.StyleClassTooltipPanel);

            BoxContainer vbox;
            AddChild(vbox = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                RectClipContent = true
            });
            var nameLabel = new RichTextLabel
            {
                MaxWidth = TooltipTextMaxWidth,
                StyleClasses = {StyleNano.StyleClassTooltipActionTitle}
            };
            nameLabel.SetMessage(name);
            vbox.AddChild(nameLabel);

            if (desc != null && !string.IsNullOrWhiteSpace(desc.ToString()))
            {
                var description = new RichTextLabel
                {
                    MaxWidth = TooltipTextMaxWidth,
                    StyleClasses = {StyleNano.StyleClassTooltipActionDescription}
                };
                description.SetMessage(desc);
                vbox.AddChild(description);
            }

            if (charges != null && !string.IsNullOrWhiteSpace(charges.ToString()))
            {
                var chargesLabel = new RichTextLabel
                {
                    MaxWidth = TooltipTextMaxWidth,
                    StyleClasses = { StyleNano.StyleClassTooltipActionCharges }
                };
                chargesLabel.SetMessage(charges);
                vbox.AddChild(chargesLabel);
            }

            vbox.AddChild(_cooldownLabel = new RichTextLabel
            {
                MaxWidth = TooltipTextMaxWidth,
                StyleClasses = {StyleNano.StyleClassTooltipActionCooldown},
                Visible = false
            });

            vbox.AddChild(_dynamicMessageLabel = new RichTextLabel
            {
                MaxWidth = TooltipTextMaxWidth,
                StyleClasses = {StyleNano.StyleClassTooltipActionDynamicMessage},
                Visible = false
            });

            if (!string.IsNullOrWhiteSpace(requires))
            {
                var requiresLabel = new RichTextLabel
                {
                    MaxWidth = TooltipTextMaxWidth,
                    StyleClasses = {StyleNano.StyleClassTooltipActionRequirements}
                };

                if (!FormattedMessage.TryFromMarkup("[color=#635c5c]" + requires + "[/color]", out var markup))
                    return;

                requiresLabel.SetMessage(markup);

                vbox.AddChild(requiresLabel);
            }
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);
            if (!Cooldown.HasValue)
            {
                _cooldownLabel.Visible = false;
            }
            else
            {
                var timeLeft = Cooldown.Value.End - _gameTiming.CurTime;
                if (timeLeft > TimeSpan.Zero)
                {
                    var duration = Cooldown.Value.End - Cooldown.Value.Start;

                    if (FormattedMessage.TryFromMarkup($"[color=#a10505]{(int) duration.TotalSeconds} sec cooldown ({(int) timeLeft.TotalSeconds + 1} sec remaining)[/color]", out var markup)){
                        _cooldownLabel.SetMessage(markup);
                        _cooldownLabel.Visible = true;
                    }
                }
                else
                {
                    _cooldownLabel.Visible = false;
                }
            }

            if (string.IsNullOrWhiteSpace(DynamicMessage))
            {
                _dynamicMessageLabel.Visible = false;
            }
            else if (FormattedMessage.TryFromMarkup($"[color=#ffffff]{DynamicMessage}[/color]", out var dynamicMarkup))
            {
                _dynamicMessageLabel.SetMessage(dynamicMarkup);
                _dynamicMessageLabel.Visible = true;
            }
        }
    }
}
