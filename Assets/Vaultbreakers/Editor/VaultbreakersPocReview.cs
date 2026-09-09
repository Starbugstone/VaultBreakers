using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vaultbreakers.Editor
{
    public static class VaultbreakersPocReview
    {
        [Serializable] private sealed class AssetRow
        {
            public string path;
            public int meshes, triangles, materialSlots, bones;
        }
        [Serializable] private sealed class Report { public List<AssetRow> assets=new(); }
        [MenuItem("Vaultbreakers/Review/Report imported art")]
        public static void ReportArt()
        {
            var report=new Report();
            foreach(var file in Directory.GetFiles("Assets/Vaultbreakers/Art","*.fbx",SearchOption.AllDirectories))
            {
                var path=file.Replace('\\','/');var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var row=new AssetRow{path=path};
                foreach(var mesh in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>())
                {
                    if(!mesh.HasVertexAttribute(VertexAttribute.Normal))throw new InvalidOperationException("Mesh has no normals: "+path+" / "+mesh.name);
                    row.meshes++;
                    for(var i=0;i<mesh.subMeshCount;i++)row.triangles+=(int)mesh.GetIndexCount(i)/3;
                }
                row.materialSlots=model.GetComponentsInChildren<Renderer>(true).Sum(r=>r.sharedMaterials.Length);
                row.bones=model.GetComponentsInChildren<Transform>(true).Count(t=>t.name is "Root" or "Hips" or "Spine" or "Chest" or "Neck" or "Head" || t.name.StartsWith("UpperArm_") || t.name.StartsWith("LowerArm_") || t.name.StartsWith("Hand_") || t.name.StartsWith("UpperLeg_") || t.name.StartsWith("LowerLeg_") || t.name.StartsWith("Foot_"));
                report.assets.Add(row);
            }
            File.WriteAllText("Docs/ART_ASSET_REPORT.json",JsonUtility.ToJson(report,true)+"\n");
            Debug.Log("Imported art report saved; "+report.assets.Count+" FBX assets checked.");
        }
        [MenuItem("Vaultbreakers/Build/Windows development POC")]
        public static void BuildWindows()
        {
            ReportArt();VaultbreakersAudioReview.Export();
            const string path="Builds/Vaultbreakers_POC/Vaultbreakers.exe";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),
                locationPathName=path,target=BuildTarget.StandaloneWindows64,
                options=BuildOptions.Development
            });
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new InvalidOperationException("POC development build failed: "+report.summary.result);
            Debug.Log("Vaultbreakers development build succeeded with explicit BuildOptions.Development.");
        }
        [MenuItem("Vaultbreakers/Review/Open prototype arena")]
        public static void OpenArena()
        {
            EditorSceneManager.OpenScene(VaultbreakersDungeonBuilder.ScenePath);
            Debug.Log("Vaultbreakers Dock 9 dungeon opened and ready.");
        }
        public static void RunGraphicsReview()
        {
            OpenArena();EditorApplication.isPlaying=true;
        }
    }
}
