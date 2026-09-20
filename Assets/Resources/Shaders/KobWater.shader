// Shallow-water surface for the Coastal map: colour and opacity fade with
// the depth of water above the terrain bed (needs the URP depth texture),
// two scrolling normal-map layers give ripples + a sun glint, and a foam
// line forms where the water meets the ground. Unlit-style on purpose
// (no shadow/GI cost); lit only by the main directional light's specular.
Shader "KingdomsOfBharat/Water"
{
    Properties
    {
        _ShallowColor ("Shallow Color", Color) = (0.32, 0.62, 0.58, 1)
        _DeepColor ("Deep Color", Color) = (0.08, 0.30, 0.42, 1)
        _ShallowAlpha ("Shallow Alpha", Range(0,1)) = 0.35
        _DeepAlpha ("Deep Alpha", Range(0,1)) = 0.92
        _DepthMax ("Depth For Full Deep Color", Float) = 3.0
        _EdgeFade ("Shore Edge Fade", Float) = 0.25
        _NormalMap ("Ripple Normal Map", 2D) = "bump" {}
        _NormalTiling ("Normal Tiling (world units per tile)", Float) = 20
        _NormalStrength ("Normal Strength", Range(0,2)) = 0.5
        _ScrollA ("Scroll A (xy)", Vector) = (0.020, 0.012, 0, 0)
        _ScrollB ("Scroll B (xy)", Vector) = (-0.014, 0.018, 0, 0)
        _Shininess ("Glint Shininess", Float) = 180
        _GlintStrength ("Glint Strength", Float) = 0.7
        _SkyColor ("Fresnel Sky Tint", Color) = (0.62, 0.78, 0.86, 1)
        _FoamColor ("Foam Color", Color) = (0.95, 0.98, 1, 1)
        _FoamWidth ("Foam Width", Float) = 0.35
        _WaveHeight ("Vertex Wave Height", Float) = 0.04
        _RefractStrength ("Refraction Strength", Float) = 0.04
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float _ShallowAlpha;
                float _DeepAlpha;
                float _DepthMax;
                float _EdgeFade;
                float _NormalTiling;
                float _NormalStrength;
                float4 _ScrollA;
                float4 _ScrollB;
                float _Shininess;
                float _GlintStrength;
                float4 _SkyColor;
                float4 _FoamColor;
                float _FoamWidth;
                float _WaveHeight;
                float _RefractStrength;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float fogFactor : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                // Gentle vertex swell (mesh is a 2.5-unit grid, see
                // ProceduralTerrain.BuildWaterPlane) - mostly reads as the
                // foam line breathing in and out along the shore.
                o.positionWS.y += (sin(o.positionWS.x * 0.55 + _Time.y * 1.1) + sin(o.positionWS.z * 0.7 - _Time.y * 0.9)) * 0.5 * _WaveHeight;
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 screenUV = input.positionCS.xy / _ScaledScreenParams.xy;
                float rawDepth = SampleSceneDepth(screenUV);
                float sceneEye = LinearEyeDepth(rawDepth, _ZBufferParams);
                float surfaceEye = input.positionCS.w;
                float depth = max(sceneEye - surfaceEye, 0);

                // Ripple normal from two counter-scrolling world-space layers.
                float2 uv = input.positionWS.xz / _NormalTiling;
                float3 nA = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv + _Time.y * _ScrollA.xy), _NormalStrength);
                float3 nB = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv * 1.7 + _Time.y * _ScrollB.xy), _NormalStrength);
                float3 nTS = normalize(float3(nA.xy + nB.xy, nA.z * nB.z));
                float3 normalWS = normalize(float3(nTS.x, nTS.z, nTS.y));

                float t = saturate(depth / _DepthMax);
                float3 col = lerp(_ShallowColor.rgb, _DeepColor.rgb, t);
                float alpha = lerp(_ShallowAlpha, _DeepAlpha, t);

                float3 viewDir = normalize(GetCameraPositionWS() - input.positionWS);
                float fres = pow(1.0 - saturate(dot(lerp(float3(0,1,0), normalWS, 0.25), viewDir)), 4.0);
                col = lerp(col, _SkyColor.rgb, fres * 0.6);
                alpha = saturate(alpha + fres * 0.3);

                Light mainLight = GetMainLight();
                float3 h = normalize(mainLight.direction + viewDir);
                float glint = pow(saturate(dot(normalWS, h)), _Shininess) * _GlintStrength;
                col += mainLight.color * glint;
                alpha = saturate(alpha + glint);

                // Foam hugging the waterline, broken up by the ripple map.
                float ripple = nA.x * 0.5 + 0.5;
                float foamMask = 1.0 - saturate(depth / max(_FoamWidth * (0.6 + ripple * 0.8), 0.001));
                foamMask = smoothstep(0.15, 1.0, foamMask);
                col = lerp(col, _FoamColor.rgb, foamMask);
                alpha = saturate(max(alpha, foamMask * 0.9));

                // Soft fade to nothing where water meets ground.
                alpha *= saturate(depth / max(_EdgeFade, 0.001)) * 0.85 + 0.15 * foamMask;

                // Refraction: bend where we look up the scene colour by the
                // ripple normal, but fall back to the undistorted sample if
                // the bent one lands on something in front of the water
                // (a boat hull), which would smear it into the surface.
                float2 refractUV = screenUV + nTS.xy * _RefractStrength * saturate(depth);
                float refractEye = LinearEyeDepth(SampleSceneDepth(refractUV), _ZBufferParams);
                refractUV = refractEye < surfaceEye ? screenUV : refractUV;
                float3 bed = SampleSceneColor(refractUV);

                float3 result = lerp(bed, col, alpha);
                result = MixFog(result, input.fogFactor);
                return half4(result, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
