using System.IO;
using Unity.Mathematics;
using UnityEngine;

namespace MRSculpture
{
    public class MainBehaviour : MonoBehaviour
    {
        /// <summary>
        /// 彫刻素材の生成範囲
        /// </summary>
        public int3 _boundsSize = new(100, 100, 100);

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
            for (int y = 0; y < _voxelDataChunk.yLength; y++)
            {
                DataChunk xzLayer = _voxelDataChunk.GetXZLayer(y);

                for (int x = 0; x < _voxelDataChunk.xLength; x++)
                {
                    for (int z = 0; z < _voxelDataChunk.zLength; z++)
                    {
                        int index = xzLayer.GetIndex(x, 0, z);
                        xzLayer.AddFlag(index, CellFlags.IsFilled);
                    }
                }
                //_renderer.AddRenderBuffer(xzLayer, y);
            }

            Debug.Log("MRSculpture New DataChunk created.");

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
                rendered = true;
            }

            _impactRange = Mathf.Min(10, (int)(_hammerController.ImpactMagnitude * 5));

            Vector3 boundingBoxSize = transform.localToWorldMatrix.MultiplyPoint(new Vector3(_boundsSize.x, _boundsSize.y, _boundsSize.z));
            Bounds boundingBox = new();
            boundingBox.SetMinMax(Vector3.zero, boundingBoxSize);

            if (_impactRange > 0)
            {
                _roundChiselController.Carve(ref _voxelDataChunk, in _impactRange, ref _renderer);
                _flatChiselController.Carve(ref _voxelDataChunk, in _impactRange, ref _renderer);
            }

            _renderer.RenderMeshes(new Bounds(boundingBoxSize * 0.5f, boundingBoxSize));
        }

        private void OnDestroy()
        {
            _renderer.Dispose();
            _voxelDataChunk.Dispose();
        }
    }
}
