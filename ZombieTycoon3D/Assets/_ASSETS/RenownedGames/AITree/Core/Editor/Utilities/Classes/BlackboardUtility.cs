/* ================================================================
   ----------------------------------------------------------------
   Project   :   AI Tree
   Publisher :   Renowned Games
   Developer :   Zinnur Davleev
   ----------------------------------------------------------------
   Copyright 2022-2023 Renowned Games All rights reserved.
   ================================================================ */

using RenownedGames.AITree;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RenownedGames.AITreeEditor
{
    public static class BlackboardUtility
    {
        /// <summary>
        /// Adds a pre-created key to the blackboard.
        /// </summary>
        /// <param name="blackboard">Blackboard.</param>
        /// <param name="key">Created key.</param>
        /// <returns>True if successful, otherwise false.</returns>
        public static bool AddKey(Blackboard blackboard, Key key)
        {
            int group = Undo.GetCurrentGroup();
            Undo.IncrementCurrentGroup();

            if (!Application.isPlaying)
            {
                Undo.RecordObject(blackboard, $"Add key to blackboard asset.");
                AssetDatabase.AddObjectToAsset(key, blackboard);
            }

            Undo.RecordObject(blackboard, $"Add key to blackboard reference.");
            blackboard.AddKey(key);
            SaveBlackboard(blackboard);
            Undo.CollapseUndoOperations(group);
            return true;
        }

        /// <summary>
        /// Creates a key and adds it to the blackboard.
        /// </summary>
        /// <param name="blackboard">Blackboard.</param>
        /// <param name="keyType">Key type.</param>
        /// <returns>True if successful, otherwise false.</returns>
        public static bool AddKey(Blackboard blackboard, Type keyType)
        {
            Key key = ScriptableObject.CreateInstance(keyType) as Key;
            if (key != null)
            {
                Undo.RegisterCreatedObjectUndo(key, $"Created new blackboard key.");

                int count = 0;
                string name = keyType.Name;
                while (blackboard.Contains(name))
                {
                    name = $"{keyType.Name} {++count}";
                }

                key.name = name;
                return AddKey(blackboard, key);
            }
            return false;
        }

        /// <summary>
        /// Removes the key from the blackboard and destroys it.
        /// </summary>
        /// <param name="blackboard">Blackboard.</param>
        /// <param name="key">Key.</param>
        /// <returns>True if successful, otherwise false.</returns>
        public static bool DeleteKey(Blackboard blackboard, Key key)
        {
            if (!blackboard.GetKeys().Contains(key))
            {
                return false;
            }

            Undo.RecordObject(blackboard, "DELETE_KEY_FROM_BLACKBOARD");
            blackboard.DeleteKey(key);
            Undo.DestroyObjectImmediate(key);
            SaveBlackboard(blackboard);

            return true;
        }

        /// <summary>
        /// Duplicate the key in the blackboard.
        /// </summary>
        /// <param name="blackboard">Blackboard.</param>
        /// <param name="key">Key.</param>
        public static void DuplicateKey(Blackboard blackboard, Key key)
        {
            Key clone = Object.Instantiate(key);
            if (clone != null)
            {
                Undo.RegisterCreatedObjectUndo(clone, $"Duplicated blackboard key.");
                AddKey(blackboard, clone);
            }
        }

        /// <summary>
        /// Get all blackboard in project.
        /// </summary>
        /// <returns>Array of blackboards.</returns>
        public static Blackboard[] GetAllBlackboards()
        {
            const string FILTER = "t:Blackboard";
            string[] guids = AssetDatabase.FindAssets(FILTER);
            Blackboard[] blackboards = new Blackboard[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                string guid = guids[i];
                string path = AssetDatabase.GUIDToAssetPath(guid);
                blackboards[i] = AssetDatabase.LoadAssetAtPath<Blackboard>(path);
            }
            return blackboards;
        }

        public static void SaveBlackboard(Blackboard blackboard)
        {
            if (!EditorApplication.isPlaying)
            {
                AssetDatabase.SaveAssetIfDirty(blackboard);
            }
        }
    }
}