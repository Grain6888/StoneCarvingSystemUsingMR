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
            // DataChunk を基準にボクセル状態を管理
            int xSize = _boundsSize.x;
            int ySize = _boundsSize.y;
            int zSize = _boundsSize.z;

            for (int y = 0; y < ySize; y++)
            {
                for (int z = 0; z < zSize; z++)
                {
                    for (int x = 0; x < xSize; x++)
                    {
                        int index = x + (z * xSize) + (y * xSize * zSize);

                        // 立方体の表面から2マス分は空、それ以外は埋める
                        bool nearSurface = (x <= 1) || (x >= xSize - 1) ||
                                           (y <= 1) || (y >= ySize - 1) ||
                                           (z <= 1) || (z >= zSize - 1);

                        if (nearSurface)
                        {
                            _voxelDataChunk.RemoveAllFlags(index);
                        }
                        else
                        {
                            _voxelDataChunk.AddFlag(index, CellFlags.IsFilled);
                        }
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

        //private void OnDrawGizmos()
        //{
        //    // Sceneビューのみ
        //    var cam = Camera.current;
        //    if (cam == null || cam.cameraType != CameraType.SceneView) return;

        //    // DataChunk が有効か
        //    if (!_voxelDataChunk.DataArray.IsCreated) return;

        //    int xSize = _voxelDataChunk.xLength;
        //    int ySize = _voxelDataChunk.yLength;
        //    int zSize = _voxelDataChunk.zLength;

        //    Vector3 cellSize = Vector3.one * _gridScale;

        //    // すべてのボクセルを描画（IsFilled: 緑、未充填: 赤）
        //    for (int y = 0; y < ySize; y++)
        //    {
        //        for (int z = 0; z < zSize; z++)
        //        {
        //            for (int x = 0; x < xSize; x++)
        //            {
        //                int index = _voxelDataChunk.GetIndex(x, y, z);
        //                bool filled = _voxelDataChunk.HasFlag(index, CellFlags.IsFilled);
        //                Gizmos.color = filled ? Color.green : Color.red;

        //                Vector3 centerLocal = new Vector3((x + 0.5f) * _gridScale, (y + 0.5f) * _gridScale, (z + 0.5f) * _gridScale);
        //                Vector3 centerWS = transform.TransformPoint(centerLocal);
        //                Gizmos.DrawWireCube(centerWS, cellSize);
        //            }
        //        }
        //    }
        //}
    }
}
