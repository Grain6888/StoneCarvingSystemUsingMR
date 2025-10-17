Shader "Unlit/GpuNaiveSurfaceNets2"
{
    Properties
    {
    }

    CGINCLUDE
    #include "UnityCG.cginc"
    #include "Lighting.cginc"

    struct appdata
    {
        uint vertexId : SV_VertexID;
    };

    struct v2f
    {
        float4 posCS : SV_POSITION;
        float3 normalOS : NORMAL;
        float3 lightDirOS : TEXCOORD0;
    };
    StructuredBuffer<float3> Vertices;
    StructuredBuffer<int3> Normals;
    StructuredBuffer<uint> Indices;
    sampler2D _MainTex;
    float4 _MainTex_ST;

    ENDCG

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "LightMode" = "ForwardBase"
        }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma multi_compile_fwdbase
            #pragma vertex vert
            #pragma fragment frag

            v2f vert (appdata v)
            {
                v2f o;

                float3 vertex = Vertices[Indices[v.vertexId]];
                float4 posOS = float4(vertex, 1.0);
                o.posCS = UnityObjectToClipPos(posOS);
#if 0
                // flat shading
                uint vindex = v.vertexId - (v.vertexId % 3);
                float3 vec0 = Vertices[Indices[vindex + 1]] - Vertices[Indices[vindex]];
                float3 vec1 = Vertices[Indices[vindex + 2]] - Vertices[Indices[vindex]];
                o.normalOS = normalize(cross(vec0, vec1));
#else
                float QUANTIIZE_FACTOR = 32768.0;
                int3 n = Normals[Indices[v.vertexId]];
                o.normalOS = normalize(float3(n.x / QUANTIIZE_FACTOR, n.y / QUANTIIZE_FACTOR, n.z / QUANTIIZE_FACTOR));
#endif
                o.lightDirOS = ObjSpaceLightDir(posOS);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 normalOS = normalize(i.normalOS);
                float3 lightDirOS = normalize(i.lightDirOS);
                float NdotL = dot(normalOS, lightDirOS) * 0.5 + 0.5;
                fixed4 col = fixed4(1,0,0,1);
                col.rgb *= NdotL;
                return col;
            }
            ENDCG
        }
    }
}
