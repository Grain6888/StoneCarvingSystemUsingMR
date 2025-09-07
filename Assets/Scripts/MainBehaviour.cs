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

        [SerializeField] private GameObject _impactCenter;
        [SerializeField] private Transform _mainBehaviourTransform;
        [SerializeField] private GameObject _chisel;
        [SerializeField] private GameObject _hammer;
        private ImpactRangeGetter _impactRangeGetter;
        public HapticSource hapticSource;
        [SerializeField] private AudioSource _audioSource;
        private int _impactRange = 0;
        private bool _ready = false;


        private void Awake()
        {
            _impactRangeGetter = _hammer.GetComponent<ImpactRangeGetter>();
        }

        public void LoadFile()
        {
            _voxelDataChunk.Dispose();
            _voxelDataChunk = new DataChunk(_boundsSize.x, _boundsSize.y, _boundsSize.z);

            // ローカル → ワールド座標系の変換行列
            Matrix4x4 localToWorldMatrix = transform.localToWorldMatrix;

            _renderer = new Renderer(_voxelMesh, _voxelMaterial, localToWorldMatrix);

            string fileName = "isfilled.txt";
            string path = Path.Combine(Application.persistentDataPath, fileName);

            if (File.Exists(path))
            {
                DataChunk.LoadIsFilledTxt("isfilled.txt", ref _voxelDataChunk);

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

                // 全レイヤ分処理
                for (int y = 0; y < _voxelDataChunk.yLength; y++)
                {
                    DataChunk xzLayer = _voxelDataChunk.GetXZLayer(y);

                    for (int i = 0; i < xzLayer.Length; i++)
                    {
                        xzLayer.AddFlag(i, CellFlags.IsFilled);
                    }
                    _renderer.AddRenderBuffer(xzLayer, y);
                }

                Debug.Log("MRSculpture New DataChunk created.");
            }

            _ready = true;
        }

        public void SaveFile()
        {
            string fileName = "isfilled.txt";
            string path = Path.Combine(Application.persistentDataPath, fileName);
            _voxelDataChunk.SaveIsFilledTxt("isfilled.txt");
            Debug.Log("MRSculpture DataChunk saved to file: " + path);
        }

        public void NewFile()
        {
            _voxelDataChunk.Dispose();
            _voxelDataChunk = new DataChunk(_boundsSize.x, _boundsSize.y, _boundsSize.z);

            // ローカル → ワールド座標系の変換行列
            Matrix4x4 localToWorldMatrix = transform.localToWorldMatrix;

            _renderer = new Renderer(_voxelMesh, _voxelMaterial, localToWorldMatrix);

            // 楕円体の中心座標（グリッド中央）
            float centerX = (_voxelDataChunk.xLength - 1) / 2.0f;
            float centerY = (_voxelDataChunk.yLength - 1) / 2.0f;
            float centerZ = (_voxelDataChunk.zLength - 1) / 2.0f;

            // 楕円体の各軸半径
            float radiusX = _voxelDataChunk.xLength / 2.0f;
            float radiusY = _voxelDataChunk.yLength / 2.0f;
            float radiusZ = _voxelDataChunk.zLength / 2.0f;

            // 内側楕円体の各軸半径（最低厚み5ブロック分小さく）
            float innerRadiusX = Mathf.Max(radiusX - 5.0f, 0.0f);
            float innerRadiusY = Mathf.Max(radiusY - 5.0f, 0.0f);
            float innerRadiusZ = Mathf.Max(radiusZ - 5.0f, 0.0f);

            for (int y = 0; y < _voxelDataChunk.yLength; y++)
            {
                DataChunk xzLayer = _voxelDataChunk.GetXZLayer(y);

                for (int x = 0; x < _voxelDataChunk.xLength; x++)
                {
                    for (int z = 0; z < _voxelDataChunk.zLength; z++)
                    {
                        // ボクセル中心座標
                        float voxelX = x + 0.5f;
                        float voxelY = y + 0.5f;
                        float voxelZ = z + 0.5f;

                        // 外側楕円体方程式判定
                        float normX = (voxelX - centerX) / radiusX;
                        float normY = (voxelY - centerY) / radiusY;
                        float normZ = (voxelZ - centerZ) / radiusZ;
                        float ellipsoid = normX * normX + normY * normY + normZ * normZ;

                        // 内側楕円体方程式判定
                        float innerNormX = innerRadiusX > 0.0f ? (voxelX - centerX) / innerRadiusX : 0.0f;
                        float innerNormY = innerRadiusY > 0.0f ? (voxelY - centerY) / innerRadiusY : 0.0f;
                        float innerNormZ = innerRadiusZ > 0.0f ? (voxelZ - centerZ) / innerRadiusZ : 0.0f;
                        float innerEllipsoid = (innerRadiusX > 0.0f && innerRadiusY > 0.0f && innerRadiusZ > 0.0f)
                            ? innerNormX * innerNormX + innerNormY * innerNormY + innerNormZ * innerNormZ
                            : -1.0f; // 半径が0以下の場合は内側判定を無効化

                        // 外側楕円体内かつ内側楕円体外のみ埋める
                        if (ellipsoid <= 1.0f && innerEllipsoid > 1.0f)
                        {
                            xzLayer.AddFlag(x, 0, z, CellFlags.IsFilled);
                        }
                    }
                }
                _renderer.AddRenderBuffer(xzLayer, y);
            }

            Debug.Log("MRSculpture New DataChunk created.");

            _ready = true;
        }

        private void Update()
        {
            if (!_ready)
            {
                return;
            }

            _impactRange = Mathf.Min(10, (int)(_impactRangeGetter.ImpactMagnitude * 5));

            Vector3 boundingBoxSize = transform.localToWorldMatrix.MultiplyPoint(new Vector3(_boundsSize.x, _boundsSize.y, _boundsSize.z));
            Bounds boundingBox = new();
            boundingBox.SetMinMax(Vector3.zero, boundingBoxSize);

            // 破壊中心のワールド座標を取得
            Vector3 impactCenterWorldPosition = _impactCenter.transform.position;
            // 破壊中心のワールド座標を、MainBehaviourのローカル座標系に変換
            Vector3 _impactCenterLocalPosition = _mainBehaviourTransform.InverseTransformPoint(impactCenterWorldPosition);
            // 破壊中心位置を基準に、最も近いボクセルグリッド座標（整数）を算出
            Vector3Int center = new(
                Mathf.RoundToInt(_impactCenterLocalPosition.x),
                Mathf.RoundToInt(_impactCenterLocalPosition.y),
                Mathf.RoundToInt(_impactCenterLocalPosition.z)
            );

            // X方向の探索範囲（visibleDistance分だけ前後に拡張、範囲外はクランプ）
            int minX = Mathf.Max(0, center.x - _impactRange);
            int maxX = Mathf.Min(_voxelDataChunk.xLength - 1, center.x + _impactRange);
            // Y方向の探索範囲
            int minY = Mathf.Max(0, center.y - _impactRange);
            int maxY = Mathf.Min(_voxelDataChunk.yLength - 1, center.y + _impactRange);
            // Z方向の探索範囲
            int minZ = Mathf.Max(0, center.z - _impactRange);
            int maxZ = Mathf.Min(_voxelDataChunk.zLength - 1, center.z + _impactRange);

            // 距離判定用にvisibleDistanceの2乗を事前計算（パフォーマンス向上のため）
            float sqrVisibleDistance = _impactRange * _impactRange;

            // 各XZレイヤごとに処理
            for (int y = minY; y <= maxY; y++)
            {
                // Y層のXZ平面のDataChunkを取得
                DataChunk xzLayer = _voxelDataChunk.GetXZLayer(y);
                bool layerBufferNeedsUpdate = false; // レンダーバッファ更新が必要かどうか

                // X方向の範囲をループ
                for (int x = minX; x <= maxX; x++)
                {
                    // Z方向の範囲をループ
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        // セルのローカル空間での中心座標を計算（各軸+0.5でセル中心）
                        Vector3 cellLocalPos = new(x + 0.5f, y + 0.5f, z + 0.5f);

                        // 破壊中心との距離がvisibleDistance以内か判定
                        if ((cellLocalPos - _impactCenterLocalPosition).sqrMagnitude > sqrVisibleDistance)
                            continue; // 範囲外ならスキップ

                        // ハプティクスを再生
                        hapticSource.Play();
                        // 破壊音を再生
                        _audioSource.Play();
                        // 対象セルからIsFilledフラグを削除
                        xzLayer.RemoveFlag(x, 0, z, CellFlags.IsFilled);
                        layerBufferNeedsUpdate = true; // このレイヤーのバッファ更新が必要
                    }
                }
                // 現在のレイヤに含まれる何らかのセルが更新された場合のみレンダーバッファを更新
                if (layerBufferNeedsUpdate)
                {
                    _renderer.UpdateRenderBuffer(xzLayer, y);
                }
            }

            _renderer.RenderMeshes(new Bounds(boundingBoxSize * 0.5f, boundingBoxSize));
        }

        private void OnDestroy()
        {
            _renderer.Dispose();
            _voxelDataChunk.Dispose();
        }

        private void OnApplicationQuit()
        {
            _voxelDataChunk.SaveIsFilledTxt("isfilled.txt");
        }
    }
}
