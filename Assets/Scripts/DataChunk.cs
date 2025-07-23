using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace MRSculpture
{
    public struct DataChunk : IDisposable
    {
        private NativeArray<CellManager> _data;

        public readonly int xLength;
        public readonly int yLength;
        public readonly int zLength;

        /// <summary>
        /// 生成範囲の配列を初期化
        /// </summary>
        /// <param name="xLength"></param>
        /// <param name="yLength"></param>
        /// <param name="zLength"></param>
        public DataChunk(int xLength, int yLength, int zLength)
        {
            this.xLength = xLength;
            this.yLength = yLength;
            this.zLength = zLength;
            _data = new NativeArray<CellManager>(this.xLength * this.yLength * this.zLength, Allocator.Persistent);
        }

        /// <summary>
        /// 既存の_dataからxLength, yLength, zLength分の要素を切り出してDataChunkを生成
        /// </summary>
        /// <param name="xLength"></param>
        /// <param name="yLength"></param>
        /// <param name="zLength"></param>
        /// <param name="data"></param>
        private DataChunk(int xLength, int yLength, int zLength, NativeArray<CellManager> data)
        {
            this.xLength = xLength;
            this.yLength = yLength;
            this.zLength = zLength;
            _data = data;
        }

        /// <summary>
        /// y層のXZ平面を取得
        /// </summary>
        /// <param name="y"></param>
        /// <returns>y層のXZ平面</returns>
        public DataChunk GetXZLayer(int y)
        {
            int startIndex = y * xLength * zLength;
            int length = xLength * zLength;

            // y層のXZ平面を切り出して新しいDataChunkを生成
            // y=0でy層の高さ情報が無視される
            return new DataChunk(xLength, 1, zLength, _data.GetSubArray(startIndex, length));
        }

        /// <summary>
        /// 配列の長さを取得
        /// </summary>
        public int Length => _data.Length;

        /// <summary>
        /// 3次元座標 → インデクスに変換，1次元配列に格納
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns>配列のインデックス</returns>
        public int GetIndex(int x, int y, int z)
        {
            return x + (z * xLength) + (y * xLength * zLength);
        }

        /// <summary>
        /// インデクス → 3次元座標を取得
        /// </summary>
        /// <param name="index"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void GetPosition(int index, out int x, out int y, out int z)
        {
            x = index % xLength;
            z = index / xLength % zLength;
            y = index / xLength / zLength;
        }

        /// <summary>
        /// 3次元座標に対応するセルにフラグを追加
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="flags"></param>
        public unsafe void AddFlag(int x, int y, int z, CellFlags flags)
        {
            int index = GetIndex(x, y, z);
            // 1次元配列の先頭ポインタ + index で index番目の要素にアクセス
            CellManager* cell = (CellManager*)_data.GetUnsafePtr() + index;
            cell->AddFlag(flags);
        }

        /// <summary>
        /// 3次元座標に対応するセルのフラグの有無を確認
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="flags"></param>
        /// <returns>フラグの有無</returns>
        public unsafe bool HasFlag(int x, int y, int z, CellFlags flags)
        {
            int index = GetIndex(x, y, z);
            // 1次元配列の先頭ポインタ + index で index番目の要素にアクセス
            CellManager* cell = (CellManager*)_data.GetUnsafePtr() + index;
            return cell->HasFlag(flags);
        }

        /// <summary>
        /// 3次元座標に対応するセルのフラグを削除
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="flags"></param>
        public unsafe void RemoveFlag(int x, int y, int z, CellFlags flags)
        {
            int index = GetIndex(x, y, z);
            // 1次元配列の先頭ポインタ + index で index番目の要素にアクセス
            CellManager* cell = (CellManager*)_data.GetUnsafePtr() + index;
            cell->RemoveFlag(flags);
        }

        /// <summary>
        /// 3次元座標に対応するセルのすべてのフラグを削除
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public unsafe void RemoveAllFlags(int x, int y, int z)
        {
            int index = GetIndex(x, y, z);
            // 1次元配列の先頭ポインタ + index で index番目の要素にアクセス
            CellManager* cell = (CellManager*)_data.GetUnsafePtr() + index;
            cell->RemoveAllFlags();
        }

        /// <summary>
        /// インデクスに対応するセルにフラグを追加
        /// </summary>
        /// <param name="index"></param>
        /// <param name="flags"></param>
        public unsafe void AddFlag(int index, CellFlags flags)
        {
            // 1次元配列の先頭ポインタ + index で index番目の要素にアクセス
            CellManager* cell = (CellManager*)_data.GetUnsafePtr() + index;
            cell->AddFlag(flags);
        }

        /// <summary>
        /// インデクスに対応するセルのフラグの有無を確認
        /// </summary>
        /// <param name="index"></param>
        /// <param name="flags"></param>
        /// <returns>フラグの有無</returns>
        public unsafe bool HasFlag(int index, CellFlags flags)
        {
            // 1次元配列の先頭ポインタ + index で index番目の要素にアクセス
            CellManager* cell = (CellManager*)_data.GetUnsafePtr() + index;
            return cell->HasFlag(flags);
        }

        /// <summary>
        /// インデクスに対応するセルのフラグを削除
        /// </summary>
        /// <param name="index"></param>
        /// <param name="flags"></param>
        public unsafe void RemoveFlag(int index, CellFlags flags)
        {
            // 1次元配列の先頭ポインタ + index で index番目の要素にアクセス
            CellManager* cell = (CellManager*)_data.GetUnsafePtr() + index;
            cell->RemoveFlag(flags);
        }

        /// <summary>
        /// インデクスに対応するセルのすべてのフラグを削除
        /// </summary>
        /// <param name="index"></param>
        public unsafe void RemoveAllFlags(int index)
        {
            // 1次元配列の先頭ポインタ + index で index番目の要素にアクセス
            CellManager* cell = (CellManager*)_data.GetUnsafePtr() + index;
            cell->RemoveAllFlags();
        }

        public void Dispose()
        {
            _data.Dispose();
        }
    }
}
