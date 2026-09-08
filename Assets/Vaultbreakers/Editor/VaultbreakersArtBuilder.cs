using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Vaultbreakers.Equipment;

namespace Vaultbreakers.Editor
{
    internal static class VaultbreakersArtBuilder
    {
        private const string AnimationPath = "Assets/Vaultbreakers/Art/Animation/Vaultbreaker_Animations.fbx";
        public static void Prepare()
        {
            foreach(var path in Directory.GetFiles("Assets/Vaultbreakers/Art", "*.fbx", SearchOption.AllDirectories))
            {
                var asset=path.Replace('\\','/');
                if(asset==VaultbreakersSetupPaths.ModelPath)continue;
                AssetDatabase.ImportAsset(asset,ImportAssetOptions.ForceSynchronousImport);
                var importer=(ModelImporter)AssetImporter.GetAtPath(asset);
                importer.globalScale=1;importer.useFileScale=true;importer.importCameras=false;importer.importLights=false;
                importer.importAnimation=asset==AnimationPath;importer.animationType=ModelImporterAnimationType.Generic;
                importer.addCollider=false;importer.isReadable=false;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
                if(asset==AnimationPath)
                {
                    var clips=importer.defaultClipAnimations;
                    foreach(var clip in clips)
                    {
                        clip.name=clip.name.Split('|').Last();
                        clip.loopTime=clip.name is "Idle" or "Move" or "Shield" or "Ranged";
                        clip.lockRootPositionXZ=true;clip.lockRootHeightY=true;clip.lockRootRotation=true;
                    }
                    importer.clipAnimations=clips;
                }
                importer.SaveAndReimport();
                RemapMaterials(importer);
            }
            BuildController();
        }
        public static void RemapMaterials(ModelImporter importer)
        {
            importer.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName,ModelImporterMaterialSearch.Everywhere);
            // Prefer the reproducible shared palette over incidental adjacent FBX extraction folders.
            foreach(var id in importer.GetExternalObjectMap().Keys.ToArray())
            {
                if(id.type!=typeof(Material))continue;
                var shared=VaultbreakersRenderSetup.LoadMaterial(id.name);
                if(shared!=null)importer.AddRemap(id,shared);
            }
            importer.SaveAndReimport();
        }
        private static void BuildController()
        {
            const string path="Assets/Vaultbreakers/Art/Animation/Combat.controller";
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(path);
            var machine=controller.layers[0].stateMachine;
            foreach(var state in machine.states)machine.RemoveState(state.state);
            var clips=AssetDatabase.LoadAllAssetsAtPath(AnimationPath).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
            foreach(var name in new[]{"Idle","Move","Melee","Ranged","Shield","ShieldHit","ShieldBreak","Dodge","Hit","Death"})
            {
                var clip=clips.FirstOrDefault(c=>c.name==name || c.name.EndsWith("|"+name));
                if(clip==null)throw new InvalidOperationException("Missing authored animation: "+name+". Imported: "+string.Join(",",clips.Select(c=>c.name)));
                var state=machine.AddState(name);state.motion=clip;if(name=="Idle")machine.defaultState=state;
            }
            EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();
        }
        public static void Animate(GameObject root,GameObject model)
        {
            var animator=model.GetComponent<Animator>();if(animator==null)animator=model.AddComponent<Animator>();
            animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Vaultbreakers/Art/Animation/Combat.controller");
            animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            root.AddComponent<CombatAnimation>().Configure(animator);
        }
        public static GameObject Model(string path,Transform parent,string name="ModelRoot")
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(source==null)throw new FileNotFoundException("Rebuild the Blender assets first",path);
            var model=(GameObject)PrefabUtility.InstantiatePrefab(source);model.name=name;model.transform.SetParent(parent,false);return model;
        }
        public static void Stage()
        {
            foreach(var name in new[]{"Floor","Wall_North","Wall_South","Wall_East","Wall_West"})GameObject.Find(name).GetComponent<Renderer>().enabled=false;
            Model("Assets/Vaultbreakers/Art/Environments/Combat_Arena.fbx",GameObject.Find("Environment").transform,"ArenaArt");
            foreach(var marker in GameObject.Find("GameplayMarkers").GetComponentsInChildren<Renderer>())marker.enabled=false;
            foreach(var name in new[]{"TargetDummy","TargetDummy_Pair","TargetDummy_Flank"})
            {
                var dummy=GameObject.Find(name);dummy.GetComponent<Renderer>().enabled=false;
                var art=Model("Assets/Vaultbreakers/Art/Environments/Training_Target.fbx",dummy.transform);
                art.transform.localPosition=Vector3.down;
            }
            var profilePath="Assets/Vaultbreakers/Settings/ArcadePost.asset";
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if(profile==null){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,profilePath);}
            if(!profile.TryGet<Bloom>(out var bloom)){bloom=profile.Add<Bloom>();AssetDatabase.AddObjectToAsset(bloom,profile);}
            bloom.intensity.Override(.65f);bloom.threshold.Override(1.1f);bloom.scatter.Override(.45f);
            // Keep contact highlights from spreading an overexposed flash across the room.
            bloom.clamp.Override(4f);
            if(!profile.TryGet<ColorAdjustments>(out var grade)){grade=profile.Add<ColorAdjustments>();AssetDatabase.AddObjectToAsset(grade,profile);}
            grade.contrast.Override(16);grade.saturation.Override(0);grade.postExposure.Override(.25f);
            EditorUtility.SetDirty(profile);
            var go=new GameObject("Arcade lighting");var volume=go.AddComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=profile;
            Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            foreach(var x in new[]{-7f,7f})
            {
                var lightObject=new GameObject("Arena rim");lightObject.transform.position=new Vector3(x,4,5);
                var light=lightObject.AddComponent<Light>();light.type=LightType.Point;light.range=16;light.intensity=5;
                light.color=x<0?new Color(.08f,.6f,1):new Color(1,.18f,.4f);light.shadows=LightShadows.None;
            }
        }
    }
}
