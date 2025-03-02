using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RadianceCascades2DGI : ScriptableRendererFeature
{
    [SerializeField] private LayerMask lightElementLayerMask;

    private Material screenUVMat;
    private Material jumpFloodMat;
    private Material distanceFieldMat;
    private Material giMat;
    private Material blitterMat;

    RC2DGIPass rc2dgiPass;
    public override void Create()
    {
        //Loading materials from Resources folder
        screenUVMat = (Material)Resources.Load("Hidden_RC2DGI_ScreenUV", typeof(Material));
        jumpFloodMat = (Material)Resources.Load("Hidden_RC2DGI_JumpFlood", typeof(Material));
        distanceFieldMat = (Material)Resources.Load("Hidden_RC2DGI_DistanceField", typeof(Material));
        giMat = (Material)Resources.Load("Hidden_RC2DGI_RadianceCascades2DGI", typeof(Material));
        blitterMat = (Material)Resources.Load("Hidden_RC2DGI_Blitter", typeof(Material));

        rc2dgiPass = new RC2DGIPass(lightElementLayerMask, screenUVMat, jumpFloodMat, distanceFieldMat, giMat, blitterMat);
        rc2dgiPass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(rc2dgiPass);
    }

    protected override void Dispose(bool disposing)
    {
        rc2dgiPass.Dispose();
    }

    class RC2DGIPass : ScriptableRenderPass
    {
        private LayerMask lightLayerMask;

        private Material screenUVMat;
        private Material jumpFloodMat;
        private Material distanceFieldMat;
        private Material giMat;
        private Material blitterMat;

        private int cascadeCount;
        private float renderScale;
        private float rayRange;

        private Vector2Int cascadeResolution;

        RTHandle colorRT;
        RTHandle distanceRT;

        RTHandle jumpFloodRT1;
        RTHandle jumpFloodRT2;

        RTHandle giRT1;
        RTHandle giRT2;

        bool jumpFlood1IsFinal = false;
        bool gi1IsFinal = false;

        private List<ShaderTagId> shaderTagsList = new List<ShaderTagId>();

        public RC2DGIPass(LayerMask lightLayerMask, Material screenUVMat, Material jumpFloodMat, Material distanceFieldMat, Material giMat, Material blitterMat)
        {
            this.lightLayerMask = lightLayerMask;
            this.screenUVMat = screenUVMat;
            this.jumpFloodMat = jumpFloodMat;
            this.distanceFieldMat = distanceFieldMat;
            this.giMat = giMat;
            this.blitterMat = blitterMat;

            shaderTagsList.Add(new ShaderTagId("SRPDefaultUnlit"));
            shaderTagsList.Add(new ShaderTagId("UniversalForward"));
            shaderTagsList.Add(new ShaderTagId("UniversalForwardOnly"));

            SetupParameters();
        }

        public void SetupParameters() {
            //Getting values from the Volume
            var volume = VolumeManager.instance.stack.GetComponent<RadianceCascades2DGIVolume>();

            cascadeCount = volume.cascadeCount.overrideState ? volume.cascadeCount.value : 6;
            renderScale = volume.renderScale.overrideState ? volume.renderScale.value : 1;
            rayRange = volume.rayRange.overrideState ? volume.rayRange.value : 1;

            int cascadeWidth = Mathf.CeilToInt((Screen.width * renderScale) / Mathf.Pow(2, cascadeCount)) * (int)Mathf.Pow(2, cascadeCount);
            int cascadeHeight = Mathf.CeilToInt((Screen.height * renderScale) / Mathf.Pow(2, cascadeCount)) * (int)Mathf.Pow(2, cascadeCount);
            cascadeResolution = new Vector2Int(cascadeWidth, cascadeHeight);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            Vector2Int textureSize = new Vector2Int(Screen.width, Screen.height);

            RenderingUtils.ReAllocateIfNeeded(ref colorRT, new RenderTextureDescriptor(textureSize.x, textureSize.y, RenderTextureFormat.ARGBFloat, 0), FilterMode.Point);
            RenderingUtils.ReAllocateIfNeeded(ref distanceRT, new RenderTextureDescriptor(textureSize.x, textureSize.y, RenderTextureFormat.RHalf, 0), FilterMode.Point);

            RenderingUtils.ReAllocateIfNeeded(ref jumpFloodRT1, new RenderTextureDescriptor(textureSize.x, textureSize.y, RenderTextureFormat.RGHalf, 0), FilterMode.Point);
            RenderingUtils.ReAllocateIfNeeded(ref jumpFloodRT2, new RenderTextureDescriptor(textureSize.x, textureSize.y, RenderTextureFormat.RGHalf, 0), FilterMode.Point);

            RenderingUtils.ReAllocateIfNeeded(ref giRT1, new RenderTextureDescriptor(cascadeResolution.x, cascadeResolution.y, RenderTextureFormat.ARGBHalf, 0), FilterMode.Bilinear);
            RenderingUtils.ReAllocateIfNeeded(ref giRT2, new RenderTextureDescriptor(cascadeResolution.x, cascadeResolution.y, RenderTextureFormat.ARGBHalf, 0), FilterMode.Bilinear);

            ConfigureTarget(colorRT);
            ConfigureClear(ClearFlag.All, new Color(0, 0, 0, 0));
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var volume = VolumeManager.instance.stack.GetComponent<RadianceCascades2DGIVolume>();

            if (!volume.isActive.value) return;

            SetupParameters();

            CommandBuffer cmd = CommandBufferPool.Get();

            Vector2 screen = new Vector2(Screen.width, Screen.height);

            using (new ProfilingScope(cmd, new ProfilingSampler("Distance Field Texture")))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                cmd.SetGlobalVector("_Aspect", screen / Mathf.Max(screen.x, screen.y));

                //Drawing only objects with the specific layerMask
                DrawingSettings drawingSettings = CreateDrawingSettings(shaderTagsList, ref renderingData, SortingCriteria.CommonTransparent);
                FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all, lightLayerMask);
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);

                //Copying the color texture to another one using Screen UV shader to start the JumpFlood Algorithm
                cmd.Blit(colorRT, jumpFloodRT1, screenUVMat);

                jumpFlood1IsFinal = true;//This variable keeps track of which texture holds the final data
                //the jump flood shader should be applied for a number of times and each iteration depends on the output of the previous one
                //So we apply the shader to the textureB with the textureA as input, next we apply the shader to the textureA with the textureB as input
                //And we keep doing this until one of the textures have the final result we want

                //Start JumpFlood Algorithm
                int max = Mathf.Max(Screen.width, Screen.height);
                int steps = Mathf.CeilToInt(Mathf.Log(max));
                float stepSize = 1;

                for (var n = 0; n < steps; n++)
                {
                    stepSize *= 0.5f;
                    cmd.SetGlobalFloat("_StepSize", stepSize);
                    //you might find setting this value as global is unecessary but for some reason when using for loops you can't set the value directly to the material with Material.SetFloat
                    //The value don't get passed correctly. Why, I have no idea

                    BlitJumpFloodRT(cmd, jumpFloodMat);//Chooses the source and destination texture based on "jumpFlood1IsFinal" boolean
                }

                //We check which texture holds the final result and we apply the DistanceField shader to it
                if (jumpFlood1IsFinal)
                {
                    cmd.Blit(jumpFloodRT1, distanceRT, distanceFieldMat);
                }
                else
                {
                    cmd.Blit(jumpFloodRT2, distanceRT, distanceFieldMat);
                }
            }

            using (new ProfilingScope(cmd, new ProfilingSampler("2D Global Illumination")))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                //Passing values to the GI Shader
                giMat.SetTexture("_ColorTex", colorRT);
                giMat.SetTexture("_DistanceTex", distanceRT);
                giMat.SetFloat("_RayRange", (screen / Mathf.Min(screen.x, screen.y)).magnitude * rayRange);
                giMat.SetInt("_CascadeCount", cascadeCount);
                giMat.SetFloat("_SkyRadiance", volume.skyRadiance.value ? 1 : 0);
                giMat.SetColor("_SkyColor", volume.skyColor.value);
                giMat.SetColor("_SunColor", volume.sunColor.value);
                giMat.SetFloat("_SunAngle", volume.sunAngle.value);
                giMat.SetVector("_CascadeResolution", (Vector2)cascadeResolution);

                gi1IsFinal = false;//Same as "jumpFlood1IsFinal"
                for (int i = cascadeCount - 1; i >= 0; i--)
                {
                    cmd.SetGlobalInt("_CascadeLevel", i);//Again setting it as global cause I can't pass it directly to the material from a for loop
                    BlitGiRT(cmd, giMat);//the shader handles the computation of the cascades and the merging at the same time
                }

                cmd.Blit(renderingData.cameraData.renderer.cameraColorTargetHandle, colorRT);

                if (gi1IsFinal)
                {
                    blitterMat.SetTexture("_GITex", giRT1);
                }
                else
                {
                    blitterMat.SetTexture("_GITex", giRT2);
                }

                cmd.Blit(colorRT, renderingData.cameraData.renderer.cameraColorTargetHandle, blitterMat);//Finaly blending the final result to the camera texture
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        private void BlitJumpFloodRT(CommandBuffer cmd, Material material) {
            if (jumpFlood1IsFinal)
            {
                cmd.Blit(jumpFloodRT1, jumpFloodRT2, material);
            }
            else {
                cmd.Blit(jumpFloodRT2, jumpFloodRT1, material);
            }

            jumpFlood1IsFinal = !jumpFlood1IsFinal;
        }

        private void BlitGiRT(CommandBuffer cmd, Material material)
        {
            if (gi1IsFinal)
            {
                cmd.Blit(giRT1, giRT2, material);
            }
            else
            {
                cmd.Blit(giRT2, giRT1, material);
            }

            gi1IsFinal = !gi1IsFinal;
        }

        public void Dispose() {
            colorRT?.Release();
            distanceRT?.Release();
            jumpFloodRT1?.Release();
            jumpFloodRT2?.Release();
            giRT1?.Release();
            giRT2?.Release();
        }
    }

}
