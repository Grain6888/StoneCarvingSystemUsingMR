using Unity.Mathematics;
using UnityEngine;

public class Behaviour : MonoBehaviour
{
    /// <summary>
    /// 彫刻素材の生成範囲
    /// </summary>
    public int3 _boundsSize = new(100, 100, 100);

    /// <summary>
    /// 層を追加するフレーム間隔
    /// </summary>
    [SerializeField, Min(0)] private int _layerAddInterval = 60;

    /// <summary>
    /// フレームごとに描画する
    /// </summary>
    [SerializeField] private bool _drawLayerPerFrame = true;

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

    private void Awake()
    {
        // DataChunkを生成し，3Dデータを保持
        _voxelDataChunk = new DataChunk(_boundsSize.x, _boundsSize.y, _boundsSize.z);

        // ローカル → ワールド座標系の変換行列
        Matrix4x4 localToWorldMatrix = transform.localToWorldMatrix;

        _renderer = new Renderer(_voxelMesh, _voxelMaterial, localToWorldMatrix);

        DataChunk initialXZLayer = _voxelDataChunk.GetXZLayer(0);
        for (int i = 0; i < initialXZLayer.Length; i++)
        {
            //initialXZLayer.AddFlag(i, CellFlags.IsFilled);

            // インデックスからXZ座標を取得
            initialXZLayer.GetPosition(i, out int x, out _, out int z);

            // 球体の中心座標（全体の中央）
            float centerX = (_boundsSize.x - 1) / 2.0f;
            float centerY = (_boundsSize.y - 1) / 2.0f;
            float centerZ = (_boundsSize.z - 1) / 2.0f;

            // 球体の半径（BoundsSizeの最小値/2）
            float radius = math.min(_boundsSize.x, math.min(_boundsSize.y, _boundsSize.z)) / 2.0f;

            // 現在のY層
            float y = _currentYIndex;

            // 球体の方程式で判定
            float dx = x - centerX;
            float dy = y - centerY;
            float dz = z - centerZ;
            float distanceSq = dx * dx + dy * dy + dz * dz;

            if (distanceSq <= radius * radius)
            {
                initialXZLayer.AddFlag(i, CellFlags.IsFilled);
            }
        }
    }

    /// <summary>
    /// 現在のY層
    /// </summary>
    private int _currentYIndex;

    /// <summary>
    /// 全層描画済みフラグ
    /// </summary>
    private bool _isAllLayerRendered = false;

    /// <summary>
    /// フレームカウンター (_layerAddFIntervalフレームごとに層を追加)
    /// </summary>
    private int _frameCounter = 0;

    void Update()
    {
        if (_currentYIndex < _voxelDataChunk.yLength)
        {
            _frameCounter++;
            if (_frameCounter >= _layerAddInterval)
            {
                _frameCounter = 0;

                if (_currentYIndex == 0)
                {
                    _renderer.AddRenderBuffer(_voxelDataChunk.GetXZLayer(0), 0);
                }
                else
                {
                    DataChunk currentXZLayer = _voxelDataChunk.GetXZLayer(_currentYIndex);

                    for (int i = 0; i < currentXZLayer.Length; i++)
                    {
                        //currentXZLayer.AddFlag(i, CellFlags.IsFilled);

                        // インデックスからXZ座標を取得
                        currentXZLayer.GetPosition(i, out int x, out _, out int z);

                        // 球体の中心座標（全体の中央）
                        float centerX = (_boundsSize.x - 1) / 2.0f;
                        float centerY = (_boundsSize.y - 1) / 2.0f;
                        float centerZ = (_boundsSize.z - 1) / 2.0f;

                        // 球体の半径（BoundsSizeの最小値/2）
                        float radius = math.min(_boundsSize.x, math.min(_boundsSize.y, _boundsSize.z)) / 2.0f;

                        // 現在のY層
                        float y = _currentYIndex;

                        // 球体の方程式で判定
                        float dx = x - centerX;
                        float dy = y - centerY;
                        float dz = z - centerZ;
                        float distanceSq = dx * dx + dy * dy + dz * dz;

                        if (distanceSq <= radius * radius)
                        {
                            currentXZLayer.AddFlag(i, CellFlags.IsFilled);
                        }
                    }

                    _renderer.AddRenderBuffer(currentXZLayer, _currentYIndex);
                }

                _currentYIndex++;
            }
        }

        if (_drawLayerPerFrame)
        {
            Vector3 boundingBoxSize = transform.localToWorldMatrix.MultiplyPoint(new Vector3(_boundsSize.x, _boundsSize.y, _boundsSize.z));
            Bounds boundingBox = new();
            boundingBox.SetMinMax(Vector3.zero, boundingBoxSize);
            _renderer.RenderMeshes(new Bounds(boundingBoxSize * 0.5f, boundingBoxSize));
        }
        else if (_currentYIndex >= _voxelDataChunk.yLength)
        {
            Vector3 boundingBoxSize = transform.localToWorldMatrix.MultiplyPoint(new Vector3(_boundsSize.x, _boundsSize.y, _boundsSize.z));
            Bounds boundingBox = new();
            boundingBox.SetMinMax(Vector3.zero, boundingBoxSize);
            _renderer.RenderMeshes(new Bounds(boundingBoxSize * 0.5f, boundingBoxSize));
            if (!_isAllLayerRendered)
            {
                _isAllLayerRendered = true;
            }
        }
    }

    private void OnDestroy()
    {
        _renderer.Dispose();
        _voxelDataChunk.Dispose();
    }

    private void OnDrawGizmos()
    {
        if (_voxelDataChunk.Equals(default(DataChunk))) return;
        if (_currentYIndex < 0 || _currentYIndex >= _voxelDataChunk.yLength) return;

        DataChunk xzLayer = _voxelDataChunk.GetXZLayer(_currentYIndex);
        Vector3 scale = transform.localScale;

        for (int i = 0; i < xzLayer.Length; i++)
        {
            xzLayer.GetPosition(i, out int x, out _, out int z);
            Gizmos.color = Color.green;
            Vector3 position = new(x * scale.x + scale.x / 2, _currentYIndex * scale.y + scale.y / 2, z * scale.z + scale.z / 2);
            Gizmos.DrawWireCube(position, scale);
        }
    }
}
