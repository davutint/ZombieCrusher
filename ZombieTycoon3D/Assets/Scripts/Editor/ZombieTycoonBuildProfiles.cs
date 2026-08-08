#if UNITY_6000_0_OR_NEWER
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEngine;

namespace ZombieTycoon3D.Editor
{
    internal static class ZombieTycoonBuildProfiles
    {
        internal const string CrazyGamesProfilePath =
            "Assets/Settings/Build Profiles/CrazyGames WebGL.asset";

        internal const string IosProfilePath =
            "Assets/Settings/Build Profiles/iOS App Store.asset";

        private const string CrazyGamesBootstrapScene =
            "Assets/Scenes/CrazyGamesBootstrap.unity";

        private const string CrazyGamesGameplayScene =
            "Assets/_ASSETS/Ash Assets/Arcade Vehicle Physics/Demo Scene/Demo.unity";

        private const string IosGameplayScene =
            "Assets/Scenes/iOS/Demo_iOS.unity";

        private const string MenuPath =
            "ZombieTycoon3D/Build/Create Or Update Platform Profiles";

        [MenuItem(MenuPath)]
        internal static void CreateOrUpdatePlatformProfiles()
        {
            EnsureAssetFolder("Assets/Settings", "Build Profiles");

            BuildProfile crazyGamesProfile = LoadOrCreateProfile(
                CrazyGamesProfilePath,
                BuildTarget.WebGL);

            ConfigureScenes(
                crazyGamesProfile,
                CrazyGamesBootstrapScene,
                CrazyGamesGameplayScene);

            BuildProfile iosProfile = LoadOrCreateProfile(
                IosProfilePath,
                BuildTarget.iOS);

            ConfigureScenes(iosProfile, IosGameplayScene);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = iosProfile;
            EditorGUIUtility.PingObject(iosProfile);

            Debug.Log(
                "ZombieTycoon3D build profiles are ready. " +
                "The global Build Settings scene list was not changed.");
        }

        private static BuildProfile LoadOrCreateProfile(
            string assetPath,
            BuildTarget buildTarget)
        {
            BuildProfile profile =
                AssetDatabase.LoadAssetAtPath<BuildProfile>(assetPath);

            if (profile != null)
                return profile;

            Type moduleUtilType = typeof(BuildProfile).Assembly.GetType(
                "UnityEditor.Build.Profile.BuildProfileModuleUtil",
                true);

            MethodInfo getPlatformIdMethod = moduleUtilType.GetMethod(
                "GetPlatformId",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[]
                {
                    typeof(BuildTarget),
                    typeof(StandaloneBuildSubtarget)
                },
                null);

            MethodInfo createProfileMethod = typeof(BuildProfile).GetMethod(
                "CreateInstance",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new[]
                {
                    typeof(GUID),
                    typeof(string)
                },
                null);

            if (getPlatformIdMethod == null || createProfileMethod == null)
            {
                throw new MissingMethodException(
                    "Unity Build Profile creation API could not be resolved.");
            }

            object platformId = getPlatformIdMethod.Invoke(
                null,
                new object[]
                {
                    buildTarget,
                    StandaloneBuildSubtarget.Player
                });

            createProfileMethod.Invoke(
                null,
                new[]
                {
                    platformId,
                    assetPath
                });

            profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(assetPath);
            if (profile == null)
            {
                throw new InvalidOperationException(
                    $"Build profile could not be created at '{assetPath}'.");
            }

            return profile;
        }

        private static void ConfigureScenes(
            BuildProfile profile,
            params string[] scenePaths)
        {
            foreach (string scenePath in scenePaths)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
                {
                    throw new InvalidOperationException(
                        $"Required scene was not found: '{scenePath}'.");
                }
            }

            profile.overrideGlobalScenes = true;
            profile.scenes = Array.ConvertAll(
                scenePaths,
                scenePath => new EditorBuildSettingsScene(scenePath, true));

            EditorUtility.SetDirty(profile);
        }

        private static void EnsureAssetFolder(
            string parentFolder,
            string folderName)
        {
            string folderPath = $"{parentFolder}/{folderName}";
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string guid = AssetDatabase.CreateFolder(parentFolder, folderName);
            if (string.IsNullOrEmpty(guid))
            {
                throw new InvalidOperationException(
                    $"Folder could not be created: '{folderPath}'.");
            }
        }
    }
}
#endif
