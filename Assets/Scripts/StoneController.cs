using System.IO;
using UnityEngine;
using MarchingCubes;

namespace MRSculpture
{
    public class StoneController : MonoBehaviour
    {
        /// <summary>
        /// 彫刻素材の生成範囲
        /// </summary>
        public Vector3Int _boundsSize = new(100, 100, 100);

        private BoxCollider _boundsCollider = null;

        [SerializeField] int _triangleBudget = 65536 * 16;
        [SerializeField] ComputeShader _builderCompute = null;
        float _builtTargetValue = 0.9f;
        int _voxelCount => _boundsSize.x * _boundsSize.y * _boundsSize.z;
        ComputeBuffer _voxelBuffer;
        MeshBuilder _builder;

        /// <summary>
        /// 彫刻素材のボクセルデータを保存するDataChunk
        /// </summary>
        private DataChunk _voxelDataChunk;

        [SerializeField] private GameObject _pinChisel;
        private ChiselController _pinChiselController;
        [SerializeField] private GameObject _roundChisel;
        private RoundChiselController _roundChiselController;
        [SerializeField] private GameObject _flatChisel;
        private FlatChiselController _flatChiselController;
        [SerializeField] private GameObject _hammer;
        private HammerController _hammerController;
        [SerializeField] private GameObject _tester;
        private ChiselController _testerController;
        private int _impactRange = 0;
        private bool _ready = false;

        private void Awake()
        {
            _boundsCollider = GetComponent<BoxCollider>();
            _boundsCollider.size = new(
                _boundsSize.x,
                _boundsSize.y,
                _boundsSize.z
            );
            _boundsCollider.center = new(
                _boundsSize.x * 0.5f,
                _boundsSize.y * 0.5f,
                _boundsSize.z * 0.5f
            );
            _hammerController = _hammer.GetComponent<HammerController>();
            _pinChiselController = _pinChisel.GetComponent<ChiselController>();
            _roundChiselController = _roundChisel.GetComponent<RoundChiselController>();
            _flatChiselController = _flatChisel.GetComponent<FlatChiselController>();
            _testerController = _tester.GetComponent<ChiselController>();

            _voxelDataChunk = new DataChunk(_boundsSize.x, _boundsSize.y, _boundsSize.z);

            // フラグ（uint）をそのまま Compute に渡す
            _voxelBuffer = new ComputeBuffer(_voxelCount, sizeof(uint));
            _builder = new MeshBuilder(_boundsSize, _triangleBudget, _builderCompute);

            NewFile();
        }

        private void Start()
        {
            _pinChiselController.AttachDataChunk(ref _voxelDataChunk);
            _testerController.AttachDataChunk(ref _voxelDataChunk);
        }

        public async void LoadFile()
        {
            string fileName = "model.dat";
            string path = Path.Combine(Application.persistentDataPath, fileName);

            if (File.Exists(path))
            {
                await System.Threading.Tasks.Task.Run(() =>
                {
                    DataChunk.LoadDat(fileName, ref _voxelDataChunk);
                });

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"MRSculpture : DataChunk loaded from {fileName}");
#endif
            }
            else
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("MRSculpture : DataChunk load failed.");
#endif
                NewFile();
            }

            _ready = true;
        }

        public async void SaveFile()
        {
            string fileName = "model.dat";

            await System.Threading.Tasks.Task.Run(() =>
            {
                _voxelDataChunk.SaveDat(fileName);
            });
        }

        public void NewFile()
        {
            for (int y = 0; y < _voxelDataChunk.yLength; y++)
            {
                for (int z = 0; z < _voxelDataChunk.zLength; z++)
                {
                    for (int x = 0; x < _voxelDataChunk.xLength; x++)
                    {
                        _voxelDataChunk.AddFlag(x, y, z, CellFlags.IsFilled);
                    }
                }
            }

            _voxelBuffer.SetData(_voxelDataChunk.DataArray);
            _builder.BuildIsosurface(_voxelBuffer, _builtTargetValue);
            GetComponent<MeshFilter>().sharedMesh = _builder.Mesh;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("MRSculpture : New DataChunk created.");
#endif

            _ready = true;
        }

        //private void Update()
        //{
        //    if (!_ready) return;

        //    _impactRange = Mathf.Min(70, (int)(_hammerController.ImpactMagnitude * 15));

        //    Vector3 boundingBoxSize = transform.localToWorldMatrix.MultiplyPoint(new Vector3(_boundsSize.x, _boundsSize.y, _boundsSize.z));
        //    Bounds boundingBox = new();
        //    boundingBox.SetMinMax(Vector3.zero, boundingBoxSize);

        //    if (_impactRange > 0)
        //    {
        //        //_pinChiselController.Carve(ref _voxelDataChunk, in _impactRange);
        //        //_roundChiselController.Carve(ref _voxelDataChunk, in _impactRange);
        //        //_flatChiselController.Carve(ref _voxelDataChunk, in _impactRange);
        //        _testerController.Carve(ref _voxelDataChunk, in _impactRange);

        //        UpdateMesh();
        //    }
        //}

        public void UpdateMesh()
        {
            _voxelBuffer.SetData(_voxelDataChunk.DataArray);
            _builder.BuildIsosurface(_voxelBuffer, _builtTargetValue);
            GetComponent<MeshFilter>().sharedMesh = _builder.Mesh;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("MRSculpture : Mesh updated.");
#endif
        }

        private void OnDestroy()
        {
            _voxelDataChunk.Dispose();
            _voxelBuffer.Dispose();
            _builder.Dispose();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            int maxVoxelsToDraw = 100 * 100 * 100;
            if (_voxelCount > maxVoxelsToDraw)
            {
                Debug.LogWarning($"MRSculpture : The number of voxels {_voxelCount} exceeds the maximum drawable voxel count {maxVoxelsToDraw}. Gizmo will be skipped.");
                return;
            }

            // Sceneビューのみ
            Camera cam = Camera.current;
            if (cam == null || cam.cameraType != CameraType.SceneView)
            {
                Debug.LogWarning("MRSculpture : Gizmos are not drawn outside the Scene view.");
                return;
            }

            // DataChunk が有効か
            if (!_voxelDataChunk.DataArray.IsCreated)
            {
                Debug.LogWarning("MRSculpture : DataChunk is not valid. Gizmo drawing will be skipped.");
                return;
            }

            // すべてのボクセルを描画（IsFilled: 緑、未充填: 赤）
            for (int y = 0; y < _voxelDataChunk.yLength; y++)
            {
                for (int z = 0; z < _voxelDataChunk.zLength; z++)
                {
                    for (int x = 0; x < _voxelDataChunk.xLength; x++)
                    {
                        int index = _voxelDataChunk.GetIndex(x, y, z);
                        bool filled = _voxelDataChunk.HasFlag(index, CellFlags.IsFilled);
                        Gizmos.color = filled ? Color.green : Color.red;

                        Vector3 centerLocal = new(x + 0.5f, y + 0.5f, z + 0.5f);
                        Vector3 centerWorld = transform.TransformPoint(centerLocal);
                        Gizmos.DrawWireCube(centerWorld, Vector3.one);
                    }
                }
            }
        }
#endif
    }
}
