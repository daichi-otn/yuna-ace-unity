# YNA ACE Unity

ブラウザで遊ぶことを前提にした Unity WebGL 版の野球ゲームです。3Dスタジアム、打撃、投球、ボールの物理、走塁、CPUの攻守、一時停止に対応しています。人物モデルにはCC0公開のモデルを使用しています。詳しくは [第三者素材の利用表記](THIRD_PARTY_NOTICES.md) をご覧ください。

## 操作

- ストライクゾーン: 打撃／投球の狙いを選ぶ
- `投球を待つ` ボタンまたは `Enter`: 打者のとき投球を開始
- 投手のときはストライクゾーンをクリックしてコースを選び投球（投球まで1.8秒）
- 投手のときは矢印キーでコースを選び、`Enter` で投球することも可能
- `Space` またはクリック: スイング
- `A`: 走者がリードを取る
- `D`: 走者を次の塁へ進める
- `P`: 一時停止
- `R`: 試合終了後に再戦

試合は9回制で、3アウトごとに攻守が入れ替わります。同点なら延長戦です。

## Unity で開く

Unity 6（6000.6.7f1）以降で `Assets/Scenes/SampleScene.unity` を開いて再生してください。

## Web アプリ（WebGL）としてビルド

WebGL 出力は `Build/WebGL` に作成します。Unity CLI が使える環境では、プロジェクト直下で以下を実行します。

```bash
Unity build . --editor-version 6000.6.7f1 --target WebGL --execute-method YnaAce.Editor.WebGlBuild.PerformBuild
```

ビルド後は、`Build/WebGL` をローカル HTTP サーバーで配信してブラウザから開きます。`file://` で直接開くことはできません。

## GitHub Pages

`.github/workflows/pages.yml` は `Build/WebGL` を GitHub Pages に公開します。GitHubリポジトリの Pages 設定で Source を `GitHub Actions` にしてから、`main` にpushしてください。
