/*
	Author: Mikael Hertz (mikael.hertz@gmail.com)
*/
Shader "SLU/VertexColor" 
{
	SubShader
	{
		Pass
		{
			LOD 200


			CGPROGRAM
			#pragma target 5.0
			#pragma vertex vert
			#pragma fragment frag

			struct VertexInput 
			{
				float4 v : POSITION;
				float4 color: COLOR;
			};

			struct VertexOutput // PS_INPUT
			{
				float4 pos : SV_POSITION;
				float4 col : COLOR;
			};

			VertexOutput vert(VertexInput i) // MainVs
			{

				VertexOutput o;
				o.pos = UnityObjectToClipPos(i.v);
				o.col = i.color;
				return o;
			}

			float4 frag(VertexOutput o) : COLOR
			{
				return o.col;
			}

			ENDCG
		}
	}
}