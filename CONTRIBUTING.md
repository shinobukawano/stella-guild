# CONTRIBUTING

Stella Guild プロジェクトへの貢献ルールです。  
小さく安全に変更を積み上げることを優先します。

## 前提環境

- Unity `6000.3.3f1`
- URP プロジェクト
- iOS 向け開発を優先

## ブランチ運用

1. `main` へ直接 push しない
2. 作業ブランチを作成する
3. 1ブランチ1テーマでまとめる

ブランチ名例:

- `feature/stage01-core-loop`
- `fix/title-page-navigation`
- `chore/readme-update`

## コミット方針

1. コミットは小さく、目的を1つに絞る
2. 自動生成物をむやみに含めない
3. Unity の `.meta` は対応アセットとセットでコミットする

コミットメッセージ例:

- `feat: add startup logo fade flow`
- `fix: wire start button to stage01 fallback`
- `docs: add designer handoff checklist`

## PR チェックリスト

- [ ] 変更意図が README / PR本文で説明されている
- [ ] 不要な差分（`Library/`, `Temp/`, `Logs/`）が含まれていない
- [ ] 必要な `.meta` が揃っている
- [ ] `Bootstrap` から `Stage01` まで基本遷移が壊れていない
- [ ] 新規 UI 追加時、既存のトークン/スタイルを優先利用している

## Unity での注意点

1. シーン・Prefab・アセットの移動は Unity Editor 上で実施する  
   `.meta` との対応が崩れるため、Finder/Explorer 直移動は避ける
2. 大きなリネーム/移動前にコミットして退避ポイントを作る
3. 警告が増えた場合は放置せず、PR 内で理由を明記する

## デザイン反映ルール

1. Figma からの素材は命名規則を統一する
2. 追加色を使う場合は HEX を記録し、トークン化を検討する
3. 先にプレースホルダーで実装し、素材差し替えしやすい構造を維持する

参照:

- `Assets/Stella/Docs/DesignerHandoffChecklist.md`
- `Assets/Stella/DesignTokens/stella-color-tokens.json`

## 後続エージェント向けメモ

1. まず `README.md` と `Assets/Stella/README.md` を読む
2. 起動確認は `Stella > Setup > Create Bootstrap + Stage01` を使う
3. 現状 `Stage01` はプレースホルダーで、本格ゲームロジックは未着手
