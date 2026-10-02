Shader "Vaultbreakers/ArcadeSlash"
{
    Properties { _Intensity ("Glow intensity", Float) = 1.6 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "Slash"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _Intensity;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                return half4(input.color.rgb * _Intensity, input.color.a);
            }
            ENDHLSL
        }
    }
}
