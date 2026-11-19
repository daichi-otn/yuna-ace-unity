# YNA ACE Unity

ブラウザで遊ぶことを前提にした Unity WebGL 版の野球ゲームです。3Dスタジアム、打撃、投球、ボールの物理、走塁、CPUの攻守、一時停止に対応しています。人物モデルにはCC0公開のモデルを使用しています。詳しくは [第三者素材の利用表記](THIRD_PARTY_NOTICES.md) をご覧ください。

## 操作

- 画面左上: 自分の役割、次の操作、直前の結果を表示
- ストライクゾーン: 打撃／投球の狙いを選ぶ。青い中央9マスがストライク
- 打者のときは自動で次の投球へ進む（走塁中・一時停止中を除く）
- `投球を待つ` ボタンまたは `Enter`: 打者のとき待ち時間を短縮して投球を開始
- 打者は赤い`＋`の投球先へ黄色い`●`を合わせ、タイミングバーの緑の範囲でスイング
- 投手のときはストライクゾーンをクリックしてコースを選び投球（投球まで1.8秒）
- 投手のときは矢印キーでコースを選び、`Enter` で投球することも可能
- `Space` またはクリック: スイング
- `A`: 走者がリードを取る
- `D`: 走者を次の塁へ進める
- `P`: 一時停止
- `R`: 試合終了後に再戦

試合は9回制で、3アウトごとに攻守が入れ替わります。同点なら延長12回まで行い、12回終了時に同点なら引き分けです。

## 開発時の検証

`Tools > YNA ACE > Validate Gameplay` は、25マスのストライク判定、打撃の狙いと投球先の分離、満塁四球、一時停止中と攻守交代中の入力制限、勝敗・延長・引き分け・サヨナラ・再戦を検証します。

バッチモードでは `-executeMethod YnaAce.Editor.GameplayValidation.Run` で実行できます。プレイモードでの一試合の自動進行は `-executeMethod YnaAce.Editor.GameplayValidation.RunPlaySession` を使用します（この方法では `-quit` を付けず、検証完了時に自動終了します）。自動検証の成功は画面の見やすさやブラウザー操作の確認とは別です。

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
