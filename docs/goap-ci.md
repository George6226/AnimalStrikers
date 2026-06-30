# GOAP 自動検証 CI

味方 NPC の GOAP（本番選出・ドライブ中追従）を GitHub Actions / ローカルで自動検証する手順です。

## クイックスタート

```bash
# ローカル Mac（Unity Hub インストール済み）
./scripts/ci/run-goap-ci.sh all

# Docker（CI と同じ経路・Secrets が必要）
export UNITY_EMAIL=... UNITY_PASSWORD=... UNITY_SERIAL=...
./scripts/ci/run-goap-docker.sh all
```

初回セットアップ: `./scripts/ci/setup-github-ci.sh`

## 実行モード

| モード | 内容 | 目安時間 |
|--------|------|----------|
| `editmode` | EditMode テストのみ | 約30秒 |
| `batch-combined` | 静止本番選出 #2-#12 | 約2分 |
| `batch-wing` | 翼ドライブ #17/#18 | 約2分 |
| `batch-cf-drive` | CFドライブ #13-#16 | 約3分 |
| `batch-defense` | 守備基本 #2-#3 | 約2分 |
| `batch-defense-tactical` | 守備戦術 #4-#6 | 約2分 |
| `batch-defense-combined` | 守備統合本番選出 #2-#6 | 約3分 |
| `batch-defense-combined-drive` | 守備統合ドライブ #7-#8 | 約2分 |
| `batch-defense-drive` | 守備ドライブ #7-#8 | 約2分 |
| `batch` | 上記8バッチ連続 | 約17分 |
| `all` | EditMode + 8バッチ（CI 相当） | 約16-20分 |

設定の単一ソース: `scripts/ci/goap-ci-config.sh`

## 合格基準

### EditMode（112 件）

- `GoapBatchVerificationLogParserTests`
- `TeammateNpcSupportPlanningEditModeTests`
- `GoapProductionSelectionExpectationsEditModeTests`
- `GoapDefenseProductionSelectionExpectationsEditModeTests`

### バッチ検証

各プロファイルは `SELECTION_TOTAL` と `RUNTIME_TOTAL` の両方が満たされていること（`resolve-batch-verify-result.sh`）。

| プロファイル | パターン | 期待 |
|-------------|---------|------|
| `combined` | #2-#12 静止本番選出 | 11/11 |
| `wingDrive` | #17/#18 翼ドライブ | SELECTION 2/2 + RUNTIME 2/2 |
| `cfDrive` | #13-#16 CFドライブ | SELECTION 4/4 + RUNTIME 4/4 |
| `defenseBaseline` | #2-#3 守備基本 | SELECTION 2/2 |
| `defenseTactical` | #4-#6 守備戦術 | SELECTION 3/3 |
| `defenseCombined` | #2-#6 守備統合本番選出 | SELECTION 5/5 |
| `defenseCombinedDrive` | #7-#8 守備統合ドライブ | SELECTION 2/2 + RUNTIME 2/2 |
| `defenseDrive` | #7-#8 守備ドライブ | SELECTION 2/2 + RUNTIME 2/2 |

Unity が終了ハングしても、`Logs/goap-batch-*-result.txt` または `GoapDiag_*_latest.txt` のマーカーで合格判定します。

## GitHub Actions

- ワークフロー: `.github/workflows/goap-ci.yml`
- トリガー: `push`（main/master/develop）、`pull_request`、`workflow_dispatch`
- 手動実行時は `mode` 入力で部分実行可能（既定: `all`）

### Secrets

| Secret | 必須 | 説明 |
|--------|------|------|
| `UNITY_EMAIL` | はい | Unity ID |
| `UNITY_PASSWORD` | はい | Unity ID パスワード |
| `UNITY_SERIAL` | 推奨 | Hub シリアル（`UNITY_LICENSE` より優先） |
| `UNITY_LICENSE` | 代替 | CI 用 .ulf XML 全文 |

## ログの見方

失敗時は Artifacts の `goap-ci-logs` またはローカル `Logs/` を確認:

| ファイル | 用途 |
|---------|------|
| `goap-editmode-results.xml` | EditMode 合否 |
| `goap-batch-*-verify.log` | Unity バッチ実行ログ |
| `goap-batch-*-result.txt` | `PASS:` / `FAIL:` 行 |
| `GoapDiag_*_latest.txt` | `BATCH_*`, `SELECTION_*`, `RUNTIME_*` マーカー |
| `ci-unity-activate.log` | ライセンス有効化 |

## スクリプト構成

```
scripts/ci/
  goap-ci-config.sh          # 共通定数・プロファイル定義
  run-goap-ci.sh             # ローカル Mac（Unity 直実行）
  run-goap-docker.sh         # Docker / GitHub Actions
  resolve-batch-verify-result.sh
  docker-unity-personal.sh   # SERIAL 優先のライセンス有効化
  setup-github-ci.sh         # セットアップ手順表示
```

薄いラッパー（互換用）:

- `run-goap-editmode-docker.sh` → `run-goap-docker.sh editmode`
- `run-goap-batch-verify-docker.sh` → `run-goap-docker.sh batch-combined`

## 環境変数

| 変数 | 既定 | 説明 |
|------|------|------|
| `UNITY_PATH` | （自動検出） | ローカル Unity 実行ファイル |
| `GOAP_UNITY_VERSION` | `6000.2.7f2` | Hub / Docker イメージのバージョン |
| `GOAP_EDITMODE_EXPECTED_TESTS` | `107` | ドキュメント・進捗表示用 |
| `GOAP_UNITY_DOCKER_TIMEOUT` | `2400` | バッチの timeout 秒 |
| `GOAP_EDITMODE_DOCKER_TIMEOUT` | `2700` | EditMode の timeout 秒 |
