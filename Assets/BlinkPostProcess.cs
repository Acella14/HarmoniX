using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BlinkPostProcess : ScriptableRendererFeature
{
    class BlinkPass : ScriptableRenderPass
    {
        private Material material;
        private RTHandle temporaryColorTexture;

        public BlinkPass(Material material)
        {
            this.material = material;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // Allocate the temporary RTHandle with the camera's descriptor
            RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref temporaryColorTexture, descriptor, name: "_TemporaryColorTexture");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (material == null)
            {
                Debug.LogWarning("Blink Material is missing.");
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get("Blink Effect");

            // Get the camera color target handle inside the render pass
            RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;

            // Blit from source to temporary render target using the blink material
            Blit(cmd, source, temporaryColorTexture, material);

            // Blit back from the temporary render target to the source
            Blit(cmd, temporaryColorTexture, source);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            // Release the temporary RTHandle
            if (temporaryColorTexture != null)
            {
                temporaryColorTexture.Release();
                temporaryColorTexture = null;
            }
        }
    }

    [SerializeField] private Material blinkMaterial;
    private BlinkPass blinkPass;

    public override void Create()
    {
        blinkPass = new BlinkPass(blinkMaterial)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (blinkMaterial == null)
        {
            Debug.LogWarning("Blink Material is not assigned.");
            return;
        }

        // Enqueue the pass without accessing the camera color target here
        renderer.EnqueuePass(blinkPass);
    }
}
