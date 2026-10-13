
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// PlayMode smoke test: loads the scene, runs the animation, starts the tour and fast-forwards
/// through every line, failing on any exception or error log. Run via Window ▸ General ▸ Test Runner.
/// </summary>
public class TourSmokeTest
{
    [UnityTest]
    public IEnumerator TourRunsThroughEveryLineWithoutErrors()
    {
        LogAssert.ignoreFailingMessages = false;
        Time.captureFramerate = 30; // deterministic frame time (headless runs otherwise tick with ~0 deltaTime)
        SceneManager.LoadScene("zeta");
        yield return null;
        yield return null;

        Main main = Object.FindAnyObjectByType<Main>();
        Assert.IsNotNull(main, "Main not found in scene");
        TourDirector director = TourDirector.Instance;
        Assert.IsNotNull(director, "TourDirector was not bootstrapped");

        // exact zeta vs Euler–Maclaurin closure
        foreach (double t in new[] { 0.5, 14.134725, 25.01, 60.0, 100.0 })
        {
            Meta.Numerics.Complex s = new(.5d, t);
            ZetaMath.SpiralDecomposition d = ZetaMath.Decompose(s, ZetaMath.TermCount(t));
            Meta.Numerics.Complex exact = ZetaMath.Exact(s);
            Assert.Less(Meta.Numerics.ComplexMath.Abs(d.Total - exact), 1e-8, $"closure error at t={t}");
        }

        // plain animation
        main.Speed = 30;
        main.Play();
        for (int i = 0; i < 90; i++) yield return null;
        Assert.Greater(main.ZeroCount, 0, "no zeros passed while animating");
        main.Pause();

        // the tour, skipping each line quickly
        director.StartTour();
        yield return null;
        yield return null;
        Assert.IsTrue(director.Running);
        int guard = 0;
        while (director.Running && guard++ < 400)
        {
            for (int i = 0; i < 6; i++) yield return null;
            director.SendMessage("SkipCurrentLine", SendMessageOptions.DontRequireReceiver);
        }

        Assert.IsFalse(director.Running, "tour did not finish");
        Assert.Greater(main.CurrentT, 10, "animation did not advance during the tour");
        Time.captureFramerate = 0;
    }
}

