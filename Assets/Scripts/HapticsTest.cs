using UnityEngine; // Unityの基本機能を利用

// Oculus触覚テスト用のMonoBehaviour
public class HapticsTest : MonoBehaviour
{
    // 触覚フィードバック用のクリップデータ
    private OVRHapticsClip _clip;

    // 振幅エンベロープ（Inspectorで設定可能）
    [SerializeField] private AnimationCurve _ampEnv;

    // オブジェクト生成時に初期化処理
    private void Awake()
    {
        // OVRHapticsの最小バッファサンプル数を出力
        Debug.Log(OVRHaptics.Config.MinimumBufferSamplesCount); // 1
        // OVRHapticsの最大バッファサンプル数を出力
        Debug.Log(OVRHaptics.Config.MaximumBufferSamplesCount); // 256
        // OVRHapticsの最適バッファサンプル数を出力
        Debug.Log(OVRHaptics.Config.OptimalBufferSamplesCount); // 20
        // OVRHapticsのサンプルレート(Hz)を出力
        Debug.Log(OVRHaptics.Config.SampleRateHz); // 320
        // OVRHapticsのサンプルサイズ(バイト)を出力
        Debug.Log(OVRHaptics.Config.SampleSizeInBytes); // 1

        // 160サンプル分の触覚クリップを生成
        _clip = new OVRHapticsClip(160);

        // 触覚クリップにサイン波＋エンベロープでサンプルを書き込む
        for (int i = 0; i < _clip.Capacity; i++)
        {
            // サイン波で基本振幅を計算
            float val = 0.5f + 0.5f * Mathf.Sin((i / 160f) * 5f * Mathf.PI * 2f);
            // エンベロープカーブで振幅を調整
            val *= _ampEnv.Evaluate(i / 160f);
            // 0〜255に変換してサンプルを書き込み
            _clip.WriteSample((byte)(val * 255f));
        }
    }

    // 毎フレーム呼ばれる処理
    public void Update()
    {
        // 左手トリガーが押されたら左チャンネルに触覚クリップを再生
        if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger))
        {
            OVRHaptics.LeftChannel.Preempt(_clip);
        }

        // 右手トリガーが押されたら右チャンネルに触覚クリップを再生
        if (OVRInput.GetDown(OVRInput.Button.SecondaryHandTrigger))
        {
            OVRHaptics.RightChannel.Preempt(_clip);
        }
    }
}
