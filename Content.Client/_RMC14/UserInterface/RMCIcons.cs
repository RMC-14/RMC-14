using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;

namespace Content.Client._RMC14.UserInterface;

public static class RMCIcons
{
    public const string FontPath = "/Fonts/FontAwesome/Font Awesome 7 Free-Solid-900.otf";

    public const string ArrowDown = "\uf063";
    public const string ArrowDown19 = "\uf162";
    public const string ArrowDown91 = "\uf886";
    public const string ArrowDownAZ = "\uf15d";
    public const string ArrowDownLong = "\uf175";
    public const string ArrowDownZA = "\uf881";
    public const string ArrowLeft = "\uf060";
    public const string ArrowLeftLong = "\uf177";
    public const string ArrowRight = "\uf061";
    public const string ArrowRightLong = "\uf178";
    public const string ArrowUp = "\uf062";
    public const string ArrowUp19 = "\uf163";
    public const string ArrowUp91 = "\uf887";
    public const string ArrowUpAZ = "\uf15e";
    public const string ArrowUpLong = "\uf176";
    public const string ArrowUpZA = "\uf882";
    public const string ArrowsLeftRight = "\uf07e";
    public const string ArrowsRotate = "\uf021";
    public const string ArrowsSplitUpAndLeft = "\ue4bc";
    public const string ArrowsUpDown = "\uf07d";
    public const string Ban = "\uf05e";
    public const string Bomb = "\uf1e2";
    public const string Book = "\uf02d";
    public const string Box = "\uf466";
    public const string Bullhorn = "\uf0a1";
    public const string Burst = "\ue4dc";
    public const string CaretDown = "\uf0d7";
    public const string CaretLeft = "\uf0d9";
    public const string CaretRight = "\uf0da";
    public const string CaretUp = "\uf0d8";
    public const string Check = "\uf00c";
    public const string ChevronDown = "\uf078";
    public const string ChevronLeft = "\uf053";
    public const string ChevronRight = "\uf054";
    public const string ChevronUp = "\uf077";
    public const string CircleDot = "\uf192";
    public const string CircleInfo = "\uf05a";
    public const string CircleMinus = "\uf056";
    public const string CirclePlus = "\uf055";
    public const string CircleQuestion = "\uf059";
    public const string CircleXmark = "\uf057";
    public const string ClipboardList = "\uf46d";
    public const string Clock = "\uf017";
    public const string Crosshairs = "\uf05b";
    public const string DiceOne = "\uf525";
    public const string DoorOpen = "\uf52b";
    public const string Download = "\uf019";
    public const string Eject = "\uf052";
    public const string Envelope = "\uf0e0";
    public const string EqualsSign = "\u003d";
    public const string Exclamation = "\u0021";
    public const string Eye = "\uf06e";
    public const string EyeSlash = "\uf070";
    public const string FileImport = "\uf56f";
    public const string Flask = "\uf0c3";
    public const string FloppyDisk = "\uf0c7";
    public const string GaugeHigh = "\uf625";
    public const string Gavel = "\uf0e3";
    public const string Gear = "\uf013";
    public const string HardDrive = "\uf0a0";
    public const string HeartPulse = "\uf21e";
    public const string House = "\uf015";
    public const string IdCard = "\uf2c2";
    public const string Industry = "\uf275";
    public const string Link = "\uf0c1";
    public const string LinkSlash = "\uf127";
    public const string Lock = "\uf023";
    public const string LockOpen = "\uf3c1";
    public const string MagnifyingGlass = "\uf002";
    public const string Map = "\uf279";
    public const string Minus = "\uf068";
    public const string PaperPlane = "\uf1d8";
    public const string Pause = "\uf04c";
    public const string Pen = "\uf304";
    public const string PersonRunning = "\uf70c";
    public const string Pills = "\uf484";
    public const string Play = "\uf04b";
    public const string Plus = "\u002b";
    public const string PowerOff = "\uf011";
    public const string Print = "\uf02f";
    public const string Radiation = "\uf7b9";
    public const string Repeat = "\uf363";
    public const string RotateLeft = "\uf2ea";
    public const string RotateRight = "\uf2f9";
    public const string Server = "\uf233";
    public const string ShieldHalved = "\uf3ed";
    public const string Skull = "\uf54c";
    public const string Snowplow = "\uf7d2";
    public const string Spinner = "\uf110";
    public const string SquareMinus = "\uf146";
    public const string SquarePlus = "\uf0fe";
    public const string Stop = "\uf04d";
    public const string Syringe = "\uf48e";
    public const string Trash = "\uf1f8";
    public const string TriangleExclamation = "\uf071";
    public const string UpRightFromSquare = "\uf35d";
    public const string Upload = "\uf093";
    public const string User = "\uf007";
    public const string Users = "\uf0c0";
    public const string Wrench = "\uf0ad";
    public const string Xmark = "\uf00d";

    public static readonly Color Idle = Color.Black;
    public static readonly Color Hover = Color.White;
    public static readonly Color Yellow = Color.FromHex("#FFD21A");

    public static RMCIconLabelRow AddIconLabel(
        this Button button,
        string icon,
        string text,
        float size = 16,
        Control.HAlignment alignment = Control.HAlignment.Left,
        Color? fontColor = null)
    {
        button.Text = null;

        var row = new RMCIconLabelRow { HorizontalAlignment = alignment };
        row.Icon.IconSize = size;
        row.SetIcon(icon);
        row.Label.Text = text;

        if (fontColor != null)
            row.Label.FontColorOverride = fontColor;

        if (alignment == Control.HAlignment.Stretch)
        {
            row.Label.HorizontalExpand = true;
            row.Label.ClipText = true;
        }

        row.BindHover(button);
        button.AddChild(row);
        return row;
    }
}
