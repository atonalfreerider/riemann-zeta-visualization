using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The dock's Models page: the model lab in VR. The models (Paths, E8, Unified, Zeros), Re-frame and Leave across the top; the readout
/// (what the picked number or root says) and a number's column laid on its side; then the active model's controls, two rows
/// of buttons and sliders. Touch a column top or a root with a fingertip to pick it; pinch to move the model as anywhere else.
/// </summary>
public partial class VRApp
{
    const float LabCell = 97f, LabRowHeight = 54f;
    RectTransform labPage;
    readonly Dictionary<LabModel, RectTransform> labControlPages = new();
    TextMeshProUGUI labStatus;
    StripView labStrip;
    int labStripN = -1, labStripLit = -1;

    void BuildModelsPage(RectTransform p)
    {
        labPage = p;
        ModelLab lab = ModelLab.Instance;
        if (lab == null)
        {
            Dock.AddLabel(10, 10, 580, 40, "The model lab is not available", 22f, VRStyle.TextDim, TextAlignmentOptions.Left, FontStyles.Normal, p);
            return;
        }

        // the models, then Re-frame and Leave, share the first row; the games take a second row of their own
        List<LabModel> models = lab.Models.FindAll(m => !m.IsGame), games = lab.Models.FindAll(m => m.IsGame);
        int slots = models.Count + 2;
        float pitch = (DockWidth - 16f) / slots, bw = pitch - 6f;
        for (int i = 0; i < models.Count; i++)
        {
            LabModel m = models[i];
            Dock.AddButton(8 + i * pitch, 2, bw, 50, m.Short, m.Icon, () =>
            {
                tour.LeaveExperiment();
                lab.Enter(m.Id);
            }, true, () => lab.Active == m, 18f, p);
        }

        Dock.AddButton(8 + models.Count * pitch, 2, bw, 50, "Re-frame", VRIcons.Id.Target, () => lab.ResetView(false), false, null, 18f, p);
        Dock.AddButton(8 + (models.Count + 1) * pitch, 2, bw, 50, "Leave", VRIcons.Id.Close, () => lab.Exit(), false, null, 18f, p);
        float top = 56f;
        if (games.Count > 0)
        {
            float gp = (DockWidth - 16f) / Mathf.Max(slots, games.Count);
            for (int i = 0; i < games.Count; i++)
            {
                LabModel m = games[i];
                Dock.AddButton(8 + i * gp, 56, gp - 6f, 50, m.Short, m.Icon, () =>
                {
                    tour.LeaveExperiment();
                    lab.Enter(m.Id);
                }, true, () => lab.Active == m, 18f, p);
            }

            top = 110f;
        }

        labStatus = Dock.AddLabel(10, top, 580, 70, "Touch a model or a game to open it, then touch its points with a fingertip, or point at them and hold", 20f, VRStyle.Text, TextAlignmentOptions.TopLeft, FontStyles.Normal, p);
        labStatus.overflowMode = TextOverflowModes.Truncate;
        labStatus.lineSpacing = -8f;

        GameObject sg = new("Lab strip");
        sg.transform.SetParent(p, false);
        RectTransform stripRoot = sg.AddComponent<RectTransform>();
        VRPanel.Place(stripRoot, 20, top + 72f, 560, 58);
        labStrip = new StripView(stripRoot, .25f, (parent, text, size) => VRStyle.Text_(parent, text, size, VRStyle.Text)) { ImageMaterial = VRStyle.UiMaterial };

        foreach (LabModel m in lab.Models)
        {
            GameObject go = new("Lab controls " + m.Id);
            go.transform.SetParent(p, false);
            RectTransform sub = go.AddComponent<RectTransform>();
            VRPanel.Place(sub, 0, top + 132f, DockWidth, 108);
            int cell = 0;
            foreach (LabControl c in m.Controls)
            {
                LabControl control = c;
                int col = cell % 6, row = cell / 6;
                if (col + control.Cells > 6)
                {
                    cell += 6 - col;
                    col = 0;
                    row++;
                }

                if (row > 1) break;
                float x = 8 + col * LabCell, y = row * LabRowHeight;
                if (control.Slider)
                    Dock.AddSlider(x, y, control.Cells * LabCell - 4, LabRowHeight - 4, control.Label, control.Min, control.Max, control.Get, control.Set, control.Format, sub);
                else
                {
                    VRButton b = Dock.AddButton(x, y, LabCell - 4, LabRowHeight - 4, control.Label, control.Icon, () => control.Clicked?.Invoke(),
                        control.On != null, control.On, 14f, sub);
                    if (control.DynamicLabel != null) b.Dynamic(() => control.Icon, control.DynamicLabel);
                }

                cell += Mathf.Max(1, control.Cells);
            }

            go.SetActive(false);
            labControlPages[m] = sub;
        }

        lab.Changed += RefreshLabPage;
        RefreshLabPage();
    }

    void RefreshLabPage()
    {
        ModelLab lab = ModelLab.Instance;
        if (lab == null || labStatus == null) return;
        foreach (var kv in labControlPages) kv.Value.gameObject.SetActive(kv.Key == lab.Active);
        // the board's small lines (58 %) are too small on a panel half a metre away
        labStatus.text = lab.Active == null ? "Touch a model or a game to open it, then touch its points with a fingertip, or point at them and hold" : lab.Status.Replace("<size=58%>", "<size=76%>");
        if (lab.StripNumber != labStripN)
        {
            labStripN = lab.StripNumber;
            labStripLit = -1;
            if (labStripN >= 2) labStrip.Show(labStripN, 540f);
            else labStrip.Clear();
        }

        if (labStripN >= 2 && lab.StripLit != labStripLit)
        {
            labStripLit = lab.StripLit;
            labStrip.Light(labStripLit);
        }
    }

    /// <summary>Leave the lab without changing the view (a chapter, a module or a view is about to take the stage).</summary>
    static void CloseLab()
    {
        if (ModelLab.Instance != null) ModelLab.Instance.Close();
    }
}
