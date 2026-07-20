# AnimalStrikers GOAP & ゲーム機能ロードマップ

最終更新: 2026-07-20

## 完了済み

- GOAP M1（保持中 Pass/Shoot/Dribble）
- GOAP M2（オフボール Support / FreeBall / IncomingPass）
- GOAP M3（守備 DefensivePositioning / EnemyBallDefense）
- Phase A 本番 Main GOAP
- Phase B 敵 Main GOAP
- **P1** 受け失敗後 NoGoal 固着対策（#41）
- **P2** キックオフ前 GOAP 停止 + GAME 開始 replan（#42）
- **GOAP 仕上げ G0〜G6**（下記アーカイブ）
- **F1〜F5**（スタミナ / ダッシュ / GK / SlideTackle / UseSpecial）
- GK follow-up: #60（狙い・守備即完了）、#61（replan churn）、#62（敵 GK ホーム深さ復帰）
- **フェーズ6（6-A〜6-H）**: #63〜#74 + 後続 #76〜#78（必殺 Photon・RegainStamina/GK バッチ）

---

## 完了: フェーズ6（6-A 〜 6-H）— **全項目完了 (#63〜#74, 後続 #76〜#78)**

**エージェント方針（アーカイブ）**: `.cursor/rules/phase6.mdc`  
**詳細表**: 下方「F5 完了後（フェーズ6）」

### 6-A 進捗 — **完了 (#63+#64)**

| スライス | 状態 | 内容 |
|----------|------|------|
| **P0** 難易度ノブ | **完了 (#63)** | Easy/Normal/Hard |
| **P1** 守備戦術ミラー | **完了 (#63)** | `CalculateDefend(mirrored)` |
| **P2** Sub 攻撃抑止 | **完了 (#64)** | Easy 時のみ敵 Sub から攻撃ゴール除外 |

#### 6-A 目視確認（P1）— **Normal: 停止 OK · パスは停止受け △（2026-07-20）**

下準備: `./scripts/playtest/prepare-6a-enemy-ai-difficulty-visual-check.sh`（`DIFFICULTY=Normal`）

##### 初回目視 — NG（main @ 5269ed5）

観戦 3 分 · 難易度差（Easy/Hard 比較）は未実施

| 指標 | 初回 | ゲート | 判定 |
|------|------|--------|------|
| `missed+nogal` | 0 | 0 | ✅ |
| `NoGoalIdle(wait>=3s)` | 0 | 0 | ✅ |
| Phase D コア | PASS 22/22 WARN 1 | — | ✅ |
| 全体 `NoGoalSelected` | 19 | < 20（G6 WARN） | ✅ |
| Crocodile `NoGoalSelected` | 14 | ≤ 20 | ✅ |
| 味方 Boar `NoGoalSelected` | 0 | ≤ 8 | ✅ |
| Pass 選択 / Dribble 選択 | 22 / 21 | — | △ Pass 偏重（体感） |
| パス受け失敗（timeout 等） | 8 | — | △ |

**初回 NG 項目**

| # | 現象 | 分類 | 対応 |
|---|------|------|------|
| 1 | 移動中パスでミス | バグ（P1） | **部分対応**（下記） |
| 2 | パスばかりで前に進まない | チューニング（P2） | **完了 (#85)** · 再目視 △ |
| 3 | 敵 GK が相手側 GK まで移動 | P0 バグ | **完了 (#81)** |
| 4 | 残り ~2:00 で全員停止 | P0 バグ | **完了 (#81+#82)** |

**P0 修正 PR**

| PR | 内容 |
|----|------|
| #81 | 敵 GK ミラー（`NPC` タグ）+ `HAS_BALL` 残存時の NoGoal 強制守備 |
| #82 | 相手 `SHOOT` 遷移中の `IsOpponentBallDefenseContext` 拡張 |

##### 再目視 — OK（main @ 533e8e4 · #82 後）

観戦 ~3 分（`15:39:01`〜`15:42:01`）· 目視: **最後まで停止なし**

| 指標 | 再目視 | ゲート | 判定 |
|------|--------|--------|------|
| `missed+nogal` | 0 | 0 | ✅ |
| `NoGoalIdle(wait>=3s)` | 0 | 0 | ✅ |
| 全体 `NoGoalSelected` | **0** | < 20 | ✅ |
| Crocodile / Boar / Lion / Gorilla NoGoal | **0** | — | ✅ |
| Pass / Dribble / Shoot 選択 | 278 / 132 / 172 | — | △ Pass 偏重 |
| パス受け失敗（timeout） | 15 | — | △ |

##### P1+#84 / P2+#85 マージ後 再目視 — NG（main @ 96f693e · 2026-07-17）

| PR | 内容 | 再目視 |
|----|------|--------|
| #84 | `TryGetReceiveMoveTarget` が PASS 中 ballKeep 追従 | パスミス継続 |
| #85 | `NormalPassPenalty` 0.40→**0.55** | Dribble↑（△〜○）· Pass 偏重は緩和 |

**目視 NG**

| # | 現象 | ログ根拠 | 対応 |
|---|------|----------|------|
| 1 | パスミス継続（受け手移動） | `ReceivePass timeout` 16 / received 少 | 停止受け方針へ（下記） |
| 2 | 残り ~30s 全員停止 | `AIContextSwitcher` PASS/FREE 中立で Abort 連発（`NoGoalSelected=0`） | **P0'**（下記） |

##### フォローアップ修正（ローカル · 未マージ PR 想定）

| ID | 内容 | EditMode | 目視 |
|----|------|----------|------|
| **P0'** | `PossessionContextSwitchRules`: PASS/FREE 中立では Abort しない | ✅ | 一部再発後さらに防御 |
| **P1b** | キック側リード無効 + intended 受け点共有 · **停止受け**優先 | ✅ | 停止受けは成功率○ |
| **P0''** | Forced 守備: 到達済み `MoveToDefensive` 除外 + Skip 時 0.75s 抑制（Forced→Skip 無限ループ） | ✅（342 件） | **2026-07-20 OK** |

**P0'' 再発経緯**: P0' 後も残り ~1:00〜1:30 で停止。原因は Abort ではなく `ForcedTactical → MoveToDefensivePosition → ActionSkipped(context_changed)` の毎フレームループ。

##### 最新目視 — 停止 OK（2026-07-20）

- **全員停止はなし**（P0'' 後）
- パス: 止まって受けるのは成功率高 · **走りながら受けは未解決**（キック／受けの約束合わせが次）

**6-A Normal フォローアップ**

| P | 内容 | 状態 |
|---|------|------|
| P0 / P0' / P0'' | 全員停止（NoGoal / Abort / Forced Skip） | **目視 OK（2026-07-20）** · コードは未マージなら PR 化 |
| P1 | 移動中パス受け | **△** 停止受けは実用可 · ラン受けは未着手 |
| P2 | Normal `PassPenalty` 0.55（#85） | **完了** · 目視はドリブル増で部分 OK |
| P1c（任意） | 走りながら受け（ラン維持 + 同一会合点） | **未着手** |

アーカイブ:

- 初回 NG: `Assets/DebugLog/archives/GoapSummary_phase6_visual_6a_normal_20260717_20260717_134145.txt`
- #82 後 OK: `Assets/DebugLog/archives/GoapSummary_phase6_visual_6a_normal_shoot_defense_ok_20260717_20260717_154307.txt`

```bash
MODE=full ./scripts/playtest/analyze-phase-d-pass-receive-log.sh \
  Assets/DebugLog/archives/GoapSummary_phase6_visual_6a_normal_shoot_defense_ok_20260717_20260717_154307.txt
```

**P1 目視残り**: Easy / Hard 比較 → 6-B GK 配球 → 6-C スタミナ → 6-E 必殺（オンライン2人）→ 6-F 得点後リスタート

### 6-B 進捗 — **完了 (#65+#66)**

| スライス | 状態 | 内容 |
|----------|------|------|
| **P0** OutOfPlay 純ロジック | **完了 (#65)** | `OutOfPlayClassifier` / `SetPieceAssignmentRules` |
| **P1** ゴールキック runtime | **完了 (#66)** | FREE + GoalKick → 守備 GK HOLD + suppress → Distribution |
| ~~P2 スローイン~~ / コーナー | **不要** | 本ゲーム非採用。#67 はクローズ |

### 6-C 進捗 — **完了 (#69)**

| スライス | 状態 | 内容 |
|----------|------|------|
| **P0** hasStamina + RegainStamina | **完了 (#69)** | Fact / Goal / StandRecover / Catalog（緊急文脈では非選出） |

### 6-D 進捗 — **完了 (#70)**

| スライス | 状態 | 内容 |
|----------|------|------|
| **P0** FreeBall / ReceivePass ダッシュ | **完了 (#70)** | 遠距離+スタミナ可で ON、近接/キャッチ相で OFF |

### 6-E 進捗 — **完了 (#71+#76)**

| スライス | 状態 | 内容 |
|----------|------|------|
| **P0** ボール保持者ネットワーク適用 | **完了 (#71)** | RPC で `BallOwnerID` / BallState / TeamBB を揃える（kickoff suppress bypass） |
| **P1** 必殺技 Photon 同期 | **完了 (#76)** | 発動時ゲージ RPC + 終了時 `EndSpecial` RPC（NPC モード skip） |

### 6-F 進捗 — **完了 (#72)**

| スライス | 状態 | 内容 |
|----------|------|------|
| **P0** 得点後リスタート安定化 | **完了 (#72)** | 失点側キックオフ割当純関数化 + 得点直後 GOAP 抑制窓（2s） |

### 6-G 進捗 — **完了 (#73)**

| スライス | 状態 | 内容 |
|----------|------|------|
| **P0** replan coalesce + Summary 整備 | **完了 (#73)** | volatile 即時 replan の coalesce、`GoapPassDiagnostic` Summary フィルタ |

### 6-H 進捗 — **完了 (#74+#77+#78)**

| スライス | 状態 | 内容 |
|----------|------|------|
| **P0** SlideTackle バッチ検証 | **完了 (#74)** | `slideTackle` プロファイル + #10 NearContact（SELECTION 1/1） |
| **P1** RegainStamina バッチ検証 | **完了 (#77)** | `regainStamina` プロファイル + #8 RwOwner_WingHold（SELECTION 1/1） |
| **P2** GK バッチ runner | **完了 (#78)** | `goalkeeper` プロファイル + 敵脅威 TrackBall（SELECTION 1/1）、CI **11** バッチ |

### フェーズ6 後続 — **完了 (#76〜#78)**

ロードマップ上の後続候補（6-E P1・6-H P1/P2）はすべて完了。未着手のフォローアップはなし。

---

## 現在: フェーズ6 実装完了 — 6-A Normal 停止 OK · パスは停止受け △

ロードマップ上の **6-A〜6-H 実装はすべて完了**。  
**6-A Normal**:

- **全員停止**: P0（#81+#82）+ P0'/P0''（ContextSwitcher Abort / Forced Skip ループ）→ **2026-07-20 目視 OK**
- **パス**: #84+#85 + 停止受け方針 → 実用可 · **走り受けは未着手（P1c）**
- **CI**: EditMode **342** 件 · `mode=all` = EditMode + **11** バッチ

**次（優先候補）**: 未マージ修正の PR 化 → Easy/Hard 比較目視 →（任意）P1c ラン受け → 6-B 以降の目視残り。  
フェーズ7 の新規着手はユーザーがスコープを指定してから。

### アーカイブ: GOAP 仕上げ（G0〜G6）— 完了

| ID | 課題 | 状態 | ブランチ/PR |
|----|------|------|-------------|
| **G0** | 本番 Main の守備デッドゾーン（Lion NoGoalSelected） | **完了 (#43)** | `fix/goap-polish-main-defense` |
| **G1** | ShootAtGoal ActionRejected ループ | **完了 (#44 / Play 合格)** | `fix/goap-g1-shoot-rejected` |
| **G2** | FreeBallRecovery PlanFailure（Boar/Gorilla） | **完了 (#45 / Play 合格)** | `fix/goap-g2-freeball-planfailure` |
| **G3** | IncomingPassReceive PlanFailure | **完了 (#46 / Play 合格)** | `fix/goap-g3-incoming-pass-planfailure` |
| **G4** | 敵 NPC NoGoalSelected（Crocodile 等） | **完了 (#47+#48+#49 / Play 合格)** | `fix/goap-g4c-post-shoot-grace` |
| **G5** | 検証スクリプト上限の更新 | **完了 (#50)** | `chore/goap-g5-play-gate-limits` |
| **G6** | 味方 Main NoGoalSelected（Lion/Gorilla 等） | **完了 (#51+#52 / Play 合格)** | `fix/goap-g6b-defense-grace-hasball` |

### G0 完了条件

- 本番 Main（操作キャラ）が敵保持時に `DefensivePositioning` 戦術パスを使う
- `EnemyBallDefense` 完了直後の `NoGoalSelected` が激減

### G1 Play 検証結果（`goap_g1_play_pass_20260710` / main @ 5ab55a4）

| 指標 | 修正前 | 今回 | 判定 |
|------|--------|------|------|
| Lion `ActionRejected(ShootAtGoal)` | 57〜96 | **0** | ✅ |
| 全体 `ActionRejected(ShootAtGoal)` | — | **0** | ✅ |
| Lion `ActionStart(ShootAtGoal)` | — | 7 | ✅ 実シュート実行 |
| `missed+nogal` | 0 | **0** | ✅ |
| `NoGoalIdle(wait>=3s)` | 0 | **0** | ✅ |
| Phase D コア | PASS 10/10 | **PASS 10/10** | ✅ |

アーカイブ: `Assets/DebugLog/archives/GoapSummary_goap_g1_play_pass_20260710_20260710_215749.txt`

### G2 Play 検証結果（`goap_g2_play_pass_20260710` / main @ bfc30c0）

| 指標 | 修正前 | 今回 | 判定 |
|------|--------|------|------|
| Boar `PlanFailure(FreeBallRecovery)` | 56 | **0** | ✅ |
| Gorilla `PlanFailure(FreeBallRecovery)` | 0 | **0** | ✅ |
| 全体 `PlanFailure(FreeBallRecovery)` | 61 | **0** | ✅ |
| `missed+nogal` | 0 | **0** | ✅ |
| `NoGoalIdle(wait>=3s)` | 0 | **0** | ✅ |
| Phase D コア | PASS 10/10 | **PASS 10/10** | ✅ |

アーカイブ: `Assets/DebugLog/archives/GoapSummary_goap_g2_play_pass_20260710_20260710_234717.txt`

### G3 Play 検証結果（`goap_g3_play_pass_20260713` / main @ 906388f）

| 指標 | 修正前 | 今回 | 判定 |
|------|--------|------|------|
| `PlanFailure(IncomingPassReceive)` | 26（Lion 15 / Boar 11） | **0** | ✅ |
| `ForcedIncomingPassReceivePlan` | — | **10**（Lion 9 / Gorilla 1） | ✅ 強制経路稼働 |
| `MoveToReceivePass` 開始/完了 | — | 50 / 49 | ✅ |
| `missed+nogal` | 0 | **0** | ✅ |
| `NoGoalIdle(wait>=3s)` | 0 | **0** | ✅ |
| G1/G2 回帰（ShootRejected / FreeBall PlanFailure） | 0 | **0** | ✅ |
| Phase D コア | PASS 10/10 | **PASS 10/10** | ✅ |

アーカイブ: `Assets/DebugLog/archives/GoapSummary_goap_g3_play_pass_20260713_20260713_113345.txt`

### G4 Play 検証結果（`goap_g4c_play_pass_20260713` / main @ 2961f2b）

| 指標 | G3 修正前 | G4b (#48) | 今回 (#49) | 判定 |
|------|-----------|-----------|------------|------|
| Crocodile `NoGoalSelected` | 49 | 52 | **13** | ✅ |
| Elephant `NoGoalSelected` | 13 | 9 | **2** | ✅ |
| 敵 NPC 合計 | 62 | 64 | **15** | ✅ |
| Shoot→NoGoal | 18 | 15 | **0** | ✅ |
| `ForcedPostShootDefensePlan(postShootGrace)` | — | 0 | **262** | ✅ |
| 全体 `NoGoalSelected` | 110 | 138 | **59** | △（G6 へ分離） |
| G1/G2/G3 回帰 | 0 | 0 | **0** | ✅ |
| Phase D コア | PASS 10/10 | PASS 10/10 | **PASS 10/10** | ✅ |

アーカイブ: `Assets/DebugLog/archives/GoapSummary_goap_g4c_play_pass_20260713_play_20260713_135442.txt`

**G4 修正の流れ**: #47 守備文脈拡張 → #48 `ForcedPostShootDefensePlan` → #49 `ShootAtGoal` 完了後 0.75s 猶予窓

### G5 完了内容

- `scripts/playtest/goap-play-gate-config.sh` — G0〜G4 Play 上限の単一ソース
- `analyze-phase-d-pass-receive-log.sh` — G1/G2/G3 回帰（FAIL）+ G4 敵 NPC（FAIL）+ 全体 NoGoal（WARN・G6 まで）
- `docs/goap-ci.md` — EditMode 期待件数 140 → **178**

G4c アーカイブでのゲート検証:

```bash
MODE=full ./scripts/playtest/analyze-phase-d-pass-receive-log.sh \
  Assets/DebugLog/archives/GoapSummary_goap_g4c_play_pass_20260713_play_20260713_135442.txt
```

### G6 Play 検証結果（`goap_g6_play_pass_20260713` / main @ 71ae3b4 + #52 修正）

| 指標 | G4c (#49) | G6 初回 (#51) | 今回 (#52) | 判定 |
|------|-----------|---------------|------------|------|
| Lion `NoGoalSelected` | 17 | 19 | **0** | ✅ |
| Gorilla `NoGoalSelected` | 16 | 2 | **2** | ✅ |
| Boar `NoGoalSelected` | 11 | 2 | **2** | ✅ |
| Crocodile `NoGoalSelected` | 13 | 33 | **0** | ✅ |
| 全体 `NoGoalSelected` | 59 | 63 | **8** | ✅ (< 20) |
| Shoot→NoGoal | 0 | 28* | **0** | ✅ |
| G1/G2/G3 回帰 | 0 | 0 | **0** | ✅ |
| Phase D コア | PASS | PASS | **PASS 22/22** | ✅ |

\*初回は `HAS_BALL` 判定順のバグ + Shoot 指標が ActionStart も含んでいたため過大計上

**#52 修正**: `NeedsForcedDefensePlanWhenNoGoal` でシュート猶予を `HAS_BALL` チェックより先に評価

アーカイブ: `Assets/DebugLog/archives/GoapSummary_goap_g6_play_pass_20260713_20260713_145638.txt`

### G6 実装内容

`SelectBestGoal returned null` 時のフォールバック強化:

- `TryBuildForcedPlanWhenSelectBestGoalNull` — 守備 / サポート / 受け / フリーボール / 攻撃の順で強制プラン
- パス直後猶予 `_postPassSupportGraceUntil`、戦術 `ActionSkipped` 直後の文脈猶予
- `goap-play-gate-config.sh` — 味方 NoGoal 上限（Lion/Gorilla/Boar ≤ 8、全体 ≤ 25 WARN）

### G6 概要（G4 Play で判明・別タスク）

G4 合格後も全体 `NoGoalSelected` が 59 件残る。主因は味方 Main（Lion 17 / Gorilla 16）と味方 Sub（Boar 11）。敵 NPC は G4 で解消済み。

| 指標 | G4c 時点 | G6 目標 |
|------|----------|---------|
| Lion `NoGoalSelected` | 17 | 激減 |
| Gorilla `NoGoalSelected` | 16 | 激減 |
| 全体 `NoGoalSelected` | 59 | **< 20** |

### GOAP 仕上げ全体の出口条件（3分 Play）

| 指標 | 目標 |
|------|------|
| `missed+nogal` | 0 維持 |
| `NoGoalIdle(wait>=3s)` | 0 維持 |
| `NoGoalSelected`（試合中） | < 20（**G6 で対応**） |
| `ActionRejected(ShootAtGoal)` | < 5 |
| `PlanFailure(FreeBallRecovery)` | < 10 |
| Phase D コア | 回帰なし |

### 検証コマンド

```bash
./scripts/playtest/prepare-goap-npc-watch-match.sh goap_polish_<日付>
# Unity 3分 Play
MODE=full ./scripts/playtest/analyze-phase-d-pass-receive-log.sh Assets/DebugLog/GoapSummary_latest.txt
# G0〜G4 ゲート（ActionRejected / PlanFailure / 敵 NoGoal 等）は同スクリプト末尾で自動判定
```

---

## 完了: ゲーム機能フェーズ（F1〜F5）

| 順 | ID | 機能 | 状態 | 主なファイル |
|----|-----|------|------|-------------|
| 1 | **F1** | スタミナ枯渇による移動速度低下 | **完了 (#53)** · 目視 OK | `AnimalHandler.cs`, `PhotonHPGauge.cs` |
| 2 | **F2** | ダッシュのスタミナ連動（不足時禁止） | **完了 (#54)** · 目視 OK | `AnimalAction_Dash.cs`, `GoapNpcMotor.cs` |
| 3 | **F3** | GK 実装（GOAP 外・独立） | **完了 (#56+#57)** · 目視 OK | `GoalkeeperNpcBrain.cs`, `GoalkeeperPositioning.cs`, `GoalkeeperDistribution.cs` |
| 4 | **F4** | Main NPC スライディング/タックル GOAP | **完了 (#58)** · 目視ログ OK | `SlideTackleActionSO.cs`, `AnimalAction_Sliding.cs` |
| 5 | **F5** | 必殺技の NPC/GOAP 接続 | **完了 (#59)** · 目視ログ OK | `UseSpecialActionSO.cs`, `AnimalAction_Special.cs` |

### F1 実装内容

- `PhotonHPGauge`: `StaminaRatio` / `IsExhausted` 公開
- `AnimalHandler.moveCommon`: 残量 25% 以下で線形減速（枯渇時 ×0.55、サメ泡と合成）
- スタミナ増減: 通常移動で回復・ダッシュ移動で消費・被弾で減少
- `ConstData`: 既定閾値・枯渇倍率

### F2 実装内容

- `AnimalAction_Dash`: スタミナ 0 でダッシュ開始拒否・枯渇時自動解除
- `AnimalAction_Move` / `AnimalActionSelector`: 速度倍率・入力経路でも同条件を適用
- `GoapNpcMotor`: `CanUseDash` / `TrySetDash` / `MoveToward(..., useDash)`（戦術統合は 6-D）

**目視確認**: `./scripts/playtest/prepare-f1-f2-stamina-visual-check.sh`（2026-07-13 全項目 OK）

### F3 実装内容（MVP）

- `GoalkeeperPositioning`: ゴールライン位置・ボール X 追従・ルーズボール接近
- `GoalkeeperNpcBrain`: 味方/敵 GK 共通の `FixedUpdate` 移動（GOAP 外）

**目視確認（位置取り）**: `./scripts/playtest/prepare-f3-goalkeeper-visual-check.sh`

**目視確認（配球 + 敵守備ミラー）**: `./scripts/playtest/prepare-f3-gk-distribution-visual-check.sh`  
（GK キャッチ後パス、味方受け位置、味方 GK 保持時の敵 Retreat / DefensivePosition）

### F4 実装内容（MVP）

- `SlideTackleActionSO` / `Runtime`: 相手ボール近接時に `AnimalAction_Sliding` を実行（守備ゴール側）
- 対象: 本番味方 Main / 敵 Main（Sub はコスト +50 で既存守備バッチを壊さない）
- 前提: `nearEnemyHasBall` + 敵保持文脈、遠距離では選出されない

**目視確認**: `./scripts/playtest/prepare-f4-slide-tackle-visual-check.sh`

### F5 実装内容（MVP）

- `UseSpecialActionSO` / `Runtime` / `GoapSpecialBridge`: ゲージ満タン時に `AnimalAction_Special` を実行
- 攻撃（BallPossessionAttack）と守備（DefensivePositioning）双方の候補（キャラ別 CanExecuteSpecial に委任）
- 対象: 本番味方 Main / 敵 Main、ゲージ未達はコスト 99
- `AnimalAction_Special.CanExecuteSpecial` にゲージ満タン条件を復帰
- ゲージ加速: `SPECIAL_GAUGE_VALUE` 0.1→0.25（シュート/被ダメ）、パス成功時 `SPECIAL_GAUGE_VALUE_ON_PASS` 0.12

**目視確認**: `./scripts/playtest/prepare-f5-use-special-visual-check.sh`

**F1 と F3 は並行可能。** F2 は F1 の直後。

---

## F5 完了後（フェーズ6）

| 順 | ID | 項目 | 理由 |
|----|-----|------|------|
| 1 | **6-A** | 敵 AI の対称化・難易度調整 | **完了 (#63+#64)** |
| 2 | **6-B** | セットプレイ（ゴールキック） | **完了 (#65+#66)**。スローイン／コーナーは本ゲーム非採用（#67 closed） |
| 3 | **6-C** | スタミナの GOAP 連携 | **完了 (#69)** |
| 4 | **6-D** | GOAP ダッシュの戦術統合 | **完了 (#70)** |
| 5 | **6-E** | マルチプレイ同期の堅牢化 | **完了 (#71+#76)** |
| 6 | **6-F** | 試合メタ（得失点後の挙動） | **完了 (#72)** |
| 7 | **6-G** | パフォーマンス・ログ整備 | **完了 (#73)** |
| 8 | **6-H** | バッチ検証 CI 拡張 | **完了 (#74+#77+#78)**。CI **11** バッチ + EditMode **314** 件 |

---

## 全体フロー

```
[完了] M1/M2/M3 + P1/P2
  ↓
[完了] GOAP仕上げ G0〜G6
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
[6-A]〜[6-H] + 後続 #76〜#78  ← 完了
  ↓
（次フェーズ未定）
```
