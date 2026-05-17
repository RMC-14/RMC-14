using System.Collections.Generic;
using Content.Shared._RMC14.Weather;
using NUnit.Framework;
using Robust.Shared.Maths;

namespace Content.Tests.Shared._RMC14.Weather;

[TestFixture]
[Parallelizable]
public sealed class RMCWeatherSightObstructionTest
{
    [Test]
    public void ProtectedTargetIsNotObscured()
    {
        var exposed = new HashSet<Vector2i>
        {
            new(1, 0),
            new(2, 0),
        };

        var depth = RMCWeatherSightObstruction.CalculateWeatherDepth(new Vector2i(0, 0),
            new Vector2i(3, 0),
            exposed.Contains);

        Assert.That(depth, Is.EqualTo(0));
    }

    [Test]
    public void IndoorOriginStartsDepthAtFirstOutdoorTile()
    {
        var exposed = new HashSet<Vector2i>();
        for (var x = 1; x <= 5; x++)
        {
            exposed.Add(new Vector2i(x, 0));
        }

        var depth = RMCWeatherSightObstruction.CalculateWeatherDepth(new Vector2i(0, 0),
            new Vector2i(5, 0),
            exposed.Contains);

        Assert.That(depth, Is.EqualTo(5));
    }

    [Test]
    public void ProtectedTilesResetWeatherDepth()
    {
        var exposed = new HashSet<Vector2i>
        {
            new(1, 0),
            new(2, 0),
            new(4, 0),
            new(5, 0),
        };

        var depth = RMCWeatherSightObstruction.CalculateWeatherDepth(new Vector2i(0, 0),
            new Vector2i(5, 0),
            exposed.Contains);

        Assert.That(depth, Is.EqualTo(2));
    }

    [Test]
    public void OutdoorOriginCountsFromPlayerTile()
    {
        var exposed = new HashSet<Vector2i>();
        for (var x = 0; x <= 5; x++)
        {
            exposed.Add(new Vector2i(x, 0));
        }

        var depth = RMCWeatherSightObstruction.CalculateWeatherDepth(new Vector2i(0, 0),
            new Vector2i(5, 0),
            exposed.Contains);

        Assert.That(depth, Is.EqualTo(6));
    }

    [Test]
    public void StrongerWeatherObscuresAtShorterDepth()
    {
        const int depth = 6;

        var low = RMCWeatherSightObstruction.GetAlpha(RMCWeatherScreenOverlay.Low, depth, 1);
        var medium = RMCWeatherSightObstruction.GetAlpha(RMCWeatherScreenOverlay.Medium, depth, 1);
        var high = RMCWeatherSightObstruction.GetAlpha(RMCWeatherScreenOverlay.High, depth, 1);

        Assert.Multiple(() =>
        {
            Assert.That(low, Is.EqualTo(0));
            Assert.That(medium, Is.GreaterThan(low));
            Assert.That(high, Is.GreaterThan(medium));
            Assert.That(high, Is.LessThan(1));
        });
    }

    [Test]
    public void ProfilesUseWorldTileDepths()
    {
        Assert.Multiple(() =>
        {
            Assert.That(RMCWeatherSightObstruction.GetProfile(RMCWeatherScreenOverlay.Low),
                Is.EqualTo(new RMCWeatherSightObstructionProfile(6.5f, 9)));
            Assert.That(RMCWeatherSightObstruction.GetProfile(RMCWeatherScreenOverlay.Medium),
                Is.EqualTo(new RMCWeatherSightObstructionProfile(5.5f, 8)));
            Assert.That(RMCWeatherSightObstruction.GetProfile(RMCWeatherScreenOverlay.High),
                Is.EqualTo(new RMCWeatherSightObstructionProfile(4.5f, 7)));
        });
    }

    [Test]
    public void WeatherFadeScalesObstructionAlpha()
    {
        var alpha = RMCWeatherSightObstruction.GetAlpha(RMCWeatherScreenOverlay.High, 7, 0.5f);

        Assert.That(alpha, Is.EqualTo(0.5f));
    }

    [Test]
    public void VisionBlocksAtConfiguredThreshold()
    {
        Assert.Multiple(() =>
        {
            Assert.That(RMCWeatherSightObstruction.IsBlocked(RMCWeatherScreenOverlay.High, 7, 1), Is.True);
            Assert.That(RMCWeatherSightObstruction.IsBlocked(RMCWeatherScreenOverlay.High, 6, 1), Is.False);
            Assert.That(RMCWeatherSightObstruction.IsBlocked(RMCWeatherScreenOverlay.High, 7, 0.5f), Is.False);
        });
    }
}
