/*
作者: topameng 日期: 2016-11-11
作用: 透明Grass
*/
Shader "hfqipai/Transparent/Vertex Wind" 
{
	Properties 
	{
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
		_Wind("Wind params",Vector) = (1,1,1,1)
		_WindEdgeFlutter("Wind edge fultter factor", float) = 0.5
		_WindEdgeFlutterFreqScale("Wind edge fultter freq scale",float) = 0.5
	}

	SubShader 
	{
		Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderType"="Transparent"}
		LOD 200
	
		Blend SrcAlpha OneMinusSrcAlpha
		Cull Off 
		ZWrite Off
		
		Pass 
		{			
			Tags { "LightMode" = "ForwardBase"}	

			CGPROGRAM	
			#define VERTEX_ALPHA_WIND 1	
			#pragma exclude_renderers ps3 xbox360 flash xboxone ps4 psp2
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma skip_variants FOG_EXP FOG_EXP2 DIRLIGHTMAP_COMBINED DIRLIGHTMAP_SEPARATE	
			#pragma multi_compile_fog
			#pragma multi_compile_fwdbase
			#include "Mobile-Grass.cginc"
			#pragma vertex vert
			#pragma fragment frag			
			ENDCG 
		}			
	}

	//Fallback "Transparent/VertexLit"	
}



