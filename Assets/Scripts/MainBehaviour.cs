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

        public GameObject leftControllerAnchor = null;
        public GameObject rightControllerAnchor = null;
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
            Vector3 leftControllerWorldPosition = leftControllerAnchor.transform.position;

            Vector3 boundingBoxSize = transform.localToWorldMatrix.MultiplyPoint(new Vector3(_boundsSize.x, _boundsSize.y, _boundsSize.z));
            Bounds boundingBox = new();
            boundingBox.SetMinMax(Vector3.zero, boundingBoxSize);

            //for (int y = 0; y < _voxelDataChunk.yLength; y++)
            //{
            //    DataChunk xzLayer = _voxelDataChunk.GetXZLayer(y);
            //    for (int i = 0; i < xzLayer.Length; i++)
            //    {
            //        xzLayer.GetPosition(i, out int x, out _, out int z);
            //        Vector3 localPos = new(x + 0.5f, y + 0.5f, z + 0.5f);
            //        Vector3 worldPos = transform.TransformPoint(localPos);

            //        if ((worldPos - leftControllerWorldPosition).sqrMagnitude > visibleDistance * visibleDistance)
            //        {
            //            xzLayer.RemoveFlag(i, CellFlags.IsSelected);
            //        }
            //        else
            //        {
            //            xzLayer.AddFlag(i, CellFlags.IsSelected);
            //            xzLayer.RemoveFlag(i, CellFlags.IsFilled);
            //        }
            //    }
            //    _renderer.UpdateRenderBuffer(xzLayer, y);
            //}

            //Vector3 leftControllerLocalPosition = mainBehaviourTransform.InverseTransformPoint(leftControllerWorldPosition);
            //Vector3Int leftControllerGridPosition = new(
            //    (int)(leftControllerLocalPosition.x),
            //    (int)(leftControllerLocalPosition.y),
            //    (int)(leftControllerLocalPosition.z)
            //);

            //if (leftControllerGridPosition.x >= 0 &&
            //    leftControllerGridPosition.x < _voxelDataChunk.xLength &&
            //    leftControllerGridPosition.y >= 0 &&
            //    leftControllerGridPosition.y < _voxelDataChunk.yLength &&
            //    leftControllerGridPosition.z >= 0 &&
            //    leftControllerGridPosition.z < _voxelDataChunk.zLength)
            //{
            //    DataChunk xzLayer = _voxelDataChunk.GetXZLayer(leftControllerGridPosition.y);
            //    xzLayer.AddFlag(leftControllerGridPosition.x, 0, leftControllerGridPosition.z, CellFlags.IsSelected);
            //    xzLayer.RemoveFlag(leftControllerGridPosition.x, 0, leftControllerGridPosition.z, CellFlags.IsFilled);
            //    _renderer.UpdateRenderBuffer(xzLayer, leftControllerGridPosition.y);
            //}

            //_renderer.RenderMeshes(new Bounds(boundingBoxSize * 0.5f, boundingBoxSize));

            Vector3 leftControllerLocalPosition = mainBehaviourTransform.InverseTransformPoint(leftControllerWorldPosition);
            Vector3Int center = new(
                Mathf.RoundToInt(leftControllerLocalPosition.x),
                Mathf.RoundToInt(leftControllerLocalPosition.y),
                Mathf.RoundToInt(leftControllerLocalPosition.z)
            );

            int minX = Mathf.Max(0, center.x - visibleDistance);
            int maxX = Mathf.Min(_voxelDataChunk.xLength - 1, center.x + visibleDistance);
            int minY = Mathf.Max(0, center.y - visibleDistance);
            int maxY = Mathf.Min(_voxelDataChunk.yLength - 1, center.y + visibleDistance);
            int minZ = Mathf.Max(0, center.z - visibleDistance);
            int maxZ = Mathf.Min(_voxelDataChunk.zLength - 1, center.z + visibleDistance);

            float sqrVisibleDistance = visibleDistance * visibleDistance;

            for (int y = minY; y <= maxY; y++)
            {
                DataChunk xzLayer = _voxelDataChunk.GetXZLayer(y);
                bool updated = false;
                for (int x = minX; x <= maxX; x++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        // セル中心座標
                        Vector3 cellLocalPos = new(x + 0.5f, y + 0.5f, z + 0.5f);
                        if ((cellLocalPos - leftControllerLocalPosition).sqrMagnitude > sqrVisibleDistance)
                            continue;

                        xzLayer.AddFlag(x, 0, z, CellFlags.IsSelected);
                        xzLayer.RemoveFlag(x, 0, z, CellFlags.IsFilled);
                        updated = true;
                    }
                }
                if (updated)
                    _renderer.UpdateRenderBuffer(xzLayer, y);
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
