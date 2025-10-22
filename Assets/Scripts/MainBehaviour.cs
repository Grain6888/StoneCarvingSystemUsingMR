using System.IO;
using Oculus.Haptics;
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
            //// 楕円体の中心座標（グリッド中央）
            //float centerX = (_voxelDataChunk.xLength - 1) / 2.0f;
            //float centerY = (_voxelDataChunk.yLength - 1) / 2.0f;
            //float centerZ = (_voxelDataChunk.zLength - 1) / 2.0f;

            //// 楕円体の各軸半径
            //float radiusX = _voxelDataChunk.xLength / 2.0f;
            //float radiusY = _voxelDataChunk.yLength / 2.0f;
            //float radiusZ = _voxelDataChunk.zLength / 2.0f;

            //// 内側楕円体の各軸半径（最低厚み5ブロック分小さく）
            //float innerRadiusX = Mathf.Max(radiusX - 5.0f, 0.0f);
            //float innerRadiusY = Mathf.Max(radiusY - 5.0f, 0.0f);
            //float innerRadiusZ = Mathf.Max(radiusZ - 5.0f, 0.0f);

            for (int y = 0; y < _voxelDataChunk.yLength; y++)
            {
                DataChunk xzLayer = _voxelDataChunk.GetXZLayer(y);

                for (int x = 0; x < _voxelDataChunk.xLength; x++)
                {
                    for (int z = 0; z < _voxelDataChunk.zLength; z++)
                    {
                        //// 楕円体の方程式で判定
                        //float nx = (x - centerX) / radiusX;
                        //float ny = (y - centerY) / radiusY;
                        //float nz = (z - centerZ) / radiusZ;
                        //float nxi = (x - centerX) / innerRadiusX;
                        //float nyi = (y - centerY) / innerRadiusY;
                        //float nzi = (z - centerZ) / innerRadiusZ;

                        //// 外側楕円体の内側かつ内側楕円体の外側のみ埋める
                        //if (nx * nx + ny * ny + nz * nz <= 1.0f &&
                        //    nxi * nxi + nyi * nyi + nzi * nzi >= 1.0f)
                        //{
                        //    int index = xzLayer.GetIndex(x, 0, z);
                        //    xzLayer.AddFlag(index, CellFlags.IsFilled);
                        //}

                        int index = xzLayer.GetIndex(x, y, z);
                        xzLayer.AddFlag(index, CellFlags.IsFilled);
                    }
                }
                _renderer.AddRenderBuffer(xzLayer, y);
            }

            Debug.Log("MRSculpture New DataChunk created.");

            _ready = true;
        }

        private void Update()
        {
            if (!_ready) return;

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
