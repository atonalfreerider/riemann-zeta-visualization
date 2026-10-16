using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Renders the VR interface from the desktop simulation to PNGs, with scripted hands, so the layout, icons, hover and
/// press feedback, palm menu, passthrough/blackout and the grab maths can be reviewed (and checked) without a headset.
///
///   ZETA_OUT=/path/out  Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter VRPreviewRenderTest
///
/// Passthrough is drawn over a flat grey "room". Skipped unless ZETA_OUT is set.
/// </summary>
public class VRPreviewRenderTest
{
    string outDir;
    Camera cam;
    RenderTexture target;

    [UnityTest]
    [Timeout(1800000)]
    public IEnumerator RenderInterface()
    {
        outDir = Environment.GetEnvironmentVariable("ZETA_OUT");
        if (string.IsNullOrEmpty(outDir))
        {
            Assert.Ignore("ZETA_OUT not set; this is a render driver, not a test.");
            yield break;
        }

        Directory.CreateDirectory(outDir);
        File.WriteAllBytes(Path.Combine(outDir, "00-icons.png"), VRIcons.DumpSheet().EncodeToPNG());

        VRMode.RequestSimulation = true;
        SceneManager.LoadScene("zeta");
        yield return null;
        yield return null;

        float waited = 0f;
        while ((VRMode.App == null || !VRMode.App.Ready) && waited < 30f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        Assert.IsNotNull(VRMode.App, "VR app did not start");
        Assert.IsTrue(VRMode.App.Ready, "VR app did not become ready");
        VRApp app = VRMode.App;
        VRRig rig = app.Rig;
        cam = rig.Cam;
        cam.fieldOfView = 80f;
        target = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };

        // look down at the dock the way a user does when reaching for it
        VRSimulatedHead head = cam.GetComponent<VRSimulatedHead>();
        head.SetLook(0f, 24f);

        // let the home view settle
        for (int i = 0; i < 90; i++) yield return null;
        yield return Shot("01-home-dock");
        Debug.Log($"VR rig scale {rig.Scale:0.0}, passthrough {rig.Passthrough}");

        // the pages of the dock: experiments, views, models, space, settings
        for (int p = 1; p <= 5; p++)
        {
            app.ShowPage(p);
            for (int i = 0; i < 12; i++) yield return null;
            yield return Shot($"02-page-{p}");
        }

        app.ShowPage(0);

        // hover and press: the right index finger over the Dial chapter's button ("5  Dial" since the tour's v12 split)
        VRButton dial = null;
        int dialIndex = -1;
        foreach (VRControl c in app.Dock.Controls)
        {
            if (c is VRButton b && b.Label != null && b.Label.EndsWith("  Dial"))
            {
                dial = b;
                dialIndex = int.Parse(b.Label.Split(' ')[0]) - 1;
            }
        }

        Assert.IsNotNull(dial, "no chapter button for the dial");
        Vector3 centre = rig.Offset.InverseTransformPoint(dial.Rt.TransformPoint(dial.Rt.rect.center));
        Vector3 inFront = -app.Dock.transform.forward; // the dock faces the user; its back points away
        Vector3 inFrontLocal = rig.Offset.InverseTransformDirection(inFront);
        app.Hands.ScriptedRight = true;
        app.Hands.ScriptedRightTip = centre + inFrontLocal * 0.035f;
        for (int i = 0; i < 20; i++) yield return null;
        yield return Shot("05-hover-dial");
        Assert.IsTrue(app.Poker.Hovering(app.Hands.Right), "fingertip 3.5 cm from the button should hover it");

        app.Hands.ScriptedRightTip = centre + inFrontLocal * 0.0f;
        for (int i = 0; i < 6; i++) yield return null;
        yield return Shot("06-press-dial");
        for (int i = 0; i < 4; i++) yield return null;
        Assert.IsTrue(app.Tour.Running, "pressing the chapter button should start the tour");
        Assert.AreEqual(dialIndex, app.Tour.CurrentChapterIndex, "pressing the Dial button starts the Dial chapter");
        app.Hands.ScriptedRightTip = centre + inFrontLocal * 0.2f; // retract
        for (int i = 0; i < 10; i++) yield return null;

        // the narrated chapter running, captions on the gaze-following HUD
        app.Dock.SetOpen(false);
        yield return Wait(9f);
        yield return Shot("07-chapter-running");
        Debug.Log($"after 9 s: chapter {app.Tour.CurrentChapterId}, line {app.Tour.CurrentLineId}, rig scale {rig.Scale:0.0}");

        // pause / resume
        app.Tour.SetPaused(true);
        float timeBefore = Time.time;
        yield return Wait(0.5f);
        Assert.AreEqual(timeBefore, Time.time, 0.001f, "game time should stand still while paused");
        app.Tour.SetPaused(false);

        // palm menu: left hand up in front of the face, palm toward the eyes
        head.SetLook(0f, 0f);
        yield return null;
        app.Hands.ScriptedLeft = true;
        Vector3 eye = rig.Cam.transform.localPosition;
        Vector3 toward = rig.YawHome * Vector3.forward;
        app.Hands.ScriptedLeftTip = eye + toward * 0.38f + Vector3.up * 0.05f + rig.YawHome * Vector3.left * 0.12f;
        app.Hands.ScriptedLeftPoint = Vector3.up;
        app.Hands.ScriptedLeftBack = toward; // the back of the hand faces away from the user, so the palm faces them
        yield return WaitUntil(() => app.Palm.Open, 6f);
        Assert.IsTrue(app.Palm.Open, "palm menu should open when the palm faces the user");
        yield return Wait(0.6f);
        yield return Shot("08-palm-menu");
        app.Hands.ScriptedLeft = false;
        yield return WaitUntil(() => !app.Palm.Open, 6f);
        Assert.IsFalse(app.Palm.Open, "palm menu should close when the hand goes away");

        // blackout
        app.SetRoom(false);
        yield return Wait(0.8f);
        yield return Shot("09-blackout");
        app.SetRoom(true);

        // grab: one hand pinches and drags the model 30 cm to the right
        app.Dock.SetOpen(false);
        app.Tour.StopTour();
        yield return Wait(0.5f);
        Vector3 start = rig.Current.Position;
        float scale0 = rig.Scale;
        Vector3 pinchAt = eye + toward * 0.7f + Vector3.up * 0.1f;
        app.Hands.ScriptedRight = true;
        app.Hands.ScriptedRightTip = pinchAt;
        app.Hands.ScriptedRightPinch = 0f;
        yield return Wait(0.3f);
        app.Hands.ScriptedRightPinch = 1f;
        yield return WaitUntil(() => app.Grab.Holding, 6f);
        Assert.IsTrue(app.Grab.Holding, "a pinch away from the menus should take hold of the model");
        for (int i = 1; i <= 20; i++)
        {
            app.Hands.ScriptedRightTip = pinchAt + rig.YawHome * Vector3.right * (0.3f * i / 20f);
            yield return null;
        }

        yield return Wait(0.2f);
        Vector3 moved = rig.Current.Position - start;
        Debug.Log($"one-hand grab: rig moved {moved} (hand moved 0.3 m right), scale {scale0:0.0} -> {rig.Scale:0.0}");
        Assert.AreEqual(0.3f * rig.Scale, moved.magnitude, 0.03f * rig.Scale, "dragging 30 cm should move the model by 30 cm in the user's space");
        Assert.IsFalse(app.Guide.Follow, "grabbing hands the model to the user");
        app.Hands.ScriptedRightPinch = 0f;
        yield return Wait(0.3f);

        // two hands pull apart to twice the distance: the model should become half the size relative to the user... i.e. the rig scale halves
        float s1 = rig.Scale;
        Vector3 leftAt = eye + toward * 0.7f - rig.YawHome * Vector3.right * 0.1f;
        Vector3 rightAt = eye + toward * 0.7f + rig.YawHome * Vector3.right * 0.1f;
        app.Hands.ScriptedLeft = true;
        app.Hands.ScriptedLeftTip = leftAt;
        app.Hands.ScriptedLeftPoint = Vector3.zero;
        app.Hands.ScriptedLeftBack = Vector3.zero;
        app.Hands.ScriptedRightTip = rightAt;
        yield return Wait(0.2f);
        app.Hands.ScriptedLeftPinch = 1f;
        app.Hands.ScriptedRightPinch = 1f;
        yield return WaitUntil(() => app.Grab.TwoHanded, 6f);
        Assert.IsTrue(app.Grab.TwoHanded, "two pinches should be a two-handed hold");
        for (int i = 1; i <= 20; i++)
        {
            float spread = 1f + i / 20f; // 1 -> 2
            app.Hands.ScriptedLeftTip = eye + toward * 0.7f - rig.YawHome * Vector3.right * 0.1f * spread;
            app.Hands.ScriptedRightTip = eye + toward * 0.7f + rig.YawHome * Vector3.right * 0.1f * spread;
            yield return null;
        }

        yield return Wait(0.2f);
        Debug.Log($"two-hand grab: rig scale {s1:0.00} -> {rig.Scale:0.00} (hands moved twice as far apart)");
        Assert.AreEqual(s1 * 0.5f, rig.Scale, s1 * 0.04f, "spreading the hands should enlarge the model by the same factor");
        yield return Shot("10-after-two-hand-grab");
        Debug.Log("VR preview finished");
    }

    static VRButton FindButton(VRApp app, string label)
    {
        foreach (VRControl c in app.Dock.Controls)
        {
            if (c is VRButton b && b.Label == label) return b;
        }

        return null;
    }

    static IEnumerator WaitUntil(Func<bool> condition, float timeout)
    {
        float t = 0f;
        while (!condition() && t < timeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    static IEnumerator Wait(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    IEnumerator Shot(string name)
    {
        yield return null;
        cam.targetTexture = target;
        cam.Render();
        cam.targetTexture = null;
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = target;
        Texture2D tex = new(target.width, target.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        tex.Apply();
        RenderTexture.active = previous;

        // composite over a flat grey "room" so passthrough (alpha 0) is visible
        Color32[] px = tex.GetPixels32();
        for (int i = 0; i < px.Length; i++)
        {
            float a = px[i].a / 255f; // the eye buffer holds premultiplied colour
            px[i] = new Color32(
                (byte)Mathf.Min(255, px[i].r + (1f - a) * 92f),
                (byte)Mathf.Min(255, px[i].g + (1f - a) * 96f),
                (byte)Mathf.Min(255, px[i].b + (1f - a) * 104f), 255);
        }

        tex.SetPixels32(px);
        File.WriteAllBytes(Path.Combine(outDir, name + ".png"), tex.EncodeToPNG());
        UnityEngine.Object.Destroy(tex);
    }
}
