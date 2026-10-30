using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.UserInterface;

public static class RMCMachineUiStyle
{
    public const string WatermarkPath = "/Textures/_RMC14/Interface/Paper/paper_heading_weya.svg.96dpi.png";
    public const string ButtonFontPath = "/Fonts/RobotoMono/RobotoMono-Regular.ttf";

    public static readonly Color WindowColor = Color.FromHex("#1F2538");
    public static readonly Color LabelColor = Color.FromHex("#7D8EA6");
    public static readonly Color FieldColor = Color.FromHex("#171C2A");

    public static readonly Color ButtonColor = Color.FromHex("#AE7F37");
    public static readonly Color ButtonSelectedColor = Color.FromHex("#E4C69A");
    public static readonly Color ButtonCautionColor = Color.FromHex("#BE6309");
    public static readonly Color ButtonDangerColor = Color.FromHex("#9B9E00");
    public static readonly Color ButtonDisabledColor = Color.FromHex("#999999");
    public static readonly Color ButtonTextColor = Color.FromHex("#161613");
    public static readonly Color BevelColor = Color.FromHex("#E8E4C9");

    public static readonly Color NoticeColor = Color.FromHex("#BB9B68");
    public static readonly Color NoticeInfoColor = Color.FromHex("#225881");
    public static readonly Color NoticeSuccessColor = Color.FromHex("#1E662F");
    public static readonly Color NoticeWarningColor = Color.FromHex("#A95319");
    public static readonly Color NoticeDangerColor = Color.FromHex("#8F2828");

    public static readonly Color GoodColor = Color.FromHex("#5BAA27");
    public static readonly Color AverageColor = Color.FromHex("#F08E0F");
    public static readonly Color BadColor = Color.FromHex("#DB2929");
    public static readonly Color CautionTextColor = Color.FromHex("#E0904A");

    public static Font ButtonFont(IResourceCache cache)
    {
        return cache.GetFont(ButtonFontPath, 12);
    }

    public static void RemoveWindowBorder(DefaultWindow window)
    {
        window.Contents.Margin = new Thickness(0);
        if (window.GetChild(0) is PanelContainer panel)
            panel.PanelOverride = new StyleBoxFlat { BackgroundColor = WindowColor };
    }

    public static StyleBoxFlat Field()
    {
        return new StyleBoxFlat
        {
            BackgroundColor = FieldColor,
            BorderColor = LabelColor,
            BorderThickness = new Thickness(1),
            ContentMarginLeftOverride = 4,
            ContentMarginRightOverride = 4,
        };
    }

    public static void Notice(PanelContainer panel, Label label, string text, Color color, Font font)
    {
        panel.PanelOverride = new RMCStripedStyleBox { BackgroundColor = color };
        label.Text = text;
        label.FontColorOverride = Luminance(color) > 0.35f ? Color.Black : Color.White;
        label.FontOverride = font;
    }

    public static void Bar(ProgressBar bar, float ratio, Color color)
    {
        bar.BackgroundStyleBoxOverride = new StyleBoxFlat
        {
            BackgroundColor = WindowColor,
            BorderColor = color,
            BorderThickness = new Thickness(1),
        };
        bar.ForegroundStyleBoxOverride = new StyleBoxFlat { BackgroundColor = color };
        bar.MinValue = 0;
        bar.MaxValue = 1;
        bar.Value = Math.Clamp(ratio, 0f, 1f);
    }

    private static float Luminance(Color color)
    {
        static float Linear(float c)
        {
            return c <= 0.03928f ? c / 12.92f : MathF.Pow((c + 0.055f) / 1.055f, 2.4f);
        }

        return 0.2126f * Linear(color.R) + 0.7152f * Linear(color.G) + 0.0722f * Linear(color.B);
    }
}
