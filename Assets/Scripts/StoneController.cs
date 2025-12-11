using System.IO;
using System.Collections.Generic;
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
        public Vector3Int BoundsSize => _boundsSize;

        /// <summary>
        /// 彫刻素材のボクセルデータを格納する DataChunk
        /// </summary>
        private DataChunk _voxelDataChunk;

        /// <summary>
        /// 彫刻前後のボクセルデータの差分を管理する構造体
        /// </summary>
        private struct VoxelDiff
        {
            /// <summary>
            /// インデックス
            /// </summary>
            public int Index;
            /// <summary>
            /// 彫刻前の状態
            /// </summary>
            public uint Before;
            /// <summary>
            /// 彫刻後の状態
            /// </summary>
            public uint After;
        }
        /// <summary>
        /// Undo の差分を管理するキュー
        /// </summary>
        /// <remarks>
        /// <para>
        /// 最大5世代を記憶
        /// </para>
        /// </remarks>
        private readonly LinkedList<List<VoxelDiff>> _undoDeque = new();
        /// <summary>
        /// Redo の差分を管理するスタック
        /// </summary>
        /// <remarks>
        /// 最大5世代を記憶
        /// </remarks>
        private readonly Stack<List<VoxelDiff>> _redoStack = new();
        /// <summary>
        /// 記憶可能な世代の最大数
        /// </summary>
        private const int MaxHistory = 5;

        /// <summary>
        /// 石材の BoxCollider
        /// </summary>
        [SerializeField] private BoxCollider _boundsCollider;

        /// <summary>
        /// メッシュ生成時の三角形の最大数
        /// </summary>
        [SerializeField] int _triangleBudget = 65536 * 16;

        /// <summary>
        /// メッシュ生成に使用する ComputeShader
        /// </summary>
        [SerializeField] ComputeShader _builderCompute;

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
        [SerializeField] private ChiselController _pinChiselController;

        /// <summary>
        /// 丸ノミ
        /// </summary>
        [SerializeField] private ChiselController _roundChiselController;

        /// <summary>
        /// 平ノミ
        /// </summary>
        [SerializeField] private ChiselController _flatChiselController;

        /// <summary>
        /// デバッグ用
        /// </summary>
        [SerializeField] private ChiselController _debugSphere;

        private void Awake()
        {
            OnNewFile();
        }

        private void CommonBehaviour()
        {
            SetupBoundsCollider();

            _voxelDataChunk = new DataChunk(_boundsSize.x, _boundsSize.y, _boundsSize.z);
            _voxelBuffer = new ComputeBuffer(VoxelCount, sizeof(uint));
            _builder = new MeshBuilder(_boundsSize, _triangleBudget, _builderCompute);
        }

        private void AttachDataChunks()
        {
            _voxelBuffer.SetData(_voxelDataChunk.DataArray);
            _builder.BuildIsosurface(_voxelBuffer, _builtTargetValue);
            GetComponent<MeshFilter>().sharedMesh = _builder.Mesh;

            _roundChiselController.AttachDataChunk(ref _voxelDataChunk);
            _pinChiselController.AttachDataChunk(ref _voxelDataChunk);
            _flatChiselController.AttachDataChunk(ref _voxelDataChunk);
            _debugSphere.AttachDataChunk(ref _voxelDataChunk);
        }

        /// <summary>
        /// <para>
        /// ファイルからボクセルデータを読み込む．ファイルが存在しない場合は新規作成する．
        /// </para>
        /// </summary>
        private void LoadFile(string fileName)
        {
            string path = Path.Combine(Application.persistentDataPath, fileName);

            if (File.Exists(path))
            {
                _voxelDataChunk.LoadDat(path);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"MRSculpture : DataChunk loaded from {fileName}");
#endif
            }
            else
            {
                NewFile();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("MRSculpture : DataChunk load failed.");
#endif
            }
        }

        public void OnLoadFile(string fileName)
        {
            OnDestroy();
            CommonBehaviour();
            LoadFile(fileName);
            AttachDataChunks();
            _undoDeque.Clear();
            _redoStack.Clear();
        }

        /// <summary>
        /// <para>
        /// 現在のボクセルデータをファイルに保存する．
        /// </para>
        /// </summary>
        private void SaveFile(string fileName)
        {
            string path = Path.Combine(Application.persistentDataPath, fileName);

            _voxelDataChunk.SaveDat(path);
        }

        public void OnSaveFile(string fileName)
        {
            SaveFile(fileName);
        }

        /// <summary>
        /// DataChunk を全て埋まった状態で初期化し，初期メッシュを生成する．
        /// </summary>
        private void NewFile()
        {
            FillVoxelDataChunk();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("MRSculpture : New DataChunk created.");
#endif
        }

        private void FillVoxelDataChunk()
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
        }

        public void OnNewFile()
        {
            OnDestroy();
            CommonBehaviour();
            NewFile();
            AttachDataChunks();
            _undoDeque.Clear();
            _redoStack.Clear();
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

        /// <summary>
        /// ボクセルデータの差分を記録する．
        /// </summary>
        /// <remarks>
        /// <para>
        /// 記録された世代数が MaxHistory を超えると最も古い世代から削除される．
        /// </para>
        /// <para>
        /// Undo の差分が記録されると Redo の差分が削除される．
        /// </para>
        /// </remarks>
        /// <param name="diffs">
        /// ボクセルデータの差分
        /// </param>
        public void SetCarveDiffs(List<(int index, uint before, uint after)> diffs)
        {
            var diffList = new List<VoxelDiff>(diffs.Count);
            foreach (var d in diffs)
            {
                diffList.Add(new VoxelDiff { Index = d.index, Before = d.before, After = d.after });
            }
            if (diffList.Count > 0)
            {
                _undoDeque.AddLast(diffList);
                if (_undoDeque.Count > MaxHistory)
                {
                    _undoDeque.RemoveFirst();
                }
                _redoStack.Clear();
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"MRSculpture : SetCarveDiffs count={diffList.Count}");
#endif
        }

        private void UndoDataChunk()
        {
            if (_undoDeque.Count == 0) return;
            var last = _undoDeque.Last.Value;
            var arr = _voxelDataChunk.DataArray;
            foreach (var diff in last)
            {
                arr[diff.Index] = new CellManager { status = diff.Before };
            }
            _redoStack.Push(last);
            _undoDeque.RemoveLast();
            UpdateMesh();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("MRSculpture : Undo");
#endif
        }

        public void OnUndo()
        {
            UndoDataChunk();
        }

        private void RedoDataChunk()
        {
            if (_redoStack.Count == 0) return;
            var last = _redoStack.Pop();
            var arr = _voxelDataChunk.DataArray;
            foreach (var diff in last)
            {
                arr[diff.Index] = new CellManager { status = diff.After };
            }
            UpdateMesh();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("MRSculpture : Redo");
#endif
        }

        public void OnRedo()
        {
            RedoDataChunk();
        }

        private void OnDestroy()
        {
            if (_voxelDataChunk.IsCreated)
            {
                _voxelDataChunk.Dispose();
            }
            _voxelBuffer?.Dispose();
            _builder?.Dispose();
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
