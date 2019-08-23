/*
	Author: Mikael Hertz (mikael.hertz@gmail.com)
*/
Shader "SLU/PointCloudXRnotGeo"
{
	// https://docs.unity3d.com/ScriptReference/MaterialPropertyDrawer.html
	Properties
	{
		[Header(Geometry)] _PointRadius("Geometry Size", Range(0.001, 0.1)) = 0.025
		[Toggle(POINT_ON)] _PointOn("Draw points", Int) = 0

		[Header(Color)] _Brightness("Brightness", Range(-1, 1)) = 0.0
		_ColorIntensity("Intensity", Range(1, 5)) = 1
		[Toggle] _AddIntensityValueToColor("Add intensity value to color", int) = 0
		[Toggle] _IntensityAsColor("Use intensity as color", int) = 0
		_SelectedPointColorBoost("Selected point color boost", Range(0, 0.5)) = 0
		[Toggle] _IntensityFilter("Intensity filter", int) = 0
		_IntensityFilterThresholdHighPass("Intensity threshold (High pass)", Range(0, 1)) = 0
		_IntensityFilterThresholdLowPass("Intensity threshold (Low pass)", Range(0, 1)) = 1
		[Toggle(INVERT_COLORS)] _InvertColors("Invert colors", Int) = 0

		[Header(Edit Tool)][Toggle] _EditToolActive("Activate Edit Tool", int) = 0
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
		[Header(Nearest Point)][Toggle] _FindNearestPoint("Find nearest point", int) = 0
		[Toggle] _ShowNearestPointFound("Show nearest point found", int) = 0
		[Header(Nearest Point)][Toggle] _TransformSelectedPoints("Transform selected points", int) = 0
		_SelectedPointsOffset("Points offset", Vector) = (0, 0, 0)

	}

	SubShader
	{
		Pass
	{
		Tags{ "RenderType" = "Opaque" }
		//LOD 200

		CGPROGRAM
		#pragma target 5.0
		#pragma vertex vert
		#pragma fragment frag
		//#pragma geometry geo

		#pragma shader_feature INVERT_COLORS
		#pragma shader_feature POINT_ON

		#pragma shader_feature SELECT

		#include "UnityCG.cginc"

		#define UNITY_SHADER_NO_UPGRADE 1 

		struct PointIn
	{
		float3	pos;
		float4  color;
		float	deleted;	// not used in shader
		float	selected;	// not used in shader
		float	intensityNormalized;
		float	classification;
		float	padding;
	};

	struct PointStatus
	{
		float	deleteMe;
		float	markMe;
	};

	struct NearestPoint
	{
		float4	position;
		float	distance;
		float	id;
	};

	struct TranslatedPoint
	{
		float3	offset;
	};

	uniform StructuredBuffer<PointIn>	computeBuffer					 : register(t1);
	RWStructuredBuffer<float>			computeBufferDelete			     : register(u1);
	RWStructuredBuffer<float>			computeBufferSelect			     : register(u2);
	RWStructuredBuffer<float>			computeBufferCom			     : register(u3);
	RWStructuredBuffer<TranslatedPoint>	computeBufferTranslatedPoints    : register(u4);
	RWStructuredBuffer<NearestPoint>	computeBufferNearestPoint	     : register(u5);
	RWStructuredBuffer<TranslatedPoint>	computeBufferOffsetPoints        : register(u6);

	struct FragmentInput // PS_INPUT
	{
		float4	pos				: POSITION;
		fixed4	color : COLOR;
		uint	deleteMe		: TEXCOORD1;
		uint	selectMe		: TEXCOORD2;
		float	classification : TEXCOORD3;
	};

	struct VertexInput
	{
		float4	pos		: POSITION;
		float4	color	: COLOR;
	};


	// **************************************************************
	// Vars															*
	// **************************************************************
	float	_ObjectDistanceFarBufferZone;
	float	_ObjectDistanceNear;
	int		_DecimatingFactor;
	int		_AnimatePointCloud;
	int		_VisualizeNormals;
	float	_PointRadius;
	float	_Brightness;
	float   _ColorIntensity;
	int		_IntensityFilter;
	float	_IntensityFilterThresholdHighPass;
	float	_IntensityFilterThresholdLowPass;
	int		_UseGeometricShader;
	float4  _EditToolPos;
	float	_EditToolRadius;
	int		_NumberOfPoints;
	float   _EditToolMode;
	int		_EditToolDelete;	// State
	int		_EditToolSelect;	// State
	int		_EditToolDeselect;  // State
	int		_EditToolActive;
	int		_AddIntensityValueToColor;
	int		_IntensityAsColor;
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

	// **************************************************************
	// Shader Programs												*
	// **************************************************************

	// Vertex Shader ------------------------------------------------
	FragmentInput vert(uint id: SV_VertexID) // MainVs
	{
		FragmentInput fragIn;

		fragIn.pos = UnityObjectToClipPos(float4(computeBuffer[id].pos, 1));
		//fragIn.pos = mul(unity_ObjectToWorld, float4(computeBuffer[id].pos, 1));

		fragIn.classification = computeBuffer[id].classification;

		fragIn.color = computeBuffer[id].color + _Brightness;

		if (_EditToolActive == 1)
		{
			float distanceFromEditTool = length(_EditToolPos.xyz - (fragIn.pos.xyz + computeBufferTranslatedPoints[id].offset.xyz + computeBufferOffsetPoints[id].offset.xyz));
			if (distanceFromEditTool <= _EditToolRadius /*&& computeBuffer[id].classification == _VegitationClass*/)
			{
				if (_EditToolDelete)
				{
					if (computeBufferDelete[id] == 0)
					{
						computeBufferDelete[id] = 1.0f;
						computeBufferCom[0] = 1.0f; // Set edit tool collision
					}
				}

				if (_EditToolSelect)
				{
					if (computeBufferSelect[id] == 0 && computeBufferDelete[id] == 0)
					{
						computeBufferCom[0] = 1.0f; // Set collision
						computeBufferSelect[id] = 1.0f;
					}
				}

				if (_EditToolDeselect)
				{
					if (computeBufferSelect[id] == 1 && computeBufferDelete[id] == 0)
					{
						computeBufferCom[0] = 1.0f; // Set collision
						computeBufferSelect[id] = 0.0f;
					}
				}
			}
		}

		fragIn.deleteMe = computeBufferDelete[id];
		fragIn.selectMe = computeBufferSelect[id];


		if (fragIn.selectMe == 1)
		{
			fragIn.color = _SelectedPointColor + _SelectedPointColorBoost;

			if (_TransformSelectedPoints == 1)
			{
				computeBufferTranslatedPoints[id].offset.x = _SelectedPointsOffset.x;// +computeBufferOffsetPoints[id].offset.x;
				computeBufferTranslatedPoints[id].offset.y = _SelectedPointsOffset.y;// +computeBufferOffsetPoints[id].offset.y;
				computeBufferTranslatedPoints[id].offset.z = _SelectedPointsOffset.z;// +computeBufferOffsetPoints[id].offset.z;
			}
		}
		/*fragIn.pos.x += computeBufferTranslatedPoints[id].offset.x + computeBufferOffsetPoints[id].offset.x;
		fragIn.pos.y += computeBufferTranslatedPoints[id].offset.y + computeBufferOffsetPoints[id].offset.y;
		fragIn.pos.z += computeBufferTranslatedPoints[id].offset.z + computeBufferOffsetPoints[id].offset.z;*/


		fragIn.color = (fragIn.color + _Brightness) * _ColorIntensity;

		if (computeBuffer[id].classification >= 64 && _ColorTreesByIntensity)
		{
			if (computeBuffer[id].intensityNormalized > _TreeIntensityThreshold)
			{
				fragIn.color = float4(_TreeColor, _TreeColor, _TreeColor, 1);
			}
			else
			{
				fragIn.color = float4(0, 1, 0, 1);
			}
		}

		if (_AddIntensityValueToColor)
		{
			fragIn.color = fragIn.color * computeBuffer[id].intensityNormalized;
		}

		if (_IntensityAsColor == 1)
		{
			if (fragIn.selectMe == 0)
			{
				fragIn.color.xyz = computeBuffer[id].intensityNormalized;
			}
		}


		// Check ground collision with User
		if (_UserPosition.y > computeBuffer[id].pos.y)
		{
			if (computeBuffer[id].classification == _GroundClass && fragIn.deleteMe != 1)
			{
				float distanceFromUser = length(_UserPosition.xyz - computeBuffer[id].pos.xyz);
				if (distanceFromUser < _GroundCollisionDistance)
				{
					computeBufferCom[1] = 1.0f; // Set User ground impact
				}
			}
		}

		if (_FindNearestPoint == 1 && fragIn.deleteMe != 1)
		{
			float shortestDistance = computeBufferNearestPoint[0].distance;

			float distanceFromSearchPoint = length(_FindNearestPointToPosition.xyz - fragIn.pos.xyz);
			if (distanceFromSearchPoint < shortestDistance)
			{
				computeBufferNearestPoint[0].position = fragIn.pos;
				computeBufferNearestPoint[0].distance = distanceFromSearchPoint;
				computeBufferNearestPoint[0].id = id;
			}
		}

		if (_ShowNearestPointFound == 1 && computeBufferNearestPoint[0].id == id)
		{
			fragIn.color = float4(1, 0, 0, 1);
		}

		return fragIn;
	}

	// Fragment Shader -----------------------------------------------
	float4 frag(FragmentInput fragIn) : COLOR
	{
		return fragIn.color;
	}

		ENDCG
	}
	}
}
