Shader "Realistic Cat Demo/URP/Skin" {
Properties { 
 _BaseMap("Albedo", 2D) = "white" {}
 _BaseColor("Tint", Color) = (1,1,1,1)
 _BumpMap("Normal", 2D) = "bump" {}
 _BumpScale("Normal Strength", Range(0,2)) = 0.7
 _RoughnessMap("Roughness (linear R)", 2D) = "white" {}
 _RoughnessScale("Skin roughness", Range(0.5,1.8)) = 1.15
 _MicroMap("Pore height (grayscale)", 2D) = "gray" {}
 _PoreSizeMM("Pore size (mm)", Range(4,100)) = 18
 _PoreNormalStrength("Pore normal strength", Range(0,2)) = 0.75
 _PoreRoughnessStrength("Pore roughness strength", Range(0,1)) = 0.55
 _TissueMap("R Skin G Thickness B Vessels A Transmission", 2D) = "white" {}
 _ThicknessMM("Maximum thickness (mm)", Range(0.1,30)) = 12
 _Transmission("Transmission", Range(0,5)) = 1.6
 _Subsurface("Local Diffusion", Range(0,1)) = 0.35
 _VesselStrength("Vessel Absorption", Range(0,3)) = 1 }
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
#include "CatSkinUrpSurface.hlsl"
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
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
#pragma multi_compile_fragment _ _LIGHT_LAYERS
#pragma multi_compile_fragment _ _LIGHT_COOKIES
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
#include "CatSkinUrpSurface.hlsl"
ENDHLSL
}
UsePass "Universal Render Pipeline/Lit/ShadowCaster"
UsePass "Universal Render Pipeline/Lit/DepthOnly"
UsePass "Universal Render Pipeline/Lit/DepthNormals"
}
Fallback Off
CustomEditor "RealisticCatDemo.Editor.CatShaderGUI"
}
