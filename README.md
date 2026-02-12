# Stella Guild (Unity)

「ラストウォー風」進行を参考にした、iOS向け Unity プロトタイプです。

## 現在の実装状況

このリポジトリには以下が含まれています。

- Unity プロジェクト基盤（URP）
- UI ページ遷移の基礎設計
- 起動ロゴ表示フロー（ロゴ -> フェード -> タイトル）
- カラーデザイントークンとボタン見た目システム
- Bootstrap シーン自動生成メニュー
- デザイナー依頼チェックリスト

`Stage01` のゲームプレイループ（移動・射撃・敵ウェーブ）は未実装です。  
現時点の `Stage01` はプレースホルダーです。

## 開発環境

- Unity: `6000.3.3f1`
- Render Pipeline: `Universal Render Pipeline (URP)`
- 主ターゲット: `iOS`

## クイックスタート

1. Unity Hub でこのフォルダを開く
2. スクリプトのインポート・コンパイル完了を待つ
3. メニュー `Stella > Setup > Create Bootstrap + Stage01` を実行
4. `Assets/Scenes/Bootstrap.unity` を開く（自動で開く設定）
5. Play 実行
6. `Start` ボタンで `Stage01` へ遷移

## 主要スクリプト

- 起動フロー: `Assets/Stella/Scripts/Flow/StartupFlowController.cs`
- UI ページルーター/基底: `Assets/Stella/Scripts/UI/UIPageRouter.cs`, `Assets/Stella/Scripts/UI/UIPage.cs`
- タイトルページ: `Assets/Stella/Scripts/UI/TitlePageController.cs`
- ボタン見た目: `Assets/Stella/Scripts/UI/Buttons/StellaButtonView.cs`
- デザイントークン: `Assets/Stella/Scripts/Design/StellaColorTokens.cs`
- 自動セットアップメニュー: `Assets/Stella/Scripts/Editor/StellaProjectSetupMenu.cs`

## 既存ドキュメント

- Stella モジュール説明: `Assets/Stella/README.md`
- デザイナー依頼チェックリスト: `Assets/Stella/Docs/DesignerHandoffChecklist.md`
- 共有カラー JSON: `Assets/Stella/DesignTokens/stella-color-tokens.json`

## デザイン参照（Figma Node ID）

- メイン画面: `8:2098`
- カラーガイド: `8:2334`
- ボタンガイド: `8:2335`
- ロゴガイド: `8:2336`

## 後続エージェント向け注意事項

- 自動生成シーンでは `skipStartupLogo = true`（高速確認用）
- Start ボタンはフォールバック経由で `Stage01` をロード
- 起動ロゴは現状プレースホルダー（本素材差し替え前提）
- `Bootstrap.unity` / `Stage01.unity` がある場合、自動生成で上書き可能

## 次のマイルストーン

1. `Stage01` のコアループ実装
- プレイヤー移動（ドラッグ）
- 自動射撃
- 敵ウェーブ生成
- 勝敗フロー
2. Figma 正式素材への差し替え
3. ロゴ素材導入後に `skipStartupLogo = false` へ変更
