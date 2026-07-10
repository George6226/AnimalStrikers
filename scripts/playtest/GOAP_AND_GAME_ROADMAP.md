# AnimalStrikers GOAP & ゲーム機能ロードマップ

最終更新: 2026-07-10

## 完了済み

- GOAP M1（保持中 Pass/Shoot/Dribble）
- GOAP M2（オフボール Support / FreeBall / IncomingPass）
- GOAP M3（守備 DefensivePositioning / EnemyBallDefense）
- Phase A 本番 Main GOAP
- Phase B 敵 Main GOAP
- **P1** 受け失敗後 NoGoal 固着対策（#41）
- **P2** キックオフ前 GOAP 停止 + GAME 開始 replan（#42）

---

## 現在: GOAP 仕上げ（G0〜G5）

| ID | 課題 | 状態 | ブランチ/PR |
|----|------|------|-------------|
| **G0** | 本番 Main の守備デッドゾーン（Lion NoGoalSelected） | **完了 (#43)** | `fix/goap-polish-main-defense` |
| **G1** | ShootAtGoal ActionRejected ループ | **実装中** | `fix/goap-g1-shoot-rejected` |
| G2 | FreeBallRecovery PlanFailure（Boar/Gorilla） | 未着手 | |
| G3 | IncomingPassReceive PlanFailure | 未着手 | |
| G4 | 敵 NPC NoGoalSelected（Crocodile 等） | 未着手 | |
| G5 | 検証スクリプト上限の更新 | 未着手 | |

### G0 完了条件

- 本番 Main（操作キャラ）が敵保持時に `DefensivePositioning` 戦術パスを使う
- `EnemyBallDefense` 完了直後の `NoGoalSelected` が激減

### GOAP 仕上げ全体の出口条件（3分 Play）

| 指標 | 目標 |
|------|------|
| `missed+nogal` | 0 維持 |
| `NoGoalIdle(wait>=3s)` | 0 維持 |
| `NoGoalSelected`（試合中） | < 20 |
| `ActionRejected(ShootAtGoal)` | < 5 |
| `PlanFailure(FreeBallRecovery)` | < 10 |
| Phase D コア | 回帰なし |

### 検証コマンド

```bash
./scripts/playtest/prepare-goap-npc-watch-match.sh goap_polish_<日付>
# Unity 3分 Play
MODE=full ./scripts/playtest/analyze-phase-d-pass-receive-log.sh Assets/DebugLog/GoapSummary_latest.txt
```

---

## 次: ゲーム機能フェーズ（F1〜F5）

| 順 | ID | 機能 | 主なファイル |
|----|-----|------|-------------|
| 1 | **F1** | スタミナ枯渇による移動速度低下 | `AnimalHandler.cs`, `PhotonHPGauge.cs` |
| 2 | **F2** | ダッシュのスタミナ連動（不足時禁止） | `AnimalAction_Dash.cs`, `GoapNpcMotor.cs` |
| 3 | **F3** | GK 実装（GOAP 外・独立） | `GoalkeeperNpcBrain.cs` |
| 4 | **F4** | Main NPC スライディング/タックル GOAP | `AnimalAction_Sliding.cs`, GOAP カタログ |
| 5 | **F5** | 必殺技の NPC/GOAP 接続 | `AnimalAction_Special.cs`, キャラ別 SpecialActions |

**F1 と F3 は並行可能。** F2 は F1 の直後。F4/F5 は GOAP 仕上げ完了後。

---

## F5 完了後（フェーズ6）

| 順 | ID | 項目 | 理由 |
|----|-----|------|------|
| 1 | **6-A** | 敵 AI の対称化・難易度調整 | 味方 GOAP 安定後に敵 Main/Sub のバランス調整 |
| 2 | **6-B** | セットプレイ（スローイン・コーナー・ゴールキック） | F3 GK の延長、`BallKickoffAssignment` 連携 |
| 3 | **6-C** | スタミナの GOAP 連携 | `hasStamina` Fact、`RegainStaminaGoalSO` |
| 4 | **6-D** | GOAP ダッシュの戦術統合 | 受け位置・ルーズボール追跡でのダッシュ判断 |
| 5 | **6-E** | マルチプレイ同期の堅牢化 | 必殺技 Photon、ボール保持者ズレ |
| 6 | **6-F** | 試合メタ（得失点後の挙動） | リスタート配置・攻守切り替え |
| 7 | **6-G** | パフォーマンス・ログ整備 | replan 負荷、Summary レベル調整 |
| 8 | **6-H** | バッチ検証 CI 拡張 | GK・スタミナ・タックルシナリオ追加 |

---

## 全体フロー

```
[完了] M1/M2/M3 + P1/P2
  ↓
[今] GOAP仕上げ G0〜G5
  ↓
[F1] スタミナ枯渇減速
  ↓
[F2] ダッシュ連動 ─┐
  ↓                │ 並行可
[F3] GK実装 ───────┘
  ↓
[F4] Main NPCタックル/スライディング
  ↓
[F5] 必殺技GOAP接続
  ↓
[6-A]〜[6-H]
```
