using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Hands.OpenXR;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.Meta;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

/// <summary>
/// One-stop Meta Quest 2 setup and build: Android/IL2CPP/ARM64/Vulkan, OpenXR with the Meta Quest feature group,
/// hand tracking, hand interaction profile and Passthrough. Run it from the menu or headless:
///
///   Unity -batchmode -projectPath . -buildTarget Android -executeMethod VRBuildSetup.BuildQuestApk -logFile build.log
///
/// Environment: ZETA_APK (output path, default Builds/zeta-vr.apk), ZETA_DEVELOPMENT=1 (development build).
/// </summary>
public static class VRBuildSetup
{
    const BuildTargetGroup Android = BuildTargetGroup.Android;
    public const string ApplicationId = "com.metavirtuoso.zetaviewer";
    const string MetaFeatureSetId = "com.unity.openxr.featureset.meta";

    [MenuItem("Zeta/VR/Configure Quest Project Settings")]
    public static void Configure()
    {
        ConfigurePlayer();
        ConfigureXR();
        AssetDatabase.SaveAssets();
        Debug.Log("VRBuildSetup: Quest project settings applied.");
    }

    [MenuItem("Zeta/VR/Build Quest APK")]
    public static void BuildQuestApk()
    {
        Configure();

        string output = Environment.GetEnvironmentVariable("ZETA_APK");
        if (string.IsNullOrEmpty(output)) output = Path.Combine("Builds", "zeta-vr.apk");
        output = Path.GetFullPath(output);
        Directory.CreateDirectory(Path.GetDirectoryName(output));

        bool development = Environment.GetEnvironmentVariable("ZETA_DEVELOPMENT") == "1";
        BuildPlayerOptions options = new()
        {
            scenes = new[] { "Assets/zeta.unity" },
            locationPathName = output,
            target = BuildTarget.Android,
            options = development ? BuildOptions.Development : BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        Debug.Log($"VRBuildSetup: build {summary.result}, {summary.totalSize / (1024f * 1024f):0.0} MB, {summary.totalTime.TotalSeconds:0}s, {summary.totalErrors} errors → {output}");
        if (Application.isBatchMode) EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
    }

    static void ConfigurePlayer()
    {
        NamedBuildTarget android = NamedBuildTarget.Android;
        PlayerSettings.SetApplicationIdentifier(android, ApplicationId);
        PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetManagedStrippingLevel(android, ManagedStrippingLevel.Low);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32; // the passthrough camera feature needs Android 12L
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36; // same target level as the other Quest apps built here
        PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.GameActivity; // required by the OpenXR Quest validation on Unity 6
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.SetMobileMTRendering(android, true);
        PlayerSettings.gpuSkinning = true;
        PlayerSettings.Android.optimizedFramePacing = false; // XR controls the pacing
        PlayerSettings.enableFrameTimingStats = true;         // GPU/CPU frame times for VRPerf
        // no "Made with Unity" card: the headset shows the launch card's still (system splash and VR loading image)
        PlayerSettings.SplashScreen.show = false;
        PlayerSettings.SplashScreen.showUnityLogo = false;
        Texture2D splash = AssetDatabase.LoadAssetAtPath<Texture2D>(SplashPath);
        if (splash != null) PlayerSettings.virtualRealitySplashScreen = splash;
        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.androidBuildType = AndroidBuildType.Release;
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
    }

    static void ConfigureXR()
    {
        // XR Plug-in Management: settings container + the OpenXR loader for Android
        MethodInfo getOrCreate = typeof(XRGeneralSettingsPerBuildTarget).GetMethod("GetOrCreate", BindingFlags.NonPublic | BindingFlags.Static);
        XRGeneralSettingsPerBuildTarget perTarget = (XRGeneralSettingsPerBuildTarget)getOrCreate.Invoke(null, null);
        if (!perTarget.HasSettingsForBuildTarget(Android)) perTarget.CreateDefaultSettingsForBuildTarget(Android);
        if (!perTarget.HasManagerSettingsForBuildTarget(Android)) perTarget.CreateDefaultManagerSettingsForBuildTarget(Android);

        XRGeneralSettings general = perTarget.SettingsForBuildTarget(Android);
        general.InitManagerOnStart = true;
        XRManagerSettings manager = perTarget.ManagerSettingsForBuildTarget(Android);
        if (!XRPackageMetadataStore.AssignLoader(manager, "UnityEngine.XR.OpenXR.OpenXRLoader", Android))
        {
            throw new Exception("VRBuildSetup: could not assign the OpenXR loader for Android.");
        }

        EditorUtility.SetDirty(general);
        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(perTarget);

        // OpenXR: the Meta Quest feature group + hands
        OpenXRSettings openXR = OpenXRSettings.GetSettingsForBuildTargetGroup(Android);
        if (openXR == null) throw new Exception("VRBuildSetup: no OpenXR settings for Android.");

        OpenXRFeatureSetManager.FeatureSet meta = OpenXRFeatureSetManager.GetFeatureSetWithId(Android, MetaFeatureSetId);
        if (meta != null)
        {
            meta.isEnabled = true;
            OpenXRFeatureSetManager.SetFeaturesFromEnabledFeatureSets(Android);
        }
        else
        {
            Debug.LogWarning("VRBuildSetup: Meta Quest feature group not found; enabling features individually.");
        }

        Enable<MetaQuestFeature>(openXR);         // Quest loader + manifest
        Enable<ARSessionFeature>(openXR);         // AR Foundation session (Meta)
        Enable<ARCameraFeature>(openXR);          // Passthrough
        Enable<HandTracking>(openXR);             // XR Hands subsystem
        Enable<MetaHandTrackingAim>(openXR);      // aim / pinch strength
        Enable<HandInteractionProfile>(openXR);   // pinch / poke / aim poses without controllers
        Enable<OculusTouchControllerProfile>(openXR); // the Quest build validation insists on one Touch profile; the app itself only reads hands

        // the Meta group switches on a lot this app never uses; each of these costs start-up work and several ask the
        // user for permissions (spatial data) the first time the app runs
        Disable<ARAnchorFeature>(openXR);
        Disable<ARBoundingBoxFeature>(openXR);
        Disable<ARMeshFeature>(openXR);
        Disable<ARPlaneFeature>(openXR);
        Disable<ARRaycastFeature>(openXR);
        Disable<AROcclusionFeature>(openXR);
        Disable<ColocationDiscoveryFeature>(openXR);
        Disable<BoundaryVisibilityFeature>(openXR);
        Disable<DisplayUtilitiesFeature>(openXR);
        Disable<MetaSocialEyeGazeFeature>(openXR);

        openXR.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
        openXR.depthSubmissionMode = OpenXRSettings.DepthSubmissionMode.None;
        EditorUtility.SetDirty(openXR);

        // Quest 2 only
        MetaQuestFeature quest = openXR.GetFeature<MetaQuestFeature>();
        if (quest != null)
        {
            // the headset's own splash while the app loads: a still of the launch card (Assets/VR/Splash, rendered by
            // ReviewRenderTest with ZETA_REVIEW=splash)
            Texture2D splash = AssetDatabase.LoadAssetAtPath<Texture2D>(SplashPath);
            SerializedObject so = new(quest);
            SerializedProperty prop = so.FindProperty("systemSplashScreen");
            if (prop != null && splash != null)
            {
                prop.objectReferenceValue = splash;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else if (splash == null) Debug.LogWarning($"VRBuildSetup: no splash image at {SplashPath}");

            EditorUtility.SetDirty(quest);
        }
    }

    public const string SplashPath = "Assets/VR/Splash/zeta-splash.png";

    static void Disable<T>(OpenXRSettings settings) where T : UnityEngine.XR.OpenXR.Features.OpenXRFeature
    {
        T feature = settings.GetFeature<T>();
        if (feature == null || !feature.enabled) return;
        feature.enabled = false;
        EditorUtility.SetDirty(feature);
    }

    static void Enable<T>(OpenXRSettings settings) where T : UnityEngine.XR.OpenXR.Features.OpenXRFeature
    {
        T feature = settings.GetFeature<T>();
        if (feature == null)
        {
            Debug.LogWarning($"VRBuildSetup: feature {typeof(T).Name} is not available.");
            return;
        }

        feature.enabled = true;
        EditorUtility.SetDirty(feature);
    }
}
