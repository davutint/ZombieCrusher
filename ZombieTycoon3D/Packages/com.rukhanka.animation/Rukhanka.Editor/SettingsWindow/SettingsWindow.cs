using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Editor
{
public class SettingsWindow: SettingsProvider
{
    ResponseFileManager
        editorRSP,
        hybridRSP,
        runtimeRSP,
        debugDrawerRSP;
    
    ShaderConfigManager scm;
    
    const string EDITOR_RSP_PATH = "Packages/com.rukhanka.animation/Rukhanka.Editor/csc.rsp";
    const string HYBRID_RSP_PATH = "Packages/com.rukhanka.animation/Rukhanka.Hybrid/csc.rsp";
    const string RUNTIME_RSP_PATH = "Packages/com.rukhanka.animation/Rukhanka.Runtime/csc.rsp";
    const string DEBUG_DRAWER_RSP_PATH = "Packages/com.rukhanka.animation/Rukhanka.DebugDrawer/csc.rsp";
    public const string SHADER_CONFIG_PATH = "Packages/com.rukhanka.animation/Rukhanka.Runtime/Common/Shaders/ShaderConf.hlsl";
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    SettingsWindow(string path, SettingsScope ss = SettingsScope.Project)
    : base(path, ss)
    { }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    [SettingsProvider]
    static SettingsProvider CreateSettingsProvider()
    {
        var rv = new SettingsWindow("Project/Rukhanka.Animation", SettingsScope.Project);
        return rv;
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public override void OnActivate(string searchContext, VisualElement rootElement)
    {
        InitSymbolManagers();
        
        var vta = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.rukhanka.animation/Rukhanka.Editor/UXML/SettingsWindow.uxml");
        var settingsInstance = vta.Instantiate();
        rootElement.Add(settingsInstance);
        
        ConfigureUI(settingsInstance);
        SetupDebugAndValidationCheckbox(settingsInstance);
        SetupToggle(settingsInstance, "ENABLE_DOTS_DEFORMATION_MOTION_VECTORS", "enableMotionVectorsToggle", new []{ runtimeRSP, hybridRSP }, scm);
    #if RUKHANKA_INPLACE_SKINNING
        var hdd = settingsInstance.Q<VisualElement>("halfDeformedData");
        var hddl = settingsInstance.Q<Label>("halfDeformedDataLabel");
        hddl.tooltip = "With in-place skinning enabled there is no internal deformed data storage";
        hdd.SetEnabled(false);
    #else
        SetupToggle(settingsInstance, "RUKHANKA_HALF_DEFORMED_DATA", "halfDeformedDataToggle", new []{ runtimeRSP }, scm);
    #endif
        SetupToggle(settingsInstance, "RUKHANKA_DUAL_QUATERNION_SKINNING", "dualQuaternionSkinningToggle", new []{ runtimeRSP }, scm);
        SetupToggle(settingsInstance, "RUKHANKA_INPLACE_SKINNING", "inplaceSkinningToggle", new []{ runtimeRSP, editorRSP }, scm);
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    void ConfigureUI(VisualElement root)
    {
        SetupHelpURLs(root);
    #if RUKHANKA_NO_DEFORMATION_SYSTEM
        var hdd = root.Q<VisualElement>("halfDeformedData");
        hdd.enabledSelf = false;
        var dqs = root.Q<VisualElement>("dualQuaternionSkinning");
        dqs.enabledSelf = false;
    #endif
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    void InitSymbolManagers()
    {
        editorRSP = new ResponseFileManager(EDITOR_RSP_PATH);
        hybridRSP = new ResponseFileManager(HYBRID_RSP_PATH);
        runtimeRSP = new ResponseFileManager(RUNTIME_RSP_PATH);
        debugDrawerRSP = new ResponseFileManager(DEBUG_DRAWER_RSP_PATH);
        
        scm = new ShaderConfigManager(SHADER_CONFIG_PATH);
    }


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    void SetupToggle(VisualElement root, string scriptSymbol, string toggleName, ResponseFileManager[] rfms, ShaderConfigManager scm)
    {
        var t = root.Q<Toggle>(toggleName);
        t.value = rfms[0].IsSymbolDefined(scriptSymbol);
        t.RegisterCallback<ChangeEvent<bool>>((evt) =>
        {
            foreach (var rfm in rfms)
            {
                rfm.ToggleScriptSymbol(scriptSymbol, evt.newValue);
                rfm.ApplyChanges();
            }
            
            scm?.ToggleScriptSymbol(scriptSymbol, evt.newValue);
            scm?.ApplyChanges();
        });
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    void SetupDebugAndValidationCheckbox(VisualElement settingsInstance)
    {
        var debugModeToggle = settingsInstance.Q<Toggle>("debugInfoToggle");
    #if RUKHANKA_DEBUG_INFO
        debugModeToggle.value = true;
    #else
        debugModeToggle.value = false;
    #endif
        debugModeToggle.RegisterCallback<ChangeEvent<bool>>((evt) => ProjectScriptSymbolManager.ToggleScriptSymbol("RUKHANKA_DEBUG_INFO", evt.newValue));
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    void SetupHelpURLs(VisualElement settingsInstance)
    {
        var debugInfoIcon = settingsInstance.Q<Label>("debugInfoHelpIcon");
        debugInfoIcon.RegisterCallback<ClickEvent>(evt => { Application.OpenURL("https://docs.rukhanka.com/Debug%20and%20Validation/validation_layer"); });
        var halfPrecisionDeformationDataIcon = settingsInstance.Q<Label>("halfDeformedDataHelpIcon");
        halfPrecisionDeformationDataIcon.RegisterCallback<ClickEvent>(evt => { Application.OpenURL("https://docs.rukhanka.com/deformation_system"); });
        var dualQuaternionSkinningIcon = settingsInstance.Q<Label>("dualQuaternionSkinningHelpIcon");
        dualQuaternionSkinningIcon.RegisterCallback<ClickEvent>(evt => { Application.OpenURL("https://docs.rukhanka.com/settings_dialog#dual-quaternion-skinning"); });
        var halfDeformedDataHelpIcon = settingsInstance.Q<Label>("halfDeformedDataHelpIcon");
        halfDeformedDataHelpIcon.RegisterCallback<ClickEvent>(evt => { Application.OpenURL("https://docs.rukhanka.com/Optimizing%20Rukhanka/half_precision_deformation_data"); });
        var inplaceSkinningHelpIcon = settingsInstance.Q<Label>("inplaceSkinningHelpIcon");
        inplaceSkinningHelpIcon.RegisterCallback<ClickEvent>(evt => { Application.OpenURL("https://docs.rukhanka.com/Optimizing%20Rukhanka/inplace_skinning"); });
    }
}
}
