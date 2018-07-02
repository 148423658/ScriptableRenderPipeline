using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using NUnit.Framework;
using UnityEditor.SceneManagement;

[TestFixture]
public class ScriptableCullingTests
{
    SceneSetup[]    m_CurrentLoadedScenes;
    Camera          m_TestCamera;

    void Setup(string testName, string cameraName)
    {
        SetupTestScene(testName);
        SetupTestCamera(cameraName);
    }

    void SetupTestScene(string testSceneName)
    {
        string scenePath = string.Format("Assets/Scenes/{0}.unity", testSceneName);

        BackupSceneManagerSetup();
        EditorSceneManager.OpenScene(scenePath);
    }

    void SetupTestCamera(string cameraName)
    {
        string fullCameraName = string.Format(cameraName);

        var cameras = UnityEngine.Object.FindObjectsOfType(typeof(Camera)) as Camera[];
        m_TestCamera = Array.Find(cameras, (value) => value.name == fullCameraName);

        if (m_TestCamera == null)
        {
            // Throw?
            Assert.IsTrue(false, string.Format("Cannot find camera: {0}", cameraName));
        }
    }

    void TearDown()
    {
        RestoreSceneManagerSetup();
    }

    void BackupSceneManagerSetup()
    {
        m_CurrentLoadedScenes = EditorSceneManager.GetSceneManagerSetup();
    }

    void RestoreSceneManagerSetup()
    {
        if ((m_CurrentLoadedScenes == null) || (m_CurrentLoadedScenes.Length == 0))
        {
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        }
        else
        {
            EditorSceneManager.RestoreSceneManagerSetup(m_CurrentLoadedScenes);
        }

        m_TestCamera = null;
    }

    [Test(Description = "Renderers frustum culling test")]
    public void RenderersFrustumCulling()
    {
        Setup("FrustumCullingTest", "Camera_FrustumCullingTest");

        CullingParameters cullingParams = new CullingParameters();
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);

        RenderersCullingResult result = new RenderersCullingResult();
        Culling.CullRenderers(cullingParams, result);

        Assert.AreEqual(3, result.GetVisibleObjectCount());
        TearDown();
    }

    [Test(Description = "Light frustum culling test")]
    public void LightFrustumCulling()
    {
        Setup("FrustumCullingTest", "Camera_FrustumCullingTest");

        CullingParameters cullingParams = new CullingParameters();
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);

        LightCullingResult result = new LightCullingResult();
        Culling.CullLights(cullingParams, result);

        Assert.AreEqual(4, result.visibleLights.Length);

        Assert.IsTrue(result.visibleLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Directional Light"));
        Assert.IsTrue(result.visibleLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Point Light Inside"));
        Assert.IsTrue(result.visibleLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Point Light 2 Inside"));
        Assert.IsTrue(result.visibleLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Spot Light Inside"));

        Assert.AreEqual(1, result.visibleShadowCastingLights.Length);
        Assert.IsTrue(result.visibleShadowCastingLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Point Light Partial"));

        Assert.AreEqual(2, result.visibleOffscreenVertexLights.Length);
        Assert.IsTrue(result.visibleOffscreenVertexLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Point Light Vertex"));
        Assert.IsTrue(result.visibleOffscreenVertexLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Point Light 2 Vertex"));

        // TODO: The number here should actually be 1 but returns 3 because the off screen vertex light culling is wrong so we have false positives.
        Assert.AreEqual(1, result.visibleOffscreenShadowCastingVertexLights.Length);
        Assert.IsTrue(result.visibleOffscreenShadowCastingVertexLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Spot Light Vertex"));

        TearDown();
    }

    [Test(Description = "Reflection Probe frustum culling test")]
    public void ReflectionProbeFrustumCulling()
    {
        Setup("FrustumCullingTest", "Camera_FrustumCullingTest");

        CullingParameters cullingParams = new CullingParameters();
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);

        ReflectionProbeCullingResult result = new ReflectionProbeCullingResult();
        Culling.CullReflectionProbes(cullingParams, result);

        var visibleProbes = result.visibleReflectionProbes;

        Assert.AreEqual(2, result.visibleReflectionProbes.Length);
        Assert.IsTrue(result.visibleReflectionProbes.Any((visibleProbe) => visibleProbe.probe.gameObject.name == "ReflectionProbe Inside"));
        Assert.IsTrue(result.visibleReflectionProbes.Any((visibleProbe) => visibleProbe.probe.gameObject.name == "ReflectionProbe Partial"));

        TearDown();
    }

    [Test(Description = "Renderers occlusion culling test")]
    public void RenderersOcclusionCulling()
    {
        Setup("OcclusionCullingTest", "Camera_OcclusionCullingTest");

        CullingParameters cullingParams = new CullingParameters();
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);
        cullingParams.parameters.cullingFlags |= CullFlag.OcclusionCull;

        RenderersCullingResult result = new RenderersCullingResult();
        Culling.CullRenderers(cullingParams, result);

        Assert.AreEqual(3, result.GetVisibleObjectCount());
        TearDown();
    }

    [Test(Description = "Light occlusion culling test")]
    public void LightOcclusionCulling()
    {
        Setup("OcclusionCullingTest", "Camera_OcclusionCullingTest");

        CullingParameters cullingParams = new CullingParameters();
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);
        cullingParams.parameters.cullingFlags |= CullFlag.OcclusionCull;

        LightCullingResult result = new LightCullingResult();
        Culling.CullLights(cullingParams, result);

        Assert.AreEqual(3, result.visibleLights.Length);

        Assert.IsTrue(result.visibleLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Directional Light"));
        Assert.IsTrue(result.visibleLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Point Light"));
        Assert.IsTrue(result.visibleLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Spot Light"));

        Assert.AreEqual(0, result.visibleShadowCastingLights.Length);
        Assert.AreEqual(0, result.visibleOffscreenVertexLights.Length);
        Assert.AreEqual(0, result.visibleOffscreenShadowCastingVertexLights.Length);

        TearDown();
    }

    [Test(Description = "Reflection Probe occlusion culling test")]
    public void ReflectionProbeOcclusionCulling()
    {
        Setup("OcclusionCullingTest", "Camera_OcclusionCullingTest");

        CullingParameters cullingParams = new CullingParameters();
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);
        cullingParams.parameters.cullingFlags |= CullFlag.OcclusionCull;

        ReflectionProbeCullingResult result = new ReflectionProbeCullingResult();
        Culling.CullReflectionProbes(cullingParams, result);

        var visibleProbes = result.visibleReflectionProbes;

        Assert.AreEqual(1, result.visibleReflectionProbes.Length);
        Assert.IsTrue(result.visibleReflectionProbes.Any((visibleProbe) => visibleProbe.probe.gameObject.name == "Reflection Probe"));

        TearDown();
    }

    [Test(Description = "Reuse Reflection Probe Result")]
    public void ReuseReflectionProbeCullingResult()
    {
        SetupTestScene("ReuseCullingResultTest");

        CullingParameters cullingParams = new CullingParameters();
        ReflectionProbeCullingResult result = new ReflectionProbeCullingResult();

        SetupTestCamera("ReuseResultCamera 1");
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);

        Culling.CullReflectionProbes(cullingParams, result);

        Assert.AreEqual(2, result.visibleReflectionProbes.Length);
        Assert.IsTrue(result.visibleReflectionProbes.Any((visibleProbe) => visibleProbe.probe.gameObject.name == "Reflection Probe 1"));
        Assert.IsTrue(result.visibleReflectionProbes.Any((visibleProbe) => visibleProbe.probe.gameObject.name == "Reflection Probe 2"));

        SetupTestCamera("ReuseResultCamera 2");
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);

        Culling.CullReflectionProbes(cullingParams, result);

        Assert.AreEqual(1, result.visibleReflectionProbes.Length);
        Assert.IsTrue(result.visibleReflectionProbes.Any((visibleProbe) => visibleProbe.probe.gameObject.name == "Reflection Probe 2"));

        TearDown();
    }

    [Test(Description = "Reuse Lighting Result")]
    public void ReuseLightCullingResult()
    {
        SetupTestScene("ReuseCullingResultTest");

        CullingParameters cullingParams = new CullingParameters();
        LightCullingResult result = new LightCullingResult();

        SetupTestCamera("ReuseResultCamera 1");
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);

        Culling.CullLights(cullingParams, result);

        Assert.AreEqual(3, result.visibleLights.Length);
        Assert.IsTrue(result.visibleLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Point Light 1"));
        Assert.IsTrue(result.visibleLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Point Light 2"));
        Assert.IsTrue(result.visibleLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Directional Light"));

        SetupTestCamera("ReuseResultCamera 2");
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);

        Culling.CullLights(cullingParams, result);

        Assert.AreEqual(2, result.visibleLights.Length);
        Assert.IsTrue(result.visibleLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Point Light 2"));
        Assert.IsTrue(result.visibleLights.Any((visibleLight) => visibleLight.light.gameObject.name == "Directional Light"));

        TearDown();
    }

    [Test(Description = "Reuse Scene Culling Result")]
    public void ReuseSceneCullingResult()
    {
        SetupTestScene("ReuseCullingResultTest");

        CullingParameters cullingParams = new CullingParameters();
        RenderersCullingResult result = new RenderersCullingResult();

        SetupTestCamera("ReuseResultCamera 1");
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);
        Culling.CullRenderers (cullingParams, result);
        Assert.AreEqual(2, result.GetVisibleObjectCount());

        SetupTestCamera("ReuseResultCamera 2");
        ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);
        Culling.CullRenderers(cullingParams, result);
        Assert.AreEqual(1, result.GetVisibleObjectCount());

        TearDown();
    }

    //[Test(Description = "Per Object Light Culling")]
    //public void PerObjectLightCulling()
    //{
    //    Setup("ReuseCullingResultTest", "ReuseResultCamera 1");

    //    CullingParameters cullingParams = new CullingParameters();
    //    ScriptableCulling.FillCullingParameters(m_TestCamera, ref cullingParams);

    //    RenderersCullingResult result = new RenderersCullingResult();
    //    Culling.CullRenderers(cullingParams, result);

    //    LightCullingResult lightResult = new LightCullingResult();
    //    Culling.CullLights(cullingParams, lightResult);

    //    Culling.PrepareRendererScene(result, lightResult, null);

    //     // egzserghz
    //    Assert.AreEqual(3, 2);

    //    TearDown();
    //}
}
