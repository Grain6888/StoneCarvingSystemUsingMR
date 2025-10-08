using UnityEngine;
using Oculus.Haptics;
using Meta.XR.ImmersiveDebugger.Gizmo;

namespace MRSculpture
{
    public class ChiselController : MonoBehaviour
    {
        public HapticSource _hapticSource;
        public AudioSource _audioSource;

        public void Carve(ref DataChunk voxelDataChunk, in Vector3Int center, in int impactRange, ref Renderer renderer)
        {
            // X方向の探索範囲（visibleDistance分だけ前後に拡張、範囲外はクランプ）
            int minX = Mathf.Max(0, center.x - impactRange);
            int maxX = Mathf.Min(voxelDataChunk.xLength - 1, center.x + impactRange);
            // Y方向の探索範囲
            int minY = Mathf.Max(0, center.y - impactRange);
            int maxY = Mathf.Min(voxelDataChunk.yLength - 1, center.y + impactRange);
            // Z方向の探索範囲
            int minZ = Mathf.Max(0, center.z - impactRange);
            int maxZ = Mathf.Min(voxelDataChunk.zLength - 1, center.z + impactRange);

            // 距離判定用にvisibleDistanceの2乗を事前計算（パフォーマンス向上のため）
            float sqrVisibleDistance = impactRange * impactRange;
            int removedCount = 0;

            // 各XZレイヤごとに処理
            for (int y = minY; y <= maxY; y++)
            {
                DataChunk xzLayer = voxelDataChunk.GetXZLayer(y);
                bool layerBufferNeedsUpdate = false;

                // Y層のXZ平面のDataChunkを取得
                for (int x = minX; x <= maxX; x++)
                {
                    // Z方向の範囲をループ
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        Vector3 cellLocalPos = new(x + 0.5f, y + 0.5f, z + 0.5f);

                        // 破壊中心との距離がvisibleDistance以内か判定
                        if ((cellLocalPos - center).sqrMagnitude > sqrVisibleDistance)
                            continue;

                        if (xzLayer.HasFlag(x, 0, z, CellFlags.IsFilled))
                        {
                            xzLayer.RemoveFlag(x, 0, z, CellFlags.IsFilled);
                            removedCount++;
                            layerBufferNeedsUpdate = true;
                        }
                    }
                }

                // 現在のレイヤに含まれる何らかのセルが更新された場合のみレンダーバッファを更新
                if (layerBufferNeedsUpdate)
                {
                    renderer.UpdateRenderBuffer(xzLayer, y);
                }
            }

            if (removedCount > 0)
            {
                float amplitude = Mathf.Clamp01((float)impactRange / 10.0f);

                if (_hapticSource != null)
                {
                    float hapticAmplitude = Mathf.Lerp(0.0f, 5.0f, amplitude);
                    //float originalAmplitude = _hapticSource.amplitude;
                    _hapticSource.amplitude = hapticAmplitude;
                    _hapticSource.Play();
                    Debug.Log($"MRSculpture : Haptic Amplitude: {_hapticSource.amplitude}");
                    //_hapticSource.amplitude = originalAmplitude;
                }

                if (_audioSource != null)
                {
                    //float originalVolume = _audioSource.volume;
                    _audioSource.volume = amplitude;
                    _audioSource.Play();
                    Debug.Log($"MRSculpture : Audio Volume: {_audioSource.volume}");
                    //_audioSource.volume = originalVolume;
                }
            }
        }
    }
}
