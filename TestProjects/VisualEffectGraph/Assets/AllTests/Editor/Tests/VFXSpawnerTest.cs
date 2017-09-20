using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.VFX;
using UnityEditor;
using UnityEngine.TestTools;
using System.Linq;
using UnityEditor.VFX.UI;
using System.Collections;
using System.Collections.Generic;

/*
namespace UnityEditor.VFX.Test
{
    public class VFXSpawnerTest
    {
        [UnityTest]
        [Timeout(1000 * 10)]
        public IEnumerator CreateAssetAndComponentSpawner()
        {
            EditorApplication.ExecuteMenuItem("Window/Game");
            var graph = ScriptableObject.CreateInstance<VFXGraph>();

            var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var blockBurst = ScriptableObject.CreateInstance<VFXSpawnerBurst>();
            var slotCount = blockBurst.GetInputSlot(0);
            var slotDelay = blockBurst.GetInputSlot(1);

            var spawnCountValue = 753.0f;
            slotCount.value = new Vector2(spawnCountValue, spawnCountValue);
            slotDelay.value = new Vector2(0.0f, 0.0f);

            spawnerContext.AddChild(blockBurst);
            graph.AddChild(spawnerContext);

            graph.vfxAsset = new VFXAsset();
            graph.RecompileIfNeeded();
            graph.vfxAsset.bounds = new Bounds(Vector3.zero, Vector3.positiveInfinity);

            var gameObj = new GameObject("CreateAssetAndComponentSpawner");
            var vfxComponent = gameObj.AddComponent<VFXComponent>();
            vfxComponent.vfxAsset = graph.vfxAsset;

            int maxFrame = 512;
            while (vfxComponent.culled && --maxFrame > 0)
            {
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);
            yield return null; //wait for exactly one more update if visible

            var spawnerState = vfxComponent.GetSpawnerState(0);
            Assert.AreEqual(spawnCountValue, spawnerState.spawnCount);

            UnityEngine.Object.DestroyImmediate(gameObj);
        }

        [UnityTest]
        [Timeout(1000 * 10)]
        public IEnumerator CreateEventAttributeAndStart()
        {
            EditorApplication.ExecuteMenuItem("Window/Game");
            var graph = ScriptableObject.CreateInstance<VFXGraph>();

            var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var blockBurst = ScriptableObject.CreateInstance<VFXSpawnerConstantRate>();

            spawnerContext.AddChild(blockBurst);
            graph.AddChild(spawnerContext);

            graph.vfxAsset = new VFXAsset();
            graph.RecompileIfNeeded();
            graph.vfxAsset.bounds = new Bounds(Vector3.zero, Vector3.positiveInfinity);

            var gameObj = new GameObject("CreateEventAttributeAndStart");
            var vfxComponent = gameObj.AddComponent<VFXComponent>();
            vfxComponent.vfxAsset = graph.vfxAsset;

            var lifeTimeIn = 28.0f;
            var vfxEventAttr = vfxComponent.CreateVFXEventAttribute();
            vfxEventAttr.SetFloat("lifeTime", lifeTimeIn);
            vfxComponent.Start(vfxEventAttr);

            int maxFrame = 512;
            while (vfxComponent.culled && --maxFrame > 0)
            {
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);
            yield return null; //wait for exactly one more update if visible

            var spawnerState = vfxComponent.GetSpawnerState(0);
            var lifeTimeOut = spawnerState.vfxEventAttribute.GetFloat("lifeTime");
            Assert.AreEqual(lifeTimeIn, lifeTimeOut);

            UnityEngine.Object.DestroyImmediate(gameObj);
        }

        [UnityTest]
        [Timeout(1000 * 10)]
        public IEnumerator CreateCustomSpawnerAndComponent()
        {
            EditorApplication.ExecuteMenuItem("Window/Game");
            var graph = ScriptableObject.CreateInstance<VFXGraph>();

            var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            var blockCustomSpawner = ScriptableObject.CreateInstance<VFXSpawnerCustomWrapper>();
            blockCustomSpawner.Init(typeof(VFXCustomSpawnerTest));

            spawnerContext.AddChild(blockCustomSpawner);
            graph.AddChild(spawnerContext);

            var valueTotalTime = 187.0f;

            blockCustomSpawner.GetInputSlot(0).value = valueTotalTime;

            graph.vfxAsset = new VFXAsset();
            graph.RecompileIfNeeded();
            graph.vfxAsset.bounds = new Bounds(Vector3.zero, Vector3.positiveInfinity);

            var gameObj = new GameObject("CreateAssetAndComponentSpawner");
            var vfxComponent = gameObj.AddComponent<VFXComponent>();
            vfxComponent.vfxAsset = graph.vfxAsset;

            int maxFrame = 512;
            while (vfxComponent.culled && --maxFrame > 0)
            {
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);
            yield return null; //wait for exactly one more update if visible

            var spawnerState = vfxComponent.GetSpawnerState(0);
            Assert.GreaterOrEqual(spawnerState.totalTime, valueTotalTime);
            Assert.AreEqual(VFXCustomSpawnerTest.s_LifeTime, spawnerState.vfxEventAttribute.GetFloat("lifeTime"));
            Assert.AreEqual(VFXCustomSpawnerTest.s_SpawnCount, spawnerState.spawnCount);
        }

        [UnityTest]
        [Timeout(1000 * 10)]
        public IEnumerator CreateCustomSpawnerLinkedWithSourceAttribute()
        {
            EditorApplication.ExecuteMenuItem("Window/Game");
            var graph = ScriptableObject.CreateInstance<VFXGraph>();

            var spawnerContext = ScriptableObject.CreateInstance<VFXBasicSpawner>();
            graph.AddChild(spawnerContext);

            var initContext = ScriptableObject.CreateInstance<VFXBasicInitialize>();
            graph.AddChild(initContext);

            spawnerContext.LinkTo(initContext);

            var initBlock = ScriptableObject.CreateInstance<VFXAllType>();
            initContext.AddChild(initBlock);

            var attributeVelocity = VFXLibrary.GetSourceAttributeParameters().FirstOrDefault(o => o.name == "velocity").CreateInstance();
            graph.AddChild(attributeVelocity);

            var targetSlot = initBlock.inputSlots.FirstOrDefault(o => o.property.type == attributeVelocity.outputSlots[0].property.type);
            attributeVelocity.outputSlots[0].Link(targetSlot);

            graph.vfxAsset = new VFXAsset();
            graph.RecompileIfNeeded();
            graph.vfxAsset.bounds = new Bounds(Vector3.zero, Vector3.positiveInfinity);

            var gameObj = new GameObject("CreateCustomSpawnerLinkedWithSourceAttribute");
            var vfxComponent = gameObj.AddComponent<VFXComponent>();
            vfxComponent.vfxAsset = graph.vfxAsset;

            int maxFrame = 512;
            while (vfxComponent.culled && --maxFrame > 0)
            {
                yield return null;
            }
            Assert.IsTrue(maxFrame > 0);
            yield return null; //wait for exactly one more update if visible

            var vfxEventAttribute = vfxComponent.CreateVFXEventAttribute();
            Assert.IsTrue(vfxEventAttribute.HasVector3("velocity"));
        }
    }
}
*/
