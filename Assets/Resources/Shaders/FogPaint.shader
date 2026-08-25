Shader "Hidden/FogPaint"
{
    Properties { _Center ("Center UV", Vector) = (0,0,0,0) _Radius ("Radius", Float) = 0.1 _Strength ("Strength", Range(0,1)) = 1 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        BlendOp Max
        Blend One One
        ZWrite Off
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Center; float _Radius, _Strength;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata v){ v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o; }
            fixed4 frag(v2f i):SV_Target{
                float d = distance(i.uv, _Center.xy);
                float a = 1.0 - smoothstep(_Radius*0.55, _Radius, d); // 柔边
                return fixed4(a*_Strength, 0, 0, 1);
            }
        ENDCG }
    }
}
