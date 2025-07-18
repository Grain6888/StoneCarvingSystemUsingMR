using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct GreedyMeshJob : IJob
{
    public DataChunk input;
    public int currentYIndex;
    [ReadOnly] public MeshData.MeshNativeData meshes;
    [WriteOnly] public NativeList<VertexData.VertexNativeData> vertices;
    [WriteOnly] public NativeList<int> triangles;

    private int _lastVertexIndex;

    public void Execute()
    {
        for (int index = 0; index < input.Length; index++)
        {
            if (!input.HasFlag(index, CellFlags.IsFilled))
            {
                continue;
            }
            if (input.HasFlag(index, CellFlags.IsMeshGenerated))
            {
                continue;
            }

            // ここから メッシュ結合処理
            GetFaceLength(ref input, index, out int xLength, out int zLength);
            index += xLength;

            input.GetPosition(index, out int x, out _, out int z);
            CreateCube(x, currentYIndex, z, xLength, 1, zLength);
            // ここまで メッシュ結合処理

            // ここから メッシュ結合無効化時の処理
            //input.GetPosition(index, out int x, out _, out int z);
            //CreateCube(x, currentYIndex, z, 1, 1, 1);
            //input.AddFlag(index, CellFlags.IsMeshGenerated);
            // ここまで メッシュ結合無効化時の処理
        }
    }

    public void CreateCube(int x, int y, int z, int xLength, int yLength, int zLength)
    {
        // 位置・スケールを考慮した変換行列を作成
        var trs = float4x4.TRS(
            new float3(x, y, z),                  // 位置
            quaternion.identity,                  // 回転なし
            new float3(xLength, yLength, zLength) // サイズ
        );

        // 基準メッシュの各頂点を変換してverticesDataに追加
        for (var index = 0; index < meshes.vertices.Length; index++)
        {
            var cv = meshes.vertices[index];
            var v = math.mul(trs, new float4(cv.x, cv.y, cv.z, 1f));
            vertices.Add(new VertexData.VertexNativeData()
            {
                position = new float3(v.x, v.y, v.z),
                normal = meshes.normals[index],
                uv = meshes.uv[index]
            });
        }

        // 三角形インデックスも追加
        foreach (var a in meshes.triangles)
        {
            triangles.Add(_lastVertexIndex + a);
        }

        _lastVertexIndex += meshes.vertices.Length;
    }

    private void GetFaceLength(ref DataChunk chunkInput, int index, out int xLength, out int zLength)
    {
        // 連続したセルを検出し、xLength, zLengthに設定
        // 処理済みセルにはIsMeshGeneratedフラグを設定

        uint meshGeneratedFlag = (uint)CellFlags.IsMeshGenerated;
        xLength = 1;
        zLength = 1;

        chunkInput.GetPosition(index, out int x, out _, out int z);

        // X軸での長さ
        while (x + xLength < chunkInput.xLength)
        {
            int xD = x + xLength;
            if (chunkInput.HasFlag(xD, 0, z, (CellFlags)meshGeneratedFlag))
            {
                break;
            }
            if (!chunkInput.HasFlag(xD, 0, z, CellFlags.IsFilled))
            {
                break;
            }
            chunkInput.AddFlag(xD, 0, z, CellFlags.IsMeshGenerated);
            xLength++;
        }

        // Z軸での長さ
        while (z + zLength < chunkInput.zLength)
        {
            int zD = z + zLength;
            bool extendHeight = false;
            for (int xOffset = 0; xOffset < xLength; xOffset++)
            {
                int xD = x + xOffset;
                if (chunkInput.HasFlag(xD, 0, zD, (CellFlags)meshGeneratedFlag))
                {
                    break;
                }
                if (!chunkInput.HasFlag(xD, 0, zD, CellFlags.IsFilled))
                {
                    break;
                }
                // 最後まで到達した場合の処理
                if (xOffset + 1 == xLength)
                {
                    extendHeight = true;
                }
            }
            if (!extendHeight)
            {
                break;
            }
            for (int xOffset = 0; xOffset < xLength; xOffset++)
            {
                int xD = x + xOffset;
                chunkInput.AddFlag(xD, 0, zD, CellFlags.IsMeshGenerated);
            }
            zLength++;
        }
    }
}
