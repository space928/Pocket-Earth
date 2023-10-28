Shader "Unlit/SimpleAtmosphere"
{
    Properties
    {
        _Tint ("Tint Colour", Color) = (1,1,1,1)
        _AtmosphereRadius("Atmosphere Radius",float) = 1
        _PlanetRadius("Planet Radius", float) = 0
        _AtmosphereBounds("Atmosphere Bounds", Vector) = (2.5, 1, 0, 0)
        _AtmosphereThickness("Atmosphere Thickness", Range(0, 10)) = 10
        [PowerSlider(2)]_AtmospherePower("Atmosphere Power", Range(0, 10)) = 1.5
        [PowerSlider(2)]_Saturation("Saturation", Range(0, 2)) = 1
        _LightDir("_LightDir", Vector) = (0,0,1,0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 300
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(3)
                float4 vertex : SV_POSITION;
                float4 worldPos : TEXCOORD1;
                //float4 viewDir : TEXCOORD2;
                float4 screenPos : TEXCOORD2;
            };

            float4 _Tint;
            float _AtmosphereRadius;
            float _PlanetRadius;
            float2 _AtmosphereBounds;
            float _AtmosphereThickness;
            float _AtmospherePower;
            float _Saturation;
            float3 _LightDir;
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul (unity_ObjectToWorld, v.vertex);
                //o.viewDir = mul(UNITY_MATRIX_V, float4(0, 0, 1, 0));

                // compute depth
                o.screenPos = ComputeScreenPos(o.vertex);
                COMPUTE_EYEDEPTH(o.screenPos.z);

                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float3 scatterApprox(float cosTheta)
            {
                return log(1+max(lerp(cosTheta*1.5, pow(cosTheta*1.5, float3(0.4, .6, 2.5))/1., _Saturation), 0.));
            }

            float4 frag (v2f i) : SV_Target
            {
                float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos.xyz);
                float3 nrm = normalize(i.worldPos.xyz);

                float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos)));
                float depth = smoothstep(-_AtmosphereRadius, -_PlanetRadius,sceneZ - i.screenPos.z);

                float r = smoothstep(_AtmosphereBounds.x, _AtmosphereBounds.y, 1-dot(nrm, viewDir));

                // Lighting
                float ndotl = dot(nrm, normalize(-_LightDir)) * 0.5 + 0.5;
                float3 scatCol = scatterApprox(pow(ndotl, _AtmospherePower)*_AtmosphereThickness);

                float4 col = float4(scatCol, saturate(r*r*r*saturate(depth))) * _Tint;

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
