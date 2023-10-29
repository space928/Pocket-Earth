Shader "Unlit/HealthBar"
{
    Properties
    {
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _Value ("_Value", Range(0, 1)) = 0.5
        _ColLow ("Colour Low", Color) = (1,0,0,1)
        _ColHigh ("Colour High", Color) = (0,1,0,1)
        [PowerSlider(2)] _ShimmerSpeed("Shimmer Speed", Range(-1, 1)) = 0
        [PowerSlider(2)] _ShimmerSize("Shimmer Size", Range(0, 0.1)) = 0
        [PowerSlider(2)] _ShimmerColor("Shimmer Color", Range(0, 2)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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
                float2 raw_uv : TEXCOORD1;
                UNITY_FOG_COORDS(2)
                float4 vertex : SV_POSITION;
            };

            sampler2D _NoiseTex;
            float4 _NoiseTex_ST;
            float _Value ;
            float4 _ColLow;
            float4 _ColHigh;
            float _ShimmerSpeed;
            float _ShimmerSize;
            float _ShimmerColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _NoiseTex);
                o.raw_uv = v.uv;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            // LAB/RGB converstions - https://code.google.com/archive/p/flowabs/
            // HSV/RGB conversion - http://lolengine.net/blog/2013/07/27/rgb-to-hsv-in-glsl
            float3 rgb2xyz( float3 c ) {
                float3 tmp;
                tmp.x = ( c.r > 0.04045 ) ? pow( ( c.r + 0.055 ) / 1.055, 2.4 ) : c.r / 12.92;
                tmp.y = ( c.g > 0.04045 ) ? pow( ( c.g + 0.055 ) / 1.055, 2.4 ) : c.g / 12.92,
                tmp.z = ( c.b > 0.04045 ) ? pow( ( c.b + 0.055 ) / 1.055, 2.4 ) : c.b / 12.92;
                return 100.0 * mul(tmp,
                    float3x3( 0.4124, 0.3576, 0.1805,
                          0.2126, 0.7152, 0.0722,
                          0.0193, 0.1192, 0.9505 ));
            }

            float3 xyz2lab( float3 c ) {
                float3 n = c / float3( 95.047, 100, 108.883 );
                float3 v;
                v.x = ( n.x > 0.008856 ) ? pow( n.x, 1.0 / 3.0 ) : ( 7.787 * n.x ) + ( 16.0 / 116.0 );
                v.y = ( n.y > 0.008856 ) ? pow( n.y, 1.0 / 3.0 ) : ( 7.787 * n.y ) + ( 16.0 / 116.0 );
                v.z = ( n.z > 0.008856 ) ? pow( n.z, 1.0 / 3.0 ) : ( 7.787 * n.z ) + ( 16.0 / 116.0 );
                return float3(( 116.0 * v.y ) - 16.0, 500.0 * ( v.x - v.y ), 200.0 * ( v.y - v.z ));
            }

            float3 rgb2lab(float3 c) {
                float3 lab = xyz2lab( rgb2xyz( c ) );
                return float3( lab.x / 100.0, 0.5 + 0.5 * ( lab.y / 127.0 ), 0.5 + 0.5 * ( lab.z / 127.0 ));
            }

            float3 lab2xyz( float3 c ) {
                float fy = ( c.x + 16.0 ) / 116.0;
                float fx = c.y / 500.0 + fy;
                float fz = fy - c.z / 200.0;
                return float3(
                     95.047 * (( fx > 0.206897 ) ? fx * fx * fx : ( fx - 16.0 / 116.0 ) / 7.787),
                    100.000 * (( fy > 0.206897 ) ? fy * fy * fy : ( fy - 16.0 / 116.0 ) / 7.787),
                    108.883 * (( fz > 0.206897 ) ? fz * fz * fz : ( fz - 16.0 / 116.0 ) / 7.787)
                );
            }

            float3 xyz2rgb( float3 c ) {
                float3 v =  mul(c / 100.0, float3x3( 
                    3.2406, -1.5372, -0.4986,
                    -0.9689, 1.8758, 0.0415,
                    0.0557, -0.2040, 1.0570
                ));
                float3 r;
                r.x = ( v.r > 0.0031308 ) ? (( 1.055 * pow( v.r, ( 1.0 / 2.4 ))) - 0.055 ) : 12.92 * v.r;
                r.y = ( v.g > 0.0031308 ) ? (( 1.055 * pow( v.g, ( 1.0 / 2.4 ))) - 0.055 ) : 12.92 * v.g;
                r.z = ( v.b > 0.0031308 ) ? (( 1.055 * pow( v.b, ( 1.0 / 2.4 ))) - 0.055 ) : 12.92 * v.b;
                return r;
            }

            float3 lab2rgb(float3 c) {
                return xyz2rgb( lab2xyz( float3(100.0 * c.x, 2.0 * 127.0 * (c.y - 0.5), 2.0 * 127.0 * (c.z - 0.5)) ) );
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 noise = tex2D(_NoiseTex, i.uv + _Time.xy * _ShimmerSpeed)*2-1;
                fixed4 noise1 = tex2D(_NoiseTex, (i.uv+0.5) + _Time.xy * _ShimmerSpeed)*2-1;
                noise = lerp(noise, noise1, abs(frac(_Time.y*0.2)*2-1));

                // Render color bar
                float3 col = lab2rgb(lerp(rgb2lab(_ColLow), rgb2lab(_ColHigh), smoothstep(0, 1, _Value + noise * _ShimmerColor)));
                col *= smoothstep(_Value+0.01, _Value, i.raw_uv.y + noise.x * _ShimmerSize * (abs(1-_Value)+0.2));
                //col = noise.xyz;

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return float4(col, 1);
            }
            ENDCG
        }
    }
}
