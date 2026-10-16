using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>One control of a lab model: a button, a toggle or a slider, shown on the desktop panel and on the VR Models page.</summary>
public class LabControl
{
    public string Label;
    public VRIcons.Id Icon;
    public Action Clicked;
    /// <summary>A toggle shows its state.</summary>
    public Func<bool> On;
    /// <summary>A label that follows the state (e.g. "Tree: 7").</summary>
    public Func<string> DynamicLabel;
    public string Keys = "";
    public bool Slider;
    public float Min, Max;
    public Func<float> Get;
    public Action<float> Set;
    public Func<float, string> Format;
    /// <summary>Width on the VR grid, in cells (sliders take two or three).</summary>
    public int Cells = 1;

    public string Text => DynamicLabel != null ? DynamicLabel() : Label;

    public static LabControl Button(string label, VRIcons.Id icon, Action clicked, string keys = "") =>
        new() { Label = label, Icon = icon, Clicked = clicked, Keys = keys };

    public static LabControl Toggle(string label, VRIcons.Id icon, Func<bool> on, Action clicked, string keys = "") =>
        new() { Label = label, Icon = icon, On = on, Clicked = clicked, Keys = keys };

    public static LabControl SliderOf(string label, float min, float max, Func<float> get, Action<float> set, Func<float, string> format, int cells = 2, string keys = "") =>
        new() { Label = label, Slider = true, Min = min, Max = max, Get = get, Set = set, Format = format, Cells = cells, Keys = keys };
}

/// <summary>Where a model is seen from: an orbit around <see cref="Focus"/> with the model's up along world −Z (as in the experiment cues).</summary>
public struct LabView
{
    public Vector3 Focus;
    public float Distance, Yaw, Pitch, Fov;

    public static readonly Vector3 Up = Vector3.back;

    public Vector3 Eye
    {
        get
        {
            float a = Yaw * Mathf.Deg2Rad, e = Pitch * Mathf.Deg2Rad;
            Vector3 h = new(Mathf.Cos(a), Mathf.Sin(a), 0f);
            return Focus + (h * Mathf.Cos(e) + Up * Mathf.Sin(e)) * Distance;
        }
    }

    /// <summary>Screen-up: the model's up tipped back by the elevation, so the view can pass overhead.</summary>
    public Vector3 ScreenUp
    {
        get
        {
            float a = Yaw * Mathf.Deg2Rad, e = Pitch * Mathf.Deg2Rad;
            Vector3 h = new(Mathf.Cos(a), Mathf.Sin(a), 0f);
            return (Up * Mathf.Cos(e) - h * Mathf.Sin(e)).normalized;
        }
    }
}

/// <summary>An interactive model: built once, far from the tour's stage, then shown, picked and steered by its controls.</summary>
public abstract class LabModel
{
    public abstract string Id { get; }
    public abstract string Title { get; }
    public abstract string Short { get; }
    public abstract VRIcons.Id Icon { get; }
    public Transform Root { get; private set; }
    public bool Built { get; private set; }
    public readonly List<LabControl> Controls = new();
    protected ModelLab Lab { get; private set; }
    /// <summary>The model's origin in the world (its floor's centre).</summary>
    protected Vector3 O { get; private set; }

    public void Build(ModelLab lab, Transform root, Vector3 origin)
    {
        Lab = lab;
        Root = root;
        O = origin;
        OnBuild();
        Built = true;
    }

    protected abstract void OnBuild();
    public abstract LabView Home { get; }
    public virtual void OnEnter() { }
    public virtual void OnExit() { }
    public virtual void Tick(float dt) { }
    public abstract int PickCount { get; }
    public abstract Vector3 PickPoint(int i);
    public virtual string PickLabel(int i) => "";
    public virtual void OnPick(int i) { }
    public virtual bool OnKey(Keyboard k) => false;
}

/// <summary>
/// The model lab: interactive versions of what the experiment videos show, one at a time, far from the tour's stage.
///   Paths    the factor funnel of "The paths back": pick any n, watch it drop to 1, see the whole tree of first steps
///   E8       the 240 roots on their Coxeter planes: turn by the Coxeter element, reflect in a root, apply a Frobenius
///   Unified  E8's wheel under the funnel, ζ(½ + it) split into its 30 hour sums, the Sacks spiral as a second layout
/// Desktop: drag to orbit, wheel to zoom, right drag (or Shift + drag) to move, click a point to pick it; each model has
/// keys, and a panel on the right lists its controls. VR: the model is placed in front of the user like a tour shot (pinch
/// moves it), touching a point with a fingertip picks it, and the dock's Models page holds the controls and the readout.
/// </summary>
public class ModelLab : MonoBehaviour
{
    public static ModelLab Instance { get; private set; }
    /// <summary>Where the lab's models stand: far from the saddle and every experiment scene.</summary>
    public static readonly Vector3 Origin = new(90000f, 0f, 0f);

    public readonly List<LabModel> Models = new();
    public LabModel Active { get; private set; }
    public string Status { get; private set; } = "";
    public int StripNumber { get; private set; }
    public int StripLit { get; private set; }
    public int Hover { get; private set; } = -1;
    /// <summary>The status, the strip or the active model changed (the menus refresh).</summary>
    public event Action Changed;

    TourDirector director;
    Transform root;
    Camera cam;
    CameraControl manual;
    bool manualWasEnabled;
    LabView view, viewTarget;
    bool pressed, dragging, panning;
    Vector2 pressPos, lastPos;
    GameObject hoverDot;
    TextBox hoverLabel;
    int hoverLabelFor = -2;
    readonly bool[] fingerArmed = { true, true };
    readonly int[] fingerOn = { -1, -1 };

    public bool IsActive => Active != null;

    public static ModelLab Create(TourDirector director)
    {
        GameObject go = new("Model Lab");
        ModelLab lab = go.AddComponent<ModelLab>();
        lab.director = director;
        lab.root = go.transform;
        lab.cam = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
        lab.manual = lab.cam != null ? lab.cam.GetComponent<CameraControl>() : null;
        lab.Models.Add(new PathsModel());
        lab.Models.Add(new E8Model());
        lab.Models.Add(new UnifiedModel());
        Instance = lab;
        if (!VRMode.Enabled) LabPanel.Create(lab);
        return lab;
    }

    public LabModel Find(string id) => Models.Find(m => m.Id == id);

    /// <summary>Show a model (building it the first time). Asking for the one already shown resets its view.</summary>
    public void Enter(string id)
    {
        LabModel m = Find(id);
        if (m == null) return;
        if (Active == m)
        {
            ResetView(true);
            return;
        }

        if (Active != null) Leave(Active);
        else TakeStage();
        foreach (LaunchTitle card in FindObjectsByType<LaunchTitle>(FindObjectsSortMode.None)) Destroy(card.gameObject);

        if (!m.Built)
        {
            GameObject r = new(m.Title);
            r.transform.SetParent(root, false);
            m.Build(this, r.transform, Origin);
        }

        m.Root.gameObject.SetActive(true);
        Active = m;
        Hover = -1;
        SetStrip(0, 0);
        m.OnEnter();
        ResetView(true);
        Changed?.Invoke();
    }

    /// <summary>Close the lab and give the stage back (the home view on the desktop, the standing view in VR).</summary>
    public void Exit()
    {
        if (Active == null) return;
        Close();
        if (VRMode.Enabled)
        {
            if (VRMode.App != null) VRMode.App.ApplyView("explore");
        }
        else if (director.Home != null) director.Home.Enter();
    }

    /// <summary>Hide the model and its readout, leaving the camera alone (a chapter, a module or a view is taking the stage).</summary>
    public void Close()
    {
        if (Active == null) return;
        Leave(Active);
        Active = null;
        SetStatus("");
        SetStrip(0, 0);
        ClearHover();
        if (manual != null) manual.enabled = manualWasEnabled;
        if (director.Captions != null) director.Captions.StripAtBottom = false;
        Changed?.Invoke();
    }

    /// <summary>Keep looking at a moving point without undoing the user's own orbit, zoom or hold on the model.</summary>
    public void FollowFocus(LabView v)
    {
        viewTarget.Focus = v.Focus;
        if (!VRMode.Enabled) return;
        VRGuide guide = VRMode.Guide;
        if (guide == null || !guide.Follow) return;
        guide.SetCue(new CameraRig.CueState { Position = v.Eye, LookAt = v.Focus, Up = v.ScreenUp, Fov = v.Fov > 0f ? v.Fov : 55f });
    }

    void Leave(LabModel m)
    {
        m.OnExit();
        m.Root.gameObject.SetActive(false);
    }

    /// <summary>Stop the story and quieten the stage: the lab is far away, but the tour, the home growth and the dial must stop.</summary>
    void TakeStage()
    {
        if (director.Running)
        {
            if (director.PlayingExperiment) director.LeaveExperiment();
            else director.StopTour();
        }

        if (director.Home != null) director.Home.Exit();
        if (director.Explore != null && director.Explore.Active) director.Explore.Exit();
        if (director.Orbit != null) director.Orbit.End();
        director.Main.CancelAnimations();
        director.Main.Pause();
        director.Rig.Release();
        if (director.Captions != null) director.Captions.StripAtBottom = true;
        if (manual != null)
        {
            manualWasEnabled = manual.enabled;
            manual.enabled = false;
        }
    }

    public void ResetView(bool snap)
    {
        if (Active == null) return;
        SetView(Active.Home, snap);
    }

    /// <summary>Frame the model: the desktop camera eases there; in VR the guide places the model so it is seen this way.</summary>
    public void SetView(LabView v, bool snap)
    {
        viewTarget = v;
        if (snap) view = v;
        if (!VRMode.Enabled) return;
        director.Rig.Release();
        VRGuide guide = VRMode.Guide;
        if (guide == null) return;
        guide.ResetOffsets();
        guide.SetFollow(true, true);
        guide.SetCue(new CameraRig.CueState { Position = v.Eye, LookAt = v.Focus, Up = v.ScreenUp, Fov = v.Fov > 0f ? v.Fov : 55f }, snap);
    }

    public void SetStatus(string text)
    {
        if (Status == text) return;
        Status = text;
        if (!VRMode.Enabled && director.Captions != null)
        {
            if (string.IsNullOrEmpty(text)) director.Captions.HideBoard();
            else director.Captions.ShowBoard(text);
        }

        Changed?.Invoke();
    }

    /// <summary>Show n's column laid on its side with <paramref name="lit"/> pieces lit (0 for n hides it).</summary>
    public void SetStrip(int n, int lit)
    {
        bool rebuilt = n != StripNumber;
        StripNumber = n;
        StripLit = lit;
        if (!VRMode.Enabled && director.Captions != null)
        {
            if (n < 2) director.Captions.HideStrip();
            else
            {
                if (rebuilt) director.Captions.ShowStrip(n);
                director.Captions.StripLight(lit);
            }
        }

        Changed?.Invoke();
    }

    // ---- per frame ---------------------------------------------------------------------------------------

    void Update()
    {
        if (Active == null) return;
        if (VRMode.Enabled) UpdateFingers();
        else UpdateDesktop();
        Active.Tick(Time.deltaTime);
    }

    void LateUpdate()
    {
        if (Active == null || VRMode.Enabled || cam == null) return;
        float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 7f);
        view.Focus = Vector3.Lerp(view.Focus, viewTarget.Focus, k);
        view.Distance = Mathf.Lerp(view.Distance, viewTarget.Distance, k);
        view.Yaw = Mathf.LerpAngle(view.Yaw, viewTarget.Yaw, k);
        view.Pitch = Mathf.Lerp(view.Pitch, viewTarget.Pitch, k);
        view.Fov = Mathf.Lerp(view.Fov <= 0f ? 55f : view.Fov, viewTarget.Fov <= 0f ? 55f : viewTarget.Fov, k);
        cam.transform.SetPositionAndRotation(view.Eye, Quaternion.LookRotation(view.Focus - view.Eye, view.ScreenUp));
        cam.fieldOfView = view.Fov;
    }

    void UpdateDesktop()
    {
        Mouse mouse = Mouse.current;
        Keyboard keys = Keyboard.current;
        if (keys != null)
        {
            if (keys.homeKey.wasPressedThisFrame) ResetView(false);
            Active.OnKey(keys);
        }

        if (mouse == null || cam == null) return;
        Vector2 pos = mouse.position.ReadValue();
        bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        bool shift = keys != null && (keys.leftShiftKey.isPressed || keys.rightShiftKey.isPressed);

        if (!pressed && !overUi && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame || mouse.middleButton.wasPressedThisFrame))
        {
            pressed = true;
            dragging = false;
            panning = !mouse.leftButton.wasPressedThisFrame || shift;
            pressPos = lastPos = pos;
        }

        if (pressed)
        {
            bool held = mouse.leftButton.isPressed || mouse.rightButton.isPressed || mouse.middleButton.isPressed;
            Vector2 d = pos - lastPos;
            lastPos = pos;
            if ((pos - pressPos).magnitude > 5f) dragging = true;
            if (dragging)
            {
                if (panning)
                {
                    Vector3 right = Vector3.Cross(viewTarget.ScreenUp, (viewTarget.Focus - viewTarget.Eye).normalized).normalized;
                    float s = viewTarget.Distance * .0016f;
                    viewTarget.Focus += (-right * d.x - viewTarget.ScreenUp * d.y) * s;
                }
                else
                {
                    viewTarget.Yaw -= d.x * .25f;
                    viewTarget.Pitch = Mathf.Clamp(viewTarget.Pitch - d.y * .2f, -75f, 89f);
                }
            }

            if (!held)
            {
                if (!dragging && !panning && Hover >= 0) Active.OnPick(Hover);
                pressed = false;
            }
        }

        float scroll = overUi ? 0f : mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > .01f) viewTarget.Distance = Mathf.Clamp(viewTarget.Distance * Mathf.Pow(.9f, Mathf.Sign(scroll)), 5f, 5000f);

        if (dragging || overUi)
        {
            SetHover(-1);
            return;
        }

        int best = -1;
        float bestD = 22f;
        for (int i = 0; i < Active.PickCount; i++)
        {
            Vector3 sp = cam.WorldToScreenPoint(Active.PickPoint(i));
            if (sp.z <= 0f) continue;
            float dd = Vector2.Distance(pos, new Vector2(sp.x, sp.y));
            if (dd < bestD)
            {
                bestD = dd;
                best = i;
            }
        }

        SetHover(best);
    }

    /// <summary>VR: a fingertip near a point hovers it; pushing into it (about a centimetre) picks it once.</summary>
    void UpdateFingers()
    {
        VRApp app = VRMode.App;
        if (app == null || !app.Ready) return;
        int hover = -1;
        float hoverD = float.MaxValue;
        VRHand[] hands = { app.Hands.Left, app.Hands.Right };
        float scale = Mathf.Max(1e-4f, app.Rig.Scale);
        for (int h = 0; h < 2; h++)
        {
            VRHand hand = hands[h];
            if (!hand.Tracked || hand.Pinching || !hand.IndexExtended || app.Grab.IsHolding(hand) || app.Poker.NearPanel(hand.IndexTip, .06f))
            {
                fingerArmed[h] = true;
                fingerOn[h] = -1;
                continue;
            }

            Vector3 tip = app.Rig.Offset.TransformPoint(hand.IndexTip);
            int best = -1;
            float bestD = float.MaxValue;
            for (int i = 0; i < Active.PickCount; i++)
            {
                float d = (Active.PickPoint(i) - tip).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    best = i;
                }
            }

            float metres = Mathf.Sqrt(bestD) / scale;
            if (best != fingerOn[h] || metres > .025f) fingerArmed[h] = true;
            fingerOn[h] = metres < .035f ? best : -1;
            if (metres < .035f && metres < hoverD)
            {
                hover = best;
                hoverD = metres;
            }

            if (best >= 0 && metres < .012f && fingerArmed[h])
            {
                fingerArmed[h] = false;
                Active.OnPick(best);
                VRAudio.Click();
            }
        }

        SetHover(hover);
    }

    void SetHover(int i)
    {
        Hover = i;
        if (i < 0 || Active == null)
        {
            ClearHover();
            return;
        }

        Vector3 p = Active.PickPoint(i);
        float unit = VRMode.Enabled && VRMode.App != null ? VRMode.App.Rig.Scale * .01f : Mathf.Max(.5f, view.Distance * .007f);
        if (hoverDot == null)
        {
            hoverDot = Factory.NewSphere("lab hover", 1f, Color.white, Factory.GlowIntensity);
            hoverDot.transform.SetParent(root, false);
        }

        hoverDot.SetActive(true);
        hoverDot.transform.position = p;
        hoverDot.transform.localScale = Vector3.one * unit * 1.1f;
        if (hoverLabelFor != i)
        {
            if (hoverLabel != null) Destroy(hoverLabel.gameObject);
            hoverLabel = null;
            string text = Active.PickLabel(i);
            if (!string.IsNullOrEmpty(text)) hoverLabel = LabDraw.Label(root, text, p, unit * 26f, Color.white);
            hoverLabelFor = i;
        }

        if (hoverLabel != null) hoverLabel.transform.position = p + (cam != null ? cam.transform.up : LabView.Up) * unit * 3.2f;
    }

    void ClearHover()
    {
        if (hoverDot != null) hoverDot.SetActive(false);
        if (hoverLabel != null) Destroy(hoverLabel.gameObject);
        hoverLabel = null;
        hoverLabelFor = -2;
    }
}
