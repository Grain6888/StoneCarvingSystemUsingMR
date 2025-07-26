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

        public GameObject leftPokeLocation = null;
        public GameObject rightPokeLocation = null;
        [SerializeField] private Transform mainBehaviourTransform;
        [SerializeField] private int visibleDistance = 10;


        private void Awake()
        {
            // DataChunkを生成し，3Dデータを保持
            _voxelDataChunk = new DataChunk(_boundsSize.x, _boundsSize.y, _boundsSize.z);

            // ローカル → ワールド座標系の変換行列
            Matrix4x4 localToWorldMatrix = transform.localToWorldMatrix;

            _renderer = new Renderer(_voxelMesh, _voxelMaterial, localToWorldMatrix);

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
        }

        void Update()
        {
            Vector3 boundingBoxSize = transform.localToWorldMatrix.MultiplyPoint(new Vector3(_boundsSize.x, _boundsSize.y, _boundsSize.z));
            Bounds boundingBox = new();
            boundingBox.SetMinMax(Vector3.zero, boundingBoxSize);

            // 左コントローラーのワールド座標を取得
            Vector3 leftControllerWorldPosition = leftPokeLocation.transform.position;
            // コントローラーのワールド座標を、MainBehaviourのローカル座標系に変換
            Vector3 leftControllerLocalPosition = mainBehaviourTransform.InverseTransformPoint(leftControllerWorldPosition);
            // コントローラー位置を基準に、最も近いボクセルグリッド座標（整数）を算出
            Vector3Int center = new(
                Mathf.RoundToInt(leftControllerLocalPosition.x),
                Mathf.RoundToInt(leftControllerLocalPosition.y),
                Mathf.RoundToInt(leftControllerLocalPosition.z)
            );

            // X方向の探索範囲（visibleDistance分だけ前後に拡張、範囲外はクランプ）
            int minX = Mathf.Max(0, center.x - visibleDistance);
            int maxX = Mathf.Min(_voxelDataChunk.xLength - 1, center.x + visibleDistance);
            // Y方向の探索範囲
            int minY = Mathf.Max(0, center.y - visibleDistance);
            int maxY = Mathf.Min(_voxelDataChunk.yLength - 1, center.y + visibleDistance);
            // Z方向の探索範囲
            int minZ = Mathf.Max(0, center.z - visibleDistance);
            int maxZ = Mathf.Min(_voxelDataChunk.zLength - 1, center.z + visibleDistance);

            // 距離判定用にvisibleDistanceの2乗を事前計算（パフォーマンス向上のため）
            float sqrVisibleDistance = visibleDistance * visibleDistance;

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

                        // コントローラーとの距離がvisibleDistance以内か判定
                        if ((cellLocalPos - leftControllerLocalPosition).sqrMagnitude > sqrVisibleDistance)
                            continue; // 範囲外ならスキップ

                        // 対象セルにIsSelectedフラグを追加
                        xzLayer.AddFlag(x, 0, z, CellFlags.IsSelected);
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

        //#if UNITY_EDITOR
        //        private void OnDrawGizmos()
        //        {
        //            Vector3 leftControllerWorldPosition = leftControllerAnchor.transform.position;
        //            Vector3 leftControllerLocalPosition = mainBehaviourTransform.InverseTransformPoint(leftControllerWorldPosition);
        //            Vector3Int leftControllerGridPosition = new(
        //                (int)(leftControllerLocalPosition.x),
        //                (int)(leftControllerLocalPosition.y),
        //                (int)(leftControllerLocalPosition.z)
        //            );
        //            Vector3 conLocalPos = new(leftControllerGridPosition.x + 0.5f, leftControllerGridPosition.y + 0.5f, leftControllerGridPosition.z + 0.5f);
        //            Vector3 conWorldPos = transform.TransformPoint(conLocalPos);
        //            UnityEditor.Handles.Label(conWorldPos, $"({conLocalPos.x:F3},{conLocalPos.y:F3},{conLocalPos.z:F3})");

        //            if (_voxelDataChunk.Equals(default(DataChunk))) return;

        //            Vector3 referencePos = Camera.current != null ? Camera.current.transform.position : Vector3.zero;
        //            float visibleDistance = 3.0f;

        //            Vector3 scale = transform.lossyScale;

        //            for (int y = 0; y < _voxelDataChunk.yLength; y++)
        //            {
        //                DataChunk xzLayer = _voxelDataChunk.GetXZLayer(y);
        //                for (int i = 0; i < xzLayer.Length; i++)
        //                {
        //                    if (!xzLayer.HasFlag(i, CellFlags.IsFilled))
        //                    {
        //                        continue;
        //                    }

        //                    xzLayer.GetPosition(i, out int x, out _, out int z);
        //                    Vector3 localPos = new(x + 0.5f, y + 0.5f, z + 0.5f);
        //                    Vector3 worldPos = transform.TransformPoint(localPos);

        //                    if ((worldPos - referencePos).sqrMagnitude > visibleDistance * visibleDistance)
        //                    {
        //                        continue;
        //                    }

        //                    if (xzLayer.HasFlag(i, CellFlags.IsSelected))
        //                    {
        //                        Gizmos.color = Color.red;
        //                        Gizmos.DrawCube(worldPos, scale);
        //                    }
        //                    else
        //                    {
        //                        Gizmos.color = Color.white;
        //                        Gizmos.DrawWireCube(worldPos, scale);
        //                    }

        //                    Gizmos.color = Color.green;
        //                    Gizmos.DrawWireCube(conWorldPos, scale);
        //                    UnityEditor.Handles.Label(worldPos, $"({localPos.x},{localPos.y},{localPos.z})");
        //                }
        //            }
        //        }
        //#endif
    }
}
