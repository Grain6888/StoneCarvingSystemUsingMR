Shader "Unlit/GpuNaiveSurfaceNets"
{
    Properties
    {
    }

    CGINCLUDE
    StructuredBuffer<float3> Vertices;
    StructuredBuffer<uint> Indices;

    ENDCG

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                uint vertexId : SV_VertexID;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(float4(Vertices[Indices[v.vertexId]], 1.0));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(1,0,0,1);
            }
            ENDCG
        }
    }
}
