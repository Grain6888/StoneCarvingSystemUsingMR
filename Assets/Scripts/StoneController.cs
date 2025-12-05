using System.IO;
using UnityEngine;
using MarchingCubes;

namespace MRSculpture
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(BoxCollider))]
    public class StoneController : MonoBehaviour
    {
        /// <summary>
        /// 石材の生成範囲 (単位はボクセル)
        /// </summary>
        [SerializeField] private Vector3Int _boundsSize = new(100, 100, 100);

        /// <summary>
        /// 彫刻素材のボクセルデータを格納する DataChunk
        /// </summary>
        private DataChunk _voxelDataChunk;

        /// <summary>
        /// 石材の BoxCollider
        /// </summary>
        private BoxCollider _boundsCollider = null;

        /// <summary>
        /// メッシュ生成時の三角形の最大数
        /// </summary>
        [SerializeField] int _triangleBudget = 65536 * 16;

        /// <summary>
        /// メッシュ生成に使用する ComputeShader
        /// </summary>
        [SerializeField] ComputeShader _builderCompute = null;

        /// <summary>
        /// 等値面の値
        /// </summary>
        private readonly float _builtTargetValue = 0.9f;

        /// <summary>
        /// 範囲内の総ボクセル数
        /// </summary>
        private int VoxelCount => _boundsSize.x * _boundsSize.y * _boundsSize.z;

        /// <summary>
        /// ComputeShader にボクセルデータを渡すための ComputeBuffer
        /// </summary>
        private ComputeBuffer _voxelBuffer;

        /// <summary>
        /// メッシュ生成処理
        /// </summary>
        private MeshBuilder _builder;

        /// <summary>
        /// 精密ノミ
        /// </summary>
        [SerializeField] private GameObject _pinChisel;

        /// <summary>
        /// 精密ノミ用コントローラ
        /// </summary>
        private ChiselController _pinChiselController;

        /// <summary>
        /// 丸ノミ
        /// </summary>
        [SerializeField] private GameObject _roundChisel;

        /// <summary>
        /// 丸ノミ用のコントローラ
        /// </summary>
        private ChiselController _roundChiselController;

        /// <summary>
        /// 平ノミ
        /// </summary>
        [SerializeField] private GameObject _flatChisel;

        /// <summary>
        /// 平ノミ用のコントローラ
        /// </summary>
        private ChiselController _flatChiselController;

        private void Awake()
        {
            _boundsCollider = GetComponent<BoxCollider>();
            SetupBoundsCollider();

            _roundChiselController = _roundChisel.GetComponent<ChiselController>();
            _pinChiselController = _pinChisel.GetComponent<ChiselController>();
            _flatChiselController = _flatChisel.GetComponent<ChiselController>();

            _voxelDataChunk = new DataChunk(_boundsSize.x, _boundsSize.y, _boundsSize.z);

            _voxelBuffer = new ComputeBuffer(VoxelCount, sizeof(uint));
            _builder = new MeshBuilder(_boundsSize, _triangleBudget, _builderCompute);

            NewFile();
        }

        private void Start()
        {
            _roundChiselController.AttachDataChunk(ref _voxelDataChunk);
            _pinChiselController.AttachDataChunk(ref _voxelDataChunk);
            _flatChiselController.AttachDataChunk(ref _voxelDataChunk);
        }

        /// <summary>
        /// <para>
        /// ファイルからボクセルデータを読み込む．ファイルが存在しない場合は新規作成する．
        /// </para>
        /// </summary>
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
        }

        /// <summary>
        /// <para>
        /// 現在のボクセルデータをファイルに保存する．
        /// </para>
        /// </summary>
        public async void SaveFile()
        {
            string fileName = "model.dat";

            await System.Threading.Tasks.Task.Run(() =>
            {
                _voxelDataChunk.SaveDat(fileName);
            });
        }

        /// <summary>
        /// DataChunk を全て埋まった状態で初期化し，初期メッシュを生成する．
        /// </summary>
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
        }

        /// <summary>
        /// 石材の生成範囲から石材の BoxCollider を設定する．
        /// </summary>
        private void SetupBoundsCollider()
        {
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
        }

        /// <summary>
        /// 石材のメッシュを更新する．
        /// </summary>
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
        /// <summary>
        /// <para>
        /// デバッグ用．
        /// </para>
        /// <para>
        /// Scene ビューで DataChunk 内の全ボクセルを Gizmo で表示する．
        /// </para>
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            int maxVoxelsToDraw = 100 * 100 * 100;
            if (VoxelCount > maxVoxelsToDraw)
            {
                Debug.LogWarning($"MRSculpture : The number of voxels {VoxelCount} exceeds the maximum drawable voxel count {maxVoxelsToDraw}. Gizmo will be skipped.");
                return;
            }

            Camera cam = Camera.current;
            if (cam == null || cam.cameraType != CameraType.SceneView)
            {
                Debug.LogWarning("MRSculpture : Gizmos are not drawn outside the Scene view.");
                return;
            }

            if (!_voxelDataChunk.DataArray.IsCreated)
            {
                Debug.LogWarning("MRSculpture : DataChunk is not valid. Gizmo drawing will be skipped.");
                return;
            }

            for (int y = 0; y < _voxelDataChunk.yLength; y++)
            {
                for (int z = 0; z < _voxelDataChunk.zLength; z++)
                {
                    for (int x = 0; x < _voxelDataChunk.xLength; x++)
                    {
                        int index = _voxelDataChunk.GetIndex(x, y, z);
                        bool filled = _voxelDataChunk.HasFlag(index, CellFlags.IsFilled);
                        Gizmos.color = filled ? Color.green : Color.red; // 埋 : 緑, 空 : 赤

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
