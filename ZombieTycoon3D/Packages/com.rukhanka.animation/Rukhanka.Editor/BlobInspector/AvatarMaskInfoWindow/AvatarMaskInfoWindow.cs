using Rukhanka.Toolbox;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Editor
{
public class AvatarMaskInfoWindow : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset visualTreeAsset = default;
    [SerializeField]
    private VisualTreeAsset includedBoneNameInfoAsset = default;
    [SerializeField]
    private VisualTreeAsset entityRefAsset = default;
    
    internal static BlobInspector.BlobAssetInfo<AvatarMaskBlob> avatarMaskBlob;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void CreateGUI()
    {
        var root = rootVisualElement;
        var doc = visualTreeAsset.Instantiate();
        root.Add(doc);
        
        titleContent = new GUIContent("Rukhanka.Animation Avatar Mask Blob Info");
        FillAvatarMaskInfo();
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    unsafe void FillAvatarMaskInfo()
    {
        ref var b = ref avatarMaskBlob.blobAsset.Value;
        var hashLabel = rootVisualElement.Q<Label>("hashLabel");
        hashLabel.text = b.hash.ToString();
        
        var nameLabel = rootVisualElement.Q<Label>("nameLabel");
        var bakingTimeLabel = rootVisualElement.Q<Label>("bakingTimeLabel");
    #if RUKHANKA_DEBUG_INFO
        nameLabel.text = b.name.ToString();
        bakingTimeLabel.text = $"{b.bakingTime:F3} sec";
    #else
        nameLabel.text = "-";
        bakingTimeLabel.text = "-";
    #endif
        
        var sizeLabel = rootVisualElement.Q<Label>("sizeLabel");
        sizeLabel.text = CommonTools.FormatMemory(avatarMaskBlob.blobAsset.m_data.Header->Length);
        
        //  Fill included bone names
    #if RUKHANKA_DEBUG_INFO
        ref var boneNames = ref b.includedBoneNames;
        var includedBonesFoldout = rootVisualElement.Q<Foldout>("includedBonesFoldout");
        includedBonesFoldout.text = $"{boneNames.Length} Included Bones";
        
        for (var i = 0; i < boneNames.Length; ++i)
        {
            ref var bn = ref boneNames[i];
            var includeBodyPartUIEntry = includedBoneNameInfoAsset.Instantiate();
            
            if (i % 2 == 0)
                includeBodyPartUIEntry.style.backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 1));
            
            var nameLabelP = includeBodyPartUIEntry.Q<Label>("nameLabel");
            nameLabelP.text = bn.ToString();
            includedBonesFoldout.Add(includeBodyPartUIEntry);
        }
    #endif
        
        //  Fill included human body parts names
        var humanBodyPartsFoldout = rootVisualElement.Q<Foldout>("humanBodyPartsFoldout");
        var totalPartsCount = 0;
        var humanBodyPartsCount = (int)AvatarMaskBodyPart.LastBodyPart;
        for (int i = 0; i < humanBodyPartsCount; ++i)
        {
            var ambp = (AvatarMaskBodyPart)i;
            if ((b.humanBodyPartsAvatarMask & 1 << i) != 0)
            {
                var humanBodyPartUIEntry = includedBoneNameInfoAsset.Instantiate();
                
                if (i % 2 == 0)
                    humanBodyPartUIEntry.style.backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 1));
                
                var nameLabelP = humanBodyPartUIEntry.Q<Label>("nameLabel");
                var hashLabelP = humanBodyPartUIEntry.Q<Label>("hashLabel");
                
                nameLabelP.text = ambp.ToString();
                hashLabelP.text = i.ToString();
                
                humanBodyPartsFoldout.Add(humanBodyPartUIEntry);
                totalPartsCount++;
            }
        }
        humanBodyPartsFoldout.text = $"{totalPartsCount} Included Human Body Parts";
        
        //  Referenced entities view
        var relatedEntitiesView = rootVisualElement.Q("relatedEntitiesView");
        var relatedEntitieLabel = rootVisualElement.Q<Label>("relatedEntitiesLabel");
        AnimatorClipBlobInfoWindow.PopulateReferencedEntities(relatedEntitiesView, relatedEntitieLabel, avatarMaskBlob.refEntities, entityRefAsset);
    }
}
}