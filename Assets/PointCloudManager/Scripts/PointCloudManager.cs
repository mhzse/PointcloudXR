/*
    Author: Mikael Hertz (mikael.hertz@gmail.com)
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using System.Threading;
using System.Linq;
using UnityEngine.Rendering;
using PCXRFile;
using LASParser;
using PointCloud.PointCloudConfig;


public struct PointXR
{
    float x;
    float y;
    float z;
    float red;
    float green;
    float blue;
    float alpha;
    float deleted;
    float selected;
    float intensityNormalized;
    float classification;
    float id;          // AppendBuffer in FrustumCulling scrambles point id. Must keep track
                       // of point id like this to identify the correct global point.
                       // Important for showing nearest point found.
    float user_red;
    float user_green;
    float user_blue;
    float user_alpha;
    float scan_angle_rank;
    float user_data;
    float point_source_id;
    float gps_time;

    float visible;
    float padding01;
    float padding02;
    float padding03;
};

public struct PointPos
{
    public Vector3 pos;
    public uint id;
};

public struct PointByte
{
    public byte val;
}

public struct LASPointXR
{
    public double X;
    public double Y;
    public double Z;
    public ushort Intensity;
    public byte Classification;
}

public struct DispatchArgs
{
    public uint x;
    public uint y;
    public uint z;
    public uint w;
};

public class PointCloudFormat
{
    public int NumberOfPoints { get; set; }
    public int Stride { get; set; }

    /*** Store original LAS variables ***/
    // LAS Specific variables BEGIN
    public double XScaleFactor { get; set; }
    public double YScaleFactor { get; set; }
    public double ZScaleFactor { get; set; }
    public double XOffset { get; set; }
    public double YOffset { get; set; }
    public double ZOffset { get; set; }
    public double MaxX { get; set; }
    public double MinX { get; set; }
    public double MaxY { get; set; }
    public double MinY { get; set; }
    public double MaxZ { get; set; }
    public double MinZ { get; set; }
    // LAS Specific variables END

    public int? AddIntensityValueToColor { get; set; }
    public int? IntensityAsColor { get; set; }
    public float? AddIntensityToColor { get; set; }
    public float UseUserColor { get; set; }
    public float? GeometrySize { get; set; }
    public float UserColorAsColor { get; set; }


    public PointCloudFormat() { }
}

public class PointCloudManager : MonoBehaviour
{
    public bool _draw_all_cameras = false;

    public Material _Material;
    public float _ShaderZDepth;
    private ComputeBuffer _COMPUTE_BUFFER_POINTS;    // Point cloud data
    private ComputeBuffer _COMPUTE_BUFFER_COM;       // Communication buffer
    private ComputeBuffer _COMPUTE_BUFFER_NEAREST_POINT_ID;
    private ComputeBuffer _COMPUTE_BUFFER_NEAREST_POINT_POSITION;
    private ComputeBuffer _COMPUTE_BUFFER_TRANSLATED_POINTS; // Offsets for points that are being translated
    private ComputeBuffer _COMPUTE_BUFFER_OFFSET_POINTS;     // Offsets for previously translated points, need to store these
                                                             // in case they are being translated again.

    private PointCloudFormat _PointCloudFormat;
    private float[] _COMPUTE_BUFFER_POINTS_DATA;
    private byte[] _COMPUTE_BUFFER_POINTS_DATA_BYTES;
    private float[] _COMPUTE_BUFFER_COM_DATA;
    private float[] _COMPUTE_BUFFER_NEAREST_POINT_DATA;
    private float[] _COMPUTE_BUFFER_TRANSLATED_POINTS_DATA;
    private float[] _COMPUTE_BUFFER_OFFSET_POINTS_DATA;

    private byte[] _pruned_points_byte_data;

    // Flags for threaded execution
    private bool _PointCloudLoaded = false;
    private bool _PointCloudSaved = false;
    private bool _PointCloudExported = false;

    public event EventHandler<EventArgs> OnEditToolCollide;
    public event EventHandler<EventArgs> OnUserGroundCollide;
    public event EventHandler<EventArgs> OnUserGroundCollideIn;
    public event EventHandler<EventArgs> OnUserGroundCollideOut;
    public event EventHandler<EventArgs> OnNearestPointFound;

    public event EventHandler<EventArgs> OnGPUDataTransfereDone;
    public event EventHandler<EventArgs> OnGPUSetDataDone;
    public event EventHandler<EventArgs> OnPointCloudLoaded;
    public event EventHandler<EventArgs> OnPointCloudSaved;
    public event EventHandler<EventArgs> OnPointCloudExported;
    public event EventHandler<EventArgs> OnProgress;

    private FileInfo[] _FilesImportLAS;
    private FileInfo[] _FilesImportTXT;
    private FileInfo[] _FilesSerialized;
    private FileInfo[] _FilesExport;


    private bool _COMPUTE_BUFFER_CREATED = false;
    private bool _GPU_DATA_LOADED = false;

    public enum PointCloudSize { SMALL, MEDIUM, LARGE };

    private string _ReadFilename;
    private string _WriteFilename;

    public bool _ActivateEditTool = false;
    public EditToolMode _EditToolAction;

    public int _POINTS_IN_CLOUD;   // Number of points in the cloud
    public int[] _POINTS_IN_FRUSTUM; // Currently number of visible points in the cloud, used for frustum culling of points.

    // Status variables when loading poincloud from file
    private float _Progress = 0;
    private string _GUIText;
    private bool _UPDATE_PROGRESS_IN_GUI = false;

    private int _STRIDE = 24;
    private int _RGB_SCALE_LAS = 65535;

    public GameObject _PointCloudPosition;
    private Vector3 _PointCloudInitialPosition;
    public GameObject _UserGameObject;
    public Camera _UserCamera;
    public GameObject _CollisionGameObject;
    private Vector3 _UserStartPosition;
    private Quaternion _UserStartRotation;

    public int _EditToolCollision = 0;
    public int _UserGroundCollision = 0;
    private int _UserGroundCollisionPrev = 0;

    public bool _ExportToLAS = false;
    public bool _SaveSelected = false;
    public bool _ExportSelected = false;
    public bool _FindNearestPoint = false;
    private bool _FindNearestPointPrev = false; // Detect transition
    public GameObject _SnapToPointGameObject;
    private Vector3[] _FindNearestPointFilterArray;
    private int _FindNearestPointFilterCount = 0;
    private static int _FindNearestPointFilterCountMax = 5;
    private Vector3 _FindNearestPointToPosition;
    private Vector3 _NearestPointFound;
    private bool _ShowNearestPoint = false;

    private Dictionary<Camera, CommandBuffer> _CameraMap = new Dictionary<Camera, CommandBuffer>();
    CameraEvent _CamEventDrawProcedural = CameraEvent.BeforeForwardOpaque;

    private Vector3 _CentroidSelectedPointsStartPosition;
    private GameObject _CentroidSelectedPoints;

    private bool _OctreeDrawGizmos = false;
    private bool _OctreeDrawEmptyNodes = false;
    public byte _OctreeDrawMaxDepth { get; set; } = 5;

    public int _ComputeBufferSetDataChunkSize = 50000;

    public ComputeShader _FindNearestPointCompute;
    private ComputeBuffer _COMPUTE_BUFFER_CS_POINTS_POSITION; // Only position data, faster buffer with smaller stride.
    private ComputeBuffer _COMPUTE_BUFFER_CS_DISTANCE_TO_POINT;
    private ComputeBuffer _COMPUTE_BUFFER_CS_DISTANCE_TO_POINT_REDUCE;
    public int _CS_NEAREST_POINT_THREADS;
    public int _CS_BATCH_SIZE;// = 16;
    public ComputeBuffer _COMPUTE_BUFFER_CS_DEBUG;
    public ComputeBuffer _COMPUTE_BUFFER_CS_DISPATH_INDIRECT_ARGS;
    public int _CS_NUM_GROUPS;

    public bool _show_number_of_points_on_screen;
    public bool _culling_enabled;
    public ComputeShader _PointsFrustumCullingCompute;
    private ComputeBuffer _COMPUTE_BUFFER_CS_POINTS_BUFFER_APPEND;
    public int _DISPATCH_POINTS_FRUSTUM_CULLING_NTH_FRAME = 1;
    private int _DISPATCH_POINTS_FRUSTUM_CULLING_NTH_FRAME_COUNTER = 0;

    private ComputeBuffer _COMPUTE_BUFFER_CS_DRAWPROCEDURAL_INDIRECT_ARGS; // Used in OnRenderObject to draw procedural geometry 
                                                                           // from buffer with dynamic size. Buffer managed by 
                                                                           // PointsFrustumCulling compute shader.
    private ComputeBuffer _COMPUTE_BUFFER_CS_POINTS_BUFFER_APPEND_COUNT;   // For debugging

    private int _CS_POINTS_BUFFER_THREADS = 1024;

    public float _CSFrustumCullMargin;

    public ComputeShader _PointGeneratorCompute;
    public ComputeShader _LAS_to_PCXR;
    public ComputeShader _PCXR_to_LAS; // TODO: Implement export LAS function using this...
    private ComputeBuffer _COMPUTE_BUFFER_CS_LAS_RAW_BYTES;
    private byte[] _COMPUTE_BUFFER_CS_LAS_POINTS_BYTES_DATA;
    public int _LasPointNummerDebug;
    private LASHeader _LAS_HEADER;

    public bool _UsePointsByteData;
    public uint _PGPoints;
    public float _PGSpacing;
    public float _LOD1Distance;
    private bool _GeneratedPoints = false;

    private AsyncGPUReadbackRequest _AsyncGPURequest;
    private AsyncGPUReadbackRequest _AsyncGPU_TransferGPU_Data_Request;
    private bool _TransferGPUData = false;
    private System.Diagnostics.Stopwatch _TransferGPUData_Simple_stopwatch;

    public ComputeShader _EditPointsCompute;

    private ComputeBuffer _POINT_CLASSES_COMPUTE_BUFFER;
    private ComputeBuffer _POINT_SELECTED_INDEX_BUFFER;

    public ComputeShader _PointClassesCompute;
    private float[] _point_classes; // Clasifications that are used by the point cloud (populated by compute shader)

    public ComputeShader _prune_pcxr;

    private Config _config;
    private string _config_file_path;

    private void Awake()
    {
        _config_file_path = Application.dataPath + @"/StreamingAssets/conf/PointCloudXR/config.json";
        _config = LoadConfig(_config_file_path);

        _POINTS_IN_FRUSTUM = new int[1];

        OnPointCloudLoaded += OnPointCloudLoadedListener;
        OnGPUSetDataDone += OnGPUSetDataListener;

        _Material.SetFloat("_ZDepth", _ShaderZDepth);

        _UserStartPosition = new Vector3(0.0f, 0.6f, 0.0f);

        if (_WriteFilename == null)
        {
            _WriteFilename = _ReadFilename;
        }

        _FindNearestPointFilterArray = new Vector3[_FindNearestPointFilterCountMax];
        for (int i = 0; i < _FindNearestPointFilterArray.Length; i++)
        {
            _FindNearestPointFilterArray[i] = Vector3.zero;
        }

        if (_config.paths.root_file_path == "")
        {
            _config.paths.root_file_path = Application.dataPath + @"/StreamingAssets/PointClouds/";
        }

        // Make sure all directories exist
        if (!Directory.Exists(_config.paths.root_file_path))
        {
            Directory.CreateDirectory(_config.paths.root_file_path);
        }

        if (!Directory.Exists(_config.paths.root_file_path + @"\export"))
        {
            Directory.CreateDirectory(_config.paths.root_file_path + @"\export");
        }

        if (!Directory.Exists(_config.paths.root_file_path + @"\serialized"))
        {
            Directory.CreateDirectory(_config.paths.root_file_path + @"\serialized");
        }

        if (!Directory.Exists(_config.paths.root_file_path + @"\import"))
        {
            Directory.CreateDirectory(_config.paths.root_file_path + @"\import");
            Directory.CreateDirectory(_config.paths.root_file_path + @"\import\LAS");
        }
        else
        {
            if (!Directory.Exists(_config.paths.root_file_path + @"\import\LAS"))
            {
                Directory.CreateDirectory(_config.paths.root_file_path + @"\import\LAS");
            }
        }

        FilesUpdate();
    }

    private Config LoadConfig(string path)
    {
        Config config = new Config();

        string json_data = File.ReadAllText(path);
        config = JsonUtility.FromJson<Config>(json_data);

        return config;
    }

    /***
     * This is a quickfix and is an approximation of the boundaries. Reuses the compute shader for finding the nearest point. 
     * The point cloud is centered at the origin... 
     ***/
    public Vector3[] GetBoundaries()
    {
        Vector3[] boundaries = new Vector3[2];

        Vector3 max = new Vector3();
        Vector3 min = new Vector3();

        max.x = GetNearestPointPosition(new Vector3(1000f, 0, 0)).x;
        max.y = GetNearestPointPosition(new Vector3(0, 1000f, 0)).y;
        max.z = GetNearestPointPosition(new Vector3(0, 0, 1000f)).z;

        min.x = GetNearestPointPosition(new Vector3(-1000f, 0, 0)).x;
        min.y = GetNearestPointPosition(new Vector3(0, -1000f, 0)).y;
        min.z = GetNearestPointPosition(new Vector3(0, 0, -1000f)).z;

        boundaries[0] = max;
        boundaries[1] = min;

        return boundaries;
    }

    public Vector3[] GetBoundariesSelected()
    {
        Vector3[] boundaries = new Vector3[2];

        Vector3 max = new Vector3();
        Vector3 min = new Vector3();

        max.x = GetNearestPointPosition(new Vector3(1000f, 0, 0), true).x;
        max.y = GetNearestPointPosition(new Vector3(0, 1000f, 0), true).y;
        max.z = GetNearestPointPosition(new Vector3(0, 0, 1000f), true).z;

        min.x = GetNearestPointPosition(new Vector3(-1000f, 0, 0), true).x;
        min.y = GetNearestPointPosition(new Vector3(0, -1000f, 0), true).y;
        min.z = GetNearestPointPosition(new Vector3(0, 0, -1000f), true).z;

        boundaries[0] = max;
        boundaries[1] = min;

        return boundaries;
    }

    public void PointsFrustumCullingActive(bool active)
    {
        _culling_enabled = active;
        if (!_culling_enabled)
        {
            _COMPUTE_BUFFER_CS_POINTS_BUFFER_APPEND.SetCounterValue((uint)_POINTS_IN_CLOUD);
            _COMPUTE_BUFFER_CS_POINTS_BUFFER_APPEND.SetData(_COMPUTE_BUFFER_POINTS_DATA_BYTES);
            ComputeBuffer.CopyCount(_COMPUTE_BUFFER_CS_POINTS_BUFFER_APPEND, _COMPUTE_BUFFER_CS_DRAWPROCEDURAL_INDIRECT_ARGS, 0);
            ComputeBuffer.CopyCount(_COMPUTE_BUFFER_CS_POINTS_BUFFER_APPEND, _COMPUTE_BUFFER_CS_POINTS_BUFFER_APPEND_COUNT, 0);
            _COMPUTE_BUFFER_CS_POINTS_BUFFER_APPEND_COUNT.GetData(_POINTS_IN_FRUSTUM);
        }
    }

    public byte[] Dispatch_PCXR_to_LAS(LASHeader14 las_header)
    {
        int num_groups = Mathf.CeilToInt(_POINTS_IN_CLOUD / (float)_CS_POINTS_BUFFER_THREADS);
        int kernel = _PCXR_to_LAS.FindKernel("PCXR_TO_LAS14PDR7");

        // LAS 1.4 PDRF 7 -> 36 bytes
        _COMPUTE_BUFFER_CS_LAS_RAW_BYTES = new ComputeBuffer((int)las_header.NumberOfPointRecords, 36, ComputeBufferType.Default);
        _PCXR_to_LAS.SetBuffer(kernel, "_LAS14PDR7", _COMPUTE_BUFFER_CS_LAS_RAW_BYTES);

        _PCXR_to_LAS.SetBuffer(kernel, "_Points", _COMPUTE_BUFFER_POINTS);


        ComputeBuffer las_header_buffer = new ComputeBuffer(1, 12 * sizeof(double), ComputeBufferType.Default);

        double[] las_header_data = new double[12];
        las_header_data[0] = las_header.Xscalefactor;
        las_header_data[1] = las_header.Yscalefactor;
        las_header_data[2] = las_header.Zscalefactor;
        las_header_data[3] = las_header.Xoffset;
        las_header_data[4] = las_header.Yoffset;
        las_header_data[5] = las_header.Zoffset;
        las_header_data[6] = las_header.MaxX;
        las_header_data[7] = las_header.MinX;
        las_header_data[8] = las_header.MaxY;
        las_header_data[9] = las_header.MinY;
        las_header_data[10] = las_header.MaxZ;
        las_header_data[11] = las_header.MinZ;

        las_header_buffer.SetData(las_header_data);
        _PCXR_to_LAS.SetBuffer(kernel, "_LAS12Header", las_header_buffer);


        _PCXR_to_LAS.Dispatch(kernel, num_groups, 1, 1);

        byte[] las_point_data = new byte[36 * (int)las_header.NumberOfPointRecords];
        _COMPUTE_BUFFER_CS_LAS_RAW_BYTES.GetData(las_point_data);

        return las_point_data;
    }

    public int[] GetSelectedPointsIndex()
    {
        int num_groups = Mathf.CeilToInt(_POINTS_IN_CLOUD / (float)_CS_POINTS_BUFFER_THREADS);
        int kernel = _EditPointsCompute.FindKernel("FindSelectedPoints");

        _POINT_SELECTED_INDEX_BUFFER = new ComputeBuffer(_POINTS_IN_CLOUD, sizeof(int), ComputeBufferType.Append);
        _POINT_SELECTED_INDEX_BUFFER.SetCounterValue(0);

        ComputeBuffer selected_points_count = new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);

        _EditPointsCompute.SetBuffer(kernel, "_Points", _COMPUTE_BUFFER_POINTS);
        _EditPointsCompute.SetBuffer(kernel, "_selected_points_index", _POINT_SELECTED_INDEX_BUFFER);

        _EditPointsCompute.Dispatch(kernel, num_groups, 1, 1);


        int[] selected_count = new int[] { 0 };
        ComputeBuffer.CopyCount(_POINT_SELECTED_INDEX_BUFFER, selected_points_count, 0);

        selected_points_count.GetData(selected_count);

        int[] index_arr = new int[selected_count[0]];
        _POINT_SELECTED_INDEX_BUFFER.GetData(index_arr);

        selected_points_count.Dispose();

        return index_arr;
    }

    private int GetSelectedPointsCount()
    {
        int num_groups = Mathf.CeilToInt(_POINTS_IN_CLOUD / (float)_CS_POINTS_BUFFER_THREADS);
        int kernel = _EditPointsCompute.FindKernel("FindSelectedPoints");

        ComputeBuffer selected_index_buffer = new ComputeBuffer(_POINTS_IN_CLOUD, sizeof(int), ComputeBufferType.Append);
        selected_index_buffer.SetCounterValue(0);

        ComputeBuffer selected_points_count = new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);

        _EditPointsCompute.SetBuffer(kernel, "_Points", _COMPUTE_BUFFER_POINTS);
        _EditPointsCompute.SetBuffer(kernel, "_selected_points_index", selected_index_buffer);

        _EditPointsCompute.Dispatch(kernel, num_groups, 1, 1);


        int[] selected_count = new int[] { 0 };
        ComputeBuffer.CopyCount(selected_index_buffer, selected_points_count, 0);

        selected_points_count.GetData(selected_count);

        selected_index_buffer.Dispose();
        selected_points_count.Dispose();

        return selected_count[0];
    }

    public byte[] GetSelectedPointsByte()
    {
        int num_groups = Mathf.CeilToInt(_POINTS_IN_CLOUD / (float)_CS_POINTS_BUFFER_THREADS);
        int kernel = _EditPointsCompute.FindKernel("GetSelectedPoints");

        ComputeBuffer selected_points_buffer = new ComputeBuffer(_POINTS_IN_CLOUD, sizeof(float) * _STRIDE, ComputeBufferType.Append);
        selected_points_buffer.SetCounterValue(0);

        _EditPointsCompute.SetBuffer(kernel, "_Points", _COMPUTE_BUFFER_POINTS);
        _EditPointsCompute.SetBuffer(kernel, "_selected_points", selected_points_buffer);

        _EditPointsCompute.Dispatch(kernel, num_groups, 1, 1);


        ComputeBuffer selected_points_count = new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);
        int[] selected_count = new int[] { 0 };
        ComputeBuffer.CopyCount(selected_points_buffer, selected_points_count, 0);
        selected_points_count.GetData(selected_count);

        int number_of_bytes = sizeof(float) * _STRIDE * selected_count[0];
        byte[] selected_points_bytes = new byte[number_of_bytes];


        selected_points_buffer.GetData(selected_points_bytes);

        return selected_points_bytes;
    }

    public void TranslateSelectedPoints(int count)
    {
        int num_groups = Mathf.CeilToInt(count / (float)_CS_POINTS_BUFFER_THREADS);
        int kernel = _EditPointsCompute.FindKernel("TranslateSelectedPoints");

        _EditPointsCompute.SetBuffer(kernel, "_Points", _COMPUTE_BUFFER_POINTS);

    }

    public void ColorByClass()
    {
        int num_groups = Mathf.CeilToInt(_POINTS_IN_CLOUD / (float)_CS_POINTS_BUFFER_THREADS);
        int kernel = _EditPointsCompute.FindKernel("ColorByClass");

        Vector4[] class_colors = new Vector4[256];
        for (int i = 0; i < 256; i++)
        {
            Color c = new Color
            (
                UnityEngine.Random.Range(0f, 1f),
                UnityEngine.Random.Range(0f, 1f),
                UnityEngine.Random.Range(0f, 1f),
                1
            );
            class_colors[i] = c;
        }
        _EditPointsCompute.SetVectorArray("_class_colors", class_colors);

        _EditPointsCompute.SetBuffer(kernel, "_Points", _COMPUTE_BUFFER_POINTS);
        _EditPointsCompute.Dispatch(kernel, num_groups, 1, 1);
    }

    public void ColorByPointsourceID()
    {
        int num_groups = Mathf.CeilToInt(_POINTS_IN_CLOUD / (float)_CS_POINTS_BUFFER_THREADS);
        int kernel = _EditPointsCompute.FindKernel("ColorByPointsourceID");

        Vector4[] pointsource_colors = new Vector4[2048];
        for (int i = 0; i < 2048; i++)
        {
            Color c = new Color
            (
                UnityEngine.Random.Range(0f, 1f),
                UnityEngine.Random.Range(0f, 1f),
                UnityEngine.Random.Range(0f, 1f),
                1
            );
            pointsource_colors[i] = c;
        }
        _EditPointsCompute.SetVectorArray("_pointsource_id_colors", pointsource_colors);

        _EditPointsCompute.SetBuffer(kernel, "_Points", _COMPUTE_BUFFER_POINTS);
        _EditPointsCompute.Dispatch(kernel, num_groups, 1, 1);
    }

    public void ColorByHeight()
    {
        int num_groups = Mathf.CeilToInt(_POINTS_IN_CLOUD / (float)_CS_POINTS_BUFFER_THREADS);
        int kernel = _EditPointsCompute.FindKernel("ColorByHigth");

        _EditPointsCompute.SetVector("_color_0", Color.blue);
        _EditPointsCompute.SetVector("_color_1", Color.green);
        _EditPointsCompute.SetVector("_color_2", Color.yellow);
        _EditPointsCompute.SetVector("_color_3", Color.red);

        float hight_interval = (float)(_PointCloudFormat.MaxY - _PointCloudFormat.MinY);
        _EditPointsCompute.SetFloat("_hight_interval", hight_interval);
        _EditPointsCompute.SetFloat("_min_y", (float)_PointCloudFormat.MinY);

        _EditPointsCompute.SetBuffer(kernel, "_Points", _COMPUTE_BUFFER_POINTS);
        _EditPointsCompute.Dispatch(kernel, num_groups, 1, 1);
    }

    public void ColorByIntensity()
    {
        int num_groups = Mathf.CeilToInt(_POINTS_IN_CLOUD / (float)_CS_POINTS_BUFFER_THREADS);
        int kernel = _EditPointsCompute.FindKernel("ColorByIntensity");

        _EditPointsCompute.SetBuffer(kernel, "_Points", _COMPUTE_BUFFER_POINTS);
        _EditPointsCompute.Dispatch(kernel, num_groups, 1, 1);
    }

    public void ColorByNone()
    {
        int num_groups = Mathf.CeilToInt(_POINTS_IN_CLOUD / (float)_CS_POINTS_BUFFER_THREADS);
        int kernel = _EditPointsCompute.FindKernel("ColorByNone");

        _EditPointsCompute.SetBuffer(kernel, "_Points", _COMPUTE_BUFFER_POINTS);
        _EditPointsCompute.Dispatch(kernel, num_groups, 1, 1);
    }

    public void SwapYZ()
    {
        int num_groups = Mathf.CeilToInt(_POINTS_IN_CLOUD / (float)_CS_POINTS_BUFFER_THREADS);
        int kernel = _EditPointsCompute.FindKernel("SwapYZ");

        _EditPointsCompute.SetBuffer(kernel, "_Points", _COMPUTE_BUFFER_POINTS);
        _EditPointsCompute.Dispatch(kernel, num_groups, 1, 1);
    }

    public void UserColorActive(bool active)
    {
        if (active)
        {
            _Material.SetInt("_user_color", 1);
        }
        else
        {
            _Material.SetInt("_user_color", 0);
        }
    }

    public void TogglePointClassVisibility(float class_id)
    {
        int num_groups = Mathf.CeilToInt(_POINTS_IN_CLOUD / (float)_CS_POINTS_BUFFER_THREADS);
        int kernel = _EditPointsCompute.FindKernel("TogglePointClassVisibility");

        _EditPointsCompute.SetFloat("_toggle_class", class_id);
        _EditPointsCompute.SetBuffer(kernel, "_Points", _COMPUTE_BUFFER_POINTS);
        _EditPointsCompute.Dispatch(kernel, num_groups, 1, 1);

        print($"TogglePointClassVisibility: {class_id}\n");
    }

    public Material GetMaterial()
    {
        return _Material;
    }

    public void DispatchPrunePCXR()
    {
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

        int num_groups = Mathf.CeilToInt(_POINTS_IN_CLOUD / (float)_CS_POINTS_BUFFER_THREADS);
        int kernel = _prune_pcxr.FindKernel("RemoveDeletedPoints");

        ComputeBuffer pruned_points = new ComputeBuffer(_POINTS_IN_CLOUD, sizeof(float) * _STRIDE, ComputeBufferType.Append);
        ComputeBuffer pruned_points_count = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);

        _prune_pcxr.SetBuffer(kernel, "_points", _COMPUTE_BUFFER_POINTS);
        _prune_pcxr.SetBuffer(kernel, "_pruned_points", pruned_points);

        pruned_points.SetCounterValue(0);

        _prune_pcxr.Dispatch(kernel, num_groups, 1, 1);

        int[] pruned_points_size = new int[1];

        ComputeBuffer.CopyCount(pruned_points, pruned_points_count, 0);
        pruned_points_count.GetData(pruned_points_size);

        int size = pruned_points_size[0] * _PointCloudFormat.Stride * sizeof(float);
        _pruned_points_byte_data = new byte[size];

        // Must have a limit on the maximun number of points to get. In case where none where pruned
        // and all threads generated points, possible that more points are added to the array than
        // exists in the cloud. Has to do with the variable NUM_THREADS in the shader.
        int max_size = _POINTS_IN_CLOUD * _STRIDE * sizeof(float);

        pruned_points.GetData(_pruned_points_byte_data);

        stopwatch.Stop();
        TimeSpan t = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds);
        string humanReadableTime = string.Format("{0:D2}h:{1:D2}m:{2:D2}s:{3:D3}ms",
                                t.Hours,
                                t.Minutes,
                                t.Seconds,
                                t.Milliseconds);
        Debug.LogFormat("DispatchPrunePCXR - runtime: {0}\n", humanReadableTime);
    }

    public void DispatchCountClasses()
    {
        int num_groups = Mathf.CeilToInt(_POINTS_IN_CLOUD / (float)_CS_POINTS_BUFFER_THREADS);
        int kernel = _PointClassesCompute.FindKernel("CountClasses");

        _PointClassesCompute.SetBuffer(kernel, "_Points", _COMPUTE_BUFFER_POINTS);
        _PointClassesCompute.SetBuffer(kernel, "_Classes", _POINT_CLASSES_COMPUTE_BUFFER);

        _PointClassesCompute.Dispatch(kernel, num_groups, 1, 1);

        _POINT_CLASSES_COMPUTE_BUFFER.GetData(_point_classes);
    }

    public void SetSelectedPointsClass(int class_id)
    {
        int num_groups = Mathf.CeilToInt(_POINTS_IN_CLOUD / (float)_CS_POINTS_BUFFER_THREADS);
        int kernel = _PointClassesCompute.FindKernel("SetSelectedClass");

        _PointClassesCompute.SetInt("_class_to_set", class_id);
        _PointClassesCompute.SetBuffer(kernel, "_Points", _COMPUTE_BUFFER_POINTS);

        _PointClassesCompute.Dispatch(kernel, num_groups, 1, 1);
    }

    public float[] GetPointClasses()
    {
        return _point_classes;
    }

    public Vector3 GetNearestPointPosition(Vector3 position, bool use_selected = false)
    {
        PointPos nearest_position = SearchNearestPoint(position,
            _COMPUTE_BUFFER_POINTS,
            use_selected);

        _Material.SetInt("_NearestPointID", (int)nearest_position.id);
        return nearest_position.pos;
    }


    public void CullingActive(bool active)
    {
        _culling_enabled = active;
    }

    public void DispatchLasToPCXR(LASHeader las_header)
    {
        float threads = 1024;
        int num_groups = Mathf.CeilToInt(las_header.Numberofpointrecords / threads);
        int kernel = -1;

        switch (las_header.PointDataFormatID)
        {
            case 0:
                kernel = _LAS_to_PCXR.FindKernel("LAS12PDR0");
                _LAS_to_PCXR.SetBuffer(kernel, "_LAS12PDR0", _COMPUTE_BUFFER_CS_LAS_RAW_BYTES);
                break;
            case 1:
                kernel = _LAS_to_PCXR.FindKernel("LAS12PDR1");
                _LAS_to_PCXR.SetBuffer(kernel, "_LAS12PDR1", _COMPUTE_BUFFER_CS_LAS_RAW_BYTES);
                break;
            case 2:
                kernel = _LAS_to_PCXR.FindKernel("LAS12PDR2");
                _LAS_to_PCXR.SetBuffer(kernel, "_LAS12PDR2", _COMPUTE_BUFFER_CS_LAS_RAW_BYTES);
                break;
            case 3:
                kernel = _LAS_to_PCXR.FindKernel("LAS12PDR3");
                _LAS_to_PCXR.SetBuffer(kernel, "_LAS12PDR3", _COMPUTE_BUFFER_CS_LAS_RAW_BYTES);
                break;
        }

        _LAS_to_PCXR.SetBuffer(kernel, "_Points", _COMPUTE_BUFFER_POINTS);

        ComputeBuffer las_header_buffer = new ComputeBuffer((int)_LAS_HEADER.Numberofpointrecords, 12 * sizeof(double), ComputeBufferType.Default);

        double[] las_header_data = new double[12];
        las_header_data[0] = las_header.Xscalefactor;
        las_header_data[1] = las_header.Yscalefactor;
        las_header_data[2] = las_header.Zscalefactor;
        las_header_data[3] = las_header.Xoffset;
        las_header_data[4] = las_header.Yoffset;
        las_header_data[5] = las_header.Zoffset;
        las_header_data[6] = las_header.MaxX;
        las_header_data[7] = las_header.MinX;
        las_header_data[8] = las_header.MaxY;
        las_header_data[9] = las_header.MinY;
        las_header_data[10] = las_header.MaxZ;
        las_header_data[11] = las_header.MinZ;

        las_header_buffer.SetData(las_header_data);

        _LAS_to_PCXR.SetBuffer(kernel, "_LAS12Header", las_header_buffer);

        _LAS_to_PCXR.Dispatch(kernel, num_groups, 1, 1);

        Vector3[] bounds = GetBoundaries();
        _PointCloudFormat.MinX = bounds[1].x;
        _PointCloudFormat.MinY = bounds[1].y;
        _PointCloudFormat.MinZ = bounds[1].z;
        _PointCloudFormat.MaxX = bounds[0].x;
        _PointCloudFormat.MaxY = bounds[0].y;
        _PointCloudFormat.MaxZ = bounds[0].z;
    }

    public int GetPointsInFrustum()
    {
        if (_culling_enabled)
        {
            return _POINTS_IN_FRUSTUM[0];
        }
        else
        {
            return _POINTS_IN_CLOUD;
        }
    }

    public PointPos SearchNearestPoint(
        Vector3 fromPosition,
        ComputeBuffer points,
        bool use_selected_points = false)
    {
        int point_count = points.count;

        ComputeBuffer points_position_buffer = new ComputeBuffer(point_count, sizeof(float) * 3 + sizeof(uint), ComputeBufferType.Default);

        if (use_selected_points)
        {
            int kernel_create_selected_points_append_buffer = _FindNearestPointCompute.FindKernel("CreateSelectedPointsAppendBuffer");

            ComputeBuffer selected_points_append_buffer = new ComputeBuffer(
                point_count,
                sizeof(float) * _STRIDE,
                ComputeBufferType.Append);

            selected_points_append_buffer.SetCounterValue(0);

            _FindNearestPointCompute.SetBuffer(
                kernel_create_selected_points_append_buffer,
                "_PointsBuffer",
                points);

            _FindNearestPointCompute.SetBuffer(
                kernel_create_selected_points_append_buffer,
                "_selected_points_append",
                selected_points_append_buffer);

            int num_groups_create_selected = Mathf.CeilToInt(point_count / (float)_CS_POINTS_BUFFER_THREADS);
            _FindNearestPointCompute.Dispatch(kernel_create_selected_points_append_buffer, num_groups_create_selected, 1, 1);

            ComputeBuffer selected_points_count = new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);
            int[] selected_count = new int[] { 0 };

            ComputeBuffer.CopyCount(selected_points_append_buffer, selected_points_count, 0);
            selected_points_count.GetData(selected_count);

            // Need the total number of selected points
            point_count = selected_count[0];


            int kernel_create_points_selected_position_buffer = _FindNearestPointCompute.FindKernel("CreatePointsSelectedPositionBuffer");
            _FindNearestPointCompute.SetBuffer(
                kernel_create_points_selected_position_buffer,
                "_selected_points_consume",
                selected_points_append_buffer);

            _FindNearestPointCompute.SetInt("_selected_points_count", point_count);


            points_position_buffer = new ComputeBuffer(point_count, sizeof(float) * 3 + sizeof(uint), ComputeBufferType.Default);

            // Some duplicate code here. Need this in order to dispose compute buffers...
            _FindNearestPointCompute.SetBuffer(kernel_create_points_selected_position_buffer, "_PointsBuffer", points);
            _FindNearestPointCompute.SetBuffer(kernel_create_points_selected_position_buffer, "_PointsPositionBuffer", points_position_buffer);

            int num_groups_create_points_position = Mathf.CeilToInt(point_count / (float)_CS_NEAREST_POINT_THREADS);
            _FindNearestPointCompute.Dispatch(kernel_create_points_selected_position_buffer, num_groups_create_points_position, 1, 1);


            selected_points_append_buffer.Dispose();
            selected_points_count.Dispose();
        }
        else
        {
            int kernel_create_points_position_buffer = _FindNearestPointCompute.FindKernel("CreatePointsPositionBuffer");

            _FindNearestPointCompute.SetBuffer(kernel_create_points_position_buffer, "_PointsBuffer", points);
            _FindNearestPointCompute.SetBuffer(kernel_create_points_position_buffer, "_PointsPositionBuffer", points_position_buffer);

            int num_groups_create_points_position = Mathf.CeilToInt(point_count / (float)_CS_NEAREST_POINT_THREADS);
            _FindNearestPointCompute.Dispatch(kernel_create_points_position_buffer, num_groups_create_points_position, 1, 1);
        }


        /*** ComputeDistanceToPoints ***/
        int kernelComputeDistanceToPoints = _FindNearestPointCompute.FindKernel("ComputeDistanceToPoints");
        ComputeBuffer distance_buffer = new ComputeBuffer(point_count, sizeof(float) + sizeof(uint), ComputeBufferType.Default);

        _FindNearestPointCompute.SetVector("_NearestPointFromPosition", fromPosition);
        _FindNearestPointCompute.SetBuffer(kernelComputeDistanceToPoints, "_DistanceBuffer", distance_buffer);
        _FindNearestPointCompute.SetBuffer(kernelComputeDistanceToPoints, "_PointsPositionBuffer", points_position_buffer);

        int num_groups_compute_dist = Mathf.CeilToInt(point_count / (float)_CS_NEAREST_POINT_THREADS);
        _FindNearestPointCompute.Dispatch(kernelComputeDistanceToPoints, num_groups_compute_dist, 1, 1);

        /*** Execute ReduceDistance ***/
        int num_threads = _CS_NEAREST_POINT_THREADS;
        int dist_reduce_groups = Mathf.FloorToInt(point_count / ((float)num_threads * 2));

        int kernel = _FindNearestPointCompute.FindKernel("ReduceDistance");

        ComputeBuffer distance_reduce_buffer = new ComputeBuffer(dist_reduce_groups, sizeof(float) + sizeof(uint), ComputeBufferType.Default);

        _FindNearestPointCompute.SetInt("dist_reduce_groups", dist_reduce_groups);
        _FindNearestPointCompute.SetBuffer(kernel, "_DistanceBuffer", distance_buffer);
        _FindNearestPointCompute.SetBuffer(kernel, "_DistanceReduceBuffer", distance_reduce_buffer);

        _FindNearestPointCompute.Dispatch(kernel, dist_reduce_groups, 1, 1);


        /*** Execute SetMinDistance ***/
        int kernelSetMinDistance = _FindNearestPointCompute.FindKernel("SetMinDistance");

        ComputeBuffer nearest_point_id = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Default);
        ComputeBuffer nearest_point_position = new ComputeBuffer(1, sizeof(float) * 3, ComputeBufferType.Default);

        _FindNearestPointCompute.SetBuffer(kernelSetMinDistance, "_DistanceReduceBuffer", distance_reduce_buffer);
        _FindNearestPointCompute.SetBuffer(kernelSetMinDistance, "_NearestPointIdBuffer", nearest_point_id);
        _FindNearestPointCompute.SetBuffer(kernelSetMinDistance, "_NearestPointPositionBuffer", nearest_point_position);
        _FindNearestPointCompute.SetBuffer(kernelSetMinDistance, "_PointsPositionBuffer", points_position_buffer);

        _FindNearestPointCompute.Dispatch(kernelSetMinDistance, 1, 1, 1);


        float[] pos = new float[3];
        nearest_point_position.GetData(pos);
        Vector3 nearest_position = new Vector3(pos[0], pos[1], pos[2]);

        uint[] nearest_id = new uint[1];

        nearest_point_id.GetData(nearest_id);

        distance_buffer.Release();
        distance_reduce_buffer.Release();
        nearest_point_id.Release();
        nearest_point_position.Release();
        points_position_buffer.Release();

        PointPos pp = new PointPos();
        pp.pos = nearest_position;
        pp.id = nearest_id[0];

        return pp;
    }

    public PointCloudFormat GetPointCloudFormat()
    {
        return _PointCloudFormat;
    }

    public void SetShaderZDepth(float z_depth)
    {
        _Material.SetFloat("_ZDepth", z_depth);
    }

    public void SetShaderGeometrySize(float size)
    {
        if (size > 0)
        {
            _Material.SetFloat("_PointRadius", size);
        }
    }
    public float GetShaderGeometrySize()
    {
        return _Material.GetFloat("_PointRadius");
    }

    public void SetShaderBrightness(float brightness)
    {
        if (brightness >= -1 && brightness <= 1)
        {
            _Material.SetFloat("_Brightness", brightness);
        }
    }
    public float GetShaderBrightness()
    {
        return _Material.GetFloat("_Brightness");
    }

    public void SetShaderIntensity(float intensity)
    {
        if (intensity >= 1 && intensity <= 5)
        {
            _Material.SetFloat("_ColorIntensity", intensity);
        }
    }

    public float GetShaderIntensity()
    {
        return _Material.GetFloat("_ColorIntensity");
    }

    public bool PointCloudIsLoaded()
    {
        if (_PointCloudFormat == null)
        {
            return false;
        }

        return true;
    }

    public GameObject GetCentroidSelectedPointsGameObject()
    {
        return _CentroidSelectedPoints;
    }

    public void SetAddIntensityToColor(bool addIntensity)
    {

        if (addIntensity)
        {
            _Material.SetInt("_AddIntensityValueToColor", 1);
            _PointCloudFormat.AddIntensityValueToColor = 1;
        }
        else
        {
            _Material.SetInt("_AddIntensityValueToColor", 0);
            _PointCloudFormat.AddIntensityValueToColor = 0;
        }
    }

    public bool GetAddIntensityToColor()
    {
        if (_Material.GetInt("_AddIntensityValueToColor") == 1)
        {
            return true;
        }
        return false;
    }

    public void UseIntensityAsColor(bool intensityAsColor)
    {
        if (intensityAsColor)
        {
            _Material.SetInt("_IntensityAsColor", 1);
        }
        else
        {
            _Material.SetInt("_IntensityAsColor", 0);
        }
    }

    public bool GetIntensityAsColor()
    {
        int intensityAsColor = _Material.GetInt("_IntensityAsColor");
        if (intensityAsColor == 1)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private void FilesUpdate()
    {
        DirectoryInfo dirImportLAS = new DirectoryInfo(_config.paths.root_file_path + @"\import\LAS");
        _FilesImportLAS = dirImportLAS.GetFiles("*.las");
        int i = 0;
        foreach (FileInfo fi in _FilesImportLAS.OrderBy(fi => fi.Name))
        {
            _FilesImportLAS[i] = fi;
            i++;
        }

        DirectoryInfo dirSerialized = new DirectoryInfo(_config.paths.root_file_path + @"\serialized");
        _FilesSerialized = dirSerialized.GetFiles("*.pcxr");
        i = 0;
        foreach (FileInfo fi in _FilesSerialized.OrderBy(fi => fi.Name))
        {
            _FilesSerialized[i] = fi;
            i++;
        }

        DirectoryInfo dirExport = new DirectoryInfo(_config.paths.root_file_path + @"\export");
        _FilesExport = dirExport.GetFiles();
        i = 0;
        foreach (FileInfo fi in _FilesExport.OrderBy(fi => fi.Name))
        {
            _FilesExport[i] = fi;
            i++;
        }
    }

    public FileInfo[] FilesImportLASGet()
    {
        FilesUpdate();
        return _FilesImportLAS;
    }

    public FileInfo[] FilesImportTXTGet()
    {
        FilesUpdate();
        return _FilesImportTXT;
    }

    public FileInfo[] FilesSerializedGet()
    {
        FilesUpdate();
        return _FilesSerialized;
    }

    public FileInfo[] FilesExportGet()
    {
        FilesUpdate();
        return _FilesExport;
    }

    public void ClearPointCloud()
    {
        if (_COMPUTE_BUFFER_CREATED)
        {
            _GPU_DATA_LOADED = false;

            _UserStartPosition = Vector3.zero;
            _UserStartRotation = Quaternion.identity;

            foreach (var cam in _CameraMap)
            {
                if (cam.Key)
                {
                    cam.Key.RemoveCommandBuffer(_CamEventDrawProcedural, cam.Value);
                }
            }

            _CameraMap = new Dictionary<Camera, CommandBuffer>();

            _COMPUTE_BUFFER_CREATED = false;

            _COMPUTE_BUFFER_POINTS.SetData(new float[0]);
            _COMPUTE_BUFFER_POINTS.Release();
            _COMPUTE_BUFFER_POINTS = null;
            _COMPUTE_BUFFER_POINTS_DATA = new float[0];

            _COMPUTE_BUFFER_POINTS_DATA_BYTES = new byte[0];

            if (_COMPUTE_BUFFER_CS_LAS_RAW_BYTES != null)
            {
                _COMPUTE_BUFFER_CS_LAS_RAW_BYTES.Release();
            }

            if (_COMPUTE_BUFFER_CS_POINTS_BUFFER_APPEND != null)
            {
                _COMPUTE_BUFFER_CS_POINTS_BUFFER_APPEND.SetData(new float[0]);
                _COMPUTE_BUFFER_CS_POINTS_BUFFER_APPEND.Release();
                _COMPUTE_BUFFER_CS_POINTS_BUFFER_APPEND = null;

                _COMPUTE_BUFFER_CS_POINTS_BUFFER_APPEND_COUNT.Release();
            }

            if (_COMPUTE_BUFFER_CS_DISPATH_INDIRECT_ARGS != null)
            {
                _COMPUTE_BUFFER_CS_DISPATH_INDIRECT_ARGS.SetData(new float[0]);
                _COMPUTE_BUFFER_CS_DISPATH_INDIRECT_ARGS.Release();
                _COMPUTE_BUFFER_CS_DISPATH_INDIRECT_ARGS = null;
            }

            if (_COMPUTE_BUFFER_CS_DRAWPROCEDURAL_INDIRECT_ARGS != null)
            {
                _COMPUTE_BUFFER_CS_DRAWPROCEDURAL_INDIRECT_ARGS.SetData(new float[0]);
                _COMPUTE_BUFFER_CS_DRAWPROCEDURAL_INDIRECT_ARGS.Release();
                _COMPUTE_BUFFER_CS_DRAWPROCEDURAL_INDIRECT_ARGS = null;
            }

            if (_POINT_CLASSES_COMPUTE_BUFFER != null)
            {
                _POINT_CLASSES_COMPUTE_BUFFER.SetData(new float[0]);
                _POINT_CLASSES_COMPUTE_BUFFER.Release();
                _POINT_CLASSES_COMPUTE_BUFFER = null;
            }

            if (_COMPUTE_BUFFER_CS_LAS_POINTS_BYTES_DATA != null)
            {
                _COMPUTE_BUFFER_CS_LAS_POINTS_BYTES_DATA = null;
                _LAS_HEADER = null;
            }

            _PointCloudFormat = null;
        }
    }

    public void StartImportLAS()
    {
        string file_path = _config.paths.root_file_path + @"\import\LAS\" + _ReadFilename + @".las";
        if (!File.Exists(file_path))
        {
            throw new FileNotFoundException("File " + file_path + " not found");
        }

        // TODO: Move this to PointcloudFormat, and implement in PCXR format.
        _Material.SetInt("_IntensityAsColor", 1);
        Thread importLASThread = new Thread(new ThreadStart(StartImportLasThread));
        importLASThread.Start();
    }

    public void StartLoadFile()
    {
        Thread loadPointCloudThread = new Thread(new ThreadStart(StartLoadPointCloudThread)) { IsBackground = true, Name = "loadPointCloudThread" };
        loadPointCloudThread.Start();
    }

    void OnGPUSetDataListener(object sender, EventArgs e)
    {

    }

    void OnPointCloudLoadedListener(object sender, EventArgs e)
    {
        StartCoroutine(ComputeBufferSetDataCoroutine());
    }

    private void StartImportLasThread()
    {
        if (_UsePointsByteData)
        {
            ImportLASThreadBytes(_config.paths.root_file_path + @"\import\LAS\" + _ReadFilename + @".las", _STRIDE, true, _RGB_SCALE_LAS);
        }
    }

    private void StartLoadPointCloudThread()
    {
        if (_UsePointsByteData)
        {
            LoadPointCloudByThreadPCXRfileBytes(_config.paths.root_file_path + @"\serialized\" + _ReadFilename + ".pcxr");
        }
    }

    public void SelectedPointColorSet(Color pointColor)
    {
        _Material.SetColor("_SelectedPointColor", pointColor);
    }

    public void ShowNearestPoint(bool show)
    {
        _ShowNearestPoint = show;
    }

    public Vector3 GetNearestPoint()
    {
        // Retrieve nearest point from shader
        return _NearestPointFound;
    }

    public void UndoDelete()
    {
        int num_groups = Mathf.CeilToInt(_POINTS_IN_CLOUD / (float)_CS_POINTS_BUFFER_THREADS);
        int kernel = _EditPointsCompute.FindKernel("DeleteNone");

        _EditPointsCompute.SetBuffer(kernel, "_Points", _COMPUTE_BUFFER_POINTS);
        _EditPointsCompute.Dispatch(kernel, num_groups, 1, 1);
    }


    public Vector3 GetUserStartPosition()
    {
        return _UserStartPosition;
    }

    public GameObject GetUserGameObject()
    {
        return _UserGameObject;
    }

    public EditToolMode GetEditToolMode()
    {
        return _EditToolAction;
    }

    private void SetPointCloudFormat(PointCloudFormat pcf)
    {
        _PointCloudFormat = pcf;
    }

    // Update shader to current EditToolMode
    public void EditToolSetMode(EditToolMode action)
    {
        _EditToolAction = action;
        if (action == EditToolMode.DELETE)
        {
            _Material.SetInt("_EditToolDelete", 1);
            Debug.Log("PCM - Action: " + action.ToString());
        }
        else
        {
            _Material.SetInt("_EditToolDelete", 0);
        }

        if (action == EditToolMode.SELECT)
        {
            _Material.SetInt("_EditToolSelect", 1);
            Debug.Log("PCM - Action: " + action.ToString());
        }
        else
        {
            _Material.SetInt("_EditToolSelect", 0);
        }

        if (action == EditToolMode.DESELECT)
        {
            _Material.SetInt("_EditToolDeselect", 1);
            Debug.Log("PCM - Action: " + action.ToString());
        }
        else
        {
            _Material.SetInt("_EditToolDeselect", 0);
        }
    }

    public EditToolMode EditToolGetAction()
    {
        return _EditToolAction;
    }

    public void EditToolSetActive(bool activate)
    {
        _Material.SetInt("_EditToolActive", Convert.ToInt32(activate));
        _ActivateEditTool = activate;
    }

    public void EditToolSetPosition(Vector3 position)
    {
        _Material.SetVector("_EditToolPos", position);
    }

    public void EditToolSetRadius(float radius)
    {
        _Material.SetFloat("_EditToolRadius", radius);
    }

    public ComputeBuffer GetComputeBufferCom()
    {
        return _COMPUTE_BUFFER_COM;
    }

    public ComputeBuffer GetComputeBufferPoints()
    {
        return _COMPUTE_BUFFER_POINTS;
    }

    public int GetPointsInCloud()
    {
        return _POINTS_IN_CLOUD;
    }

    public int GetComputeShaderPointsBufferThreads()
    {
        return _CS_POINTS_BUFFER_THREADS;
    }

    public void SelectNone()
    {
        int num_groups = Mathf.CeilToInt(_POINTS_IN_CLOUD / (float)_CS_POINTS_BUFFER_THREADS);
        int kernel = _EditPointsCompute.FindKernel("SelectNone");

        _EditPointsCompute.SetBuffer(kernel, "_Points", _COMPUTE_BUFFER_POINTS);
        _EditPointsCompute.Dispatch(kernel, num_groups, 1, 1);
    }

    public void ActivateTransformSelectedPoints(bool activate, bool debug)
    {
        if (activate)
        {
            _CentroidSelectedPointsStartPosition = GetCentroidOfSelectedPoints(debug);
            _CentroidSelectedPoints = new GameObject();
            _CentroidSelectedPoints.name = "CentroidSelectedPoints";
            _CentroidSelectedPoints.transform.position = _CentroidSelectedPointsStartPosition;
            _Material.SetInt("_TransformSelectedPoints", 1);
        }
        else
        {
            if (_COMPUTE_BUFFER_TRANSLATED_POINTS != null)
            {
                float[] transladed_points_data = new float[_PointCloudFormat.NumberOfPoints * 3];
                _COMPUTE_BUFFER_TRANSLATED_POINTS.GetData(transladed_points_data);

                float[] offset_points_data = new float[_PointCloudFormat.NumberOfPoints * 3];
                _COMPUTE_BUFFER_OFFSET_POINTS.GetData(offset_points_data);

                // TODO: Optimize this for multi-core
                for (int i = 0; i < transladed_points_data.Length; i++)
                {
                    if (transladed_points_data[i] != 0)
                    {
                        offset_points_data[i] += transladed_points_data[i];
                        transladed_points_data[i] = 0f;
                    }
                }
                _COMPUTE_BUFFER_OFFSET_POINTS.SetData(offset_points_data);
                _COMPUTE_BUFFER_TRANSLATED_POINTS.SetData(transladed_points_data);
            }

            _Material.SetInt("_TransformSelectedPoints", 0);
            GameObject centroid = GameObject.Find("CentroidSelectedPoints");
            if (centroid != null)
            {
                Destroy(centroid);
            }

            GameObject centroidDebugPoints = GameObject.Find("centroidDebugPoints");
            if (centroidDebugPoints != null)
            {
                Destroy(centroidDebugPoints);
            }
        }
    }

    private Vector3 GetCentroidOfSelectedPoints(bool debug)
    {
        float xMax = -100000, yMax = -100000, zMax = -100000, xMin = 100000, yMin = 100000, zMin = 100000;

        float[] buffer_select_data = ComputeBufferDataFactory.CreateSelectBufferData(_PointCloudFormat.NumberOfPoints);

        float[] buffer_offset = ComputeBufferDataFactory.CreateOffsetBufferData(_PointCloudFormat.NumberOfPoints);
        _COMPUTE_BUFFER_OFFSET_POINTS.GetData(buffer_offset);

        Vector3 xMaxPoint = new Vector3(xMax, yMax, zMax);
        Vector3 yMaxPoint = new Vector3(xMax, yMax, zMax);
        Vector3 zMaxPoint = new Vector3(xMax, yMax, zMax);

        Vector3 xMinPoint = new Vector3(xMin, yMin, zMin);
        Vector3 yMinPoint = new Vector3(xMin, yMin, zMin);
        Vector3 zMinPoint = new Vector3(xMin, yMin, zMin);

        Vector3 currentPoint = new Vector3(buffer_offset[0] + _COMPUTE_BUFFER_POINTS_DATA[0],
                                           buffer_offset[1] + _COMPUTE_BUFFER_POINTS_DATA[1],
                                           buffer_offset[2] + _COMPUTE_BUFFER_POINTS_DATA[2]);
        // Find the centroid of selected points
        for (int i = 0; i < buffer_select_data.Length; i++)
        {
            if (buffer_select_data[i] == 1)
            {
                currentPoint.x = _COMPUTE_BUFFER_POINTS_DATA[i * _PointCloudFormat.Stride + 0] + buffer_offset[i * 3 + 0];
                currentPoint.y = _COMPUTE_BUFFER_POINTS_DATA[i * _PointCloudFormat.Stride + 1] + buffer_offset[i * 3 + 1];
                currentPoint.z = _COMPUTE_BUFFER_POINTS_DATA[i * _PointCloudFormat.Stride + 2] + buffer_offset[i * 3 + 2];

                xMaxPoint = currentPoint.x > xMaxPoint.x ? currentPoint : xMaxPoint;
                xMinPoint = currentPoint.x < xMinPoint.x ? currentPoint : xMinPoint;

                yMaxPoint = currentPoint.y > yMaxPoint.y ? currentPoint : yMaxPoint;
                yMinPoint = currentPoint.y < yMinPoint.y ? currentPoint : yMinPoint;

                zMaxPoint = currentPoint.z > zMaxPoint.z ? currentPoint : zMaxPoint;
                zMinPoint = currentPoint.z < zMinPoint.z ? currentPoint : zMinPoint;
            }
        }

        Vector3 midPoint = (xMaxPoint + xMinPoint + yMaxPoint + yMinPoint + zMaxPoint + zMinPoint) / 6f;

        if (debug)
        {
            GameObject centroidDebugPoints = GameObject.Find("centroidDebugPoints");
            if (centroidDebugPoints != null)
            {
                Destroy(centroidDebugPoints);
            }
            centroidDebugPoints = new GameObject();
            centroidDebugPoints.transform.position = midPoint;
            centroidDebugPoints.name = "centroidDebugPoints";

            GameObject midPointCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            midPointCube.transform.position = midPoint;
            midPointCube.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            midPointCube.name = "midPoint";
            midPointCube.transform.parent = centroidDebugPoints.transform;
        }

        return midPoint;
    }

    public void DeleteSelected()
    {
        int num_groups = Mathf.CeilToInt(_POINTS_IN_CLOUD / (float)_CS_POINTS_BUFFER_THREADS);
        int kernel = _EditPointsCompute.FindKernel("DeleteSelected");

        _EditPointsCompute.SetBuffer(kernel, "_Points", _COMPUTE_BUFFER_POINTS);
        _EditPointsCompute.Dispatch(kernel, num_groups, 1, 1);
    }

    public void SelectPointsInList(float[] point_list)
    {
        if (point_list.Length > 0)
        {
            int num_groups = Mathf.CeilToInt(point_list.Length / (float)_CS_POINTS_BUFFER_THREADS);


            int kernel = _EditPointsCompute.FindKernel("SelectPointsInList");

            _EditPointsCompute.SetBuffer(kernel, "_Points", _COMPUTE_BUFFER_POINTS);

            ComputeBuffer points_in_list = new ComputeBuffer(point_list.Length, 1 * sizeof(float), ComputeBufferType.Default);
            points_in_list.SetData(point_list);
            _EditPointsCompute.SetBuffer(kernel, "_points_to_select", points_in_list);
            _EditPointsCompute.SetInt("_points_to_select_length", point_list.Length);

            _EditPointsCompute.Dispatch(kernel, num_groups, 1, 1);

            points_in_list.Dispose();
        }
    }

    public void ExportSelected(bool exportSelected)
    {
        _ExportSelected = exportSelected;
    }

    public void SelectInverse()
    {
        int num_groups = Mathf.CeilToInt(_POINTS_IN_CLOUD / (float)_CS_POINTS_BUFFER_THREADS);
        int kernel = _EditPointsCompute.FindKernel("SelectInverse");

        _EditPointsCompute.SetBuffer(kernel, "_Points", _COMPUTE_BUFFER_POINTS);
        _EditPointsCompute.Dispatch(kernel, num_groups, 1, 1);
    }

    public void SelectByClass(int class_id)
    {
        int num_groups = Mathf.CeilToInt(_POINTS_IN_CLOUD / (float)_CS_POINTS_BUFFER_THREADS);
        int kernel = _EditPointsCompute.FindKernel("SelectByClass");

        _EditPointsCompute.SetInt("_selection_class", class_id);
        _EditPointsCompute.SetBuffer(kernel, "_Points", _COMPUTE_BUFFER_POINTS);
        _EditPointsCompute.Dispatch(kernel, num_groups, 1, 1);
    }

    private void SavePointCloudPCXRInternalByThreadPruned()
    {
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        Debug.Log("SavePointCloudPCXRInternalByThreadPruned - start\n");

        string pathFull = _config.paths.root_file_path + @"\serialized\" + _WriteFilename + ".pcxr";
        ushort stride = (ushort)_PointCloudFormat.Stride;

        uint numberOfPoints = (uint)(_pruned_points_byte_data.Length / (stride * sizeof(float)));

        Debug.Log($"pruned numberOfPoints: {numberOfPoints}\n");
        PCXRHeader header = new PCXRHeader();
        header.NumberOfPoints = numberOfPoints;
        header.Stride = stride;

        header.UserStartPositionX = _UserStartPosition.x;
        header.UserStartPositionY = _UserStartPosition.y;
        header.UserStartPositionZ = _UserStartPosition.z;

        header.UserStartRotationW = _UserStartRotation.w;
        header.UserStartRotationX = _UserStartRotation.x;
        header.UserStartRotationY = _UserStartRotation.y;
        header.UserStartRotationZ = _UserStartRotation.z;

        header.AddIntensityValueToColor = (int)_PointCloudFormat.AddIntensityValueToColor;
        header.IntensityAsColor = (int)_PointCloudFormat.IntensityAsColor;
        header.ColorIntensity = (float)_PointCloudFormat.AddIntensityToColor;
        header.GeometrySize = (float)_PointCloudFormat.GeometrySize;
        header.UserColorAsColor = (float)_PointCloudFormat.UserColorAsColor;

        header.MaxX = (float)_PointCloudFormat.MaxX;
        header.MaxY = (float)_PointCloudFormat.MaxY;
        header.MaxZ = (float)_PointCloudFormat.MaxZ;
        header.MinX = (float)_PointCloudFormat.MinX;
        header.MinY = (float)_PointCloudFormat.MinY;
        header.MinZ = (float)_PointCloudFormat.MinZ;

        header.XScaleFactor = _PointCloudFormat.XScaleFactor;
        header.YScaleFactor = _PointCloudFormat.YScaleFactor;
        header.ZScaleFactor = _PointCloudFormat.ZScaleFactor;
        header.XOffset = _PointCloudFormat.XOffset;
        header.YOffset = _PointCloudFormat.YOffset;
        header.ZOffset = _PointCloudFormat.ZOffset;

        PCXRWriterBuffered writer = new PCXRWriterBuffered(pathFull, header);

        writer.WritePointArray(_pruned_points_byte_data);
        writer.Close();

        stopwatch.Stop();
        TimeSpan t = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds);
        string humanReadableTime = string.Format("{0:D2}h:{1:D2}m:{2:D2}s:{3:D3}ms",
                                t.Hours,
                                t.Minutes,
                                t.Seconds,
                                t.Milliseconds);
        Debug.LogFormat("SavePointCloudPCXRInternalByThreadPruned - runtime: {0}\n", humanReadableTime);

        _PointCloudSaved = true;
    }

    private void UpdateProgressInGUI(uint currentPointNumber, uint numberOfPoints, string txt)
    {
        if (numberOfPoints != 0)
        {
            _Progress = (float)currentPointNumber / numberOfPoints;
        }
        else
        {
            _Progress = 0;
        }

        _GUIText = txt;

        OnProgress?.Invoke(this, EventArgs.Empty);
    }

    public float ProgressGet()
    {
        return _Progress;
    }

    private void LoadPointCloudByThreadPCXRfileBytes(string pathAndName)
    {
        PCXRHeader header = new PCXRHeader();
        header.ReadHeader(pathAndName);

        PCXRReaderIO pcxrReaderIO = new PCXRReaderIO(pathAndName, header);

        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        Debug.LogFormat("LoadPointCloudByThreadPCXRfileBytes - start\n");

        PointCloudFormat pointCloudFormat = new PointCloudFormat();

        uint numberOfPoints;
        ushort stride;


        numberOfPoints = pcxrReaderIO._Header.NumberOfPoints;
        stride = pcxrReaderIO._Header.Stride;

        pointCloudFormat.NumberOfPoints = (int)pcxrReaderIO._Header.NumberOfPoints;
        pointCloudFormat.Stride = pcxrReaderIO._Header.Stride;

        _UserStartPosition.x = pcxrReaderIO._Header.UserStartPositionX;
        _UserStartPosition.y = pcxrReaderIO._Header.UserStartPositionY;
        _UserStartPosition.z = pcxrReaderIO._Header.UserStartPositionZ;
        _UserStartRotation.x = pcxrReaderIO._Header.UserStartRotationX;
        _UserStartRotation.y = pcxrReaderIO._Header.UserStartRotationY;
        _UserStartRotation.z = pcxrReaderIO._Header.UserStartRotationZ;
        _UserStartRotation.w = pcxrReaderIO._Header.UserStartRotationW;

        pointCloudFormat.AddIntensityValueToColor = pcxrReaderIO._Header.AddIntensityValueToColor;
        pointCloudFormat.IntensityAsColor = pcxrReaderIO._Header.IntensityAsColor;
        pointCloudFormat.AddIntensityToColor = pcxrReaderIO._Header.ColorIntensity;
        pointCloudFormat.GeometrySize = pcxrReaderIO._Header.GeometrySize;
        pointCloudFormat.UserColorAsColor = pcxrReaderIO._Header.UserColorAsColor;
        pointCloudFormat.MaxX = pcxrReaderIO._Header.MaxX;
        pointCloudFormat.MaxY = pcxrReaderIO._Header.MaxY;
        pointCloudFormat.MaxZ = pcxrReaderIO._Header.MaxZ;
        pointCloudFormat.MinX = pcxrReaderIO._Header.MinX;
        pointCloudFormat.MinY = pcxrReaderIO._Header.MinY;
        pointCloudFormat.MinZ = pcxrReaderIO._Header.MinZ;
        pointCloudFormat.XScaleFactor = pcxrReaderIO._Header.XScaleFactor;
        pointCloudFormat.YScaleFactor = pcxrReaderIO._Header.YScaleFactor;
        pointCloudFormat.ZScaleFactor = pcxrReaderIO._Header.ZScaleFactor;

        float[] selectData = new float[numberOfPoints];
        float[] deleteData = new float[numberOfPoints];


        Debug.LogFormat("LoadPointCloudByThreadPCXRfileBytes - {0} points loaded\n", numberOfPoints);

        _POINTS_IN_CLOUD = (int)numberOfPoints;

        _COMPUTE_BUFFER_POINTS_DATA_BYTES = new byte[0];

        _COMPUTE_BUFFER_POINTS_DATA_BYTES = pcxrReaderIO.GetByteData();

        SetPointCloudFormat(pointCloudFormat);
        stopwatch.Stop();
        TimeSpan t = TimeSpan.FromMilliseconds(stopwatch.ElapsedMilliseconds);
        string humanReadableTime = string.Format("{0:D2}h:{1:D2}m:{2:D2}s:{3:D3}ms",
                                t.Hours,
                                t.Minutes,
                                t.Seconds,
                                t.Milliseconds);
        Debug.LogFormat("LoadPointCloudByThreadPCXRfileBytes - Total runtime: {0}\n", humanReadableTime);

        _UPDATE_PROGRESS_IN_GUI = false;

        _PointCloudLoaded = true;
    }

    public void GeneratePoints(int number_of_points, float spacing)
    {
        int pointcount_factor = 1;
        number_of_points *= pointcount_factor;

        ClearPointCloud();
        PointCloudFormat pointCloudFormat = new PointCloudFormat();

        pointCloudFormat.NumberOfPoints = number_of_points;
        pointCloudFormat.Stride = 24;


        int kernel = _PointGeneratorCompute.FindKernel("Generator");

        _PointGeneratorCompute.SetInt("_number_of_points", number_of_points);
        _PointGeneratorCompute.SetFloat("_spacing", spacing);
        _PointGeneratorCompute.SetFloat("_UserCameraY", _UserCamera.transform.position.y);

        _COMPUTE_BUFFER_POINTS = new ComputeBuffer(number_of_points, sizeof(float) * pointCloudFormat.Stride, ComputeBufferType.Default);
        _PointGeneratorCompute.SetBuffer(kernel, "_Points", _COMPUTE_BUFFER_POINTS);
        int num_groups = Mathf.CeilToInt(number_of_points / (float)_CS_POINTS_BUFFER_THREADS);

        _PointGeneratorCompute.Dispatch(kernel, num_groups, 1, 1);


        _POINTS_IN_CLOUD = number_of_points;
        SetPointCloudFormat(pointCloudFormat);

        _GeneratedPoints = true;

        if (_UsePointsByteData)
        {
            _COMPUTE_BUFFER_POINTS_DATA_BYTES = new byte[_PointCloudFormat.NumberOfPoints * _PointCloudFormat.Stride * sizeof(float)];
            _COMPUTE_BUFFER_POINTS.GetData(_COMPUTE_BUFFER_POINTS_DATA_BYTES);
        }
        else
        {
            _COMPUTE_BUFFER_POINTS_DATA = new float[_PointCloudFormat.NumberOfPoints * _PointCloudFormat.Stride];
            _COMPUTE_BUFFER_POINTS.GetData(_COMPUTE_BUFFER_POINTS_DATA);
        }

        SetShaderZDepth(0.9f);
        _PointCloudLoaded = true;
    }

    private IEnumerator ComputeBufferSetDataCoroutine()
    {
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // 128-bit stride for performance. 16 bytes.
        // Used by compute shader _PointBufferManager to fill append buffer for shader (frustum culling)
        _COMPUTE_BUFFER_POINTS = new ComputeBuffer(_POINTS_IN_CLOUD, sizeof(float) * _STRIDE, ComputeBufferType.Default);
        _COMPUTE_BUFFER_CREATED = true;


        if (_UsePointsByteData)
        {
            if (_COMPUTE_BUFFER_CS_LAS_POINTS_BYTES_DATA == null)
            {
                _COMPUTE_BUFFER_POINTS.SetData(_COMPUTE_BUFFER_POINTS_DATA_BYTES);
            }
            else
            {
                try
                {
                    switch (_LAS_HEADER.PointDataFormatID)
                    {
                        case 0:
                            _COMPUTE_BUFFER_CS_LAS_RAW_BYTES = new ComputeBuffer((int)_LAS_HEADER.Numberofpointrecords, 20, ComputeBufferType.Default);
                            break;
                        case 1:
                            _COMPUTE_BUFFER_CS_LAS_RAW_BYTES = new ComputeBuffer((int)_LAS_HEADER.Numberofpointrecords, 28, ComputeBufferType.Default);
                            break;
                        case 2:
                            _COMPUTE_BUFFER_CS_LAS_RAW_BYTES = new ComputeBuffer((int)_LAS_HEADER.Numberofpointrecords, (26 + 2), ComputeBufferType.Default);
                            break;
                        case 3:
                            _COMPUTE_BUFFER_CS_LAS_RAW_BYTES = new ComputeBuffer((int)_LAS_HEADER.Numberofpointrecords, (34 + 2), ComputeBufferType.Default);
                            break;
                    }
                }
                catch (Exception e)
                {
                    Debug.Log("Failed to create ComputeBuffer: " + e.ToString());
                }

                _COMPUTE_BUFFER_CS_LAS_RAW_BYTES.SetData(_COMPUTE_BUFFER_CS_LAS_POINTS_BYTES_DATA);

                DispatchLasToPCXR(_LAS_HEADER);

                ColorByHeight();
            }
        }
        else
        {
            int chunk = _STRIDE * _ComputeBufferSetDataChunkSize;
            int chunksLoaded = 0;
            int loadedData = 0;
            for (int i = 0; i < _COMPUTE_BUFFER_POINTS_DATA.Length; i += chunk)
            {
                if ((i + chunk) < _COMPUTE_BUFFER_POINTS_DATA.Length)
                {
                    _COMPUTE_BUFFER_POINTS.SetData(_COMPUTE_BUFFER_POINTS_DATA, i, i, chunk);
                    chunksLoaded++;
                }
                else
                {
                    loadedData = i;
                }
            }

            if (loadedData != _COMPUTE_BUFFER_POINTS_DATA.Length) // Load the remaining data to the GPU 
            {
                int dataRemaining = _COMPUTE_BUFFER_POINTS_DATA.Length - loadedData;
                _COMPUTE_BUFFER_POINTS.SetData(_COMPUTE_BUFFER_POINTS_DATA, loadedData, loadedData, (dataRemaining));
            }
        }

        float[] init_data_1 = new float[_POINTS_IN_CLOUD];
        float[] init_data_3 = new float[_POINTS_IN_CLOUD * 3];

        _COMPUTE_BUFFER_TRANSLATED_POINTS = new ComputeBuffer(_POINTS_IN_CLOUD, sizeof(float) * 3, ComputeBufferType.Default);
        _COMPUTE_BUFFER_TRANSLATED_POINTS.SetData(init_data_3);

        _COMPUTE_BUFFER_OFFSET_POINTS = new ComputeBuffer(_POINTS_IN_CLOUD, sizeof(float) * 3, ComputeBufferType.Default);
        _COMPUTE_BUFFER_OFFSET_POINTS.SetData(init_data_3);

        _COMPUTE_BUFFER_COM = new ComputeBuffer(2, sizeof(float), ComputeBufferType.Default);
        _COMPUTE_BUFFER_COM.SetData(new float[2] { 0, 0 });

        _COMPUTE_BUFFER_CS_DISPATH_INDIRECT_ARGS = new ComputeBuffer(1, sizeof(uint) * 4, ComputeBufferType.IndirectArguments);
        DispatchArgs dArgs;
        dArgs.x = (uint)Mathf.CeilToInt(_POINTS_IN_CLOUD / (float)_CS_POINTS_BUFFER_THREADS);
        dArgs.y = 1;
        dArgs.z = 1;
        dArgs.w = 1;
        _COMPUTE_BUFFER_CS_DISPATH_INDIRECT_ARGS.SetData(new DispatchArgs[1] { dArgs });

        _COMPUTE_BUFFER_CS_DEBUG = new ComputeBuffer(_POINTS_IN_CLOUD, sizeof(float) * 4, ComputeBufferType.Default);

        _COMPUTE_BUFFER_CS_POINTS_BUFFER_APPEND = new ComputeBuffer(_POINTS_IN_CLOUD, sizeof(float) * _STRIDE, ComputeBufferType.Append);

        _COMPUTE_BUFFER_CS_DRAWPROCEDURAL_INDIRECT_ARGS = new ComputeBuffer(_POINTS_IN_CLOUD, sizeof(uint) * 4, ComputeBufferType.IndirectArguments);
        _COMPUTE_BUFFER_CS_DRAWPROCEDURAL_INDIRECT_ARGS.SetData(new uint[4] { 1, 1, 0, 0 });
        _COMPUTE_BUFFER_CS_POINTS_BUFFER_APPEND_COUNT = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);

        _point_classes = new float[256];
        _POINT_CLASSES_COMPUTE_BUFFER = new ComputeBuffer(256, sizeof(float), ComputeBufferType.Default);
        _POINT_CLASSES_COMPUTE_BUFFER.SetData(_point_classes);

        _UserGameObject.transform.position = _UserStartPosition;
        _UserGameObject.transform.rotation = _UserStartRotation;

        _GPU_DATA_LOADED = true;

        SetMaterialComputeBuffers();
        DispatchCountClasses();

        OnGPUSetDataDone?.Invoke(this, EventArgs.Empty);

        yield return null;

        stopwatch.Stop();
    }

    public void SetPointCloudMaxMins()
    {
        Vector3[] bounds = GetBoundaries();

        _PointCloudFormat.MinX = bounds[1].x;
        _PointCloudFormat.MinY = bounds[1].y;
        _PointCloudFormat.MinZ = bounds[1].z;
        _PointCloudFormat.MaxX = bounds[0].x;
        _PointCloudFormat.MaxY = bounds[0].y;
        _PointCloudFormat.MaxZ = bounds[0].z;
    }

    public void CalculateCentroid()
    {
        bool debug = false;
        Vector3 currentPoint = new Vector3(_COMPUTE_BUFFER_POINTS_DATA[0], _COMPUTE_BUFFER_POINTS_DATA[1], _COMPUTE_BUFFER_POINTS_DATA[2]);

        float xMax = currentPoint.x, yMax = currentPoint.y, zMax = currentPoint.z, xMin = currentPoint.x, yMin = currentPoint.y, zMin = currentPoint.z;

        Vector3 xMaxPoint = new Vector3(xMax, yMax, zMax);
        Vector3 yMaxPoint = new Vector3(xMax, yMax, zMax);
        Vector3 zMaxPoint = new Vector3(xMax, yMax, zMax);

        Vector3 xMinPoint = new Vector3(xMin, yMin, zMin);
        Vector3 yMinPoint = new Vector3(xMin, yMin, zMin);
        Vector3 zMinPoint = new Vector3(xMin, yMin, zMin);

        // Find the centroid of selected points
        for (int i = 0; i < _PointCloudFormat.NumberOfPoints; i++)
        {
            currentPoint.x = _COMPUTE_BUFFER_POINTS_DATA[i * _PointCloudFormat.Stride + 0];
            currentPoint.y = _COMPUTE_BUFFER_POINTS_DATA[i * _PointCloudFormat.Stride + 1];
            currentPoint.z = _COMPUTE_BUFFER_POINTS_DATA[i * _PointCloudFormat.Stride + 2];

            xMaxPoint = currentPoint.x > xMaxPoint.x ? currentPoint : xMaxPoint;
            xMinPoint = currentPoint.x < xMinPoint.x ? currentPoint : xMinPoint;

            yMaxPoint = currentPoint.y > yMaxPoint.y ? currentPoint : yMaxPoint;
            yMinPoint = currentPoint.y < yMinPoint.y ? currentPoint : yMinPoint;

            zMaxPoint = currentPoint.z > zMaxPoint.z ? currentPoint : zMaxPoint;
            zMinPoint = currentPoint.z < zMinPoint.z ? currentPoint : zMinPoint;
        }


        _PointCloudFormat.MaxX = xMaxPoint.x;
        _PointCloudFormat.MinX = xMinPoint.x;
        _PointCloudFormat.MaxY = yMaxPoint.y;
        _PointCloudFormat.MinY = yMinPoint.y;
        _PointCloudFormat.MaxZ = zMaxPoint.z;
        _PointCloudFormat.MinZ = zMinPoint.z;

        Vector3 midPoint = (xMaxPoint + xMinPoint + yMaxPoint + yMinPoint + zMaxPoint + zMinPoint) / 6f;

        if (debug)
        {
            float centroidSize = 0.3f;
            float limitsSize = 0.2f;
            Material centroidMaterial = new Material(Shader.Find("Standard"));
            centroidMaterial.SetColor("_Color", Color.green);

            Material limitsMaterial = new Material(Shader.Find("Standard"));
            limitsMaterial.SetColor("_Color", Color.red);

            GameObject centroidDebugPoints = new GameObject();
            centroidDebugPoints.transform.position = midPoint;
            centroidDebugPoints.name = "centroidPoints";

            GameObject centroid = GameObject.CreatePrimitive(PrimitiveType.Cube);
            centroid.transform.position = midPoint;
            centroid.transform.localScale = new Vector3(centroidSize, centroidSize, centroidSize);
            centroid.name = "centroid";
            centroid.transform.parent = centroidDebugPoints.transform;
            centroid.GetComponent<Renderer>().material = centroidMaterial;

            GameObject xMaxPointCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            xMaxPointCube.transform.position = xMaxPoint;
            xMaxPointCube.transform.localScale = new Vector3(limitsSize, limitsSize, limitsSize);
            xMaxPointCube.name = "xMax";
            xMaxPointCube.transform.parent = centroidDebugPoints.transform;
            xMaxPointCube.GetComponent<Renderer>().material = limitsMaterial;

            GameObject xMinPointCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            xMinPointCube.transform.position = xMinPoint;
            xMinPointCube.transform.localScale = new Vector3(limitsSize, limitsSize, limitsSize);
            xMinPointCube.name = "xMin";
            xMinPointCube.transform.parent = centroidDebugPoints.transform;
            xMinPointCube.GetComponent<Renderer>().material = limitsMaterial;

            GameObject yMaxPointCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            yMaxPointCube.transform.position = yMaxPoint;
            yMaxPointCube.transform.localScale = new Vector3(limitsSize, limitsSize, limitsSize);
            yMaxPointCube.name = "yMax";
            yMaxPointCube.transform.parent = centroidDebugPoints.transform;
            yMaxPointCube.GetComponent<Renderer>().material = limitsMaterial;

            GameObject yMinPointCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            yMinPointCube.transform.position = yMinPoint;
            yMinPointCube.transform.localScale = new Vector3(limitsSize, limitsSize, limitsSize);
            yMinPointCube.name = "yMin";
            yMinPointCube.transform.parent = centroidDebugPoints.transform;
            yMinPointCube.GetComponent<Renderer>().material = limitsMaterial;

            GameObject zMaxPointCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            zMaxPointCube.transform.position = zMaxPoint;
            zMaxPointCube.transform.localScale = new Vector3(limitsSize, limitsSize, limitsSize);
            zMaxPointCube.name = "zMax";
            zMaxPointCube.transform.parent = centroidDebugPoints.transform;
            zMaxPointCube.GetComponent<Renderer>().material = limitsMaterial;

            GameObject zMinPointCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            zMinPointCube.transform.position = zMinPoint;
            zMinPointCube.transform.localScale = new Vector3(limitsSize, limitsSize, limitsSize);
            zMinPointCube.name = "zMin";
            zMinPointCube.transform.parent = centroidDebugPoints.transform;
            zMinPointCube.GetComponent<Renderer>().material = limitsMaterial;
        }
    }


    private void ImportLASThread(string filePath, int stride, bool invertYZ, int scale, int colorScaler)
    {
        Debug.Log("ImportLASThread - Start\n");
        System.Diagnostics.Stopwatch stopwatchTotal = System.Diagnostics.Stopwatch.StartNew();
        System.Diagnostics.Stopwatch stopwatchLoad = System.Diagnostics.Stopwatch.StartNew();

        PointCloudFormat pointCloudFormat = new PointCloudFormat();
        pointCloudFormat.Stride = stride;

        float intensityMax = 0;
        float intensityMin = short.MaxValue * 2;

        LASReaderBuffered lasreader = new LASReaderBuffered(filePath, 1000);
        LASHeader lasheader = lasreader._Header;

        _UPDATE_PROGRESS_IN_GUI = true;
        Debug.Log("ImportLASThread - Loading LAS data\n");

        uint numberOfPoints = lasheader.Numberofpointrecords;

        LASPointXR[] lasPoints = new LASPointXR[numberOfPoints];

        LASPoint lasReaderPoint;
        for (uint i = 0; i < numberOfPoints; i++)
        {
            lasReaderPoint = lasreader.GetNextPoint();
            lasPoints[i] = new LASPointXR();
            lasPoints[i].X = lasReaderPoint.X;
            lasPoints[i].Y = lasReaderPoint.Y;
            lasPoints[i].Z = lasReaderPoint.Z;
            lasPoints[i].Intensity = lasReaderPoint.Intensity;
            lasPoints[i].Classification = lasReaderPoint.Classification;

            if (lasPoints[i].Intensity < intensityMin)
            {
                intensityMin = lasPoints[i].Intensity;
            }
            if (lasPoints[i].Intensity > intensityMax)
            {
                intensityMax = lasPoints[i].Intensity;
            }
        }

        stopwatchLoad.Stop();
        TimeSpan t = TimeSpan.FromMilliseconds(stopwatchLoad.ElapsedMilliseconds);
        string humanReadableTime = string.Format("{0:D2}h:{1:D2}m:{2:D2}s:{3:D3}ms",
                                t.Hours,
                                t.Minutes,
                                t.Seconds,
                                t.Milliseconds);
        Debug.LogFormat("ImportLASThread - LAS load time: {0}\n", humanReadableTime);
        Debug.LogFormat("ImportLASThread - intensityMax = {0}, intensityMin = {1}\n", intensityMax, intensityMin);

        System.Diagnostics.Stopwatch stopwatchProcess = System.Diagnostics.Stopwatch.StartNew();

        double xMin = lasheader.MinX;
        double yMin = lasheader.MinY;
        double zMin = lasheader.MinZ;

        double xMax = lasheader.MaxX;
        double yMax = lasheader.MaxY;
        double zMax = lasheader.MaxZ;

        double xDouble;
        double yDouble;
        double zDouble;

        float xFloat;
        float yFloat;
        float zFloat;

        // Calculate centroid for xy-plane. Used for centering the pointcloud at world space origin.
        double centroidX = ((xMax - xMin) / 2) + xMin;
        double centroidY = ((yMax - yMin) / 2) + yMin;

        int pointsArraySize = (int)numberOfPoints * stride;
        _COMPUTE_BUFFER_POINTS_DATA = new float[pointsArraySize];

        // Artifical color by height and intensity values
        float hightInterval = (float)zMax - (float)zMin;
        float colorR = 0;
        float colorG = 0;
        float colorB = 0;

        float colorB_start = 0;
        float colorG_start = hightInterval * 0.1f;
        float colorR_start = hightInterval * 0.5f;
        float overlap = 0.1f;

        byte classification = 0;
        float intensityNormalized = 0;

        LASPointXR laspoint;
        for (uint i = 0; i < numberOfPoints; i++)
        {
            laspoint = lasPoints[i];

            classification = laspoint.Classification;

            if (invertYZ)
            {
                xDouble = laspoint.X - centroidX;
                yDouble = laspoint.Z - zMin;
                zDouble = laspoint.Y - centroidY;
            }
            else
            {
                xDouble = laspoint.X - centroidX;
                yDouble = laspoint.Y - centroidY;
                zDouble = laspoint.Z - zMin;
            }

            xFloat = (float)xDouble * scale;
            yFloat = (float)yDouble * scale;
            zFloat = (float)zDouble * scale;


            //// Coordinates ////
            _COMPUTE_BUFFER_POINTS_DATA[i * stride + 0] = xFloat;
            _COMPUTE_BUFFER_POINTS_DATA[i * stride + 1] = yFloat;
            _COMPUTE_BUFFER_POINTS_DATA[i * stride + 2] = zFloat;

            //// Color ////
            // Blue
            float colorB_tranzitZone = hightInterval * overlap;
            float colorB_low = colorB_start;
            float colorB_high = colorG_start + colorB_tranzitZone;
            if (yFloat >= colorB_low && yFloat <= colorB_high)
            {
                float colorB_transitHigh_begin = colorG_start + colorB_tranzitZone * 0.1f;
                if (yFloat < colorB_transitHigh_begin)
                {
                    colorB = 1;
                }
                else if (yFloat >= colorB_transitHigh_begin && yFloat < colorB_high)
                {
                    colorB = 1 - (yFloat - colorB_transitHigh_begin) / (colorB_high - colorB_transitHigh_begin);
                }
            }
            else
            {
                colorB = 0;
            }

            // Green
            float colorG_tranzitZone = hightInterval * overlap;
            float colorG_low = colorG_start - colorG_tranzitZone;
            float colorG_high = colorR_start + colorG_tranzitZone;
            if (yFloat >= colorG_low && yFloat <= colorG_high)
            {
                float colorG_transitLow_end = colorG_low + colorG_tranzitZone * 0.9f;
                float colorG_transitHigh_begin = colorR_start + colorG_tranzitZone * 0.1f;

                if (yFloat <= colorG_transitLow_end)
                {
                    colorG = (yFloat - colorG_low) / (colorG_transitLow_end - colorG_low);
                }

                if (yFloat > colorG_transitLow_end && yFloat < colorG_transitHigh_begin)
                {
                    colorG = 1;
                }

                if (yFloat >= colorG_transitHigh_begin && yFloat < colorG_high)
                {
                    colorG = 1 - (yFloat - colorG_transitHigh_begin) / (colorG_high - colorG_transitHigh_begin);
                }
            }
            else
            {
                colorG = 0;
            }

            // Red
            float colorR_tranzitZone = hightInterval * overlap;
            float colorR_low = colorR_start - colorR_tranzitZone;
            float colorR_high = hightInterval + colorR_tranzitZone;
            if (yFloat >= colorR_low && yFloat <= colorR_high)
            {
                float colorR_transitHigh_begin = hightInterval - colorR_tranzitZone / 2;
                float colorR_transitLow_end = colorR_low + colorR_tranzitZone * 0.9f;

                if (yFloat >= colorR_low && yFloat <= colorR_transitLow_end)
                {
                    colorR = (yFloat - colorR_low) / (colorR_transitLow_end - colorR_low);
                }

                if (yFloat > colorR_transitLow_end && yFloat < colorR_transitHigh_begin)
                {
                    colorR = 1;
                }

                if (yFloat >= colorR_transitHigh_begin && yFloat < colorR_high)
                {
                    colorR = 1 - (yFloat - colorR_transitHigh_begin) / (colorR_high - colorR_transitHigh_begin);
                }
            }
            else
            {
                colorR = 0;
            }

            _COMPUTE_BUFFER_POINTS_DATA[i * stride + 3] = colorR;
            _COMPUTE_BUFFER_POINTS_DATA[i * stride + 4] = colorG;
            _COMPUTE_BUFFER_POINTS_DATA[i * stride + 5] = colorB;
            _COMPUTE_BUFFER_POINTS_DATA[i * stride + 6] = 1.0f;

            if (classification == 2)
            {
                // sadlebrown
                _COMPUTE_BUFFER_POINTS_DATA[i * stride + 3] = 139.0f / 255.0f;
                _COMPUTE_BUFFER_POINTS_DATA[i * stride + 4] = 69.0f / 255.0f;
                _COMPUTE_BUFFER_POINTS_DATA[i * stride + 5] = 19.0f / 255.0f;
                _COMPUTE_BUFFER_POINTS_DATA[i * stride + 6] = 1.0f;
            }

            if (classification == 3)
            {
                // forestgreen
                _COMPUTE_BUFFER_POINTS_DATA[i * stride + 3] = 34.0f / 255.0f;
                _COMPUTE_BUFFER_POINTS_DATA[i * stride + 4] = 139.0f / 255.0f;
                _COMPUTE_BUFFER_POINTS_DATA[i * stride + 5] = 34.0f / 255.0f;
                _COMPUTE_BUFFER_POINTS_DATA[i * stride + 6] = 1.0f;
            }

            intensityNormalized = (laspoint.Intensity - intensityMin) / (intensityMax - intensityMin);
            if (intensityNormalized == 0)
            {
                intensityNormalized = 0.001f;
            }

            _COMPUTE_BUFFER_POINTS_DATA[i * stride + 7] = 0.0f;
            _COMPUTE_BUFFER_POINTS_DATA[i * stride + 8] = 0.0f;
            _COMPUTE_BUFFER_POINTS_DATA[i * stride + 9] = intensityNormalized;
            _COMPUTE_BUFFER_POINTS_DATA[i * stride + 10] = classification;
            _COMPUTE_BUFFER_POINTS_DATA[i * stride + 11] = 0.0f;
        }
        lasreader.Close();

        _POINTS_IN_CLOUD = (int)numberOfPoints;
        pointCloudFormat.NumberOfPoints = (int)numberOfPoints;
        pointCloudFormat.MaxX = xMax;
        pointCloudFormat.MaxY = yMax;
        pointCloudFormat.MaxZ = zMax;
        pointCloudFormat.MinX = xMin;
        pointCloudFormat.MinY = yMin;
        pointCloudFormat.MinZ = zMin;
        SetPointCloudFormat(pointCloudFormat);

        _UPDATE_PROGRESS_IN_GUI = false;

        stopwatchProcess.Stop();
        stopwatchTotal.Stop();
        _PointCloudLoaded = true;
    }

    private void ImportLASThreadBytes(string filePath, int stride, bool invertYZ, int colorScaler)
    {
        Debug.Log("ImportLASThreadBytes - Start\n");

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File " + filePath + " not found");
        }

        LASReaderBuffered las_reader = new LASReaderBuffered(filePath, 1000);
        _LAS_HEADER = las_reader._Header;

        PointCloudFormat pointCloudFormat = new PointCloudFormat();
        pointCloudFormat.Stride = stride;
        pointCloudFormat.AddIntensityValueToColor = 0;
        pointCloudFormat.IntensityAsColor = 1;
        pointCloudFormat.AddIntensityToColor = 0;
        pointCloudFormat.UserColorAsColor = 1;
        pointCloudFormat.GeometrySize = 0.02f;
        pointCloudFormat.XScaleFactor = _LAS_HEADER.Xscalefactor;
        pointCloudFormat.YScaleFactor = _LAS_HEADER.Yscalefactor;
        pointCloudFormat.ZScaleFactor = _LAS_HEADER.Zscalefactor;
        pointCloudFormat.XOffset = _LAS_HEADER.Xoffset;
        pointCloudFormat.YOffset = _LAS_HEADER.Yoffset;
        pointCloudFormat.ZOffset = _LAS_HEADER.Zoffset;


        Debug.Log("ImportLASThreadBytes - Loading LAS data\n");

        switch (las_reader._Header.PointDataFormatID)
        {
            case 0:
                _COMPUTE_BUFFER_CS_LAS_POINTS_BYTES_DATA = las_reader.GetPointsByteData();
                break;
            case 1:
                _COMPUTE_BUFFER_CS_LAS_POINTS_BYTES_DATA = las_reader.GetPointsByteData();
                break;
            case 2:
                _COMPUTE_BUFFER_CS_LAS_POINTS_BYTES_DATA = las_reader.PointsPDRPadded2bytes();
                break;
            case 3:
                _COMPUTE_BUFFER_CS_LAS_POINTS_BYTES_DATA = las_reader.PointsPDRPadded2bytes();
                break;
        }

        _POINTS_IN_CLOUD = (int)_LAS_HEADER.Numberofpointrecords;
        pointCloudFormat.NumberOfPoints = (int)_LAS_HEADER.Numberofpointrecords;

        SetPointCloudFormat(pointCloudFormat);

        las_reader.Close();

        _PointCloudLoaded = true;
    }

    public void ExportLASClassLibrary()
    {
        float[] data = _COMPUTE_BUFFER_POINTS_DATA;

        string pathFull = _config.paths.root_file_path + @"\export\" + _WriteFilename + ".las";

        uint numberOfPoints = (uint)(data.Length / _STRIDE);
        uint exportedPoints = 0;

        string numberOfPointsToExportString = numberOfPoints.ToString("N0", CultureInfo.CreateSpecificCulture("sv-SE"));

        _UPDATE_PROGRESS_IN_GUI = true;

        DateTime now = new DateTime();

        LASHeader lasHeader = new LASHeader();
        lasHeader.FileSignature = "LASF".ToCharArray();
        lasHeader.FileSourceID = 0;
        lasHeader.GlobalEncoding = 0;
        lasHeader.ProjectIDGUIDdata1 = 0;
        lasHeader.ProjectIDGUIDdata2 = 0;
        lasHeader.ProjectIDGUIDdata3 = 0;
        lasHeader.ProjectIDGUIDdata4 = "0".ToCharArray();
        lasHeader.VersionMajor = 1;
        lasHeader.VersionMinor = 2;
        lasHeader.SystemIdentifier = "PCXR".ToCharArray();
        lasHeader.GeneratingSoftware = "PointCloudXR".ToCharArray();
        lasHeader.FileCreationDayofYear = (ushort)now.DayOfYear;
        lasHeader.FileCreationYear = (ushort)now.Year;
        lasHeader.HeaderSize = 227;
        lasHeader.Offsettopointdata = 227;
        lasHeader.NumberofVariableLengthRecords = 0;
        lasHeader.PointDataFormatID = 3;
        lasHeader.PointDataRecordLength = 34;
        lasHeader.Numberofpointsbyreturn = new uint[] { 1, 1, 1, 1, 1 };
        lasHeader.Numberofpointrecords = numberOfPoints;
        lasHeader.Xscalefactor = 0.0001;
        lasHeader.Yscalefactor = 0.0001;
        lasHeader.Zscalefactor = 0.0001;
        lasHeader.Xoffset = 0;
        lasHeader.Yoffset = 0;
        lasHeader.Zoffset = 0;


        Vector3[] bounds = GetBoundaries();
        lasHeader.MinX = bounds[1].x;
        lasHeader.MinY = bounds[1].y;
        lasHeader.MinZ = bounds[1].z;
        lasHeader.MaxX = bounds[0].x;
        lasHeader.MaxY = bounds[0].y;
        lasHeader.MaxZ = bounds[0].z;

        LASWriter laswriter = new LASWriter(pathFull, lasHeader);
        laswriter.WriteHeaderBytes();
        LASPoint lasPoint = new LASPoint();
        for (int i = 0; i < numberOfPoints; i++)
        {
            lasPoint.X = data[i * _STRIDE + 0];
            lasPoint.Z = data[i * _STRIDE + 1];
            lasPoint.Y = data[i * _STRIDE + 2];
            lasPoint.Red = (ushort)(data[i * _STRIDE + 3] * 65535);
            lasPoint.Green = (ushort)(data[i * _STRIDE + 4] * 65535);
            lasPoint.Blue = (ushort)(data[i * _STRIDE + 5] * 65535);
            lasPoint.Intensity = (ushort)(data[i * _STRIDE + 9] * 65535);
            lasPoint.Classification = (byte)data[i * _STRIDE + 10];
            laswriter.WritePoint(lasPoint);

            exportedPoints++;

            //// Update progress in GUI
            if (exportedPoints % ((int)(numberOfPoints * 0.005)) == 0 || exportedPoints == numberOfPoints)
            {
                UpdateProgressInGUI((exportedPoints + 1), (numberOfPoints), (exportedPoints + 1).ToString("N0", CultureInfo.CreateSpecificCulture("sv-SE")) +
                                    " out of " + numberOfPointsToExportString + " exported");
            }
        }
        laswriter.Close();


        _UPDATE_PROGRESS_IN_GUI = false;


        _PointCloudExported = true;
    }

    public void ExportLAS()
    {
        byte[] data = _COMPUTE_BUFFER_POINTS_DATA_BYTES;

        string pathFull = _config.paths.root_file_path + @"\export\" + _WriteFilename + "_Export.las"; ;

        uint numberOfPoints = (uint)(data.Length / (_STRIDE * sizeof(float)));
        uint exportedPoints = 0;
        uint numberOfPointsToDelete = 0;

        uint numberOfPointsToExport = numberOfPoints - numberOfPointsToDelete;

        _UPDATE_PROGRESS_IN_GUI = true;
        LASHeader lasHeader = new LASHeader();

        DateTime now = new DateTime();
        lasHeader.FileSignature = "LASF".ToCharArray();
        lasHeader.FileSourceID = 0;
        lasHeader.GlobalEncoding = 0;
        lasHeader.ProjectIDGUIDdata1 = 0;
        lasHeader.ProjectIDGUIDdata2 = 0;
        lasHeader.ProjectIDGUIDdata3 = 0;
        lasHeader.ProjectIDGUIDdata4 = "0".ToCharArray();
        lasHeader.VersionMajor = 1;
        lasHeader.VersionMinor = 2;
        lasHeader.SystemIdentifier = "PCXR".ToCharArray();
        lasHeader.GeneratingSoftware = "PointCloudXR".ToCharArray();
        lasHeader.FileCreationDayofYear = (ushort)now.DayOfYear;
        lasHeader.FileCreationYear = (ushort)now.Year;
        lasHeader.HeaderSize = 227;
        lasHeader.Offsettopointdata = 227;
        lasHeader.NumberofVariableLengthRecords = 0;
        lasHeader.PointDataFormatID = 3;
        lasHeader.PointDataRecordLength = 34;
        lasHeader.Numberofpointsbyreturn = new uint[] { 1, 1, 1, 1, 1 };
        lasHeader.Numberofpointrecords = numberOfPoints;
        lasHeader.Xscalefactor = _PointCloudFormat.XScaleFactor;
        lasHeader.Yscalefactor = _PointCloudFormat.YScaleFactor;
        lasHeader.Zscalefactor = _PointCloudFormat.ZScaleFactor;
        lasHeader.Xoffset = _PointCloudFormat.XOffset;
        lasHeader.Yoffset = _PointCloudFormat.YOffset;
        lasHeader.Zoffset = _PointCloudFormat.ZOffset;


        int stride_bytes = _STRIDE * sizeof(float);


        Vector3[] bounds = GetBoundaries();
        lasHeader.MinX = bounds[1].x;
        lasHeader.MinY = bounds[1].y;
        lasHeader.MinZ = bounds[1].z;
        lasHeader.MaxX = bounds[0].x;
        lasHeader.MaxY = bounds[0].y;
        lasHeader.MaxZ = bounds[0].z;

        LASPoint lasPoint = new LASPoint();
        LASWriter laswriter = new LASWriter(pathFull, lasHeader);
        laswriter.WriteHeaderBytes();
        for (int i = 0; i < numberOfPoints; i++)
        {
            lasPoint.X = BitConverter.ToSingle(data, i * stride_bytes + 0 * sizeof(float));
            lasPoint.Z = BitConverter.ToSingle(data, i * stride_bytes + 1 * sizeof(float));
            lasPoint.Y = BitConverter.ToSingle(data, i * stride_bytes + 2 * sizeof(float));
            lasPoint.Intensity = (ushort)(BitConverter.ToSingle(data, i * stride_bytes + 9 * sizeof(float)) * 65535);
            lasPoint.Bitfields = 0;
            lasPoint.Classification = (byte)BitConverter.ToSingle(data, i * stride_bytes + 10 * sizeof(float));
            lasPoint.ScanAngleRank = 0; // 16 byte offset
            lasPoint.UserData = 0;
            lasPoint.PointSourceID = (ushort)(BitConverter.ToSingle(data, i * stride_bytes + 18 * sizeof(float)));
            lasPoint.GPSTime = 0;
            lasPoint.Red = (ushort)(BitConverter.ToSingle(data, i * stride_bytes + 3 * sizeof(float)) * 65535);
            lasPoint.Green = (ushort)(BitConverter.ToSingle(data, i * stride_bytes + 4 * sizeof(float)) * 65535);
            lasPoint.Blue = (ushort)(BitConverter.ToSingle(data, i * stride_bytes + 5 * sizeof(float)) * 65535);

            laswriter.WritePoint(lasPoint);

            exportedPoints++;
        }
        laswriter.Close();

        Debug.LogFormat("ExportLASClassLibraryBytes - {0} points exported\n", exportedPoints.ToString("N0", CultureInfo.CreateSpecificCulture("sv-SE")));

        _UPDATE_PROGRESS_IN_GUI = false;

        _PointCloudExported = true;
    }

    public void ExportSelectedLAS()
    {
        byte[] data = GetSelectedPointsByte();

        string pathFull = _config.paths.root_file_path + @"\export\" + _WriteFilename + "_Selected_Export_" + Guid.NewGuid().ToString().Substring(0, 4) + ".las";

        int number_of_selected_points = data.Length / (_STRIDE * sizeof(float));
        uint exportedPoints = 0;

        _UPDATE_PROGRESS_IN_GUI = true;

        LASHeader lasHeader = new LASHeader();

        DateTime now = new DateTime();
        lasHeader.FileSignature = "LASF".ToCharArray();
        lasHeader.FileSourceID = 0;
        lasHeader.GlobalEncoding = 0;
        lasHeader.ProjectIDGUIDdata1 = 0;
        lasHeader.ProjectIDGUIDdata2 = 0;
        lasHeader.ProjectIDGUIDdata3 = 0;
        lasHeader.ProjectIDGUIDdata4 = "0".ToCharArray();
        lasHeader.VersionMajor = 1;
        lasHeader.VersionMinor = 2;
        lasHeader.SystemIdentifier = "PCXR".ToCharArray();
        lasHeader.GeneratingSoftware = "PointCloudXR".ToCharArray();
        lasHeader.FileCreationDayofYear = (ushort)now.DayOfYear;
        lasHeader.FileCreationYear = (ushort)now.Year;
        lasHeader.HeaderSize = 227;
        lasHeader.Offsettopointdata = 227;
        lasHeader.NumberofVariableLengthRecords = 0;
        lasHeader.PointDataFormatID = 3;
        lasHeader.PointDataRecordLength = 34;
        lasHeader.Numberofpointsbyreturn = new uint[] { 1, 1, 1, 1, 1 };
        lasHeader.Numberofpointrecords = (uint)number_of_selected_points;
        lasHeader.Xscalefactor = _PointCloudFormat.XScaleFactor;
        lasHeader.Yscalefactor = _PointCloudFormat.YScaleFactor;
        lasHeader.Zscalefactor = _PointCloudFormat.ZScaleFactor;
        lasHeader.Xoffset = _PointCloudFormat.XOffset;
        lasHeader.Yoffset = _PointCloudFormat.YOffset;
        lasHeader.Zoffset = _PointCloudFormat.ZOffset;

        int stride_bytes = _STRIDE * sizeof(float);

        Vector3[] bounds = GetBoundariesSelected();
        lasHeader.MinX = bounds[1].x;
        lasHeader.MinY = bounds[1].y;
        lasHeader.MinZ = bounds[1].z;
        lasHeader.MaxX = bounds[0].x;
        lasHeader.MaxY = bounds[0].y;
        lasHeader.MaxZ = bounds[0].z;

        LASPoint lasPoint = new LASPoint();
        LASWriter laswriter = new LASWriter(pathFull, lasHeader);
        laswriter.WriteHeaderBytes();
        for (int i = 0; i < number_of_selected_points; i++)
        {
            lasPoint.X = BitConverter.ToSingle(data, i * stride_bytes + 0 * sizeof(float));
            lasPoint.Z = BitConverter.ToSingle(data, i * stride_bytes + 1 * sizeof(float));
            lasPoint.Y = BitConverter.ToSingle(data, i * stride_bytes + 2 * sizeof(float));
            lasPoint.Intensity = (ushort)(BitConverter.ToSingle(data, i * stride_bytes + 9 * sizeof(float)) * 65535);
            lasPoint.Bitfields = 0;
            lasPoint.Classification = (byte)BitConverter.ToSingle(data, i * stride_bytes + 10 * sizeof(float));
            lasPoint.ScanAngleRank = 0; // 16 byte offset
            lasPoint.UserData = 0;
            lasPoint.PointSourceID = (ushort)(BitConverter.ToSingle(data, i * stride_bytes + 18 * sizeof(float)));
            lasPoint.GPSTime = 0;
            lasPoint.Red = (ushort)(BitConverter.ToSingle(data, i * stride_bytes + 3 * sizeof(float)) * 65535);
            lasPoint.Green = (ushort)(BitConverter.ToSingle(data, i * stride_bytes + 4 * sizeof(float)) * 65535);
            lasPoint.Blue = (ushort)(BitConverter.ToSingle(data, i * stride_bytes + 5 * sizeof(float)) * 65535);

            laswriter.WritePoint(lasPoint);

            exportedPoints++;
        }

        laswriter.Close();

        _UPDATE_PROGRESS_IN_GUI = false;

        _PointCloudExported = true;
    }

    public void Export_LAS14_Compute()
    {
        byte[] data = _COMPUTE_BUFFER_POINTS_DATA_BYTES;

        string pathFull = _config.paths.root_file_path + @"\export\" + _WriteFilename + ".las"; ;

        Debug.Log("Export_LAS_Compute - start\n");

        uint numberOfPoints = (uint)(data.Length / (_STRIDE * sizeof(float)));
        print("num_points: " + (data.Length / (_STRIDE * sizeof(float))));

        Debug.LogFormat("Export_LAS_Compute - numberOfPoints: {0}\n", numberOfPoints);

        Vector3[] bounds = GetBoundaries();

        DateTime now = new DateTime();

        LASHeader14 las_header = new LASHeader14();
        las_header.FileSignature = "LASF".ToCharArray();
        las_header.FileSourceID = 0;
        las_header.GlobalEncoding = 0;
        las_header.ProjectIDGUIDdata1 = 0;
        las_header.ProjectIDGUIDdata2 = 0;
        las_header.ProjectIDGUIDdata3 = 0;
        las_header.ProjectIDGUIDdata4 = "0".ToCharArray();
        las_header.VersionMajor = 1;
        las_header.VersionMinor = 4;
        las_header.SystemIdentifier = "PCXR".ToCharArray();
        las_header.GeneratingSoftware = "PointCloudXR".ToCharArray();
        las_header.FileCreationDayofYear = (ushort)now.DayOfYear;
        las_header.FileCreationYear = (ushort)now.Year;
        las_header.HeaderSize = 375;
        las_header.Offsettopointdata = 375;
        las_header.NumberofVariableLengthRecords = 0;
        las_header.PointDataFormatID = 7;
        las_header.PointDataRecordLength = 36;
        las_header.LegacyNumberofpointsbyreturn = new uint[5];
        las_header.LegacyNumberofpointrecords = 0;
        las_header.Xscalefactor = _PointCloudFormat.XScaleFactor;
        las_header.Yscalefactor = _PointCloudFormat.YScaleFactor;
        las_header.Zscalefactor = _PointCloudFormat.ZScaleFactor;
        las_header.Xoffset = _PointCloudFormat.XOffset;
        las_header.Yoffset = _PointCloudFormat.YOffset;
        las_header.Zoffset = _PointCloudFormat.ZOffset;

        las_header.MinX = bounds[1].x;
        las_header.MinY = bounds[1].y;
        las_header.MinZ = bounds[1].z;
        las_header.MaxX = bounds[0].x;
        las_header.MaxY = bounds[0].y;
        las_header.MaxZ = bounds[0].z;

        las_header.StartOfWaveformDataPacketRecord = 0;
        las_header.StartOfFirstExtendedVariableLengthRecord = 0;
        las_header.NumberOfExtendedVariableLengthRecords = 0;
        las_header.NumberOfPointRecords = numberOfPoints;
        las_header.NumberOfPointsByReturn = new ulong[15];


        byte[] las_point_data = Dispatch_PCXR_to_LAS(las_header);

        LAS14Writer laswriter = new LAS14Writer(pathFull, las_header);
        laswriter.WriteHeaderBytes();
        laswriter.WritePointBytesData(las_point_data);
        laswriter.Close();


        _PointCloudExported = true;
    }

    void OnDestroy()
    {
        if (_COMPUTE_BUFFER_CREATED && _COMPUTE_BUFFER_POINTS != null)
        {
            _COMPUTE_BUFFER_POINTS.Release();
            _COMPUTE_BUFFER_COM.Release();
            _COMPUTE_BUFFER_OFFSET_POINTS.Release();
            _COMPUTE_BUFFER_CS_POINTS_BUFFER_APPEND_COUNT.Release();
            _COMPUTE_BUFFER_CS_DRAWPROCEDURAL_INDIRECT_ARGS.Release();
            _COMPUTE_BUFFER_CS_POINTS_BUFFER_APPEND.Release();
            _COMPUTE_BUFFER_CS_DEBUG.Release();
            _COMPUTE_BUFFER_TRANSLATED_POINTS.Release();
            _COMPUTE_BUFFER_CS_DISPATH_INDIRECT_ARGS.Release();
            _COMPUTE_BUFFER_CS_DEBUG.Release();
        }
    }

    private bool ComputeBuffersCreated()
    {
        if (_COMPUTE_BUFFER_POINTS != null &&
           _COMPUTE_BUFFER_COM != null
           )
        {
            return true;
        }
        return false;
    }

    void OnBecameVisible()
    {
        print("*** OnBecameVisible ***");
    }

    // Use OnBecameVisible instead. That is only called once per object, results in less work. 
    void OnRenderObject()
    {
        if (_GPU_DATA_LOADED)
        {
            Camera cam = _UserCamera; // Draws geometry only in Game window

            if (_draw_all_cameras)
            {
                cam = Camera.current; // Draws geometry in editor Scene window and Game windows
            }

            if (_CameraMap.ContainsKey(cam))
            {
                return;
            }

            CommandBuffer cb = new CommandBuffer();
            _CameraMap[cam] = cb;
            cb.name = "PointCloudXRcb";
            if (_culling_enabled)
            {
                cb.DrawProceduralIndirect
                (
                    transform.localToWorldMatrix,
                    _Material,
                    0,
                    MeshTopology.Points,
                    _COMPUTE_BUFFER_CS_DRAWPROCEDURAL_INDIRECT_ARGS,
                    0
                );
            }
            else
            {
                cb.DrawProcedural
                (
                    transform.localToWorldMatrix,
                    _Material,
                    0,
                    MeshTopology.Points,
                    _POINTS_IN_CLOUD,
                    0
                );
            }
            cam.AddCommandBuffer(_CamEventDrawProcedural, cb);

        }
    }

    private void SetMaterialComputeBuffers()
    {
        _Material.SetPass(0);

        _Material.SetBuffer("pointsAll", _COMPUTE_BUFFER_POINTS);

        _Material.SetBuffer("pointsInFrustum", _COMPUTE_BUFFER_CS_POINTS_BUFFER_APPEND);

        _Material.SetBuffer("computeBufferCom", _COMPUTE_BUFFER_COM);

        Graphics.ClearRandomWriteTargets();
        Graphics.SetRandomWriteTarget(3, _COMPUTE_BUFFER_COM, true);
        Graphics.SetRandomWriteTarget(5, _COMPUTE_BUFFER_CS_POINTS_BUFFER_APPEND, true);
        Graphics.SetRandomWriteTarget(6, _COMPUTE_BUFFER_POINTS, true);
    }

    private void FixedUpdate()
    {
        if (ComputeBuffersCreated())
        {
            float[] compute_buffer_com_data = new float[2];

            _COMPUTE_BUFFER_COM.GetData(compute_buffer_com_data);
            _EditToolCollision = (int)compute_buffer_com_data[0];

            if (_EditToolCollision == 1)
            {
                compute_buffer_com_data[0] = 0;
                _COMPUTE_BUFFER_COM.SetData(compute_buffer_com_data);

                OnEditToolCollide?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public void TransferGPUDataSimple()
    {
        _COMPUTE_BUFFER_POINTS_DATA_BYTES = new byte[_PointCloudFormat.NumberOfPoints * _PointCloudFormat.Stride * sizeof(float)];
        _COMPUTE_BUFFER_POINTS.GetData(_COMPUTE_BUFFER_POINTS_DATA_BYTES);

        OnGPUDataTransfereDone?.Invoke(this, EventArgs.Empty);
    }

    public void ReadFilenameSet(string fileName)
    {
        _ReadFilename = fileName;
    }

    public string ReadFilenameGet()
    {
        return _ReadFilename;
    }

    public void WriteFilenameSet(string fileName)
    {
        _WriteFilename = fileName;
    }

    public string WriteFilenameGet()
    {
        return _WriteFilename;
    }


    public void ExportLASThreaded()
    {
        ExportLAS();
    }

    public void ExportSelectedSetActive(bool active)
    {
        _ExportSelected = active;
    }

    public void SaveSelectedSetActive(bool active)
    {
        _SaveSelected = active;
    }

    public void SavePointCloudByThread()
    {
        _PointCloudFormat.AddIntensityValueToColor = _Material.GetInt("_AddIntensityValueToColor");
        _PointCloudFormat.AddIntensityToColor = _Material.GetFloat("_ColorIntensity");
        _PointCloudFormat.GeometrySize = _Material.GetFloat("_PointRadius");
        _PointCloudFormat.UserColorAsColor = _Material.GetFloat("_user_color");

        DispatchPrunePCXR();
        new Thread(SavePointCloudPCXRInternalByThreadPruned).Start();
    }

    public void UserStartPoisitionSet()
    {
        Debug.Log("Setting User Start position and rotation\n");
        _UserStartPosition = _UserGameObject.transform.position;
        _UserStartRotation = _UserGameObject.transform.rotation;
    }

    private void Update()
    {

        if (_GPU_DATA_LOADED)
        {
            GetComponent<MeshRenderer>().enabled = true;
        }

        if (_PointCloudLoaded)
        {
            if (_PointCloudFormat.AddIntensityValueToColor != null)
            {
                _Material.SetInt("_AddIntensityValueToColor", (int)_PointCloudFormat.AddIntensityValueToColor);
            }
            if (_PointCloudFormat.IntensityAsColor != null)
            {
                _Material.SetInt("_IntensityAsColor", (int)_PointCloudFormat.IntensityAsColor);
            }
            if (_PointCloudFormat.GeometrySize != null)
            {
                _Material.SetFloat("_PointRadius", (float)_PointCloudFormat.GeometrySize);
            }

            _Material.SetFloat("_user_color", _PointCloudFormat.UserColorAsColor);

            OnPointCloudLoaded?.Invoke(this, EventArgs.Empty);
            _PointCloudLoaded = false;
        }

        _Material.SetVector("_UserPosition", _UserCamera.transform.position);

        if (_PointCloudSaved)
        {
            OnPointCloudSaved?.Invoke(this, EventArgs.Empty);
            _PointCloudSaved = false;
        }

        if (_PointCloudExported)
        {
            OnPointCloudExported?.Invoke(this, EventArgs.Empty);
            _PointCloudExported = false;
        }

        if (_CentroidSelectedPoints)
        {
            Vector3 offset = new Vector3(_CentroidSelectedPoints.transform.position.x - _CentroidSelectedPointsStartPosition.x,
                                         _CentroidSelectedPoints.transform.position.y - _CentroidSelectedPointsStartPosition.y,
                                         _CentroidSelectedPoints.transform.position.z - _CentroidSelectedPointsStartPosition.z);
            _Material.SetVector("_SelectedPointsOffset", offset);
        }
    }

    private void OnApplicationQuit()
    {
        if (_COMPUTE_BUFFER_POINTS != null)
        {
            _COMPUTE_BUFFER_POINTS.Release();
        }

        if (_COMPUTE_BUFFER_COM != null)
        {
            _COMPUTE_BUFFER_COM.Release();
        }

        foreach (var cam in _CameraMap)
        {
            if (cam.Key)
            {
                cam.Key.RemoveCommandBuffer(_CamEventDrawProcedural, cam.Value);
            }
        }
    }
}
