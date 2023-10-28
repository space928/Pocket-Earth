Shader"Unlit/Atmosphere"
{
    Properties
    {
        _AtmosphereRadius("Atmosphere Radius", float) = 100
        _PlanetRadius("Planet Radius", float) = 100
        _AtmosphereThickness("Atmosphere Thickness", Range(0, 10)) = 10
        [PowerSlider(3)]_AtmosphereDensity("Atmosphere Density", Range(0, 1)) = 1
        _LightDir("_LightDir", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 300

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
                UNITY_FOG_COORDS(2)
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD1;
            };

            float _AtmosphereRadius;
            float _PlanetRadius;
            float _AtmosphereThickness;
            float _AtmosphereDensity;
            float4 _LightDir;
            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o,o.vertex);
    
                // compute depth
                o.screenPos = ComputeScreenPos(o.vertex);
                COMPUTE_EYEDEPTH(o.screenPos.z);
    
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos)));
                float depth = sceneZ - i.screenPos.z;
    
                float thickness = _AtmosphereRadius - _PlanetRadius;
                float4 col = float4(0.5, 0.5, 0.5, saturate(exp(depth*_AtmosphereDensity)-1));
    
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
