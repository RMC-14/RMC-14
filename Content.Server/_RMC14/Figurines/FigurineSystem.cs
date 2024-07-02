using System.IO;
using System.Reflection;
using Content.Shared._RMC14.Figurines;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Content.Server._RMC14.Figurines;

public sealed class FigurineSystem : EntitySystem
{
    public override void Initialize()
    {
#if !FULL_RELEASE
        SubscribeNetworkEvent<FigurineImageEvent>(OnFigurineImage);
#endif
    }

    private void OnFigurineImage(FigurineImageEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } ent)
            return;

        var name = FormatSpriteName(Name(ent)).ToLowerInvariant();
        var rsi = GetRsiPath();
        var sprites = Path.Combine(rsi, $"{name}.png");
        var baseSprite = Image.Load<Rgba32>(Path.Combine(GetRsiParent(), "patron_base.rsi/base.png"));
        var image = Image.Load<Rgba32>(ev.Image);
        baseSprite.Mutate(x =>
        {
            var imageSize = image.Size;
            var point = new Point((baseSprite.Width - imageSize.Width) / 2, (baseSprite.Height - imageSize.Height) - 2);
            x.DrawImage(image, point, 1f);
        });

        baseSprite.SaveAsPng(sprites);

        var meta = Path.Combine(rsi, "meta.json");
        var reader = new StreamReader(File.OpenRead(meta));
        var json = reader.ReadToEnd();
        json = json[..json.LastIndexOf("\"", StringComparison.InvariantCulture)];
        json = @$"{json}""
    }},
    {{
      ""name"": ""{name}""
    }}
  ]
}}
";

        reader.Close();
        File.WriteAllText(meta, json);
    }

    public string FormatId(string name)
    {
        return name.Replace(" ", "").Replace("'", "");
    }

    public string FormatSpriteName(string name)
    {
        return name.Replace(" ", "_").Replace("'", "").ToLowerInvariant();
    }

    public string GetResourcesPath()
    {
        var entry = new DirectoryInfo(Assembly.GetEntryAssembly()!.Location);
        return Path.Combine(entry.Parent!.Parent!.Parent!.FullName, "Resources");
    }

    public string GetRsiParent()
    {
        return Path.Combine(GetResourcesPath(), "Textures/_RMC14/Objects");
    }

    public string GetRsiPath()
    {
        return Path.Combine(GetRsiParent(), "patron_figurines.rsi");
    }
}
