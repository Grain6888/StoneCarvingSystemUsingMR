# 本システムについて

<img width="100%" height="auto" alt="image" src="https://github.com/user-attachments/assets/e2e8f802-ee23-4aab-bf15-5714a4917ed6" />

石彫を手軽かつ安全に制作できる『MRを用いた石彫制作システム』を開発しました．本システムでは，ユーザが扱う工具の向きや力加減に合わせて，実物体を観察しながら石材をインタラクティブに削ることができます．また，制作した作品を3Dプリンタで印刷することで，MRゴーグルがなくても作品を鑑賞することができます．これにより，利用者に石彫の機会と楽しさを提供し，観察力の向上を図ります．

# 環境構築

1. エディタをインストール  
   
   | 名称 | バージョン | 説明 |
   | --- | --- | --- |
   | Unity Hub | 3.16.2 | UnityエディタのインストールとUnityプロジェクトの管理<br>任意の最新版でよい |
   | Unity | 6000.3.2f1 | Unityプロジェクトの編集<br>Unity Hubからインストール<br>**Android Build Support**に✅を |
   | Visual Studio 2026 | 18.3.1 | スクリプトの編集<br>任意の最新版でよい<br>**Unityによるゲーム開発**に✅を |
   | Meta Haptic Studio | 2.0.0 | コントローラの振動の編集<br>任意の最新版でよい<br>振動波形を編集したい場合のみ必須 |
   | Creality Print | 7.0 | 3Dプリンタ用データの編集<br>任意の最新版でよい<br>印刷する場合のみ必須 |
   | GitHub Desktop | 3.5.5 | 任意の最新版でよい<br>バージョン管理する場合のみ必須 |
   | Git | 2.53.0.windows.1 | 任意の最新版でよい<br>バージョン管理する場合のみ必須 |
   | Git LFS | 3.7.1 | 任意の最新版でよい<br>100MB超ファイルをバージョン管理する場合のみ必須 |

3. ソースコードを配置  
   [参考：GitHubからソースコードをダウンロードする完全ガイド！初心者でも迷わない全方法を解説](https://www.choge-blog.com/web/githubdownloadsourcecode/)

   <details>
     <summary>スクリプト一覧</summary>

   | 名称 | 説明 | アタッチすべきオブジェクト |
   | :--: | --- | --- |
   | CellFlags | ボクセルの状態を定義するビットフラグ | なし |
   | CellManager | CellFlagsの操作 | なし |
   | ChiselController | ノミの制御<br>切削時のボクセルの削除 | `Chisel` |
   | DataChunk | ボクセルを格納する1次元配列の管理<br>作品データの入出力 | なし |
   | HammerController | ハンマーの制御<br>衝撃範囲の算出 | `Hammer` |
   | HandDeviceController | コントローラの表示/非表示の制御 | `Camera Rig/OVRInteractionComprehensive/OVRControllerPrefab` |
   | MaterialSwitcher | 石材のマテリアルの切り替え制御 | `Stone` |
   | MeshBuilder | `MarchingCubes.compute`の制御<br>石材の描画 | なし |
   | StoneController | 石材の制御，操作履歴管理<br>石材の初期化 | `Stone` |
   | TriangleTable | Marching Cubes法の定義済みテーブル | なし |
   | Util | `MeshBuilder`が利用するユーティリティ群 | なし |

   </details>

   <details>
     <summary>シェーダ一覧</summary>
     
     | 名称 | 説明 |
     | :--: | --- |
     | MarchingCubes.compute | Marching Cubes法によるリアルタイムメッシュ生成をGPUを用いて演算 |
   
   </details>

4. Unity Hubにプロジェクトを追加  
   [参考：Unity Hubに既存プロジェクトを追加する方法](https://soft-rime.com/post-26163/)

> [!note]  
> イチからUnityプロジェクトを作りたい場合は別途以下のアセットを導入すること．もちろん勧めない．
>
> | 名称 | 説明 |
> | :--: | --- |
> | 25+ Free Realistic Textures | 石材のテクスチャセット |
> | Meta XR All-in-One SDK | UnityでMeta Quest用アプリケーションを制作する<br>付随するアセットも自動で導入 |
> | Unity OpenXR Meta | MRに関する機能を追加する |
> | Android Logcat | Meta Quest上でアプリケーションを実行したときにLogをUnity側で出力する |  
>
> [参考：Meta XR SDKのインストールとプロジェクトの設定](https://qiita.com/Tks_Yoshinaga/items/7bd7ee0f30702581f68b)

> [!warning]  
> Meta All in One SDKが提供するツールバーに関する警告が出る場合はツールバー上の空白を右クリックして**Unsupported User Elements**を有効化すると解決する  
> <img width="360" height="auto" alt="image" src="https://github.com/user-attachments/assets/43f7f12e-d541-4a6a-88a3-868e8dd02e04" />

# 使い方

## UnityでアプリケーションをビルドしてMeta Quest 3で実行する

1. **File**から**Project Settings**を選択
2. **Burst AOT Settings**の**Enable Burst Compilation**を有効化
3. 同じく**Project Settings**の**Player**を選択
4. **Player**の**Script Compilation**にある**Allow 'Unsafe' Code**を有効化
5. **File**から**Build Profiles**を選択
6. **Build Profiles**の**Platforms**から**Meta Quest**を選択
7. **Scene List**の**Open Scene List**からビルドしたいシーンを選択
8. **Platforms Settings**の**Run Device**から**Oculus Quest 3**を選択
9. **Build and Run**を押下後，`APK`のエクスポート先を指定(どこでもよい)

> [!note]  
> Logの内容は，**Android Logcat**に出力される

> [!important]  
> Logを出力する場合は**Platforms Settings**の**Development Build**を有効化すること

> [!warning]  
> **Run Device**に**Oculus Quest 3**が出ないときは次を試す
> 
> 1. Meta Quest 3とPCを再接続
> 2. Meta Quest 3で**USBデバッグを許可**みたいなのが出るので**常に許可**を選択
> 3. **Refresh**を押下

## 作品を制作する

### 工具の使い方

コントローラを操作することで，工具を握って操作することができる．

1. コントローラを工具の方へ向ける  
   <img width="360" height="auto" alt="db4d9a12-04" src="https://github.com/user-attachments/assets/f00d3576-fdcf-48c2-b0e1-a46941d11c15" />

2. コントローラの中指のボタンを握りこむ  
   <img width="360" height="auto" alt="db4d9a12-05" src="https://github.com/user-attachments/assets/2ad04739-f70c-42fc-9baa-e41b8ceb26c3" />

3. ノミの先端を石材の表面にあてがう
4. ノミの持ち手側をハンマーで叩く

> [!tip]  
> 勢いよく叩くと大きく削れ，ゆっくり叩くと小さく削れる

> [!note]  
> 人差し指のボタンを握りこむと手ブレ防止機能が有効になる

> [!caution]  
> 左右逆のコントローラで工具を操作すると正常に動作しない

### ノミの種類

<img width="360" height="auto" alt="db4d9a12-08" src="https://github.com/user-attachments/assets/6b4b2f70-7b4e-4a7a-af91-f7c7f11a7223" />

削れる形状が異なるノミを3種類用意しており，用途に合わせたものを選択して石材を整形することができる．

| 名称 | 用途 |
| --- | --- |
| 平ノミ | 平面および凸形状の整形 |
| 丸ノミ | 曲面および凹形状の整形 |
|精密ノミ | 細部および狭窄部の整形 |

### リストメニューの使い方

<img width="360" height="auto" alt="db4d9a12-09" src="https://github.com/user-attachments/assets/31514129-db72-4310-a8f4-62222f5964a8" />

コントローラのメニューボタンを操作することで，リストメニューを表示する．メニューのボタンはコントローラで直接押下するか，コントローラのレイをボタンの方へ向けた状態で人差し指のボタンを握りこむことで選択することができる．

+ ファイル操作系
  + 保存：常に上書きする
  + 新規作成：石材を初期状態に戻す
  + 開く：最後に保存した作品を開く
  + 元に戻す：5つ前の状態まで戻す
  + やり直す：5つ後の状態まで直す
+ 見た目系
  + 荒い岩
  + 磨かれた大理石
  + 氷塊
 
> [!important]  
> 見た目を変えても削った時の音や破片の見た目は変わらない

### 石材の操作

<img width="360" height="auto" alt="db4d9a12-10" src="https://github.com/user-attachments/assets/3942a38c-f9a7-4bee-95fa-20df98edeb69" />

コントローラを操作することで，石材をつかんで操作することができる．

1. コントローラを石材にあてがう
2. コントローラの中指のボタンを握りこむ
3. コントローラを動かして石材を操作する

| 操作 | コントローラの動き |
| :--: | :--: |
| 移動 | <img width="auto" height="128" alt="移動" src="https://github.com/user-attachments/assets/fe0e2c87-464c-4af3-9d84-c3400a5d1a65" /> |
| 回転 | <img width="auto" height="128" alt="回転" src="https://github.com/user-attachments/assets/1dc4e10a-ac7b-40d2-9b07-151af966bc6c" /> |
| 拡大 | <img width="auto" height="128" alt="拡大" src="https://github.com/user-attachments/assets/f2ba65ff-293f-4e42-ac32-45c042f655b9" /> |
| 縮小 | <img width="auto" height="128" alt="縮小" src="https://github.com/user-attachments/assets/133f4f70-a7cb-4b01-a49c-f7aa763cbd25" /> |


## 制作した作品を3Dモデルに変換する

1. システム内のリストメニューで**保存**を押下
2. Meta Quest 3とPCを接続
3. Meta Quest 3で**ファイルへのアクセスを許可する**みたいな通知を押下
4. PCで作品データをコピー
5. Google Colabに`DatToObjConverter`をアップロード
6. Google Colabの**content**フォルダ内に`model.dat`をアップロード
7. **すべてのセルを実行**を押下して待機
8. `output.obj`が出力される

> [!note]  
> 作品データはMeta Quest 3内の次のディレクトリに出力される  
> `Android/data/アプリケーション名/model.dat`

> [!warning]  
> Google Colabで**すべてのセルを実行**するとランタイムがクラッシュする場合は再度**すべてのセルを実行**すると解決する

> [!important]  
> ファイル名と入力ディレクトリは`content/model.dat`のみ対応

## 3Dモデルを3Dプリンタで印刷する

1. **Creality Print**に`output.obj`をインポート
2. **プリンター**から**Creality K1C 0.4 nozzle**を選択
3. **フィラメント**から**Hyper PLA**を選択
4. **フィラメント**のプリセットから**0.20mm Standard @Creality K1C 0.4 nozzle**を選択
5. **フィラメント**の**サポート**タブで**サポート**を有効化し，**タイプ**から**ツリー**を選択
6. **スライス**を押下
7. **送信印刷**から**G-Codeをエクスポート**を押下

# Unityプロジェクトの編集

## アプリケーションのアイコンを変更する

1. **Edit**から**Project Settings**を選択
2. **Player**の**Default Icon**にベースとなるアイコン(512x512)をアタッチ  
   <img width="64" height="64" alt="Base" src="https://github.com/user-attachments/assets/0d8abb76-8927-4139-83c6-f91331729b1b" />


4. **Icon**の**Adaptive icons**を選択
5. **xxxhdpi**の**Background**にアイコンの背景画像(512x512)をアタッチ  
   <img width="64" height="64" alt="Background" src="https://github.com/user-attachments/assets/28561321-5992-418c-9f06-aab45c0e41fb" />

7. 同じく**xxxhdpi**の**Foreground**にアイコンのメイン画像(512x512)をアタッチ  
   <img width="64" height="64" alt="Cutout" src="https://github.com/user-attachments/assets/08f38e54-fa97-4f50-880f-165b57a4fed7" />


> [!note]  
> androidの仕様上，アイコンとして指定できる画像はAdaptive iconsに準拠する必要がある  
> [参考：モバイルアプリのアイコン設定](https://www.am10.blog/archives/348)

> [!tip]  
> **Splash Image**の**Show Splash Screen**を無効にすることで，最初に出てくる黒字にUnityのロード画面を無効化できる  
> <img width="360" height="auto" alt="image" src="https://github.com/user-attachments/assets/6b25570d-e114-45ed-b9dc-f4126fb302e6" />

> [!tip]  
> ロード中に表示されるアイコンは**Camera Rig**オブジェクトにアタッチされた**OVR Manager**コンポーネントの**Quest Features**の**General**に含まれる**System Splash Screen**から変更できる  
> <img width="360" height="auto" alt="image" src="https://github.com/user-attachments/assets/ae334b15-92a8-45b6-811a-0dedca3b4d19" />

## Stone

### Stone Controller

石材として使う空オブジェクトに必要なコンポーネント．
  
| パラメータ | 説明 |
| --- | --- |
| Bounds Size | 各軸方向の石材の細かさ(ボクセル数) |
| Bounds Collider | 石材の**Collider** |
| Triangle Budget | 三角メッシュ格納用バッファの確保サイズ |
| Builder Compute | メッシュ生成用の**ComputeShader** |
| Pin Chisel Controller | 精密ノミの**ChiselController** |
| Round Chisel Controller | 丸ノミの**ChiselController** |
| Flat Chisel Controller | 平ノミの**ChiselController** |
| Debug Sphere | デバッグ用ツールの**ChiselController** |
  
> [!tip]  
> 解像度を変えずに石材の大きさを変えたい場合は，**Transform**から，**Scale**を変更する

## Chisel

### Chisel Controller

オブジェクトをノミとして使う場合に必要なコンポーネント．

| パラメータ | 説明 |
| --- | --- |
| Stone | 石材オブジェクト |
| Collider | **Chisel**の子要素の**Impact Center**の**Collider** |
| Hammer | ハンマーオブジェクト |
| Max Impact Range | 削れる範囲(Impact Range)の最大径 |
| Sensitivity | 削れやすさ |
| Haptic Source | ノミで石材を削った時の振動を提示する**Haptic Source** |
| Audio System | ノミで石材を削った時の音を提示する**Audio Source** |
| Particle System | ノミで石材を削った時の破片を提示する**Particle System** |

> [!note]  
> **Sensitivity**が大きいほどハンマーで弱く叩いても大きく削れる

> [!tip]
> **Sensitivity**を0にするとハンマーで叩かなくても常に**Max Impact Range**で削れる  
> デバッグ用におすすめ

> [!warning]  
> 新しく追加したノミに適用した**Chisel Controler**が正常に動作しない場合は次を試す  
> 
> 1. ノミのオブジェクトをプレハブ化
> 2. **プロジェクト**の中でプレハブを選択して右クリック
> 3. メニューの中の**Create**から**Prefab Variant**を押下  
>    [参考：**Prefab**と**Prefab Variant**の違い](https://qiita.com/riekure/items/48b24e84cc8850621176)
> 4. 生成されたPrefab VariantをScene内に配置

## Hammer

### Hammer Controller

オブジェクトをハンマーとして使う場合に必要なコンポーネント．

| パラメータ | 説明 |
| --- | --- |
| Controller With Hammer | ハンマーを持つ方のコントローラ |
| Audio Source | ハンマーでノミを叩いたときの音を提示する**Audio Source** |
| Haptic Source | ハンマーでノミを叩いたときの振動を提示する**Haptic Source** |

## WristMenu

### Toggle

ボタンを押したときに実行される処理を定義するために必要なコンポーネント．

**On Value Changed**に実行したいスクリプトやコンポーネントをアタッチすることで，処理を定義．

> [!note]  
> 例えば，**保存ボタン**の動作は`WristMenu/ISDKMainMenu/PanelInteractable/CanvasPanel/UIBackplate+GridLayoutGroup/`の子要素である`SaveFile`オブジェクトにアタッチされた**Toggle**コンポーネントで定義される．

### Image

ボタンのアイコンを表示するために必要なコンポーネント．

> [!note]  
> 例えば，**保存ボタン**のアイコンは`ISDKMainMenu/PanelInteractable/CanvasPanel/UIBackplate+GridLayoutGroup/SaveFile/Content/Background/Elements/`の子要素である`Icon`オブジェクトにアタッチされた**Image**コンポーネントで定義される．

### Text Mesh Pro

ボタンの名前や説明文を表示するために必要なコンポーネント．

> [!note]  
> 例えば，**保存ボタン**の文字は`ISDKMainMenu/PanelInteractable/CanvasPanel/UIBackplate+GridLayoutGroup/SaveFile/Content/Background/Elements/`の子要素である`Label`，`Label (1)`オブジェクトにアタッチされた**Text Mesh Pro**コンポーネントで定義される．
  
# 参考

+ ボクセルデータの管理手法：[rarudo / UnityLifeGameTowerSample](https://github.com/rarudo/UnityLifeGameTowerSample)
+ GPUを用いたリアルタイムメッシュレンダリング：[keijiro / ComputeMarchingCubes](https://github.com/keijiro/ComputeMarchingCubes)
+ Meta Quest 3の境界線を無効化する方法：[MRTKv2.xを使ってMetaQuest3向けのUnityプロジェクト作成を行う その２２（アプリの境界線を無効化する） - MRが楽しい](https://bluebirdofoz.hatenablog.com/entry/2024/07/22/231952)
+ Meta XR SDKを用いたMeta Quest 3向けアプリケーションの開発：[Unity6とMETA XR SDK(v69-83)でQuestアプリ開発](https://www.docswell.com/s/Ovjang/KYDEPE-Unity6-MetaXRSDK)
+ Meta XR SDKを用いたインタラクティブなアプリケーションの基礎：[Meta XR SDKではじめようQuestアプリ開発](https://qiita.com/Tks_Yoshinaga/items/32bfe7567abbb2ac9521)
+ Haptic Sourceの使い方：[ハプティクスを追加する](https://developers.meta.com/horizon/documentation/unity/unity-haptics-sdk-integrate/)
+ UnityのTextMeshProで日本語フォントを使う方法：[Text Mesh Proで日本語を表示する方法](https://zenn.dev/kametani256/articles/63c083ab318136)
+ Unityのインスペクタに表示する項目の編集方法：[Inspectorをきれいに](https://qiita.com/OKsaiyowa/items/07e69f15e28387adbcf8)
+ Unityのプレハブをプロジェクト間でコピーする方法：[Unityで他のプロジェクトで作ったものをそのまま使いたいとき。](https://tech.motoki-watanabe.net/entry/2018/11/24/232450)
+ UnityのScene内のToolsを再表示する方法：[UnityのSceneにあるToolsが消えてしまった時に、再度出す方法。](http://halcyonsystemblog.jp/blog-entry-1063.html)

# 著作権情報

+ Chisels(3D model)：[Nillusion / Chisels](https://www.fab.com/listings/b9e04a99-ea62-4e71-8c30-f814556c14dd)
+ Hammer(3D model)：[FlorisArt / Hammer Handpainted Stylized - VR/Game Ready](https://www.fab.com/listings/b3a1faf7-1480-411f-bbd1-9b5bb75b1d8c)
