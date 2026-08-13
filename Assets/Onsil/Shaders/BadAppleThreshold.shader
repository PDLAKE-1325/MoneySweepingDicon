Shader "Onsil/BadAppleThreshold"
{
    Properties
    {
        _Threshold ("Threshold", Range(0,1)) = 0.5
        _Amount ("Amount", Range(0,1)) = 1
        _Softness ("Softness", Range(0.001,0.4)) = 0.05
        _Desat ("Desaturate first", Range(0,1)) = 1
        _Invert ("Invert", Range(0,1)) = 0
        _Bright ("Bright colour", Color) = (1,1,1,1)
        _Dark ("Dark colour", Color) = (0,0,0,1)
        _Bloom ("Bloom", Range(0,3)) = 0.9
        _BloomRadius ("Bloom radius (px)", Range(1,24)) = 7
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "BadApple"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

            TEXTURE2D_X(_BlitTexture);
            float4 _BlitScaleBias;
            float4 _BlitTexture_TexelSize;

            float _Threshold, _Amount, _Softness, _Desat, _Invert;
            float _Bloom, _BloomRadius;
            float4 _Bright, _Dark;

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

                // fullscreen triangle generated from the vertex id
                float4 pos = GetFullScreenTriangleVertexPosition(input.vertexID);
                float2 uv  = GetFullScreenTriangleTexCoord(input.vertexID);

                o.positionCS = pos;
                o.texcoord   = uv * _BlitScaleBias.xy + _BlitScaleBias.zw;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half4 src = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, i.texcoord);

                // _Amount == 0 must be a perfect passthrough
                float lum   = dot(src.rgb, float3(0.299, 0.587, 0.114));
                float3 gray = lerp(src.rgb, lum.xxx, saturate(_Desat * _Amount));

                float t = smoothstep(_Threshold - _Softness, _Threshold + _Softness, lum);
                t = lerp(t, 1.0 - t, _Invert);

                float3 bin  = lerp(_Dark.rgb, _Bright.rgb, t);
                float3 outc = lerp(gray, bin, _Amount);
                return half4(outc, src.a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
