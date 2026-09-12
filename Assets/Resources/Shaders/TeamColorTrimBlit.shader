// Wave 5 item 29 follow-up: a pure texture-compositing blit shader used
// offscreen (via Graphics.Blit) to bake a team-colored variant of a
// building's albedo texture, using its existing metallic map as the mask
// (bright = trim/gilt, gets recolored; dark = plain stone, unchanged). See
// Core/TeamColorBuildingTint.cs. Never assigned directly to a scene
// renderer's material - this never touches lighting/PBR at all.
Shader "Hidden/KingdomsOfBharat/TeamColorTrimBlit"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _MaskTex ("Metallic Mask", 2D) = "black" {}
        _TeamColor ("Team Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);
            float4 _TeamColor;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, IN.uv).r;
                half3 blended = lerp(albedo.rgb, _TeamColor.rgb, mask);
                return half4(blended, albedo.a);
            }
            ENDHLSL
        }
    }
}
