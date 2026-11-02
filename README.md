# YNA ACE Unity

ブラウザで遊ぶことを前提にした Unity WebGL 版の野球ゲームです。現段階では、追加アセットなしで 3D スタジアム、打撃、投球、ボールの物理、走塁、CPU の守備、一時停止を動かします。

## 操作

- `Enter`: CPU の投球を開始（打者時）
- ストライクゾーン: 投球／打撃の狙いを選ぶ
- `Space` またはクリック: スイング
- `A` / `D`: 左足／右足で走者を動かす
- `P`: 一時停止
- `R`: 試合終了後に再戦

試合は9回制で、3アウトごとに攻守が入れ替わります。同点なら延長戦です。

## Unity で開く

Unity 6.6.7f1 以降で `Assets/Scenes/SampleScene.unity` を開いて再生してください。

## Web アプリ（WebGL）としてビルド

WebGL 出力は `Build/WebGL` に作成します。Unity CLI が使える環境では、プロジェクト直下で以下を実行します。

```bash
Unity build . --editor-version 6000.6.7f1 --target WebGL --execute-method YnaAce.Editor.WebGlBuild.PerformBuild
```

ビルド後は、`Build/WebGL` をローカル HTTP サーバーで配信してブラウザから開きます。`file://` で直接開くことはできません。
