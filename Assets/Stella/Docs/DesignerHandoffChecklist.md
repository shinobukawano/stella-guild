# Stella Guild デザイナー依頼チェックリスト

最終更新: 2026-02-12  
対象: Stella Guild (iOS / Unity)

## 目的

実装を止めずに進めるため、デザイナー依頼物を優先度順に整理する。  
このドキュメントの `P0` を揃えると、MVPの起動〜プレイ導線が成立する。

## 現在の実装前提

- Unity UI は `uGUI + TextMeshPro`
- カラーは実装済みトークンを使用
  - `Assets/Stella/DesignTokens/stella-color-tokens.json`
- ボタン挙動（押下オフセット）と起動ロゴフローは実装済み
  - `Assets/Stella/Scripts/UI/Buttons/StellaButtonView.cs`
  - `Assets/Stella/Scripts/Flow/StartupFlowController.cs`

## 依頼物（優先度順）

### P0（MVPブロッカー）

1. 起動ロゴ
- 用途: ゲーム起動直後に表示
- 元デザイン: Figma `node-id=8:2336`
- 納品形式: `PNG (透過背景)`
- 推奨サイズ: 長辺 `2048px` 以上（Retina対策）
- 命名: `logo_stella_guild_main.png`
- 配置先: `Assets/Stella/UI/Logos/`

2. フォント指定
- 用途: 全画面の文字統一
- 必須情報:
  - フォント名
  - 利用ライセンス可否（ゲーム配布可否）
  - 使用ウェイト（例: Regular/Bold）
- 納品形式:
  - 可能なら `OTF/TTF`
  - 不可なら代替指定（Google Fonts等）
- 命名例: `font_ui_primary_bold.otf`
- 配置先: `Assets/Stella/UI/Fonts/`

3. ボタンスタイル確定（最低3種）
- 元デザイン: Figma `node-id=8:2335`
- 対象:
  - `ActionRed`（使う / 購入）
  - `NavigationBlue`（戻る / タブ）
  - `MainPrimary`（丸メイン）
- 必須情報:
  - 通常状態と押下状態の見た目差分
  - ラベルの文字色
  - アイコン有無
- 納品形式:
  - 基本は `PNG`（必要なら9-slice前提画像）
  - 可能ならFigma上で state 切替コンポーネント化
- 命名例:
  - `btn_action_red_default.png`
  - `btn_action_red_pressed.png`
  - `btn_nav_blue_default.png`
- 配置先: `Assets/Stella/UI/Sprites/Buttons/`

### P1（MVP品質向上）

1. 画面別UI書き出し（Title / HUD / Result）
- Figma Frame 名をUnity画面名に合わせる
  - `TitlePage`, `BattleHudPage`, `ResultPage`
- 納品内容:
  - 背景
  - アイコン
  - 装飾（枠/吹き出し等）
- 形式:
  - 写真調素材は `PNG`
  - 単純図形は `SVG` も可（運用方針次第）

2. アイコンセット
- 対象例: `戦闘機`, `荷物`, `履歴`, `生産`, `建造`, `設定`
- 必須: `通常`, `選択中` の2状態
- 命名例: `icon_cargo_default.png`, `icon_cargo_selected.png`
- 配置先: `Assets/Stella/UI/Sprites/Icons/`

### P2（後追いでOK）

1. ローディング演出素材
- スピナー、装飾パーツ、遷移時エフェクト素材

2. チュートリアル用オーバーレイ素材
- 指差し、ハイライト枠、吹き出し

## 納品ルール（共通）

1. 余白
- 透過PNGは不要な余白を極力削る

2. 命名
- `category_purpose_state` 形式（小文字 + snake_case）
- 例: `btn_action_red_default.png`

3. バージョン更新
- 同名上書きではなく `_v2` などで更新管理
- 例: `logo_stella_guild_main_v2.png`

4. カラー差分
- 既存トークン外の色を使う場合はHEX明記

## 依頼テンプレ（そのまま送付可）

以下をデザイナーへ送る:

```text
ステラ・ギルドのUnity実装用に、まずP0素材の納品をお願いしたいです。

【P0】
1) 起動ロゴ（node-id=8:2336）
- PNG透過 / 長辺2048px以上
- ファイル名: logo_stella_guild_main.png

2) フォント指定
- 使用フォント名、ウェイト、ライセンス可否
- 可能ならOTF/TTFファイル

3) ボタン3種（node-id=8:2335）
- ActionRed / NavigationBlue / MainPrimary
- default と pressed の2状態
- ラベル文字色指定

納品先ルール:
- 命名は snake_case
- 既存トークン外の色を使う場合はHEXを併記
```

## 受け入れチェック（実装側）

- [ ] ロゴを `Assets/Stella/UI/Logos/` に配置できた
- [ ] フォントを `Assets/Stella/UI/Fonts/` に配置できた
- [ ] ボタン画像を `Assets/Stella/UI/Sprites/Buttons/` に配置できた
- [ ] 追加色が必要な場合、HEX情報を受領した
- [ ] Unity上でタイトル画面表示崩れがない
