using System.IO;
using Unity.Mathematics;
using UnityEngine;
using MarchingCubes;

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
        [SerializeField] ComputeShader _builderCompute = null;   // MarchingCubes.compute
        float _builtTargetValue = 1.0f;
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

            _voxelBuffer = new ComputeBuffer(_voxelCount, sizeof(float));
            _builder = new MeshBuilder(_boundsSize, _triangleBudget, _builderCompute);

            NewFile();
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
            // VoxelBuffer を指定の規則で初期化
            int xSize = _boundsSize.x;
            int ySize = _boundsSize.y;
            int zSize = _boundsSize.z;
            float[] voxels = new float[_voxelCount];

            for (int y = 0; y < ySize; y++)
            {
                for (int z = 0; z < zSize; z++)
                {
                    for (int x = 0; x < xSize; x++)
                    {
                        // 指定のインデックス規則
                        int index = x + (z * xSize) + (y * xSize * zSize);

                        // 立方体の表面から2マス以内は 0.0、それ以外は 1.0
                        bool nearSurface = (x < 2) || (x >= xSize - 2) ||
                                           (y < 2) || (y >= ySize - 2) ||
                                           (z < 2) || (z >= zSize - 2);
                        voxels[index] = nearSurface ? 0.0f : 1.0f;
                    }
                }
            }

            _voxelBuffer.SetData(voxels);

            Debug.Log("MRSculpture New voxel buffer created.");

            _ready = true;
        }

        private bool rendered = false;

        private void Update()
        {
            if (!_ready) return;

            if (rendered)
            {
                return;
            }
            else
            {
                _builder.BuildIsosurface(_voxelBuffer, _builtTargetValue, _gridScale);
                GetComponent<MeshFilter>().sharedMesh = _builder.Mesh;
                rendered = true;
            }

            //_impactRange = Mathf.Min(10, (int)(_hammerController.ImpactMagnitude * 5));

            //Vector3 boundingBoxSize = transform.localToWorldMatrix.MultiplyPoint(new Vector3(_boundsSize.x, _boundsSize.y, _boundsSize.z));
            //Bounds boundingBox = new();
            //boundingBox.SetMinMax(Vector3.zero, boundingBoxSize);

            //if (_impactRange > 0)
            //{
            //    _roundChiselController.Carve(ref _voxelDataChunk, in _impactRange, ref _renderer);
            //    _flatChiselController.Carve(ref _voxelDataChunk, in _impactRange, ref _renderer);
            //}

            //_renderer.RenderMeshes(new Bounds(boundingBoxSize * 0.5f, boundingBoxSize));
        }

        private void OnDestroy()
        {
            _renderer.Dispose();
            _voxelDataChunk.Dispose();
            _voxelBuffer.Dispose();
            _builder.Dispose();
        }
    }
}
