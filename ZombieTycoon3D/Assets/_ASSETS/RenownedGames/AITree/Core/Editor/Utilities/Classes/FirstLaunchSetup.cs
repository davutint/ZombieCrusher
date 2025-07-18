/* ================================================================
   ----------------------------------------------------------------
   Project   :   AI Tree
   Publisher :   Renowned Games
   Developer :   Tamerlan Shakirov
   ----------------------------------------------------------------
   Copyright 2024 Renowned Games All rights reserved.
   ================================================================ */

using RenownedGames.ApexEditor;
using UnityEditor;

namespace RenownedGames.AITreeEditor
{
    [InitializeOnLoad]
    static class FirstLaunchSetup
    {
        static FirstLaunchSetup()
        {
            const string FIRST_LAUNCH_KEY = "AI Tree First Launch Key";
            if (!EditorPrefs.GetBool(FIRST_LAUNCH_KEY, false))
            {
                Initialize();
                EditorPrefs.SetBool(FIRST_LAUNCH_KEY, true);
            }
        }

        /// <summary>
        /// Called once when Unity launched with AI Tree at first time.
        /// </summary>
        private static void Initialize()
        {
            SetupApexAssemblyScopes();
        }

        /// <summary>
        /// Setup Apex assembly scopes within AI Tree project assemblies.
        /// </summary>
        private static void SetupApexAssemblyScopes()
        {
            ApexSettings settings = ApexSettings.instance;
            settings.IncludeAllAssembly(false);
            settings.IncludeDefaultAssembly(false);
            settings.SetAssemblyScopes(new AssemblyScope[6]
            {
                new AssemblyScope("AITree", string.Empty),
                new AssemblyScope("AITree.Nodes", string.Empty),
                new AssemblyScope("AITree.EQS", string.Empty),
                new AssemblyScope("AITree.PerceptionSystem", string.Empty),
                new AssemblyScope("AITree.Demo", string.Empty),
                new AssemblyScope("AITreeEditor", string.Empty),
            });
            settings.Save();
        }
    }
}