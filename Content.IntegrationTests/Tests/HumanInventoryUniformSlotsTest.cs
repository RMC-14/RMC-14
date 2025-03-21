using Content.Shared.Inventory;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests
{
    // Tests the behavior of InventoryComponent.
    // i.e. the interaction between uniforms and the pocket/ID slots.
    // and also how big items don't fit in pockets.
    [TestFixture]
    public sealed class HumanInventoryUniformSlotsTest
    {
        [TestPrototypes]
        private const string Prototypes = @"
- type: entity
  name: HumanUniformDummy
  id: HumanUniformDummy
  components:
  - type: Inventory
  - type: ContainerContainer

- type: entity
  name: UniformDummy
  id: UniformDummy
  components:
  - type: Clothing
    slots: [innerclothing]
  - type: Item
    size: Tiny

- type: entity
  name: IDCardDummy
  id: IDCardDummy
  components:
  - type: Clothing
    slots:
    - idcard
  - type: Item
    size: Tiny
  - type: IdCard

- type: entity
  name: FlashlightDummy
  id: FlashlightDummy
  components:
  - type: Item
    size: Tiny

- type: entity
  name: ToolboxDummy
  id: ToolboxDummy
  components:
  - type: Item
    size: Huge

- type: entity
  name: BeltDummy
  id: BeltDummy
  components:
  - type: Clothing
    slots:
    - belt
  - type: Item
    size: Tiny
";
        [Test]
        public async Task Test()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;
            var testMap = await pair.CreateTestMap();
            var coordinates = testMap.GridCoords;

            EntityUid human = default;
            EntityUid uniform = default;
            EntityUid idCard = default;
            EntityUid pocketItem = default;
            EntityUid belt = default;

            InventorySystem invSystem = default!;
            var mapSystem = server.System<SharedMapSystem>();
            var entityMan = server.ResolveDependency<IEntityManager>();

            await server.WaitAssertion(() =>
            {
                invSystem = entityMan.System<InventorySystem>();

                human = entityMan.SpawnEntity("HumanUniformDummy", coordinates);
                uniform = entityMan.SpawnEntity("UniformDummy", coordinates);
                idCard = entityMan.SpawnEntity("IDCardDummy", coordinates);
                pocketItem = entityMan.SpawnEntity("FlashlightDummy", coordinates);
                belt = entityMan.SpawnEntity("BeltDummy", coordinates);
                var tooBigItem = entityMan.SpawnEntity("ToolboxDummy", coordinates);


                Assert.Multiple(() =>
                {
                    Assert.That(invSystem.CanEquip(human, uniform, "jumpsuit", out _));
                    Assert.That(invSystem.CanEquip(human, idCard, "id", out _));

                    // Can't equip any of these since no uniform!
                    Assert.That(invSystem.CanEquip(human, pocketItem, "pocket1", out _), Is.False);
                    Assert.That(invSystem.CanEquip(human, tooBigItem, "pocket2", out _), Is.False); // This one fails either way.
                    Assert.That(invSystem.CanEquip(human, belt, "belt", out _), Is.False);
                });

                Assert.Multiple(() =>
                {
                    Assert.That(invSystem.TryEquip(human, uniform, "jumpsuit"));
                    Assert.That(invSystem.TryEquip(human, idCard, "id"));
                });

#pragma warning disable NUnit2045
                Assert.That(invSystem.CanEquip(human, tooBigItem, "pocket1", out _), Is.False); // Still failing!
                Assert.That(invSystem.TryEquip(human, pocketItem, "pocket1"));
                Assert.That(invSystem.TryEquip(human, belt, "belt"));
#pragma warning restore NUnit2045

                Assert.Multiple(() =>
                {
                    Assert.That(IsDescendant(idCard, human, entityMan));
                    Assert.That(IsDescendant(pocketItem, human, entityMan));
                    Assert.That(IsDescendant(belt, human, entityMan));
                });

                // Now drop the jumpsuit.
                Assert.That(invSystem.TryUnequip(human, "jumpsuit"));
            });

            await server.WaitRunTicks(2);

            await server.WaitAssertion(() =>
            {
                Assert.Multiple(() =>
                {
                    // Items have been dropped!
                    Assert.That(IsDescendant(uniform, human, entityMan), Is.False);
                    Assert.That(IsDescendant(idCard, human, entityMan));
                    Assert.That(IsDescendant(pocketItem, human, entityMan), Is.False);
                    Assert.That(IsDescendant(belt, human, entityMan), Is.False);

                    // Ensure everything null here.
                    Assert.That(!invSystem.TryGetSlotEntity(human, "jumpsuit", out _));
                    Assert.That(!invSystem.TryGetSlotEntity(human, "id", out _), Is.False);
                    Assert.That(!invSystem.TryGetSlotEntity(human, "pocket1", out _));
                    Assert.That(!invSystem.TryGetSlotEntity(human, "belt", out _));
                });

                mapSystem.DeleteMap(testMap.MapId);
            });

            await pair.CleanReturnAsync();
        }

        private static bool IsDescendant(EntityUid descendant, EntityUid parent, IEntityManager entManager)
        {
            var xforms = entManager.GetEntityQuery<TransformComponent>();
            var tmpParent = xforms.GetComponent(descendant).ParentUid;
            while (tmpParent.IsValid())
            {
                if (tmpParent == parent)
                {
                    return true;
                }

                tmpParent = xforms.GetComponent(tmpParent).ParentUid;
            }

            return false;
        }
    }
}
