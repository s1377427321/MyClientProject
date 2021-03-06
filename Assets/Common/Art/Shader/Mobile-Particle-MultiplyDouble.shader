// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "hfqipai/Particles/Multiply (Double)" 
{
	Properties 
	{
		_MainTex ("Particle Texture", 2D) = "white" {}		
	}
	
	SubShader 
	{
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }

		Blend DstColor SrcColor
		ColorMask RGB
		Cull Off 
		Lighting Off 
		ZWrite Off

		Pass 
		{		
			CGPROGRAM
			#pragma exclude_renderers ps3 xbox360 flash xboxone ps4 psp2
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma vertex vert
			#pragma fragment frag			
			//#pragma multi_compile_fog

			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed4 _TintColor;
			
			struct appdata_t 
			{
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f 
			{
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				//UNITY_FOG_COORDS(1)
			};				

			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.color = v.color;
				o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
				//UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}					
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col;
				fixed4 tex = tex2D(_MainTex, i.texcoord);
				col.rgb = tex.rgb * i.color.rgb * 2;
				col.a = i.color.a * tex.a;
				col = lerp(fixed4(0.5f,0.5f,0.5f,0.5f), col, col.a);
				//UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(0.5,0.5,0.5,0.5));
				return col;
			}
			ENDCG 
		}
	}
}
