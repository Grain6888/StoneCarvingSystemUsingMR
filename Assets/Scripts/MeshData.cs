using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class MeshData
{
    public struct MeshNativeData
    {
        public NativeArray<float3> vertices;
        public NativeArray<int> triangles;
        public NativeArray<float2> uv;
        public NativeArray<float3> normals;
    }

    public static Mesh CreateMesh(ref DataChunk XZLayer, int y, ref MeshNativeData meshData)
    {
        NativeList<VertexData.VertexNativeData> verticesData = new(allocator: Allocator.TempJob);
        NativeList<int> triangles = new(allocator: Allocator.TempJob);

        JobHandle handle = new GreedyMeshJob()
        {
            input = XZLayer,
            currentYIndex = y,
            meshes = meshData,
            vertices = verticesData,
            triangles = triangles
        }.Schedule();
        handle.Complete();

        Mesh mesh = new()
        {
            hideFlags = HideFlags.DontSave
        };

        mesh.SetVertexBufferParams(
            verticesData.Length,
            new VertexAttributeDescriptor(VertexAttribute.Position),
            new VertexAttributeDescriptor(VertexAttribute.Normal),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2)
            );
        mesh.SetVertexBufferData(
            verticesData.AsArray(),
            0, 0, verticesData.Length
            );
        mesh.SetIndexBufferParams(
            triangles.Length,
            IndexFormat.UInt32
            );
        mesh.SetIndexBufferData(
            triangles.AsArray(),
            0, 0, triangles.Length,
            MeshUpdateFlags.DontValidateIndices
            );

        SubMeshDescriptor desc = new(0, triangles.Length);
        mesh.SetSubMesh(0, desc, MeshUpdateFlags.DontValidateIndices);
        mesh.RecalculateBounds();

        triangles.Dispose();
        verticesData.Dispose();
        return mesh;
    }
}
