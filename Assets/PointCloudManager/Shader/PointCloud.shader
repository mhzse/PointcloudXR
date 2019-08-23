/*
	Author: Mikael Hertz (mikael.hertz@gmail.com)
*/
Shader "SLU/PointCloud"
{
	Properties
	{
		_NumberOfPoints("Number of Points", int) = 0
		_SelectPointID("Select Point ID", int) = 0
		[Header(Geometry)] _PointRadius("Point radius", Range(0.001, 0.5)) = 0.025
		[Toggle(POINT_ON)] _PointOn("Draw points", Int) = 0
		[Toggle] _Circles("Draw Circles", int) = 0
		[Toggle] _Debug("Debug", int) = 0
		_ZDepth("ZDepth", Range(-1, 1)) = 0
		_UserPosition("User position", Vector) = (0, 0, 0)

		[Header(Color)] _Brightness("Brightness", Range(-1, 1)) = 0.0
		_ColorIntensity("Intensity", Range(-1, 5)) = 1
		[Toggle] _AddIntensityValueToColor("Add intensity value to color", int) = 0
		_SelectedPointColorBoost("Selected point color boost", Range(0, 0.5)) = 0
		[Toggle] _LinearToGamma("Linear to gamma", int) = 0
		[Toggle] _GammaToLinear("Gamma to linear", int) = 0
		[Toggle] _user_color("Use User Color", int) = 0

		[Header(Edit Tool)] [Toggle] _EditToolActive("Activate Edit Tool", int) = 0

		[Toggle] _EditToolDelete("Edit Tool Delete", int) = 0
		[Toggle] _EditToolSelect("Edit Tool Select", int) = 0
		[Toggle] _EditToolDeselect("Edit Tool Deselect", int) = 0
		_EditToolRadius("Edit tool radius", Range(0.1,50)) = 0.5
		_SelectedPointColor("Selected point color", Color) = (1, 1, 0, 1)

		[Header(Classification)] _GroundClass("Ground class", int) = 2
		_VegitationClass("Vegitation class", int) = 3
		_GroundCollisionDistance("Ground collision distance", float) = 0.2
		_EditClass("Edit class", int) = 3
		[Toggle] _ColorTreesByIntensity("Tree color by intensity", int) = 0
		_TreeIntensityThreshold("Tree intensity threshold", Range(0, 1)) = 0.5
		[Toggle] _EnableVegetation("Enable vegetation", int) = 1
		_TreeColor("Tree color", Range(0,1)) = 0.8
		[Header(Nearest Point)] [Toggle] _FindNearestPoint("Find nearest point", int) = 0
		[Toggle] _ShowNearestPointFound("Show nearest point found", int) = 0

		[Header(Nearest Point)][Toggle] _TransformSelectedPoints("Transform selected points", int) = 0
		_SelectedPointsOffset("Points offset", Vector) = (0, 0, 0)
		_NearestPointID("Nearest point ID", int) = 0
	}

	SubShader
	{
		Pass
		{
			CGPROGRAM
			#define UNITY_SHADER_NO_UPGRADE 1 
			#pragma target 5.0
			#pragma vertex vert
			#pragma fragment frag
		
			#pragma shader_feature INVERT_COLORS
			#pragma shader_feature POINT_ON

			#pragma shader_feature SELECT

			#pragma geometry geo

			#include "UnityCG.cginc"
			#include "Assets/PointCloudManager/Shader/include/PointCloudXR_include.cginc"

			struct TranslatedPoint
			{
				float3	offset;
			};

			RWStructuredBuffer<float>	computeBufferCom			: register(u3);
			RWStructuredBuffer<PointXR>	pointsInFrustum             : register(u5);
			RWStructuredBuffer<PointXR>	_points_all                 : register(u6); // Use this to update the pointcloud

			struct VertOut
			{
				float4	pos				    : POSITION;
				float4	color			    : COLOR;
				uint	deleteMe		    : TEXCOORD0;
				uint	selectMe		    : TEXCOORD1;
				float	classification	    : TEXCOORD2;
				float2  uv                  : TEXCOORD3;
				float4  viewposition        : TEXCOORD4;
				float   intensityNormalized : TEXCOORD5;
				float   id                  : TEXCOORD6; // ID in pointsAll
				float   visible             : TEXCOORD7;
				float   radius              : TEXCOORD8;
				float   sv_vertex_id        : TEXCOORD9; // ID in pointsInFrustum
			};

			struct GeoOut
			{
				float4	pos				    : POSITION;
				float4	color               : COLOR;
				float2  uv                  : TEXCOORD0;
				float4  viewposition        : TEXCOORD1;
				float   intensityNormalized : TEXCOORD2;
				float   id                  : TEXCOORD3;
				float   sv_vertex_id        : TEXCOORD4;
			};

			struct FragmentOutput
			{
				float4 color : SV_TARGET;
				float  depth : SV_DEPTH;
			};

			struct FragmentOutputSimple
			{
				float4 color : SV_TARGET;
			};

			int		_NumberOfPoints;
			int     _SelectPointID;
			int     _LinearToGamma;
			int     _GammaToLinear;
			int     _user_color;
			int     _Circles;
			int     _Debug;
			float   _ZDepth;
			float	_ObjectDistanceFarBufferZone;
			float	_ObjectDistanceNear;
			int		_DecimatingFactor;
			int		_AnimatePointCloud;
			int		_VisualizeNormals;
			float	_PointRadius;
			float	_Brightness;
			float   _ColorIntensity;
			int		_UseGeometricShader;
			float4  _EditToolPos;
			float	_EditToolRadius;
			float   _EditToolMode;
			int		_EditToolDelete;	// State
			int		_EditToolSelect;	// State
			int		_EditToolDeselect;  // State
			int		_EditToolActive;
			int		_AddIntensityValueToColor;
			float	_SelectedPointColorBoost;
			int		_GroundClass;
			int     _VegitationClass;
			float3  _UserPosition;
			float	_GroundCollisionDistance;
			int		_EditClass;
			int     _ColorTreesByIntensity;
			float   _TreeIntensityThreshold;
			int     _EnableVegetation;
			float   _TreeColor;
			int     _FindNearestPoint;
			float4  _FindNearestPointToPosition;
			int     _ShowNearestPointFound;
			float4  _SelectedPointColor;
			int		_TransformSelectedPoints;
			float3  _SelectedPointsOffset;
			int     _NearestPointID;


			VertOut vert(uint id: SV_VertexID)
			{
				VertOut fragIn;

				fragIn.id = id;
				fragIn.selectMe = _points_all[id].selected;
				fragIn.deleteMe = _points_all[id].deleted;
				fragIn.visible  = _points_all[id].visible;
				fragIn.pos      = float4(_points_all[id].pos, 1);
				fragIn.color    = _points_all[id].color * (!_user_color) + _points_all[id].user_color * (_user_color);
				fragIn.classification      = _points_all[id].classification;
				fragIn.intensityNormalized = _points_all[id].intensityNormalized;

				fragIn.uv           = float2(0, 0);
				fragIn.viewposition = float4(0, 0, 0, 0);
				fragIn.radius       = _PointRadius;
				fragIn.sv_vertex_id = id;
			
				if (_ShowNearestPointFound)
				{
					if (fragIn.id == _NearestPointID)
					{
						fragIn.color = float4(1, 0, 0, 1);
					}
				}

				return fragIn;
			}
			
			[maxvertexcount(4)]
			void geo(point VertOut p[1], inout TriangleStream<GeoOut> triStream)
			{
				p[0].color = float4(1, 0, 0, 1) * (p[0].selectMe) + p[0].color * !p[0].selectMe;

				float3 up      = UNITY_MATRIX_IT_MV[1].xyz;
				float3 tangent = UNITY_MATRIX_IT_MV[0].xyz;

				float4 v[4];
				v[0] = float4(p[0].pos - p[0].radius * tangent - _PointRadius * up, 1.0f);
				v[1] = float4(p[0].pos - p[0].radius * tangent + _PointRadius * up, 1.0f);
				v[2] = float4(p[0].pos + p[0].radius * tangent - _PointRadius * up, 1.0f);
				v[3] = float4(p[0].pos + p[0].radius * tangent + _PointRadius * up, 1.0f);
				
				GeoOut gout0;
				gout0.pos                 = UnityObjectToClipPos(v[0]) * (!p[0].deleteMe) * (p[0].visible);
				gout0.color               = p[0].color;
				gout0.uv				  = float2(-1,-1);
				gout0.viewposition        = mul(UNITY_MATRIX_MV, v[0]);
				gout0.intensityNormalized = p[0].intensityNormalized;
				gout0.id                  = p[0].id;
				gout0.sv_vertex_id        = p[0].sv_vertex_id;
				
				GeoOut gout1;
				gout1.pos                 = UnityObjectToClipPos(v[1]) * (!p[0].deleteMe) * (p[0].visible);
				gout1.color               = p[0].color;
				gout1.uv                  = float2(-1, 1);
				gout1.viewposition        = mul(UNITY_MATRIX_MV, v[1]);
				gout1.intensityNormalized = p[0].intensityNormalized;
				gout1.id                  = p[0].id;
				gout1.sv_vertex_id        = p[0].sv_vertex_id;
				
				GeoOut gout2;
				gout2.pos                 = UnityObjectToClipPos(v[2]) * (!p[0].deleteMe) * (p[0].visible);
				gout2.color               = p[0].color;
				gout2.uv                  = float2(1, -1);
				gout2.viewposition        = mul(UNITY_MATRIX_MV, v[2]);
				gout2.intensityNormalized = p[0].intensityNormalized;
				gout2.id                  = p[0].id;
				gout2.sv_vertex_id        = p[0].sv_vertex_id;

				GeoOut gout3;
				gout3.pos                 = UnityObjectToClipPos(v[3]) * (!p[0].deleteMe) * (p[0].visible);
				gout3.color               = p[0].color;
				gout3.uv                  = float2(1, 1);
				gout3.viewposition        = mul(UNITY_MATRIX_MV, v[3]);
				gout3.intensityNormalized = p[0].intensityNormalized;
				gout3.id                  = p[0].id;
				gout3.sv_vertex_id        = p[0].sv_vertex_id;
				
				triStream.Append(gout0);
				triStream.Append(gout1);
				triStream.Append(gout2);
				triStream.Append(gout3);
			}

		    // For Circles
			FragmentOutput frag(GeoOut geoOut)
			{
				FragmentOutput fragOut;
				float uvlen = geoOut.uv.x * geoOut.uv.x + geoOut.uv.y * geoOut.uv.y;

				if (_Circles == 1 && (uvlen) > 1)
				{
					discard;
				}

				geoOut.viewposition.z += ((1 - uvlen) * _PointRadius) * _Circles;
				float4 pos = mul(UNITY_MATRIX_P, geoOut.viewposition);
				pos /= pos.w;

				fragOut.depth = pos.z;

				fragOut.color = (geoOut.color - uvlen * _ZDepth) * _Circles + geoOut.color * !_Circles;
				fragOut.color = fragOut.color * geoOut.intensityNormalized * _AddIntensityValueToColor + fragOut.color * !_AddIntensityValueToColor;

				return fragOut;
			}
			ENDCG
		}
	}
}
