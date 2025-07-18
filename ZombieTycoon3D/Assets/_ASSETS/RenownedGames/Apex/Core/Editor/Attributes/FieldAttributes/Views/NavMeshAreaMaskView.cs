/* ================================================================
  ---------------------------------------------------
  Project   :    Apex
  Publisher :    Renowned Games
  Developer :    Tamerlan Shakirov
  ---------------------------------------------------
  Copyright 2020-2023 Renowned Games All rights reserved.
  ================================================================ */

using RenownedGames.Apex;
using UnityEditor;
using UnityEngine;

namespace RenownedGames.ApexEditor
{
    [ViewTarget(typeof(NavMeshAreaMaskAttribute))]
    public sealed class NavMeshAreaMaskView : FieldView, ITypeValidationCallback
    {
        private SerializedProperty property;

        public override void Initialize(SerializedField serializedField, ViewAttribute viewAttribute, GUIContent label)
        {
            property = serializedField.GetSerializedProperty();
        }

        public override void OnGUI(Rect position, SerializedField serializedField, GUIContent label)
        {
#if UNITY_6000_0_OR_NEWER
            string[] navMeshAreaNames = UnityEngine.AI.NavMesh.GetAreaNames();
#else
            string[] navMeshAreaNames = GameObjectUtility.GetNavMeshAreaNames();
#endif
            long longValue = property.longValue;
            int num = 0;
            for (int i = 0; i < navMeshAreaNames.Length; i++)
            {
#if UNITY_6000_0_OR_NEWER
                int navMeshAreaFromName = UnityEngine.AI.NavMesh.GetAreaFromName(navMeshAreaNames[i]);
#else
                int navMeshAreaFromName = GameObjectUtility.GetNavMeshAreaFromName(navMeshAreaNames[i]);
#endif
                if ((1L << (navMeshAreaFromName & 31) & longValue) != 0L)
                {
                    num |= 1 << i;
                }
            }
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            int num2 = EditorGUI.MaskField(position, label, num, navMeshAreaNames, EditorStyles.layerMaskField);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                if (num2 == -1)
                {
                    property.longValue = -1;
                }
                else
                {
                    uint num3 = 0u;
                    for (int j = 0; j < navMeshAreaNames.Length; j++)
                    {
                        if ((num2 >> j & 1) != 0)
                        {
#if UNITY_6000_0_OR_NEWER
                            num3 |= 1u << UnityEngine.AI.NavMesh.GetAreaFromName(navMeshAreaNames[j]);
#else
                            num3 |= 1u << GameObjectUtility.GetNavMeshAreaFromName(navMeshAreaNames[j]);
#endif
                        }
                    }
                    property.longValue = (long)((ulong)num3);
                }
            }
        }

        /// <summary>
        /// Return true if this property valid the using with this attribute.
        /// If return false, this property attribute will be ignored.
        /// </summary>
        /// <param name="property">Reference of serialized property.</param>
        public bool IsValidProperty(SerializedProperty property)
        {
            return property.propertyType == SerializedPropertyType.Integer;
        }
    }
}