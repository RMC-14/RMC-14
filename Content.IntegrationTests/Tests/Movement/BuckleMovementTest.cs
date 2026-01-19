using Content.Shared.Alert;
using Content.Shared.Buckle.Components;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests.Movement;

public sealed class BuckleMovementTest : MovementTest
{
    // Check that interacting with a chair straps you to it and prevents movement.
    [Test]
    public async Task ChairTest()
    {
        await SpawnTarget("Chair");

        var cAlert = Client.System<AlertsSystem>();
        var sAlert = Server.System<AlertsSystem>();
        var buckle = Comp<BuckleComponent>(Player);
        var strap = Comp<StrapComponent>(Target);

#pragma warning disable RA0002
        buckle.Delay = TimeSpan.Zero;
#pragma warning restore RA0002

        // Initially not buckled to the chair, and standing off to the side
        Assert.That(Delta(), Is.InRange(0.9f, 1.1f));
        Assert.That(buckle.Buckled, Is.False);
        Assert.That(buckle.BuckledTo, Is.Null);
        Assert.That(strap.BuckledEntities, Is.Empty);
        if (strap.BuckledAlertType != null) //RMC14
        {
            Assert.That(cAlert.IsShowingAlert(CPlayer, strap.BuckledAlertType.Value), Is.False);
            Assert.That(sAlert.IsShowingAlert(SPlayer, strap.BuckledAlertType.Value), Is.False);
        }

        // Interact results in being buckled to the chair
        await Interact();
        Assert.That(Delta(), Is.InRange(-0.01f, 0.01f));
        Assert.That(buckle.Buckled, Is.True);
        Assert.That(buckle.BuckledTo, Is.EqualTo(STarget));
        Assert.That(strap.BuckledEntities, Is.EquivalentTo(new[] { SPlayer }));
        if (strap.BuckledAlertType != null)// RMC14
        {
            Assert.That(cAlert.IsShowingAlert(CPlayer, strap.BuckledAlertType.Value), Is.True);
            Assert.That(sAlert.IsShowingAlert(SPlayer, strap.BuckledAlertType.Value), Is.True);
        }

        // Attempting to walk away does nothing
        await Move(DirectionFlag.East, 1);
        Assert.That(Delta(), Is.InRange(-0.01f, 0.01f));
        Assert.That(buckle.Buckled, Is.True);
        Assert.That(buckle.BuckledTo, Is.EqualTo(STarget));
        Assert.That(strap.BuckledEntities, Is.EquivalentTo(new[] { SPlayer }));
        if (strap.BuckledAlertType != null) //RMC14
        {
            Assert.That(cAlert.IsShowingAlert(CPlayer, strap.BuckledAlertType.Value), Is.True);
            Assert.That(sAlert.IsShowingAlert(SPlayer, strap.BuckledAlertType.Value), Is.True);
        }

        // Interacting again will unbuckle the player
        await Interact();
        Assert.That(Delta(), Is.InRange(-0.5f, 0.5f));
        Assert.That(buckle.Buckled, Is.False);
        Assert.That(buckle.BuckledTo, Is.Null);
        Assert.That(strap.BuckledEntities, Is.Empty);
        if (strap.BuckledAlertType != null) //RMC14
        {
            Assert.That(cAlert.IsShowingAlert(CPlayer, strap.BuckledAlertType.Value), Is.False);
            Assert.That(sAlert.IsShowingAlert(SPlayer, strap.BuckledAlertType.Value), Is.False);
        }

        // And now they can move away
        await Move(DirectionFlag.SouthEast, 1);
        Assert.That(Delta(), Is.LessThan(-1));
    }
}
