using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace MRSculpture
{
    public class Renderer : IDisposable
    {
        private readonly List<Mesh> _meshes = new();
        private readonly RenderParams _renderParams;
        private MeshData.MeshNativeData _mesh;
        private readonly Matrix4x4 _localToWorld;

        public Renderer(Mesh mesh, Material material, Matrix4x4 localToWorld)
        {
            _renderParams = new RenderParams(material)
            {
                shadowCastingMode = ShadowCastingMode.On,
                reflectionProbeUsage = ReflectionProbeUsage.BlendProbesAndSkybox
            };
            _localToWorld = localToWorld;

            // メッシュの頂点情報をNativeArrayに変換して保存
            NativeArray<float3> vertices = new(mesh.vertices.Length, allocator: Allocator.Persistent);
            // Unityのグリッドに合わせて頂点を調整
            Vector3 positionOffset = new(0.5f, 0.5f, 0.5f);
            for (int i = 0; i < mesh.vertices.Length; i++)
            {
                vertices[i] = mesh.vertices[i] + positionOffset;
            }

            // メッシュの三角形インデックスをNativeArrayに変換して保存
            NativeArray<int> triangles = new(mesh.triangles.Length, allocator: Allocator.Persistent);
            for (int i = 0; i < mesh.triangles.Length; i++)
            {
                triangles[i] = mesh.triangles[i];
            }

            // メッシュのUV座標をNativeArrayに変換して保存
            NativeArray<float2> uv = new(mesh.uv.Length, allocator: Allocator.Persistent);
            for (int i = 0; i < mesh.uv.Length; i++)
            {
                uv[i] = mesh.uv[i];
            }

            // メッシュの法線をNativeArrayに変換して保存
            NativeArray<float3> normals = new(mesh.normals.Length, Allocator.Persistent);
            for (int i = 0; i < mesh.normals.Length; i++)
            {
                normals[i] = mesh.normals[i];
            }

            // これらの情報をまとめた構造体に格納
            _mesh = new MeshData.MeshNativeData()
            {
                vertices = vertices,
                triangles = triangles,
                uv = uv,
                normals = normals
            };
        }

        public void AddRenderBuffer(DataChunk XZlayer, int y)
        {
            //Mesh mesh = MeshData.CreateMesh(ref XZlayer, y, ref _mesh);
            //_meshes.Add(mesh);
        }

        public void UpdateRenderBuffer(DataChunk xzLayer, int y)
        {
            for (int i = 0; i < xzLayer.Length; i++)
            {
                xzLayer.RemoveFlag(i, CellFlags.IsMeshGenerated);
            }
            //Mesh mesh = MeshData.CreateMesh(ref xzLayer, y, ref _mesh);
            //_meshes[y] = mesh;
        }

        public void RenderMeshes(Bounds boundingBox)
        {
            foreach (Mesh mesh in _meshes)
            {
                Graphics.RenderMesh(_renderParams, mesh, 0, _localToWorld * Matrix4x4.identity);
            }
        }

        public void Dispose()
        {
            _mesh.vertices.Dispose();
            _mesh.triangles.Dispose();
            _mesh.uv.Dispose();
            _mesh.normals.Dispose();
        }
    }
}
