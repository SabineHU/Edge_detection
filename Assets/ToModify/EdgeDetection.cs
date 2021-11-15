using UnityEngine;

public class EdgeDetection : MonoBehaviour
{
    // Different filtering modes
    public enum EdgeDetectionMode
    {
        Depth = 0,
        Color = 1,
        Normal = 2,
        Custom = 3
    }

    // Boolean that triggers or disables the effect
    public bool enableFilter = false;

    // Detection mode for the edges
    public EdgeDetectionMode detectionMode = EdgeDetectionMode.Color;

    // Do not modify (compute shader used for the modification)
    public ComputeShader edgeDetection = null;

    void OnPreRender()
    {
        Camera cam = GetComponent<Camera>();
        if (enableFilter && edgeDetection != null)
            cam.depthTextureMode = DepthTextureMode.DepthNormals;
        else
            cam.depthTextureMode = DepthTextureMode.Depth;
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (enableFilter && edgeDetection != null)
        {
            // Request the render texture we will be needing
            RenderTextureDescriptor rtD = new RenderTextureDescriptor();
            rtD.width = src.width;
            rtD.height = src.height;
            rtD.volumeDepth = 1;
            rtD.msaaSamples = 1;
            rtD.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
            rtD.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat;
            rtD.enableRandomWrite = true;
            RenderTexture edgeBuffer = RenderTexture.GetTemporary(rtD);

            // Run the edge detection kernel
            int tileX = (src.width + 7) / 8;
            int tileY = (src.height + 7) / 8;
            int kernelID = (int)detectionMode;

            int blurID = 4;
            RenderTexture blurBuffer = RenderTexture.GetTemporary(rtD);
            edgeDetection.SetTexture(blurID, "_CameraColorBuffer", src);
            edgeDetection.SetTexture(blurID, "_BlurBufferRW", blurBuffer);
            edgeDetection.Dispatch(blurID, tileX, tileY, 1);

            edgeDetection.SetTexture(kernelID, "_CameraColorBuffer", src);
            edgeDetection.SetTexture(kernelID, "_BlurColorBuffer", blurBuffer);
            edgeDetection.SetTexture(kernelID, "_EdgesBufferRW", edgeBuffer);
            edgeDetection.Dispatch(kernelID, tileX, tileY, 1);

            // Copy into the Screen
            Graphics.Blit(edgeBuffer, dest);
        }
        else
        {
            // Copy into the Screen
            Graphics.Blit(src, dest);
        }
    }
}
