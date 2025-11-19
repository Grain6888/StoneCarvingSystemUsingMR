using System.IO;
using Unity.Mathematics;
using UnityEngine;
using MarchingCubes;
using Unity.Collections;

namespace MRSculpture
{
    public class MainBehaviour : MonoBehaviour
    {
        /// <summary>
        /// 彫刻素材の生成範囲
        /// </summary>
        public Vector3Int _boundsSize = new(100, 100, 100);

        [SerializeField] float _gridScale = 1.0f;
        [SerializeField] int _triangleBudget = 65536 * 16;
        [SerializeField] ComputeShader _builderCompute = null; // MarchingCubes.compute
        float _builtTargetValue = 0.9f;
        int _voxelCount => _boundsSize.x * _boundsSize.y * _boundsSize.z;
        ComputeBuffer _voxelBuffer;
        MeshBuilder _builder;

        /// <summary>
        /// 彫刻素材のボクセルデータを保存するDataChunk
        /// </summary>
        private DataChunk _voxelDataChunk;

        /// <summary>
        /// ボクセル → メッシュに変換・描画を管理するレンダラ
        /// </summary>
        private Renderer _renderer;

        /// <summary>
        /// ボクセルメッシュ
        /// </summary>
        [SerializeField] private Mesh _voxelMesh;

        /// <summary>
        /// ボクセルマテリアル
        /// </summary>
        [SerializeField] private Material _voxelMaterial;

        [SerializeField] private Transform _mainBehaviourTransform;
        [SerializeField] private GameObject _roundChisel;
        [SerializeField] private GameObject _flatChisel;
        [SerializeField] private GameObject _hammer;
        private HammerController _hammerController;
        private RoundChiselController _roundChiselController;
        private FlatChiselController _flatChiselController;
        private int _impactRange = 0;
        private bool _ready = false;

        private void Awake()
        {
            _hammerController = _hammer.GetComponent<HammerController>();
            _roundChiselController = _roundChisel.GetComponent<RoundChiselController>();
            _flatChiselController = _flatChisel.GetComponent<FlatChiselController>();

            _voxelDataChunk = new DataChunk(_boundsSize.x, _boundsSize.y, _boundsSize.z);

            // ローカル → ワールド座標系の変換行列
            Matrix4x4 localToWorldMatrix = transform.localToWorldMatrix;

            _renderer = new Renderer(_voxelMesh, _voxelMaterial, localToWorldMatrix);

            // フラグ（uint）をそのまま Compute に渡す
            _voxelBuffer = new ComputeBuffer(_voxelCount, sizeof(uint));
            _builder = new MeshBuilder(_boundsSize, _triangleBudget, _builderCompute);

            NewFile();

            _voxelBuffer.SetData(_voxelDataChunk.DataArray);
            _builder.BuildIsosurface(_voxelBuffer, _builtTargetValue, _gridScale);
            GetComponent<MeshFilter>().sharedMesh = _builder.Mesh;
            Debug.Log("MRSculpture : Initial mesh built.");
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

                for (int y = 0; y < _voxelDataChunk.yLength; y++)
                {
                    DataChunk xzLayer = _voxelDataChunk.GetXZLayer(y);
                    _renderer.AddRenderBuffer(xzLayer, y);
                }

                Debug.Log("MRSculpture DataChunk loaded from file.");
            }
            else
            {
                Debug.Log("MRSculpture DataChunk load failed from file.");
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
                        int index = _voxelDataChunk.GetIndex(x, y, z);
                        //_voxelDataChunk.RemoveAllFlags(index);
                        _voxelDataChunk.AddFlag(index, CellFlags.IsFilled);
                    }
                }
            }

            _voxelBuffer.SetData(_voxelDataChunk.DataArray);

            Debug.Log("MRSculpture : New voxel flags uploaded to GPU from DataChunk.");

            _ready = true;
        }

        private void Update()
        {
            if (!_ready) return;

            _impactRange = Mathf.Min(70, (int)(_hammerController.ImpactMagnitude * 15));

            Vector3 boundingBoxSize = transform.localToWorldMatrix.MultiplyPoint(new Vector3(_boundsSize.x, _boundsSize.y, _boundsSize.z));
            Bounds boundingBox = new();
            boundingBox.SetMinMax(Vector3.zero, boundingBoxSize);

            if (_impactRange > 0)
            {
                _roundChiselController.Carve(ref _voxelDataChunk, in _impactRange, ref _renderer);
                _flatChiselController.Carve(ref _voxelDataChunk, in _impactRange, ref _renderer);
                _voxelBuffer.SetData(_voxelDataChunk.DataArray);
                _builder.BuildIsosurface(_voxelBuffer, _builtTargetValue, _gridScale);
                GetComponent<MeshFilter>().sharedMesh = _builder.Mesh;
                Debug.Log("MRSculpture : Mesh updated.");
            }

            //_renderer.RenderMeshes(new Bounds(boundingBoxSize * 0.5f, boundingBoxSize));

            //_frameCount = 0;
        }

        private void OnDestroy()
        {
            _renderer.Dispose();
            _voxelDataChunk.Dispose();
            _voxelBuffer.Dispose();
            _builder.Dispose();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            int maxVoxelsToDraw = 100 * 100 * 100;
            if (_voxelCount > maxVoxelsToDraw)
            {
                Debug.LogWarning($"MRSculpture : ボクセル数 {_voxelCount} が描画可能な最大ボクセル数 {maxVoxelsToDraw} を超えました．Gizmoの描画は省略されます．");
                return;
            }

            // Sceneビューのみ
            Camera cam = Camera.current;
            if (cam == null || cam.cameraType != CameraType.SceneView)
            {
                Debug.LogWarning("MRSculpture : Sceneビュー以外ではGizmoは描画されません．");
                return;
            }

            // DataChunk が有効か
            if (!_voxelDataChunk.DataArray.IsCreated)
            {
                Debug.LogWarning("MRSculpture : DataChunkが有効ではありません．Gizmoの描画は省略されます．");
                return;
            }

            // すべてのボクセルを描画（IsFilled: 緑、未充填: 赤）
            Vector3 cellSize = Vector3.one * _gridScale;
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
                        Vector3 centerLocalScaled = centerLocal * _gridScale;
                        Vector3 centerWorld = transform.TransformPoint(centerLocalScaled);
                        Gizmos.DrawWireCube(centerWorld, cellSize);
                    }
                }
            }
        }
    }
#endif
}
