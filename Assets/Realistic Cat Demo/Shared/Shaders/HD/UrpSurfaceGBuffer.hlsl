#ifndef RIPVERTICES_URP_SURFACE_GBUFFER_INCLUDED
#define RIPVERTICES_URP_SURFACE_GBUFFER_INCLUDED

#if UNITY_VERSION >= 600100
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferOutput.hlsl"
#define RipVerticesGBufferOutput GBufferFragOutput
#define RipVerticesPackGBuffer PackGBuffersBRDFData
#else
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"
#define RipVerticesGBufferOutput FragmentOutput
#define RipVerticesPackGBuffer BRDFDataToGbuffer
#endif

RipVerticesGBufferOutput WriteSurfaceGBuffer(InputData inputData, SurfaceData surface)
{
    BRDFData brdf;
    InitializeBRDFData(surface, brdf);
    half3 indirect = GlobalIllumination(brdf, inputData.bakedGI, surface.occlusion,
        inputData.positionWS, inputData.normalWS, inputData.viewDirectionWS);
    return RipVerticesPackGBuffer(brdf, inputData, surface.smoothness,
        indirect + surface.emission, surface.occlusion);
}

#endif
