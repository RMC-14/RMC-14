using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client._RMC14.Photo;

public sealed class PhotoWindow : DefaultWindow
{
    private readonly TextureRect _image;
    private static readonly Vector2i MaxDefaultWindowSize = new (640, 640);

    public PhotoWindow()
    {
        RobustXamlLoader.Load(this);

        _image = new TextureRect
        {
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            VerticalAlignment = VAlignment.Center,
            HorizontalAlignment = HAlignment.Center,
        };

        Contents.AddChild(_image);
    }

    public void SetImage(OwnedTexture texture)
    {
        _image.Texture = texture;

        var size = texture.Size;
        if (size > MaxDefaultWindowSize)
            size = MaxDefaultWindowSize;

        SetSize = size + new Vector2i(50, 50);
    }

    public void SetName(string name)
    {
        Title = name;
    }
}
