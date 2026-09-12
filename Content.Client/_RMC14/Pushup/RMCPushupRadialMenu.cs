using Content.Client.UserInterface.Controls;
using Content.Shared._RMC14.Pushup;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Pushup;

public static class RMCPushupRadialMenu
{
    private const string PushupRoutineEmote = "RMCPushupRoutine";
    private static readonly ResPath Icon = new("_RMC14/Actions/pushups.rsi");

    public static bool TryCreate(
        EmotePrototype emote,
        IEntityManager entityManager,
        out RadialMenuOption? option)
    {
        if (emote.ID != PushupRoutineEmote)
        {
            option = null;
            return false;
        }

        option = new RadialMenuNestedLayerOption(
            new RadialMenuOption[]
            {
                CreateFormOption(entityManager, RMCPushupForm.Proper, "rmc-pushup-form-proper", "proper"),
                CreateFormOption(entityManager, RMCPushupForm.Knees, "rmc-pushup-form-knees", "knees"),
            })
        {
            Sprite = emote.Icon,
            ToolTip = Loc.GetString(emote.Name),
        };
        return true;
    }

    private static RadialMenuActionOption<RMCPushupForm> CreateFormOption(
        IEntityManager entityManager,
        RMCPushupForm form,
        string tooltip,
        string iconState)
    {
        return new RadialMenuActionOption<RMCPushupForm>(
            selected => entityManager.RaisePredictiveEvent(new RMCPushupSelectedEvent(selected)),
            form)
        {
            Sprite = new SpriteSpecifier.Rsi(Icon, iconState),
            ToolTip = Loc.GetString(tooltip),
        };
    }
}
