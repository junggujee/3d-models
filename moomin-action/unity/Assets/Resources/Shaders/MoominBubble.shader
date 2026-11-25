// 비눗방울 · 공기방울 막(용궁 돔 · 버블 감옥). 기본 파이프라인 · URP 모두(조명 태그 없는 CG 패스).
// 가장자리일수록 진하고(프레넬) 무지갯빛이 돌며, 가운데는 거의 투명. 앞뒤 모두 그림(돔 안에서도 보이게). 깊이는 쓰지 않음.
Shader "MoominAction/Bubble"
{
    Properties
    {
        _Tint ("색", Color) = (0.7, 0.9, 1, 1)
        _Alpha ("가운데 투명도", Range(0, 1)) = 0.06
        _RimAlpha ("가장자리 진하기", Range(0, 1)) = 0.55
        _RimPower ("가장자리 폭", Range(0.5, 8)) = 2.5
        _Iris ("무지갯빛", Range(0, 1)) = 0.6
        _Speed ("일렁임", Float) = 0.6
    }
    SubShader
    {
        Tags { "Queue" = "Transparent+10" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Tint; float _Alpha, _RimAlpha, _RimPower, _Iris, _Speed;
            struct v2f { float4 pos : SV_POSITION; float3 n : TEXCOORD0; float3 w : TEXCOORD1; };
            v2f vert(appdata_base v)
            {
                v2f o; float3 w = mul(unity_ObjectToWorld, v.vertex).xyz;
                w += normalize(mul((float3x3)unity_ObjectToWorld, v.normal)) * sin(_Time.y * _Speed * 3.0 + w.x * 0.7 + w.z * 0.5) * 0.04;   // 살짝 출렁
                o.w = w; o.n = normalize(mul((float3x3)unity_ObjectToWorld, v.normal)); o.pos = mul(UNITY_MATRIX_VP, float4(w, 1)); return o;
            }
            fixed4 frag(v2f i, float facing : VFACE) : SV_Target
            {
                float3 V = normalize(_WorldSpaceCameraPos - i.w); float3 N = normalize(i.n) * (facing < 0 ? -1.0 : 1.0);
                float rim = pow(1.0 - saturate(abs(dot(N, V))), _RimPower);
                float h = frac(rim * 1.3 + dot(i.w, float3(0.05, 0.08, 0.03)) + _Time.y * _Speed * 0.1);
                float3 iris = saturate(abs(frac(h + float3(0, 0.33, 0.67)) * 6.0 - 3.0) - 1.0);
                float3 col = lerp(_Tint.rgb, iris, _Iris * rim) + rim * 0.35;
                return fixed4(col, saturate(_Alpha + rim * _RimAlpha));
            }
            ENDCG
        }
    }
    FallBack Off
}
