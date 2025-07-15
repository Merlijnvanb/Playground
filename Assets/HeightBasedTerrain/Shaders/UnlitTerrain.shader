Shader "Unlit/UnlitTerrain"
{
    Properties
    {
        //MaterialProperties
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
            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            #include "UnityIndirect.cginc"

            struct v2f
            {
                UNITY_FOG_COORDS(1)
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            struct VertData
            {
                float3 Position;
                float2 UV;
            };

            StructuredBuffer<VertData> _VertData;
            uniform float4x4 _ObjectToWorld;

            v2f vert (uint svVertexID : SV_VertexID)
            {
                InitIndirectDrawArgs(0);
                v2f o;
                VertData vert = _VertData[GetIndirectVertexID(svVertexID)];
                
                float3 pos = vert.Position;
                float4 wpos = mul(_ObjectToWorld, float4(pos, 1));
                o.pos = mul(UNITY_MATRIX_VP, wpos);
                o.uv = vert.UV;
                
                UNITY_TRANSFER_FOG(o,o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = fixed4(i.uv, 0, 0);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
