// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "hfqipai/Particles/Additive (Soft)" 
{
	Properties 
	{ 		
 		_MainTex ("Particle Texture", 2D) = "white" { }
	}

	SubShader 
	{	
 		Tags { "Queue"="Transparent" "IgnoreProjector"="true" "RenderType"="Transparent" }

 		Pass 
 		{  
 			ZWrite Off
 			Cull Off
 			Blend One OneMinusSrcColor
 			ColorMask RGB

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

			struct appdata 
			{
  				float4 pos : POSITION;
  				half4 color : COLOR;
  				float4 uv0 : TEXCOORD0;
			};

			struct v2f 
			{
				float4 pos : SV_POSITION;
  				fixed4 color : COLOR0;
  				float2 uv0 : TEXCOORD0;  				
  				//UNITY_FOG_COORDS(1)
			};

			v2f vert (appdata v) 
			{
  				v2f o;  	
  				UNITY_INITIALIZE_OUTPUT(v2f, o);					  						;
  				o.pos = UnityObjectToClipPos(v.pos);
  				o.uv0 = TRANSFORM_TEX(v.uv0, _MainTex);
  				o.color = saturate(v.color);  				
  				//UNITY_TRANSFER_FOG(o, o.pos);    				
  				return o;
			}

			fixed4 frag (v2f IN) : SV_Target 
			{  						  		
  				fixed4 col = tex2D (_MainTex, IN.uv0) * IN.color;  							  			
  				col.rgb *= col.a;
  				//UNITY_APPLY_FOG(IN.fogCoord, col);  
  				return col;
			}
			ENDCG
 		}
	}
}