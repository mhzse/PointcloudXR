// One struct to rule them all...
struct PointXR
{
	float3 pos;
	float4 color;
	float  deleted;
	float  selected;
	float  intensityNormalized;
	float  classification;
	float  id;			// AppendBuffer in FrustumCulling scrambles point id. Must keep track
						// of point id like this to identify the correct global point.
						// Important for showing nearest point found.
	float4 user_color;
	float  scan_angle_rank;
	float  user_data;
	float  point_source_id;
	float  gps_time;

	float  visible;
	float  padding01;
	float  padding02;
	float  padding03;
};