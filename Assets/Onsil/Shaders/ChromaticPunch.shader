Shader "Onsil/ChromaticPunch"
{
    Properties
    {
        _Amount ("Amount", Range(0,1)) = 0
        _Strength ("Aberration strength (px at 1080p)", Range(0,64)) = 18
        _Falloff ("Centre falloff", Range(0,4)) = 1.6
        _Tint ("Edge tint", Color) = (1,0.9,0.5,1)
        _TintAmount ("Edge tint amount", Range(0,1)) = 0.25
        _Vignette ("Vignette", Range(0,2)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "ChromaticPunch"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            TEXTURE2D_X(_BlitTexture);
            float4 _BlitScaleBias;
            float4 _BlitTexture_TexelSize;

            float _Amount, _Strength, _Falloff, _TintAmount, _Vignette;
            float4 _Tint;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord   : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                float2 uv    = GetFullScreenTriangleTexCoord(input.vertexID);
                o.texcoord   = uv * _BlitScaleBias.xy + _BlitScaleBias.zw;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 uv = i.texcoord;

                half4 src = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                if (_Amount < 0.001) return src;

                // offset grows toward the edges, so the centre of the hit stays sharp
                float2 c  = uv - 0.5;
                float  r  = saturate(length(c) * 2.0);
                float  w  = pow(r, _Falloff) * _Amount;

                float2 dir = normalize(c + 1e-5);
                float2 px  = dir * (_Strength * w) * _BlitTexture_TexelSize.xy;

                // red pushed out, blue pulled in - classic lateral aberration
                half rC = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + px).r;
                half gC = src.g;
                half bC = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - px).b;

                half3 outc = half3(rC, gC, bC);

                // a warm bias at the rim, nowhere near a full-screen wash
                outc = lerp(outc, outc * _Tint.rgb, w * _TintAmount);

                // slight darkening at the corners pushes the eye inward
                outc *= 1.0 - saturate(pow(r, 2.0) * _Vignette * _Amount);

                return half4(outc, src.a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
