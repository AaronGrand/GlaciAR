Shader "Custom/DepthVisualizer"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }
        SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float depth : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                // Calculate depth
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                float4 viewPos = mul(UNITY_MATRIX_V, worldPos);
                o.depth = viewPos.z / _ProjectionParams.w; // Perspective camera depth
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Visualize depth (0 near, 1 far from the camera)
                float depth = i.depth;
                return fixed4(depth, depth, depth, 1.0);
            }
            ENDCG
        }
    }
}