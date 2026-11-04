Shader "Realistic Cat Demo/URP/Eye" {
Properties {  _BaseMap("UV0 iris / albedo atlas", 2D) = "white" {}
 _IrisMap("Iris texture (UV1)", 2D) = "white" {}
 _ScleraMap("Sclera", 2D) = "white" {}
 _ScleraColor("Sclera color", Color) = (1,1,1,1)
 _IrisColor("Iris color", Color) = (1,1,1,1)
 _IrisHue("Iris hue", Range(-180,180)) = 0
 _IrisSaturation("Iris saturation", Range(0,2)) = 1
 _BaseColor("Tint", Color) = (1,1,1,1)
 _LowerShadowStrength("Lower eyelid shadow strength", Range(0,1)) = 0.1
 _LowerShadowHeight("Lower eyelid height", Range(0,0.7)) = 0.22
 _CornerShadowStrength("Corners shadow strength", Range(0,1)) = 0.18
 _CornerShadowWidth("Corners shadow width", Range(0.01,0.4)) = 0.18
 _EyeForwardOS("Eye forward (local XYZ)", Vector) = (0,0,1,0)
 _EyeUpOS("Eye up (local XYZ)", Vector) = (0,1,0,0)
 _IrisDepth("Iris depth (parallax)", Range(0,0.08)) = 0.025
 _ShadowStrength("Eyelid shadow strength", Range(0,1)) = 0.35
 _ShadowSoftness("Eyelid shadow softness", Range(0.01,0.4)) = 0.15
 _ShadowHeight("Upper eyelid height (UV1)", Range(0.3,1)) = 0.72
 _Pupil("Pupil dilation", Range(0,1)) = 0.25
 _PupilShape("Pupil shape (slit to round)", Range(0,1)) = 0
 _PupilWidth("Pupil width", Range(0.25,1.5)) = 1
 _PupilHeight("Pupil height", Range(0.25,1.5)) = 1
 _IrisRotation("Iris / pupil rotation (degrees)", Range(-180,180)) = 0
 [Enum(UV0,0,UV1,1)] _IrisUVSet("Iris placement UV set", Float) = 1
 _IrisUVOffsetX("Iris UV offset X", Range(-0.5,0.5)) = 0
 _IrisUVOffsetY("Iris UV offset Y", Range(-0.5,0.5)) = 0
 _IrisRadius("Iris radius in normalized eye UV", Range(0.1,0.5)) = 0.4
 [HideInInspector] _EyeRect("Atlas eye rect xy=min zw=size", Vector) = (0,0,1,1)
 _CorneaSmoothness("Cornea Smoothness", Range(0.7,0.99)) = 0.94
 _CorneaBulge("Cornea bulge", Range(0,1)) = 0.22 }
SubShader {
Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
Pass {
Name "ForwardLit"
Tags { "LightMode"="UniversalForward" }
HLSLPROGRAM
#pragma target 3.5
#pragma vertex Vert
#pragma fragment Frag
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
#if UNITY_VERSION >= 600100
#pragma multi_compile _ _CLUSTER_LIGHT_LOOP
#else
#pragma multi_compile _ _FORWARD_PLUS
#endif
#pragma multi_compile_fragment _ _LIGHT_COOKIES
#pragma multi_compile_fragment _ _LIGHT_LAYERS
#pragma multi_compile_fog
#pragma multi_compile_instancing
#include "CatEyeUrpSurface.hlsl"
ENDHLSL
}
Pass {
Name "GBuffer"
Tags { "LightMode"="UniversalGBuffer" }
HLSLPROGRAM
#pragma target 4.5
#pragma exclude_renderers gles3 glcore
#pragma vertex Vert
#pragma fragment Frag
#pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
#pragma multi_compile _ SHADOWS_SHADOWMASK
#pragma multi_compile_fragment _ _RENDER_PASS_ENABLED
#pragma multi_compile_instancing
#pragma instancing_options renderinglayer
#if UNITY_VERSION >= 600100
#pragma multi_compile _ _CLUSTER_LIGHT_LOOP
#endif
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
#if UNITY_VERSION >= 600100
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferOutputFormat.hlsl"
#endif
#define RIPVERTICES_URP_GBUFFER
#include "CatEyeUrpSurface.hlsl"
ENDHLSL
}
UsePass "Universal Render Pipeline/Lit/ShadowCaster"
UsePass "Universal Render Pipeline/Lit/DepthOnly"
UsePass "Universal Render Pipeline/Lit/DepthNormals"
}
Fallback Off
CustomEditor "RealisticCatDemo.Editor.CatShaderGUI"
}
