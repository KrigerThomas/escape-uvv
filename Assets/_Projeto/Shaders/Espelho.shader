// Espelho: mostra a imagem que a câmera do reflexo (script Espelho.cs) desenhou.
Shader "EscapeUVV/Espelho"
{
    Properties
    {
        _ReflexoTex ("Reflexo (preenchido pelo script)", 2D) = "gray" {}
        _Cor ("Cor do vidro", Color) = (0.9, 0.93, 0.95, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_ReflexoTex); SAMPLER(sampler_ReflexoTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _Cor;
                float4 _ReflexoTex_ST;
            CBUFFER_END

            struct Entrada { float4 positionOS : POSITION; };
            struct Saida { float4 positionCS : SV_POSITION; float4 posTela : TEXCOORD0; };

            Saida vert (Entrada i)
            {
                Saida o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.posTela = ComputeScreenPos(o.positionCS);
                return o;
            }

            half4 frag (Saida i) : SV_Target
            {
                float2 uv = i.posTela.xy / i.posTela.w;
                uv.x = 1.0 - uv.x; // a câmera do reflexo vê a cena invertida na horizontal
                half4 c = SAMPLE_TEXTURE2D(_ReflexoTex, sampler_ReflexoTex, uv);
                return half4(c.rgb * _Cor.rgb, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Entrada { float4 positionOS : POSITION; };
            float4 vert (Entrada i) : SV_POSITION { return TransformObjectToHClip(i.positionOS.xyz); }
            half frag () : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Entrada { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Saida { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; };
            Saida vert (Entrada i) { Saida o; o.positionCS = TransformObjectToHClip(i.positionOS.xyz); o.normalWS = TransformObjectToWorldNormal(i.normalOS); return o; }
            half4 frag (Saida i) : SV_Target { return half4(NormalizeNormalPerPixel(i.normalWS), 0); }
            ENDHLSL
        }
    }
}
