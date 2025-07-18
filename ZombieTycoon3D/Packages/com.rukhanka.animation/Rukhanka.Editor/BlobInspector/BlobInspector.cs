using System;
using System.Collections.Generic;
using System.IO;
using Rukhanka.Hybrid;
using Rukhanka.Toolbox;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.UIElements;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Editor
{
public class BlobInspector : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset menuAsset = default;
    [SerializeField]
    private VisualTreeAsset blobDBPaneAsset = default;
    [SerializeField]
    private VisualTreeAsset blobCachePaneAsset = default;
    [SerializeField]
    private VisualTreeAsset blobCacheEntryAsset = default;
    [SerializeField]
    private VisualTreeAsset listViewLabelAsset = default;
    [SerializeField]
    private VisualTreeAsset listViewInfoBtnAsset = default;
    
    VisualElement
        menuElem,
        blobCachePane,
        blobDBPane
        ;
    
    TwoPaneSplitView splitView;
    
    internal enum BlobType
    {
        AnimatorController,
        AnimationClip,
        RigInfo,
        SkinnedMeshInfo,
        AvatarMask,
        Total
    }
    
    internal class BlobAssetInfo<T> where T: unmanaged
    {
        public BlobAssetReference<T> blobAsset;
        public List<Entity> refEntities;
    }
    
    class BlobAssetsSummary
    {
        public int sizeInBytes;
        public int totalCount;
    }
        
    List<BlobAssetInfo<ControllerBlob>> allControllerBlobAssets = new ();
    List<BlobAssetInfo<AnimationClipBlob>> allAnimationClipBlobAssets = new ();
    List<BlobAssetInfo<RigDefinitionBlob>> allRigBlobAssets = new ();
    List<BlobAssetInfo<SkinnedMeshInfoBlob>> allSkinnedMeshBlobAssets = new ();
    List<BlobAssetInfo<AvatarMaskBlob>> allAvatarMaskBlobAssets = new ();
    BlobAssetsSummary blobAssetsSummary;
    
    readonly string nameColumnName = "name";
    readonly string hashColumnName = "hash";
    readonly string referencesColumnName = "references";
    readonly string sizeColumnName = "size";
    readonly string bakingTimeColumnName = "bakingTime";
    readonly string infoColumnName = "info";
        
    internal static World currentWorld;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    [MenuItem("Window/Rukhanka.Animation/Blob Inspector")]
    public static void ShowWindow()
    {
        var wnd = GetWindow<BlobInspector>();
        wnd.minSize = new Vector2(1000, 400);
        wnd.titleContent = new GUIContent("Rukhanka.Animation Blob Inspector");
    }
	
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        var root = rootVisualElement;

        splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
        root.Add(splitView);
        
        menuElem = menuAsset.Instantiate();
        menuElem.Query<Button>().ForEach((btn) => {btn.RegisterCallback<ClickEvent>(ev => MenuButtonClick(btn));});
        splitView.Add(menuElem);
        
        blobDBPane = blobDBPaneAsset.Instantiate()[0];
        blobCachePane = blobCachePaneAsset.Instantiate()[0];
        splitView.Add(blobDBPane);
        
        MenuButtonClick(menuElem.Q("blobDBBtn") as Button);
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    void MenuButtonClick(Button btn)
    {
        menuElem.Query<Button>().ForEach((btn) => {btn.style.backgroundColor = new StyleColor(Color.clear);});
        btn.style.backgroundColor = new StyleColor(new Color(0.4f, 0.4f, 0.4f, 1));

        splitView.RemoveAt(1);
        switch (btn.name)
        {
            case "blobDBBtn":
                SwitchToBlobDBPane();
                break;
            case "blobCacheBtn":
                SwitchToBlobCachePane();
                break;
        }
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    void FillBlobDBInifo()
    {
        FillWorldList();
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    void FillWorldList()
    {
        var worldSelector = blobDBPane.Q<DropdownField>("worldSelector");
        worldSelector.RegisterValueChangedCallback((worldName) => { ChangeWorld(worldName.newValue); });
        worldSelector.choices.Clear();
        foreach (var world in World.All)
        {
            worldSelector.choices.Add(world.Name);
        }
        if (worldSelector.index < 0 || worldSelector.index > worldSelector.choices.Count)
            worldSelector.index = 0;
        worldSelector.value = worldSelector.choices[worldSelector.index];
        
        var worldReloadBtn = worldSelector.Q<Button>("worldReloadBtn");
        Action clickLambda = () => { SwitchToBlobDBPane(); ChangeWorld(worldSelector.choices[worldSelector.index]); };
        var clk = new Clickable(clickLambda);
        worldReloadBtn.clickable = clk;
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    void ChangeWorld(string worldName)
    {
        World world = null;
        for (var i = 0; i < World.All.Count && world == null; ++i)
        {
            if (World.All[i].Name == worldName)
                world = World.All[i];
        }
        
        if (world != null)
        {
            GatherAllBlobAssets(world);
            var totalInfoLabel = blobDBPane.Q<Label>("blobInfoTotal");
            totalInfoLabel.text = GatherBlobDBInfo();
            CreateBlobAssetList();
        }
        currentWorld = world;
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    unsafe BlobAssetInfo<T> RegisterBlobAsset<T>(BlobAssetReference<T> bar, List<BlobAssetInfo<T>> allBlobs) where T: unmanaged, GenericAssetBlob
    {
        blobAssetsSummary.totalCount += 1;
        blobAssetsSummary.sizeInBytes += bar.m_data.Header->Length;
        var alreadyExistIndex = allBlobs.FindIndex(x => x.blobAsset.Value.Hash() == bar.Value.Hash());
        if (alreadyExistIndex >= 0)
            return allBlobs[alreadyExistIndex];
        
        var rv = new BlobAssetInfo<T>()
        {
            blobAsset = bar,
            refEntities = new ()
        };
        allBlobs.Add(rv);
        return rv;
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    void GatherAllBlobAssets(World world)
    {
        blobAssetsSummary = new ();
        
        //  Gather blob assets from database
        var dbQ = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<BlobDatabaseSingleton>()
            .Build(world.EntityManager);
        
        if (dbQ.TryGetSingleton<BlobDatabaseSingleton>(out var db))
        {
            allAnimationClipBlobAssets.Clear();
            foreach (var kv in db.animations)
            {
                if (!BlobDatabaseSingleton.IsBlobValid(kv.Value))
                    continue;
                
                RegisterBlobAsset(kv.Value, allAnimationClipBlobAssets);
            }
            
            allAvatarMaskBlobAssets.Clear();
            foreach (var kv in db.avatarMasks)
            {
                if (!BlobDatabaseSingleton.IsBlobValid(kv.Value))
                    continue;
                
                RegisterBlobAsset(kv.Value, allAvatarMaskBlobAssets);
            }
        }
        
        //  Gather animator, animation and avatar mask blob assets from entities
        allControllerBlobAssets.Clear();
        var eQ = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<AnimatorControllerLayerComponent>()
            .Build(world.EntityManager);
        
        var animControllersChunks = eQ.ToArchetypeChunkArray(Allocator.Temp);
        var controllerLayerBufHandle = world.EntityManager.GetBufferTypeHandle<AnimatorControllerLayerComponent>(true);
        var entityHandle = world.EntityManager.GetEntityTypeHandle();
        for (var i = 0; i < animControllersChunks.Length; ++i)
        {
            var chunk = animControllersChunks[i];
            var bufAcc = chunk.GetBufferAccessor(ref controllerLayerBufHandle);
            var entities = chunk.GetNativeArray(entityHandle);
            for (var k = 0; k < chunk.Count; ++k)
            {
                var lb = bufAcc[k];
                var e = entities[k];
                for (var l = 0; l < lb.Length; ++l)
                {
                    var acl = lb[l];
                    
                    var controllerBlobInfo = RegisterBlobAsset(acl.controller, allControllerBlobAssets);
                    controllerBlobInfo.refEntities.Add(e);
                    
                    ref var layers = ref acl.controller.Value.layers;
                    for (var m = 0; m < layers.Length; ++m)
                    {
                        ref var layer = ref layers[m];
                        foreach (var amb in allAvatarMaskBlobAssets)
                        {
                            if (amb.blobAsset.Value.Hash() == layer.avatarMaskBlobHash)
                                amb.refEntities.Add(e);
                        }
                    }

                    ref var anims = ref acl.animations.Value.animations;
                    for (var m = 0; m < anims.Length; ++m)
                    {
                        var anmHash = anims[m];
                        foreach (var acb in allAnimationClipBlobAssets)
                        {
                            if (acb.blobAsset.Value.Hash() == anmHash)
                                acb.refEntities.Add(e);
                        }
                    }
                }
            }
        }
        
        //  Gather skinned mesh blob assets from entities
        allSkinnedMeshBlobAssets.Clear();
        var eSMR = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<AnimatedSkinnedMeshComponent>()
            .Build(world.EntityManager);
        
        var smrChunks = eSMR.ToArchetypeChunkArray(Allocator.Temp);
        var smrTypeHandle = world.EntityManager.GetComponentTypeHandle<AnimatedSkinnedMeshComponent>(true);
        for (var i = 0; i < smrChunks.Length; ++i)
        {
            var chunk = smrChunks[i];
            var smrs = chunk.GetNativeArray(ref smrTypeHandle);
            var entities = chunk.GetNativeArray(entityHandle);
            for (var k = 0; k < chunk.Count; ++k)
            {
                var smr = smrs[k];
                var e = entities[k];
                    
                var skinnedMeshBlobInfo = RegisterBlobAsset(smr.smrInfoBlob, allSkinnedMeshBlobAssets);
                skinnedMeshBlobInfo.refEntities.Add(e);
            }
        }
        
        //  Gather rig definition blob assets from entities
        allRigBlobAssets.Clear();
        var eRig = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<RigDefinitionComponent>()
            .Build(world.EntityManager);
        
        var rigChunks = eRig.ToArchetypeChunkArray(Allocator.Temp);
        var rigDefTypeHandle = world.EntityManager.GetComponentTypeHandle<RigDefinitionComponent>(true);
        for (var i = 0; i < rigChunks.Length; ++i)
        {
            var chunk = rigChunks[i];
            var rigs = chunk.GetNativeArray(ref rigDefTypeHandle);
            var entities = chunk.GetNativeArray(entityHandle);
            for (var k = 0; k < chunk.Count; ++k)
            {
                var rigDef = rigs[k];
                var e = entities[k];
                    
                var rigBlobInfo = RegisterBlobAsset(rigDef.rigBlob, allRigBlobAssets);
                rigBlobInfo.refEntities.Add(e);
            }
        }
        
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    string GatherBlobDBInfo()
    {
        var rv = $"Summary: {blobAssetsSummary.totalCount} blob assets, total memory {CommonTools.FormatMemory(blobAssetsSummary.sizeInBytes)}";
        return rv;
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    void SwitchToBlobCachePane()
    {
        splitView.Add(blobCachePane);
        var clearCacheBtn = rootVisualElement.Q<Button>("clearCacheBtn");
        var disableEnableCacheBtn = rootVisualElement.Q<Button>("disableEnableCacheBtn");
        
        Action clearCacheLambda = () =>
        {
            Directory.Delete(BlobCache.GetControllerBlobCacheDirPath(), true);
            Directory.Delete(BlobCache.GetAnimationBlobCacheDirPath(), true);
            FillBlobCacheInfo();
        };
        clearCacheBtn.clickable = new Clickable(clearCacheLambda);
        
        Action disableEnableCacheLambda = () =>
        {
            var bt = EditorUserBuildSettings.activeBuildTarget;
            var btg = BuildPipeline.GetBuildTargetGroup(bt);
            var target = NamedBuildTarget.FromBuildTargetGroup(btg);
            PlayerSettings.GetScriptingDefineSymbols(target, out var defines);
        #if RUKHANKA_NO_BLOB_CACHE
            var l = new List<string>(defines);
            l.Remove("RUKHANKA_NO_BLOB_CACHE");
            defines = l.ToArray();
        #else
            var newDefines = new string[defines.Length + 1];
            Array.Copy(defines, newDefines, defines.Length);
            newDefines[^1] = "RUKHANKA_NO_BLOB_CACHE";
            defines = newDefines;
        #endif
            PlayerSettings.SetScriptingDefineSymbols(target, defines);
            CompilationPipeline.RequestScriptCompilation();
        };
        disableEnableCacheBtn.clickable = new Clickable(disableEnableCacheLambda);
    #if RUKHANKA_NO_BLOB_CACHE
        disableEnableCacheBtn.text = "Enable Cache";
    #else
        disableEnableCacheBtn.text = "Disable Cache";
        var blobCacheDisabledLabel = rootVisualElement.Q<VisualElement>("blobCacheDisabledLabel");
        blobCacheDisabledLabel.style.display = DisplayStyle.None;
    #endif
        
        FillBlobCacheInfo();
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    void SwitchToBlobDBPane()
    {
        splitView.Add(blobDBPane);
    #if RUKHANKA_DEBUG_INFO
        var noDebugInfoWarning = blobDBPane.Q("noRukhankaDebugInfoWarning");
        noDebugInfoWarning.style.display = DisplayStyle.None;
    #endif
        FillBlobDBInifo();
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    unsafe void FillMultiColumnViewList<T>(MultiColumnListView lv, List<BlobAssetInfo<T>> allBlobAssets, Action<int> infoBtnClickAction) where T: unmanaged, GenericAssetBlob
    {
        lv.itemsSource = allBlobAssets;
        
        lv.columns[infoColumnName].makeCell = () => listViewInfoBtnAsset.Instantiate().Q<Button>("btn");
        lv.columns[nameColumnName].makeCell = () => listViewLabelAsset.Instantiate().Q<Label>("label");
        lv.columns[hashColumnName].makeCell = () => listViewLabelAsset.Instantiate().Q<Label>("label");
        lv.columns[referencesColumnName].makeCell = () => listViewLabelAsset.Instantiate().Q<Label>("label");
        lv.columns[bakingTimeColumnName].makeCell = () => listViewLabelAsset.Instantiate().Q<Label>("label");
        lv.columns[sizeColumnName].makeCell = () => listViewLabelAsset.Instantiate().Q<Label>("label");
        
        lv.columns[infoColumnName].bindCell = (VisualElement ve, int index) =>
            (ve as Button).clicked += () => infoBtnClickAction(index);
        lv.columns[referencesColumnName].bindCell = (VisualElement ve, int index) =>
            (ve as Label).text = allBlobAssets[index].refEntities.Count.ToString();
        lv.columns[nameColumnName].bindCell = (VisualElement ve, int index) =>
        #if RUKHANKA_DEBUG_INFO
            (ve as Label).text = allBlobAssets[index].blobAsset.Value.Name();
        #else
            (ve as Label).text = "-";
        #endif
        lv.columns[hashColumnName].bindCell = (VisualElement ve, int index) =>
            (ve as Label).text = allBlobAssets[index].blobAsset.Value.Hash().ToString();
        lv.columns[bakingTimeColumnName].bindCell = (VisualElement ve, int index) =>
        {
            var bt = -1.0f;
        #if RUKHANKA_DEBUG_INFO
            bt = allBlobAssets[index].blobAsset.Value.BakingTime();
        #endif
            (ve as Label).text = bt < 0 ? "-" : $"{bt:F3} sec";
        };
        
        lv.columns[sizeColumnName].bindCell = (VisualElement ve, int index) =>
            (ve as Label).text = CommonTools.FormatMemory(allBlobAssets[index].blobAsset.m_data.Header->Length);
    }

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    void CreateBlobAssetList()
    {
        var animationClipsList = blobDBPane.Q<MultiColumnListView>("animationClipList");
        Action<int> infoCallback = (idx) =>
        {
            AnimatorClipBlobInfoWindow.animationClipBlob = allAnimationClipBlobAssets[idx];
            var wnd = GetWindow<AnimatorClipBlobInfoWindow>();
            wnd.Show();
        };
        FillMultiColumnViewList(animationClipsList, allAnimationClipBlobAssets, infoCallback);
        
        var animatorControllersList = blobDBPane.Q<MultiColumnListView>("controllerList");
        infoCallback = (idx) =>
        {
            AnimatorControllerBlobInfoWindow.controllerBlob = allControllerBlobAssets[idx];
            var wnd = GetWindow<AnimatorControllerBlobInfoWindow>();
            wnd.Show();
        };
        FillMultiColumnViewList(animatorControllersList, allControllerBlobAssets, infoCallback);
        
        var rigList = blobDBPane.Q<MultiColumnListView>("rigList");
        infoCallback = (idx) =>
        {
            RigBlobInfoWindow.rigBlob = allRigBlobAssets[idx];
            var wnd = GetWindow<RigBlobInfoWindow>();
            wnd.Show();
        };
        FillMultiColumnViewList(rigList, allRigBlobAssets, infoCallback);
        
        var avatarMaskList = blobDBPane.Q<MultiColumnListView>("avatarMaskList");
        infoCallback = (idx) =>
        {
            AvatarMaskInfoWindow.avatarMaskBlob = allAvatarMaskBlobAssets[idx];
            var wnd = GetWindow<AvatarMaskInfoWindow>();
            wnd.Show();
        };
        FillMultiColumnViewList(avatarMaskList, allAvatarMaskBlobAssets, infoCallback);
        
        var smrList = blobDBPane.Q<MultiColumnListView>("smrList");
        infoCallback = (idx) =>
        {
            SkinnedMeshBlobInfoWindow.skinnedMeshBlob = allSkinnedMeshBlobAssets[idx];
            var wnd = GetWindow<SkinnedMeshBlobInfoWindow>();
            wnd.Show();
        };
        FillMultiColumnViewList(smrList, allSkinnedMeshBlobAssets, infoCallback);
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            /*
    unsafe void FillBlobAssetInfoText(BlobAssetListInfo li, BlobAssetInfo ba)
    {
        switch (ba.blobType)
        {
        case BlobType.AnimationClip:
        {
            var acb = ba.blobAsset.Reinterpret<AnimationClipBlob>();
            li.name = "-";
        #if RUKHANKA_DEBUG_INFO
            li.name = acb.Value.name.ToString();
        #endif
            li.references = ba.refEntities.Count;
            li.size = ba.blobAsset.m_data.Header->Length;
            li.bakingTime = -1;
        #if RUKHANKA_DEBUG_INFO
            li.bakingTime = acb.Value.bakingTime;
        #endif
            break;
        }
        case BlobType.AnimatorController:
        {
            var acb = ba.blobAsset.Reinterpret<ControllerBlob>();
            var nameText = $"[{acb.Value.hash}]";
        #if RUKHANKA_DEBUG_INFO
            nameText += $" '{acb.Value.name.ToString()}'";
        #endif
            var infoText = $"References: {ba.refEntities.Count}, Size: { CommonTools.FormatMemory(ba.blobAsset.m_data.Header->Length) }";
        #if RUKHANKA_DEBUG_INFO
            infoText += $" Baking time: {acb.Value.bakingTime:F3} sec";
        #endif
            infoLabel.text = infoText;
            nameLabel.text = nameText;
            infoBtn.clicked += () =>
            {
                AnimatorControllerBlobInfoWindow.controllerBlob = ba;
                var wnd = GetWindow<AnimatorControllerBlobInfoWindow>();
                wnd.Show();
            };
            break;
        }
        case BlobType.RigInfo:
        {
            var acb = ba.blobAsset.Reinterpret<RigDefinitionBlob>();
            var nameText = $"[{acb.Value.hash}]";
        #if RUKHANKA_DEBUG_INFO
            nameText = $" '{acb.Value.name.ToString()}'";
        #endif
            var infoText = $"References: {ba.refEntities.Count}, Size: {CommonTools.FormatMemory(ba.blobAsset.m_data.Header->Length)}";
        #if RUKHANKA_DEBUG_INFO
            infoText += $" Baking time: {acb.Value.bakingTime:F3} sec";
        #endif
            infoLabel.text = infoText;
            nameLabel.text = nameText;
            infoBtn.clicked += () =>
            {
                RigBlobInfoWindow.rigBlob = ba;
                var wnd = GetWindow<RigBlobInfoWindow>();
                wnd.Show();
            };
            break;
        }
        case BlobType.AvatarMask:
        {
            var acb = ba.blobAsset.Reinterpret<AvatarMaskBlob>();
            var nameText = $"[{acb.Value.hash}]";
        #if RUKHANKA_DEBUG_INFO
            nameText += $" '{acb.Value.name.ToString()}'";
        #endif
            var infoText = $"References: {ba.refEntities.Count}, Size: { CommonTools.FormatMemory(ba.blobAsset.m_data.Header->Length) }";
        #if RUKHANKA_DEBUG_INFO
            infoText += $" Baking time: {acb.Value.bakingTime:F3} sec";
        #endif
            infoLabel.text = infoText;
            nameLabel.text = nameText;
            infoBtn.clicked += () =>
            {
                AvatarMaskInfoWindow.avatarMaskBlob = ba;
                var wnd = GetWindow<AvatarMaskInfoWindow>();
                wnd.Show();
            };
            break;
        }
        case BlobType.SkinnedMeshInfo:
        {
            var acb = ba.blobAsset.Reinterpret<SkinnedMeshInfoBlob>();
            var nameText = $"[{acb.Value.hash}]";
        #if RUKHANKA_DEBUG_INFO
            nameText += $" '{acb.Value.skeletonName.ToString()}'";
        #endif
            var infoText = $"References: {ba.refEntities.Count}, Size: {CommonTools.FormatMemory(ba.blobAsset.m_data.Header->Length)}";
        #if RUKHANKA_DEBUG_INFO
            infoText += $" Baking time: {acb.Value.bakingTime:F3} sec";
        #endif
            infoLabel.text = infoText;
            nameLabel.text = nameText;
            infoBtn.clicked += () =>
            {
                SkinnedMeshBlobInfoWindow.skinnedMeshBlob = ba;
                var wnd = GetWindow<SkinnedMeshBlobInfoWindow>();
                wnd.Show();
            };
            break;
        }
        }
    }
        */
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    void FillBlobCacheInfo()
    {
        var cachePathLabel = rootVisualElement.Q<Label>("blobCachePathLabel");
        cachePathLabel.text = $"Cache Path: '{BlobCache.GetBlobCacheDirPath()}'";
        
        var controllerMem = FillBlobList("animatorControllerBlobsFoldout", "Animator Controller Blobs", BlobCache.GetControllerBlobCacheDirPath());
        var animationsMem = FillBlobList("animationBlobsFoldout", "Animation Clip Blobs", BlobCache.GetAnimationBlobCacheDirPath());
        
        var animMemLabel = rootVisualElement.Q<Label>("totalAnimationsMemLabel");
        var controllerMemLabel = rootVisualElement.Q<Label>("totalControllerMemLabel");
        animMemLabel.text = $"Total animation blob cache size: {CommonTools.FormatMemory(animationsMem)}";
        controllerMemLabel.text = $"Total animator controller blob cache size: {CommonTools.FormatMemory(controllerMem)}";
        
    }
    
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    long FillBlobList(string foldoutName, string foldoutCaption, string cachePath)
    {
        var foldout = rootVisualElement.Q<Foldout>(foldoutName);
        foldout.Clear();
        var numCachedBlobs = 0;
        var rv = 0L;
        
        if (Directory.Exists(cachePath))
        {
            var files = Directory.GetFiles(cachePath);
            for (var i = 0; i < files.Length; ++i)
            {
                var entry = blobCacheEntryAsset.Instantiate();
                var file = files[i].Replace('\\', '/');
                
                if (i % 2 == 0)
                    entry.style.backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 1));
                
                var entryPath = entry.Q<Label>("pathLabel");
                var sizeLabel = entry.Q<Label>("sizeLabel");
                entryPath.text = file.Replace(BlobCache.GetBlobCacheDirPath(), "");
                var fs = new FileInfo(file).Length;
                sizeLabel.text = CommonTools.FormatMemory(fs);
                rv += fs;
                
                foldout.Add(entry);
            }
            numCachedBlobs += files.Length;
        }
        foldout.text = $"{numCachedBlobs} {foldoutCaption}";
        return rv;
    }
}
}