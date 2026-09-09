using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Vaultbreakers.Tests.EditMode
{
    public sealed class ArtImportTests
    {
        [Test] public void AllTenAuthoredClipsImportAndKeepTranslationInGameplay()
        {
            var clips=AssetDatabase.LoadAllAssetsAtPath("Assets/Vaultbreakers/Art/Animation/Vaultbreaker_Animations.fbx").OfType<AnimationClip>().ToArray();
            foreach(var name in new[]{"Idle","Move","Melee","Ranged","Shield","ShieldHit","ShieldBreak","Dodge","Hit","Death"})
            {
                var clip=clips.FirstOrDefault(c=>c.name==name);Assert.That(clip,Is.Not.Null,name);
                Assert.That(clip.length,Is.GreaterThan(.05f),name);
            }
            foreach(var path in new[]{"Assets/Vaultbreakers/Prefabs/Player/PF_Vaultbreaker_POC.prefab","Assets/Vaultbreakers/Prefabs/Enemies/PF_Grunt.prefab","Assets/Vaultbreakers/Prefabs/Enemies/PF_Shooter.prefab","Assets/Vaultbreakers/Prefabs/Enemies/PF_Bruiser.prefab"})
            {
                var animator=AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentInChildren<Animator>(true);
                Assert.That(animator,Is.Not.Null,path);Assert.That(animator.applyRootMotion,Is.False,path);Assert.That(animator.runtimeAnimatorController,Is.Not.Null,path);
            }
        }
        [Test] public void UpperBodyMaskKeepsLocomotionBonesIndependent()
        {
            var mask=AssetDatabase.LoadAssetAtPath<AvatarMask>("Assets/Vaultbreakers/Art/Animation/UpperBody.mask");
            Assert.That(mask,Is.Not.Null);var upper=0;var legs=0;
            for(var i=0;i<mask.transformCount;i++)
            {
                var path=mask.GetTransformPath(i);
                if(path.EndsWith("Chest") || path.EndsWith("Hand_L")){Assert.That(mask.GetTransformActive(i),Is.True,path);upper++;}
                if(path.EndsWith("Hips") || path.Contains("UpperLeg")){Assert.That(mask.GetTransformActive(i),Is.False,path);legs++;}
            }
            Assert.That(upper,Is.GreaterThan(0));Assert.That(legs,Is.GreaterThan(0));
        }
        [Test] public void EveryClipKeepsSocketLocalBindingsAndPlayerRootFixed()
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Vaultbreakers/Prefabs/Player/PF_Vaultbreaker_POC.prefab");
            var instance=Object.Instantiate(source);
            try
            {
                var model=instance.transform.Find("ModelRoot").gameObject;
                var sockets=model.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("SOCKET_") || t.name.StartsWith("ANCHOR_")).ToArray();
                var positions=sockets.Select(t=>t.localPosition).ToArray();
                foreach(var clip in AssetDatabase.LoadAllAssetsAtPath("Assets/Vaultbreakers/Art/Animation/Vaultbreaker_Animations.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")))
                    foreach(var fraction in new[]{0f,.5f,1f})
                    {
                        clip.SampleAnimation(model,clip.length*fraction);
                        for(var i=0;i<sockets.Length;i++)Assert.That(Vector3.Distance(sockets[i].localPosition,positions[i]),Is.LessThan(.0001f),clip.name+" / "+sockets[i].name);
                        Assert.That(instance.transform.position,Is.EqualTo(Vector3.zero));
                    }
            }
            finally {Object.DestroyImmediate(instance);}
        }
        [Test] public void RebuiltAssetsHaveFiniteGeometryAndConsistentImportSettings()
        {
            var paths=new[]{"Characters/Enemies/Enemy_Grunt","Characters/Enemies/Enemy_Shooter","Characters/Enemies/Enemy_Bruiser","Environments/Combat_Arena","Environments/Dock9_Dungeon","Environments/Training_Target","Environments/Showcase_Deck","VFX/Enemy_Bolt","VFX/Player_Bolt"};
            foreach(var suffix in paths)
            {
                var path="Assets/Vaultbreakers/Art/"+suffix+".fbx";
                var importer=(ModelImporter)AssetImporter.GetAtPath(path);Assert.That(importer,Is.Not.Null,path);
                Assert.That(importer.globalScale,Is.EqualTo(1));Assert.That(importer.addCollider,Is.False);
                var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);var meshes=root.GetComponentsInChildren<MeshFilter>(true);
                Assert.That(meshes.Length,Is.GreaterThan(0));
                foreach(var filter in meshes){Assert.That(filter.sharedMesh,Is.Not.Null);Assert.That(float.IsNaN(filter.sharedMesh.bounds.size.sqrMagnitude),Is.False);Assert.That(filter.sharedMesh.vertexCount,Is.GreaterThan(0));}
            }
        }
    }
}
