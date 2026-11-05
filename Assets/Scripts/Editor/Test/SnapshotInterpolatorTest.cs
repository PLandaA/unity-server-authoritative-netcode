using NUnit.Framework;
using UnityEngine;

public class SnapshotInterpolatorTest{
    private SnapshotInterpolator snapshotInterpolator;

    [SetUp]
    public void Setup(){
        snapshotInterpolator = new();
    }

    [Test]
    public void Sample_BetweenTwo_Interpolates(){
        snapshotInterpolator.Add(0, new Vector2(0, 0));
        snapshotInterpolator.Add(50, new Vector2(1, 0));
        Assert.AreEqual(0.5f, snapshotInterpolator.Sample(25).x);
        Assert.AreEqual(0, snapshotInterpolator.Sample(0).x);
        Assert.AreEqual(1, snapshotInterpolator.Sample(50).x);
    }

    [Test]
    public void Sample_AfterLast_ClampsToLast(){
        snapshotInterpolator.Add(0, new Vector2(0, 0));
        snapshotInterpolator.Add(50, new Vector2(1, 0));
        Assert.AreEqual(1, snapshotInterpolator.Sample(70).x);
    }

    [Test]
    public void Sample_BeforeFirst_ClampsToFirst(){
        snapshotInterpolator.Add(0, new Vector2(0, 0));
        snapshotInterpolator.Add(50, new Vector2(1, 0));
        Assert.AreEqual(0, snapshotInterpolator.Sample(-10).x);
    }

    [Test]
    public void Sample_SingleSample_ReturnsIt(){
        snapshotInterpolator.Add(50, new Vector2(1, 0));
        Assert.AreEqual(1, snapshotInterpolator.Sample(0).x);
        Assert.AreEqual(1, snapshotInterpolator.Sample(50).x);
        Assert.AreEqual(1, snapshotInterpolator.Sample(100).x);
    }

    [Test]
    public void Sample_Empty_ReturnsZero(){
        Assert.AreEqual(0, snapshotInterpolator.Sample(0).x);
    }

    [Test]
    public void Sample_PrunesOlderSamples(){
        snapshotInterpolator.Add(0, new Vector2(0, 0));
        snapshotInterpolator.Add(50, new Vector2(1, 0));
        snapshotInterpolator.Add(100, new Vector2(2, 0));
        snapshotInterpolator.Sample(75);
        Assert.AreEqual(2, snapshotInterpolator.Count);
    }

    [Test]
    public void Sample_SameTimestamp_DoesNotThrow(){
        snapshotInterpolator.Add(50, new Vector2(0, 0));
        snapshotInterpolator.Add(50, new Vector2(1, 0));
        Assert.DoesNotThrow(() => snapshotInterpolator.Sample(50));
    }
}