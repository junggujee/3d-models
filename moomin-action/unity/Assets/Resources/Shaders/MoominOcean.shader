// 바다 물(Unity 기본 파이프라인 · URP 모두: 조명 태그 없는 CG 패스 → URP 에서는 SRPDefaultUnlit 로 그려짐).
// 정점: 게르스트너 파도 4개(World/OceanMath.cs 와 같은 식) — 얕은 곳은 작게(깊이 그림), 카메라에서 멀면 0 으로(먼 곳 깜빡임 방지).
// 조각: 해석 법선 + 흐르는 잔물결 노멀 2장 · 프레넬(하늘 반사) · 얕은 곳 청록 → 깊은 곳 남색 · 물마루 속빛 · 해 반짝임 ·
//       물가 거품(깊이 띠가 밀려옴) + 물마루 거품 · 먼 곳 수평선 색 · 물속에서 올려다보면 스넬 창(밝은 원) 아래는 물색.
// 전역 값은 World/Ocean.cs 가 매 프레임 넣음(_Ocean*). 굴절·반사 카메라·깊이 텍스처를 쓰지 않아 가벼움(그리기 1번).
Shader "MoominAction/Ocean"
{
    Properties
    {
        _DeepColor ("깊은 물", Color) = (0.03, 0.22, 0.38, 1)
        _ShallowColor ("얕은 물", Color) = (0.12, 0.66, 0.66, 1)
        _SkyColor ("하늘(위)", Color) = (0.5, 0.75, 0.97, 1)
        _HorizonColor ("수평선", Color) = (0.84, 0.92, 0.98, 1)
        _FoamColor ("거품", Color) = (0.97, 0.99, 1, 1)
        _UnderColor ("물속에서 본 수면", Color) = (0.06, 0.36, 0.46, 1)
        _DetailNormal ("잔물결 노멀", 2D) = "bump" {}
        _FoamTex ("거품 노이즈", 2D) = "white" {}
        _DetailScale ("잔물결 크기", Float) = 0.07
        _DetailStrength ("잔물결 세기", Range(0, 1)) = 0.45
    }
    SubShader
    {
        Tags { "Queue" = "Transparent-50" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        LOD 200
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            float4 _DeepColor, _ShallowColor, _SkyColor, _HorizonColor, _FoamColor, _UnderColor;
            sampler2D _DetailNormal, _FoamTex; float _DetailScale, _DetailStrength;
            float4 _OceanWave0, _OceanWave1, _OceanWave2, _OceanWave3;   // dir.x, dir.z, k, ω
            float4 _OceanAmp;                                            // 파도별 진폭(m)
            float4 _OceanParams;                                         // 시간, 해수면 높이, 물결 줄기 시작 거리, 끝 거리
            float4 _OceanDepthRect;                                      // 깊이 그림 영역: minX, minZ, 1/크기, 있음(1)
            sampler2D _OceanDepthTex;                                    // R = (깊이+2)/42 · G = (깊이+2)/6 (얕은 곳 정밀)
            float4 _OceanSunDir, _OceanSunColor, _OceanFog;              // 해 쪽 방향 · 햇빛 색 · 안개(시작, 끝)

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 world : TEXCOORD0; float3 nrm : TEXCOORD1; float2 cd : TEXCOORD2; };   // cd = (물마루 높이, 깊이)

            float SeaDepth(float2 xz)
            {
                if (_OceanDepthRect.w < 0.5) return 40.0;
                float2 uv = (xz - _OceanDepthRect.xy) * _OceanDepthRect.z;
                if (uv.x < 0.0 || uv.y < 0.0 || uv.x > 1.0 || uv.y > 1.0) return 40.0;
                float4 t = tex2Dlod(_OceanDepthTex, float4(uv, 0, 0));
                return t.g < 0.995 ? t.g * 6.0 - 2.0 : t.r * 42.0 - 2.0;
            }
            float AmpScale(float d) { return 0.35 + 0.65 * saturate(d / 6.0); }

            void Gerstner(float4 w, float a, float2 xz, float t, inout float3 p, inout float3 tg, inout float3 bn)
            {
                float2 d = w.xy; float f = w.z * dot(d, xz) - w.w * t; float s, c; sincos(f, s, c);
                p.x += d.x * a * c; p.z += d.y * a * c; p.y += a * s;
                float ka = w.z * a;
                tg += float3(-d.x * d.x * ka * s, d.x * ka * c, -d.x * d.y * ka * s);
                bn += float3(-d.x * d.y * ka * s, d.y * ka * c, -d.y * d.y * ka * s);
            }

            v2f vert(appdata v)
            {
                v2f o;
                float3 w = mul(unity_ObjectToWorld, v.vertex).xyz; float2 xz = w.xz; float t = _OceanParams.x;
                float depth = SeaDepth(xz);
                float far = saturate((distance(xz, _WorldSpaceCameraPos.xz) - _OceanParams.z) / max(1.0, _OceanParams.w - _OceanParams.z));
                float a = AmpScale(depth) * (1.0 - far);
                float3 p = float3(xz.x, _OceanParams.y, xz.y), tg = float3(1, 0, 0), bn = float3(0, 0, 1);
                Gerstner(_OceanWave0, _OceanAmp.x * a, xz, t, p, tg, bn);
                Gerstner(_OceanWave1, _OceanAmp.y * a, xz, t, p, tg, bn);
                Gerstner(_OceanWave2, _OceanAmp.z * a, xz, t, p, tg, bn);
                Gerstner(_OceanWave3, _OceanAmp.w * a, xz, t, p, tg, bn);
                o.world = p; o.pos = mul(UNITY_MATRIX_VP, float4(p, 1.0));
                o.nrm = normalize(cross(bn, tg)); o.cd = float2(p.y - _OceanParams.y, depth);
                return o;
            }

            float3 Detail(float2 uv) { float2 xy = tex2D(_DetailNormal, uv).rg * 2.0 - 1.0; return float3(xy.x, 0.0, xy.y); }

            fixed4 frag(v2f i, float facing : VFACE) : SV_Target
            {
                float t = _OceanParams.x; float3 toCam = _WorldSpaceCameraPos - i.world; float dist = length(toCam); float3 V = toCam / max(dist, 1e-4);
                float detail = _DetailStrength * (1.0 - saturate(dist / 220.0));
                float3 dn = Detail(i.world.xz * _DetailScale + float2(t * 0.02, t * 0.013)) + Detail(i.world.xz * _DetailScale * 1.9 + float2(-t * 0.017, t * 0.021));
                float3 N = normalize(i.nrm + dn * detail);
                float depth = i.cd.y, crest = i.cd.x;

                if (facing < 0)   // 물속에서 올려다봄: 스넬 창(바로 위는 밝은 하늘) · 비스듬하면 전반사로 물색
                {
                    float c = saturate(dot(-N, V));
                    float3 col = lerp(_UnderColor.rgb * 0.75, _SkyColor.rgb * 1.15, smoothstep(0.62, 0.82, c));
                    col += pow(saturate(dot(-normalize(_OceanSunDir.xyz), -V)), 40.0) * _OceanSunColor.rgb * 0.6;
                    return fixed4(col, 0.94);
                }

                float3 L = normalize(_OceanSunDir.xyz);
                float ndv = saturate(dot(N, V));
                float fres = 0.02 + 0.98 * pow(1.0 - ndv, 5.0);
                float3 R = reflect(-V, N);
                float3 sky = lerp(_HorizonColor.rgb, _SkyColor.rgb, saturate(R.y * 2.5));
                float shallow = exp(-max(depth, 0.0) / 2.5);
                float3 body = lerp(_DeepColor.rgb, _ShallowColor.rgb, shallow);
                body += _ShallowColor.rgb * saturate(crest / 0.6) * pow(saturate(dot(V, -L) * 0.5 + 0.5), 3.0) * 0.6;   // 물마루 속빛
                float3 col = lerp(body * (0.65 + 0.35 * saturate(dot(N, L))) * _OceanSunColor.rgb, sky, fres * 0.85);
                float spec = pow(saturate(dot(N, normalize(L + V))), 380.0) * 3.5 * (1.0 - saturate(dist / 900.0));

                float fn = tex2D(_FoamTex, i.world.xz * 0.12 + float2(t * 0.03, 0.0)).r;
                float band = sin(depth * 7.0 - t * 1.8 + fn * 3.0) * 0.5 + 0.5;                                     // 물가로 밀려오는 거품 띠
                float foam = saturate(1.0 - depth / 1.4) * smoothstep(0.35, 0.9, band * 0.6 + fn * 0.5) + saturate(1.0 - depth / 0.25) * 0.8;
                foam += smoothstep(0.42, 0.62, crest + fn * 0.25) * 0.6;                                             // 물마루 거품
                foam = saturate(foam) * (1.0 - saturate(dist / 400.0));
                col = lerp(col, _FoamColor.rgb, foam) + spec * _OceanSunColor.rgb;

                float fog = saturate((dist - _OceanFog.x) / max(1.0, _OceanFog.y - _OceanFog.x));
                col = lerp(col, _HorizonColor.rgb, fog * 0.85);
                float alpha = saturate(0.45 + max(depth, 0.0) * 0.25 + fres * 0.5 + foam);                           // 물가는 투명해 모래가 비침
                return fixed4(col, alpha);
            }
            ENDCG
        }
    }
    FallBack Off
}
