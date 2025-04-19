﻿using System.IO;
using System.Reflection;
using Content.Server._RMC14.LinkAccount;
using Content.Server.GameTicking;
using Content.Server.Hands.Systems;
using Content.Server.Storage.EntitySystems;
using Content.Shared._RMC14.Figurines;
using Content.Shared._RMC14.Marines;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Storage;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Content.Server._RMC14.Figurines;

public sealed class FigurineSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly LinkAccountManager _linkAccount = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StorageSystem _storage = default!;

    private readonly Dictionary<string, EntProtoId> _allFigurines = new();
    private readonly HashSet<EntProtoId> _figurines = [];

    public override void Initialize()
    {
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);

        SubscribeLocalEvent<RandomPatronFigurineSpawnerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RandomPatronFigurineSpawnerComponent, GameRunLevelChangedEvent>(OnRunLevelChanged);

#if !FULL_RELEASE
        SubscribeNetworkEvent<FigurineImageEvent>(OnFigurineImage);
#endif

        _linkAccount.PatronsReloaded += ReloadActiveFigurines;
        ReloadAllFigurines();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _linkAccount.PatronsReloaded -= ReloadActiveFigurines;
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        if (ev.WasModified<EntityPrototype>())
            ReloadAllFigurines();
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (!HasComp<MarineComponent>(ev.Mob))
            return;

        if (!_allFigurines.TryGetValue(ev.Player.UserId.ToString(), out var figurineId))
            return;

        var figurine = Spawn(figurineId, MapCoordinates.Nullspace);
        if (_hands.TryPickupAnyHand(ev.Mob, figurine, false))
            return;

        var backs = _inventory.GetSlotEnumerator(ev.Mob, SlotFlags.BACK);
        while (backs.MoveNext(out var slot))
        {
            if (!TryComp(slot.ContainedEntity, out StorageComponent? storage))
                continue;

            if (_storage.Insert(slot.ContainedEntity.Value, figurine, out _, storageComp: storage))
                return;
        }
    }

    private void ReloadAllFigurines()
    {
        _allFigurines.Clear();
        foreach (var prototype in _prototypes.EnumeratePrototypes<EntityPrototype>())
        {
            if (prototype.TryGetComponent(out PatronFigurineComponent? figurine, _compFactory))
            {
                if (_allFigurines.ContainsKey(figurine.Id))
                    Log.Error($"Duplicate id {figurine.Id} found for figurine {prototype.ID}");

                _allFigurines[figurine.Id] = prototype.ID;
            }
        }

        ReloadActiveFigurines();
    }

    private void ReloadActiveFigurines()
    {
        _figurines.Clear();

        var available = _linkAccount.GetFigurines();
        foreach (var (_, figurineId) in _allFigurines)
        {
            if (_prototypes.TryIndex(figurineId, out var figurine) &&
                figurine.TryGetComponent(out PatronFigurineComponent? figurineComp, _compFactory))
            {
                if (!Guid.TryParse(figurineComp.Id, out var guid))
                {
                    Log.Error($"Invalid {figurineId} figurine ID found: {figurineComp.Id}");
                    continue;
                }

                if (!available.Contains(guid))
                    continue;

                _figurines.Add(figurineId);
            }
        }
    }

    private void OnMapInit(Entity<RandomPatronFigurineSpawnerComponent> spawner, ref MapInitEvent args)
    {
        if (_figurines.Count == 0)
            return;

        if (!Deleted(spawner))
            Spawn(_random.Pick(_figurines), Transform(spawner).Coordinates);

        QueueDel(spawner);
    }

    private void OnRunLevelChanged(Entity<RandomPatronFigurineSpawnerComponent> ent, ref GameRunLevelChangedEvent args)
    {
        if (args.New == GameRunLevel.PreRoundLobby)
            ReloadActiveFigurines();
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
            var point = new Point((baseSprite.Width - imageSize.Width) / 2, baseSprite.Height - imageSize.Height - 2);
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
