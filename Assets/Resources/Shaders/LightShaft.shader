Shader "Game/LightShaft"
{
    Properties { _Color ("Color", Color) = (1,0.9,0.7,1) _Intensity ("Intensity", Range(0,4)) = 1.2 _Width ("Width falloff", Range(0,1)) = 0.35 _TipFade ("Tip fade", Range(0,1)) = 0.6 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend One One
        ZWrite Off
        Cull Off
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Color; float _Intensity,_Width,_TipFade;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata v){ v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o; }
            fixed4 frag(v2f i):SV_Target{
                float across = abs(i.uv.x-0.5)*2.0;
                float w = 1.0 - smoothstep(0.0, _Width, across);
                float along = i.uv.y;
                float tip = 1.0 - smoothstep(_TipFade, 1.0, along);
                float a = w*tip*_Intensity;
                return fixed4(_Color.rgb*a, a);
            }
        ENDCG }
    }
}
