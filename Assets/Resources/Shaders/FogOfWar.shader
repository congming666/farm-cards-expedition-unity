Shader "Game/FogOfWar"
{
    Properties { _MaskTex ("Explored Mask", 2D) = "black" {} _FogColor ("Fog Color", Color) = (0.04,0.06,0.10,1) _PlayerPos ("Player World XZ", Vector) = (0,0,0,0) _VisibleRadius ("Visible Radius", Float) = 8 _ExploredDim ("Explored Dim", Range(0,1)) = 0.5 _MapSize ("World Size", Float) = 60 }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass { CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MaskTex; float4 _FogColor; float4 _PlayerPos; float _VisibleRadius, _ExploredDim, _MapSize;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 worldUV:TEXCOORD0; float4 worldPos:TEXCOORD1; };
            v2f vert(appdata v){ v2f o; o.pos=UnityObjectToClipPos(v.vertex); float3 w=mul(unity_ObjectToWorld,v.vertex).xyz; o.worldPos=float4(w,1); float wx=(w.x)/_MapSize; float wz=(w.z)/_MapSize; if(wz<0)wz+=1; if(wz>1)wz-=1; o.worldUV=float2(wx,wz); return o; }
            fixed4 frag(v2f i):SV_Target{
                float explored = tex2D(_MaskTex, i.worldUV).r;
                float d = distance(i.worldPos.xz, _PlayerPos.xz);
                float vis = 1.0 - smoothstep(_VisibleRadius*0.55, _VisibleRadius, d);
                float fogA = (1.0-explored) + explored*(1.0-vis)*_ExploredDim;
                fogA = saturate(fogA);
                return fixed4(_FogColor.rgb, fogA);
            }
        ENDCG }
    }
}
