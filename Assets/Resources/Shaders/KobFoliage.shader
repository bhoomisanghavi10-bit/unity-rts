// Alpha-clipped foliage cards (grass tufts, reeds) for the instanced terrain
// clutter. Adds what URP Lit can't: wind sway (weighted by height up the
// card, phase varying per instance so a field doesn't move in lockstep), and
// a smooth distance shrink so far instances scale away instead of popping
// when their patch is culled. Wrap lighting + sun shadows + ambient (SH) so
// it sits in the scene lighting; darkened toward the base as cheap AO.
Shader "KingdomsOfBharat/Foliage"
{
    Properties
    {
        _BaseMap ("Card Texture", 2D) = "white" {}
        _BaseColor ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        _SwayStrength ("Sway Strength (tip, world units at scale 1)", Float) = 0.12
        _SwaySpeed ("Sway Speed", Float) = 1.6
        _SwayScale ("Sway Spatial Scale", Float) = 0.35
        _BaseDarken ("Base Darkening", Range(0,1)) = 0.45
        _FadeStart ("Distance Shrink Start", Float) = 45
        _FadeEnd ("Distance Shrink End", Float) = 66
    }

    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _Cutoff;
                float _SwayStrength;
                float _SwaySpeed;
                float _SwayScale;
                float _BaseDarken;
                float _FadeStart;
                float _FadeEnd;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);

                float4x4 m = GetObjectToWorldMatrix();
                float3 originWS = float3(m._m03, m._m13, m._m23);
                float instScale = length(float3(m._m00, m._m10, m._m20));
                float3 posWS = TransformObjectToWorld(input.positionOS);

                // Wind: sway grows with height up the card (uv.y: 0 base,
                // 1 tip); per-instance phase from world position.
                float h = saturate(input.uv.y);
                float phase = dot(originWS.xz, float2(0.71, 1.13)) * _SwayScale + _Time.y * _SwaySpeed;
                float gust = sin(phase) + 0.5 * sin(phase * 2.3 + originWS.x * 0.4);
                posWS.xz += float2(gust, gust * 0.6) * _SwayStrength * instScale * h * h;

                // Smooth distance shrink toward the instance origin.
                float dist = distance(originWS, _WorldSpaceCameraPos);
                float fade = saturate((_FadeEnd - dist) / max(_FadeEnd - _FadeStart, 0.01));
                posWS = originWS + (posWS - originWS) * fade;

                o.positionWS = posWS;
                o.positionCS = TransformWorldToHClip(posWS);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                // Mostly up-facing normals so cards shade like the ground under them.
                o.normalWS = normalize(lerp(float3(0, 1, 0), TransformObjectToWorldNormal(input.normalOS), 0.25));
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                clip(tex.a - _Cutoff);

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half ndl = saturate(dot(input.normalWS, mainLight.direction)) * 0.55 + 0.45; // wrap: foliage is translucent
                half3 light = mainLight.color * ndl * mainLight.shadowAttenuation + SampleSH(input.normalWS);
                half ao = lerp(1.0 - _BaseDarken, 1.0, saturate(input.uv.y));

                half3 col = tex.rgb * light * ao;
                col = MixFog(col, input.fogFactor);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
