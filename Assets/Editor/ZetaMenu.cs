using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>Editor conveniences for running and recording the tour.</summary>
public static class ZetaMenu
{
    [MenuItem("Zeta/Play Tour")]
    static void PlayTour()
    {
        EditorPrefs.SetBool("Zeta.AutoStartTour", true);
        EditorApplication.EnterPlaymode();
    }

    [MenuItem("Zeta/Play Tour and Record 1080x1920")]
    static void PlayTourAndRecord()
    {
        EditorPrefs.SetBool("Zeta.RecordTour", true);
        EditorApplication.EnterPlaymode();
    }

    [MenuItem("Zeta/Play Tour from Chapter...")]
    static void PlayTourFromChapter()
    {
        TourScriptData script = TourScriptLoader.Load();
        GenericMenu menu = new();
        foreach (TourChapter chapter in script.chapters)
        {
            string id = chapter.id;
            menu.AddItem(new GUIContent($"{chapter.title} ({id})"), false, () =>
            {
                EditorPrefs.SetString("Zeta.StartChapter", id);
                EditorPrefs.SetBool("Zeta.AutoStartTour", true);
                EditorApplication.EnterPlaymode();
            });
        }

        menu.ShowAsContext();
    }

    const string VRSimulateMenu = "Zeta/VR/Desktop Simulation (mouse as hands)";

    /// <summary>
    /// Enter Play mode with the VR interface and a mouse standing in for the hands: move the mouse to move the
    /// fingertip, scroll to push it nearer or farther, hold the left button to poke, Shift + left button to pinch,
    /// right button + WASD/QE to look and move.
    /// </summary>
    [MenuItem(VRSimulateMenu)]
    static void ToggleVRSimulation()
    {
        bool on = !EditorPrefs.GetBool("Zeta.VRSimulate", false);
        EditorPrefs.SetBool("Zeta.VRSimulate", on);
        Debug.Log("Zeta VR desktop simulation " + (on ? "ON: enter Play mode to try the headset interface with the mouse." : "off."));
    }

    [MenuItem(VRSimulateMenu, true)]
    static bool ToggleVRSimulationChecked()
    {
        Menu.SetChecked(VRSimulateMenu, EditorPrefs.GetBool("Zeta.VRSimulate", false));
        return true;
    }

    [MenuItem("Zeta/Game View - Portrait 1080x1920")]
    static void SetPortraitGameView()
    {
        try
        {
            Assembly editor = typeof(Editor).Assembly;
            Type sizesType = editor.GetType("UnityEditor.GameViewSizes");
            Type singletonType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            object instance = singletonType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static).GetValue(null, null);
            Type groupTypeEnum = editor.GetType("UnityEditor.GameViewSizeGroupType");
            object standalone = Enum.Parse(groupTypeEnum, "Standalone");
            MethodInfo getGroup = sizesType.GetMethod("GetGroup");
            object group = getGroup.Invoke(instance, new[] { standalone });
            Type groupType = group.GetType();

            int builtin = (int)groupType.GetMethod("GetBuiltinCount").Invoke(group, null);
            int custom = (int)groupType.GetMethod("GetCustomCount").Invoke(group, null);
            MethodInfo getSize = groupType.GetMethod("GetGameViewSize");
            Type sizeType = editor.GetType("UnityEditor.GameViewSize");
            int found = -1;
            for (int i = 0; i < builtin + custom; i++)
            {
                object size = getSize.Invoke(group, new object[] { i });
                int w = (int)sizeType.GetProperty("width").GetValue(size, null);
                int h = (int)sizeType.GetProperty("height").GetValue(size, null);
                if (w == 1080 && h == 1920)
                {
                    found = i;
                    break;
                }
            }

            if (found < 0)
            {
                Type sizeKind = editor.GetType("UnityEditor.GameViewSizeType");
                object fixedResolution = Enum.Parse(sizeKind, "FixedResolution");
                ConstructorInfo ctor = sizeType.GetConstructor(new[] { sizeKind, typeof(int), typeof(int), typeof(string) });
                object newSize = ctor.Invoke(new[] { fixedResolution, 1080, 1920, "Portrait 1080x1920" });
                groupType.GetMethod("AddCustomSize").Invoke(group, new[] { newSize });
                found = builtin + custom;
            }

            Type gameViewType = editor.GetType("UnityEditor.GameView");
            EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
            PropertyInfo selected = gameViewType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            selected.SetValue(gameView, found, null);
            gameView.Repaint();
            Debug.Log("Game view set to Portrait 1080x1920");
        }
        catch (Exception e)
        {
            Debug.LogWarning("Could not set the Game view size automatically (" + e.Message + "). Pick 1080x1920 from the Game view aspect dropdown instead; the tour pillarboxes to 9:16 regardless.");
        }
    }
}
