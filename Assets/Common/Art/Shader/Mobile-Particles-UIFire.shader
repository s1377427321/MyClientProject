// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "hfqipai/Particles/UIFire"
{
	Properties 
	{
		_Color("_Color", Color) = (1,1,1,1)
		_MainTex("Main Tex", 2D) = "black" {}
		_niuqu("niuqu Tex", 2D) = "white" {}
		_Mask("mask Tex", 2D) = "white" {}		
		_niuqu_sudu("scroll speed", Float) = 1
		_niuqudu("_niuqudu", Float) = 0
	}
	
	SubShader 
	{
		Tags {"Queue"="Transparent" "IgnoreProjector"="False" "RenderType"="Transparent"}
		
		Cull Off
		ZWrite Off		
		ColorMask RGB
		Blend SrcColor One		

		Pass
		{
			CGPROGRAM		
			#pragma exclude_renderers ps3 xbox360 flash xboxone ps4 psp2
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma vertex vert
			#pragma fragment frag		
			#include "UnityCG.cginc"		

			sampler2D _MainTex;
			sampler2D _niuqu;
			sampler2D _Mask;
			float4 _Color;
			float _niuqu_sudu;
			float _niuqudu;	

			float4 _MainTex_ST;
			float4 _niuqu_ST;
			float4 _Mask_ST;

			struct appdata_color
			{
				fixed4 color  : COLOR;
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 texcoord : TEXCOORD0;	
			};
				
			struct v2f 
			{
				float4 pos : SV_POSITION;
				float4 color : COLOR;
				float2 uv_main : TEXCOORD0;
				float2 uv_niuqu : TEXCOORD1;
				float2 uv_mask : TEXCOORD2;
			};

			v2f vert (appdata_color v)
			{
				v2f o;
				UNITY_INITIALIZE_OUTPUT(v2f, o);
				o.pos = UnityObjectToClipPos(v.vertex);
				o.color = saturate(v.color * _Color);  
				
				o.uv_main = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.uv_mask = TRANSFORM_TEX(v.texcoord ,_Mask);
				o.uv_niuqu = TRANSFORM_TEX(v.texcoord ,_niuqu);
				o.uv_niuqu.y += (_Time * _niuqu_sudu).x;
				return o;
			}

			fixed4 frag(v2f IN) : SV_Target 
			{
				fixed4 Tex2D1 = tex2D(_niuqu, IN.uv_niuqu);
				fixed4 Multiply1 = _niuqudu * Tex2D1;
				float2 Add0 = IN.uv_main.xy + Multiply1.xy;
				fixed4 Tex2D0 = tex2D(_MainTex, Add0);
				fixed4 Tex2D2 = tex2D(_Mask, IN.uv_mask);			
				return Tex2D0 * Tex2D2 * IN.color;						
			}
			ENDCG
		}
	}
}