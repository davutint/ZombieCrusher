#if UNITY_EDITOR && UNITY_IOS
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ZombieTycoon3D.Editor
{
    internal sealed class IosAppStoreBuildGuard :
        IPreprocessBuildWithReport
    {
        [Serializable]
        private sealed class ProgressionValidationData
        {
            public int version;
            public int scrap;
            public long lifetimeZombieKills;
            public List<string> ownedVehicleIds;
            public List<string> ownedAttachmentIds;
        }

        private const string MinimumIosVersion = "15.6";
        private const string ExpectedVersion = "1.0.0";
        private const string ExpectedCompanyName = "PixiCorp";
        private const string ExpectedProductName = "Scrap the Dead";
        private const string ExpectedBundleId =
            "com.pixicorp.scrapthedead";
        private const string TestAdMobAppId =
            "ca-app-pub-3940256099942544~1458002511";
        private const string TestRewardedAdUnitId =
            "ca-app-pub-3940256099942544/1712485313";
        private const string GoogleSettingsPath =
            "Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset";
        private const string AppIconPath =
            "Assets/Branding/iOS/AppIcon_ScrapTheDead_1024.png";
        private const string LaunchScreenLogoPath =
            "Assets/Branding/iOS/LaunchScreenLogo_ScrapTheDead.png";

        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS)
            {
                return;
            }

            ApplyIosPlayerSettings();

            bool isDevelopment =
                (report.summary.options & BuildOptions.Development) != 0;
            IosPlatformSettings settings = IosPlatformSettings.Load();
            if (settings == null)
            {
                throw new BuildFailedException(
                    "Assets/Resources/IosPlatformSettings.asset is missing.");
            }

            if (isDevelopment)
            {
                SynchronizeGoogleMobileAdsAppId(TestAdMobAppId);
                return;
            }

            SynchronizeGoogleMobileAdsAppId(settings.AdMobAppId);
            ValidateReleaseConfiguration(settings);
        }

        [MenuItem(
            "ZombieTycoon3D/iOS/Validate App Store Configuration")]
        private static void ValidateFromMenu()
        {
            IosPlatformSettings settings = IosPlatformSettings.Load();
            if (settings == null)
            {
                throw new BuildFailedException(
                    "Assets/Resources/IosPlatformSettings.asset is missing.");
            }

            SynchronizeGoogleMobileAdsAppId(settings.AdMobAppId);
            ValidateReleaseConfiguration(settings);
            Debug.Log("iOS App Store configuration is complete.");
        }

        [MenuItem("ZombieTycoon3D/iOS/Apply Approved App Icon")]
        private static void ApplyApprovedAppIconFromMenu()
        {
            ApplyIosAppIcon();
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"Approved iOS app icon applied from '{AppIconPath}'.");
        }

        [MenuItem("ZombieTycoon3D/iOS/Apply Approved Launch Screen")]
        private static void ApplyApprovedLaunchScreenFromMenu()
        {
            ApplyIosLaunchScreen();
            AssetDatabase.SaveAssets();
            Debug.Log(
                "Approved iOS launch screen applied from "
                + $"'{LaunchScreenLogoPath}'.");
        }

        [MenuItem(
            "ZombieTycoon3D/iOS/Apply Approved First Release Settings")]
        private static void ApplyApprovedFirstReleaseSettingsFromMenu()
        {
            IosPlatformSettings settings = IosPlatformSettings.Load();
            if (settings == null)
            {
                throw new BuildFailedException(
                    "Assets/Resources/IosPlatformSettings.asset is missing.");
            }

            PlayerSettings.bundleVersion = ExpectedVersion;
            PlayerSettings.iOS.buildNumber = "1";
            ApplyIosPlayerSettings();
            SynchronizeGoogleMobileAdsAppId(settings.AdMobAppId);
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"Approved iOS release settings applied: "
                + $"version {ExpectedVersion} (1), Unity splash disabled.");
        }

        private static void ApplyIosPlayerSettings()
        {
            PlayerSettings.defaultInterfaceOrientation =
                UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.iOS.targetDevice =
                iOSTargetDevice.iPhoneAndiPad;
            PlayerSettings.iOS.targetOSVersionString =
                MinimumIosVersion;
            PlayerSettings.iOS.requiresFullScreen = true;
            PlayerSettings.iOS.cameraUsageDescription = string.Empty;
            PlayerSettings.iOS.microphoneUsageDescription = string.Empty;
            PlayerSettings.iOS.locationUsageDescription = string.Empty;
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            ApplyIosAppIcon();
            ApplyIosLaunchScreen();
        }

        private static void ApplyIosAppIcon()
        {
            Texture2D appIcon =
                AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconPath);
            if (appIcon == null)
            {
                throw new BuildFailedException(
                    $"Approved iOS app icon is missing: '{AppIconPath}'.");
            }

            NamedBuildTarget target = NamedBuildTarget.iOS;
            PlatformIconKind[] kinds =
                PlayerSettings.GetSupportedIconKinds(target);

            foreach (PlatformIconKind kind in kinds)
            {
                PlatformIcon[] icons =
                    PlayerSettings.GetPlatformIcons(target, kind);
                foreach (PlatformIcon icon in icons)
                {
                    icon.SetTexture(appIcon, 0);
                }

                PlayerSettings.SetPlatformIcons(target, kind, icons);
            }
        }

        private static void ApplyIosLaunchScreen()
        {
            Texture2D launchScreenLogo =
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    LaunchScreenLogoPath);
            if (launchScreenLogo == null)
            {
                throw new BuildFailedException(
                    "Approved iOS launch screen logo is missing: "
                    + $"'{LaunchScreenLogoPath}'.");
            }

            PlayerSettings.iOS.SetiPhoneLaunchScreenType(
                iOSLaunchScreenType.ImageAndBackgroundRelative);
            PlayerSettings.iOS.SetiPadLaunchScreenType(
                iOSLaunchScreenType.ImageAndBackgroundRelative);
            PlayerSettings.iOS.SetLaunchScreenImage(
                launchScreenLogo,
                iOSLaunchScreenImageType.iPhoneLandscapeImage);
            PlayerSettings.iOS.SetLaunchScreenImage(
                launchScreenLogo,
                iOSLaunchScreenImageType.iPhonePortraitImage);
            PlayerSettings.iOS.SetLaunchScreenImage(
                launchScreenLogo,
                iOSLaunchScreenImageType.iPadImage);
        }

        private static void ValidateReleaseConfiguration(
            IosPlatformSettings settings)
        {
            ValidateCloudProgressionReconciliation();
            List<string> missing = new();
            string bundleId = PlayerSettings.GetApplicationIdentifier(
                NamedBuildTarget.iOS);

            if (!string.Equals(
                    bundleId,
                    ExpectedBundleId,
                    StringComparison.Ordinal))
            {
                missing.Add(
                    $"the approved iOS Bundle ID '{ExpectedBundleId}'");
            }

            if (!string.Equals(
                    PlayerSettings.companyName,
                    ExpectedCompanyName,
                    StringComparison.Ordinal))
            {
                missing.Add(
                    $"the approved company name '{ExpectedCompanyName}'");
            }

            if (!string.Equals(
                    PlayerSettings.productName,
                    ExpectedProductName,
                    StringComparison.Ordinal))
            {
                missing.Add(
                    $"the approved product name '{ExpectedProductName}'");
            }

            if (!string.Equals(
                    PlayerSettings.bundleVersion,
                    ExpectedVersion,
                    StringComparison.Ordinal))
            {
                missing.Add(
                    $"the approved app version '{ExpectedVersion}'");
            }

            if (!int.TryParse(
                    PlayerSettings.iOS.buildNumber,
                    out int buildNumber)
                || buildNumber < 1)
            {
                missing.Add("a positive iOS build number");
            }

            if (PlayerSettings.SplashScreen.show)
            {
                missing.Add("the approved disabled Unity splash screen");
            }

            if (string.IsNullOrWhiteSpace(
                    settings.LifetimeZombieKillsLeaderboardId))
            {
                missing.Add("the Game Center leaderboard ID");
            }

            if (!IsProductionAdMobAppId(settings.AdMobAppId))
            {
                missing.Add("a production AdMob iOS App ID");
            }

            if (!IsProductionRewardedAdUnitId(
                    settings.RewardedAdUnitId))
            {
                missing.Add("a production AdMob rewarded ad unit ID");
            }

            if (string.IsNullOrWhiteSpace(
                    settings.AdFreeRewardsProductId))
            {
                missing.Add("the App Store non-consumable product ID");
            }

            if (!IsHttpsUrl(settings.PrivacyPolicyUrl))
            {
                missing.Add("a public HTTPS privacy policy URL");
            }

            if (!IsHttpsUrl(settings.SupportUrl))
            {
                missing.Add("a public HTTPS support URL");
            }

            if (!CloudProjectSettings.projectBound
                || string.IsNullOrWhiteSpace(
                    CloudProjectSettings.projectId))
            {
                missing.Add("a linked Unity Cloud Project");
            }

            ValidateGoogleMobileAdsReleaseConfiguration(
                missing,
                settings.AdMobAppId);

            if (missing.Count > 0)
            {
                throw new BuildFailedException(
                    "App Store release build blocked. Configure "
                    + string.Join(", ", missing)
                    + " in Player Settings and IosPlatformSettings. "
                    + "Use a Development build while testing with Google test ads.");
            }
        }

        private static void ValidateGoogleMobileAdsReleaseConfiguration(
            List<string> missing,
            string expectedAppId)
        {
            UnityEngine.Object asset =
                AssetDatabase.LoadMainAssetAtPath(GoogleSettingsPath);
            if (asset == null)
            {
                missing.Add("Google Mobile Ads settings");
                return;
            }

            SerializedObject serializedSettings = new(asset);
            SerializedProperty iosAppId = serializedSettings.FindProperty(
                "adMobIOSAppId");
            SerializedProperty trackingDescription =
                serializedSettings.FindProperty(
                    "userTrackingUsageDescription");
            if (iosAppId == null || trackingDescription == null)
            {
                missing.Add(
                    "a compatible Google Mobile Ads privacy configuration");
                return;
            }

            if (!string.Equals(
                    iosAppId.stringValue,
                    expectedAppId,
                    StringComparison.Ordinal))
            {
                missing.Add("the production AdMob App ID in Google settings");
            }

            if (!string.IsNullOrWhiteSpace(
                    trackingDescription.stringValue))
            {
                missing.Add(
                    "an empty NSUserTrackingUsageDescription for the approved no-ATT release");
            }
        }

        private static void ValidateCloudProgressionReconciliation()
        {
            const string preferred =
                "{\"version\":3,\"scrap\":120,\"lifetimeZombieKills\":5,"
                + "\"selectedVehicleId\":\"car-a\","
                + "\"ownedVehicleIds\":[\"car-a\"],"
                + "\"ownedAttachmentIds\":[\"ram-a\"],"
                + "\"vehicleLoadouts\":[]}";
            const string secondary =
                "{\"version\":3,\"scrap\":999,\"lifetimeZombieKills\":12,"
                + "\"selectedVehicleId\":\"car-b\","
                + "\"ownedVehicleIds\":[\"car-b\"],"
                + "\"ownedAttachmentIds\":[\"blade-b\"],"
                + "\"vehicleLoadouts\":[]}";
            string reconciled =
                GarageEconomyController.ReconcileCloudProgression(
                    preferred,
                    secondary);
            ProgressionValidationData data =
                JsonUtility.FromJson<ProgressionValidationData>(
                    reconciled);
            bool valid = data != null
                         && data.version == 3
                         && data.scrap == 120
                         && data.lifetimeZombieKills == 12
                         && data.ownedVehicleIds != null
                         && data.ownedVehicleIds.Contains("car-a")
                         && data.ownedVehicleIds.Contains("car-b")
                         && data.ownedAttachmentIds != null
                         && data.ownedAttachmentIds.Contains("ram-a")
                         && data.ownedAttachmentIds.Contains("blade-b");
            if (!valid)
            {
                throw new BuildFailedException(
                    "Cloud progression reconciliation self-check failed.");
            }
        }

        private static bool IsProductionAdMobAppId(string value)
        {
            return !string.IsNullOrWhiteSpace(value)
                   && !string.Equals(
                       value,
                       TestAdMobAppId,
                       StringComparison.Ordinal)
                   && value.StartsWith(
                       "ca-app-pub-",
                       StringComparison.Ordinal);
        }

        private static bool IsProductionRewardedAdUnitId(string value)
        {
            return !string.IsNullOrWhiteSpace(value)
                   && !string.Equals(
                       value,
                       TestRewardedAdUnitId,
                       StringComparison.Ordinal)
                   && value.StartsWith(
                       "ca-app-pub-",
                       StringComparison.Ordinal);
        }

        private static bool IsHttpsUrl(string value)
        {
            return Uri.TryCreate(value, UriKind.Absolute, out Uri uri)
                   && string.Equals(
                       uri.Scheme,
                       Uri.UriSchemeHttps,
                       StringComparison.OrdinalIgnoreCase);
        }

        private static void SynchronizeGoogleMobileAdsAppId(string appId)
        {
            UnityEngine.Object asset =
                AssetDatabase.LoadMainAssetAtPath(GoogleSettingsPath);
            if (asset == null)
            {
                throw new BuildFailedException(
                    $"Google Mobile Ads settings asset is missing: {GoogleSettingsPath}");
            }

            SerializedObject serializedSettings = new(asset);
            SerializedProperty iosAppId = serializedSettings.FindProperty(
                "adMobIOSAppId");
            if (iosAppId == null)
            {
                throw new BuildFailedException(
                    "The installed Google Mobile Ads package has an incompatible settings format.");
            }

            if (string.Equals(
                    iosAppId.stringValue,
                    appId,
                    StringComparison.Ordinal))
            {
                return;
            }

            iosAppId.stringValue = appId;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
