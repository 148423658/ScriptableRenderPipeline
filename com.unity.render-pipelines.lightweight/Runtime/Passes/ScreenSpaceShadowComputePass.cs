using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.VoxelizedShadows;

namespace UnityEngine.Rendering.LWRP
{
    internal class ScreenSpaceShadowComputePass : ScriptableRenderPass
    {
        private static class VxShadowMapConstantBuffer
        {
            public static int _InvViewProjMatrixID;
            public static int _ScreenSizeID;
            public static int _VoxelZBiasID;
            public static int _VoxelUpBiasID;

            public static int _VxShadowMapsBufferID;
            public static int _CameraDepthTextureID;
            public static int _ScreenSpaceShadowOutputID;
        }

        static readonly int TileSize = 8;
        static readonly int TileAdditive = TileSize - 1;

        ComputeShader m_ScreenSpaceShadowsComputeShader;
        RenderTargetHandle m_ScreenSpaceShadowmapTexture;
        RenderTextureDescriptor m_RenderTextureDescriptor;
        RenderTextureFormat m_ColorFormat;
        const string k_CollectShadowsTag = "Collect Shadows";

        public ScreenSpaceShadowComputePass(RenderPassEvent evt, ComputeShader computeShader)
        {
            VxShadowMapConstantBuffer._InvViewProjMatrixID = Shader.PropertyToID("_InvViewProjMatrix");
            VxShadowMapConstantBuffer._ScreenSizeID = Shader.PropertyToID("_ScreenSize");
            VxShadowMapConstantBuffer._VoxelZBiasID = Shader.PropertyToID("_VoxelZBias");
            VxShadowMapConstantBuffer._VoxelUpBiasID = Shader.PropertyToID("_VoxelUpBias");

            VxShadowMapConstantBuffer._VxShadowMapsBufferID = Shader.PropertyToID("_VxShadowMapsBuffer");
            VxShadowMapConstantBuffer._CameraDepthTextureID = Shader.PropertyToID("_CameraDepthTexture");
            VxShadowMapConstantBuffer._ScreenSpaceShadowOutputID = Shader.PropertyToID("_ScreenSpaceShadowOutput");

            bool R8_UNorm = SystemInfo.IsFormatSupported(GraphicsFormat.R8_UNorm, FormatUsage.LoadStore);
            bool R8_SNorm = SystemInfo.IsFormatSupported(GraphicsFormat.R8_SNorm, FormatUsage.LoadStore);
            bool R8_UInt  = SystemInfo.IsFormatSupported(GraphicsFormat.R8_UInt,  FormatUsage.LoadStore);
            bool R8_SInt  = SystemInfo.IsFormatSupported(GraphicsFormat.R8_SInt,  FormatUsage.LoadStore);
            
            bool R8 = R8_UNorm || R8_SNorm || R8_UInt || R8_SInt;
            
            m_ColorFormat = R8 ? RenderTextureFormat.R8 : RenderTextureFormat.RFloat;
            m_ScreenSpaceShadowsComputeShader = computeShader;
            m_ScreenSpaceShadowmapTexture.Init("_ScreenSpaceShadowmapTexture");
            renderPassEvent = evt;
        }

        private MainLightShadowCasterPass mainLightShadowCasterPass;
        private bool mainLightDynamicShadows = false;
        DirectionalVxShadowMap dirVxShadowMap;

        public void Setup(
            RenderTextureDescriptor baseDescriptor,
            MainLightShadowCasterPass mainLightShadowCasterPass,
            bool mainLightDynamicShadows)
        {
            m_RenderTextureDescriptor = baseDescriptor;
            m_RenderTextureDescriptor.autoGenerateMips = false;
            m_RenderTextureDescriptor.useMipMap = false;
            m_RenderTextureDescriptor.sRGB = false;
            m_RenderTextureDescriptor.enableRandomWrite = true;
            m_RenderTextureDescriptor.depthBufferBits = 0;
            m_RenderTextureDescriptor.colorFormat = m_ColorFormat;

            this.mainLightShadowCasterPass = mainLightShadowCasterPass;
            this.mainLightDynamicShadows = mainLightDynamicShadows;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cmd.GetTemporaryRT(m_ScreenSpaceShadowmapTexture.id, m_RenderTextureDescriptor, FilterMode.Point);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            int kernel = GetComputeShaderKernel(ref renderingData.shadowData);
            if (kernel == -1)
                return;

            var shadowLightIndex = renderingData.lightData.mainLightIndex;
            var shadowLight = renderingData.lightData.visibleLights[shadowLightIndex];

            var light = shadowLight.light;
            dirVxShadowMap = light.GetComponent<DirectionalVxShadowMap>();

            CommandBuffer cmd = CommandBufferPool.Get(k_CollectShadowsTag);

            if (mainLightDynamicShadows)
            {
                mainLightShadowCasterPass.SetMainLightShadowReceiverConstantsOnComputeShader(
                    cmd, ref renderingData.shadowData, shadowLight, m_ScreenSpaceShadowsComputeShader);
            }

            SetupVxShadowReceiverConstants(
                cmd, kernel, ref m_ScreenSpaceShadowsComputeShader, ref renderingData.cameraData.camera, ref shadowLight);

            int x = (renderingData.cameraData.camera.pixelWidth + TileAdditive) / TileSize;
            int y = (renderingData.cameraData.camera.pixelHeight + TileAdditive) / TileSize;

            cmd.DispatchCompute(m_ScreenSpaceShadowsComputeShader, kernel, x, y, 1);

            // even if the main light doesn't have dynamic shadows,
            // cascades keyword is needed for screen space shadow map texture in opaque rendering pass.
            if (mainLightDynamicShadows == false)
            {
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadows, true);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadowCascades, true);
            }
            else
            {
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadowCascades, true);
            }

            if (renderingData.cameraData.isStereoEnabled)
            {
                Camera camera = renderingData.cameraData.camera;
                context.StartMultiEye(camera);
                context.ExecuteCommandBuffer(cmd);
                context.StopMultiEye(camera);
            }
            else
            {
                context.ExecuteCommandBuffer(cmd);
            }
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            cmd.ReleaseTemporaryRT(m_ScreenSpaceShadowmapTexture.id);
        }

        private int GetComputeShaderKernel(ref ShadowData shadowData)
        {
            int kernel = -1;

            if (m_ScreenSpaceShadowsComputeShader != null)
            {
                string blendModeName;
                if (mainLightDynamicShadows)
                    blendModeName = "BlendDynamicShadows";
                else
                    blendModeName = "NoBlend";

                string filteringName = "Nearest";
                switch (shadowData.mainLightVxShadowQuality)
                {
                    case 1: filteringName = "Bilinear";  break;
                    case 2: filteringName = "Trilinear"; break;
                }

                string kernelName = blendModeName + filteringName;

                kernel = m_ScreenSpaceShadowsComputeShader.FindKernel(kernelName);
            }

            return kernel;
        }

        void SetupVxShadowReceiverConstants(CommandBuffer cmd, int kernel, ref ComputeShader computeShader, ref Camera camera, ref VisibleLight shadowLight)
        {
            var light = shadowLight.light;

            float screenSizeX = (float)camera.pixelWidth;
            float screenSizeY = (float)camera.pixelHeight;
            float invScreenSizeX = 1.0f / screenSizeX;
            float invScreenSizeY = 1.0f / screenSizeY;

            var gpuView = camera.worldToCameraMatrix;
            var gpuProj = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);

            var viewMatrix = gpuView;
            var projMatrix = gpuProj;
            var viewProjMatrix = projMatrix * viewMatrix;

            var vxShadowMapsBuffer = VxShadowMapsManager.instance.VxShadowMapsBuffer;

            int voxelZBias = 2;
            float voxelUpBias = 1 * (dirVxShadowMap.volumeScale / dirVxShadowMap.voxelResolutionInt);

            cmd.SetComputeMatrixParam(computeShader, VxShadowMapConstantBuffer._InvViewProjMatrixID, viewProjMatrix.inverse);
            cmd.SetComputeVectorParam(computeShader, VxShadowMapConstantBuffer._ScreenSizeID, new Vector4(screenSizeX, screenSizeY, invScreenSizeX, invScreenSizeY));
            cmd.SetComputeIntParam(computeShader, VxShadowMapConstantBuffer._VoxelZBiasID, voxelZBias);
            cmd.SetComputeFloatParam(computeShader, VxShadowMapConstantBuffer._VoxelUpBiasID, voxelUpBias);

            cmd.SetComputeBufferParam(computeShader, kernel, VxShadowMapConstantBuffer._VxShadowMapsBufferID, vxShadowMapsBuffer);
            cmd.SetComputeTextureParam(computeShader, kernel, VxShadowMapConstantBuffer._ScreenSpaceShadowOutputID, m_ScreenSpaceShadowmapTexture.Identifier());
        }
    }
}
