#!/usr/bin/env bash
# GitHub Actions 用 Unity ライセンスのセットアップ手順を表示する。
# 実行: ./scripts/ci/setup-github-ci.sh
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
# shellcheck source=goap-ci-config.sh
source "${SCRIPT_DIR}/goap-ci-config.sh"

cat <<EOF
=== GitHub CI セットアップ ===

1) リポジトリを push する
   git remote add origin https://github.com/<user>/<repo>.git
   git push -u origin main

2) GitHub Secrets（いずれかのライセンス方式 + 認証情報）

   必須:
   | Secret | 内容 |
   |--------|------|
   | UNITY_EMAIL | Unity ID のメール |
   | UNITY_PASSWORD | Unity ID のパスワード |

   ライセンス（どちらか一方。両方登録時は UNITY_SERIAL を優先し UNITY_LICENSE は無視）:
   | Secret | 内容 |
   |--------|------|
   | UNITY_SERIAL | Hub の本物シリアル（方法 A・推奨） |
   | UNITY_LICENSE | CI 用 .ulf の XML 全文（方法 B） |

   方法 A 利用時は UNITY_LICENSE Secret を削除するか空にすること（古い Mac 用 ulf があると失敗）

   シリアル確認: ./scripts/ci/extract-unity-personal-serial.sh

3) （参考・通常不要）旧 game-ci 用 .ulf 手順

   Mac の Unity_lic.ulf は CI では使えません。unityci Docker 用の .ulf が必要です。

   方法 A（おすすめ・ローカル Docker 不要）:
   a) このリポジトリを push 済みであること
   b) GitHub → Actions → "Request Unity CI license ALF" → Run workflow
   c) 完了後 Artifacts から .alf をダウンロード

   方法 B（ローカル Docker がある場合）:
   a) Docker Desktop を起動（macOS 13 の場合は下記「Docker について」参照）
   b) ./scripts/ci/generate-ci-unity-license.sh

   共通の続き:
   c) 生成された .alf を https://license.unity3d.com/manual にアップロード
   d) Personal を選び .ulf をダウンロード
      ※ Personal が見えない場合: Inspect で display:none を削除（game.ci 公式回避策）
   e) .ulf をテキストエディタで開き、XML 全文を UNITY_LICENSE Secret に貼り付け

   参考: https://game.ci/docs/github/activation

4) Actions を手動実行 (workflow_dispatch) または push で確認

   ワークフロー: .github/workflows/goap-ci.yml（GOAP 自動検証）
   ローカル相当: ./scripts/ci/run-goap-docker.sh all

   合格基準（mode=all）:
   | ステップ | 内容 |
   |---------|------|
   | EditMode | ${GOAP_EDITMODE_EXPECTED_TESTS} 件 |
   | combined | 本番選出 #2-#12（11/11） |
   | wingDrive | 翼ドライブ #17/#18（SELECTION+RUNTIME 2/2） |
   | cfDrive | CFドライブ #13-#16（SELECTION+RUNTIME 4/4） |
   | defenseBaseline | 守備基本 #2-#3（SELECTION 2/2） |
   | defenseTactical | 守備戦術 #4-#6,#9（SELECTION 4/4） |
   | defenseCombined | 守備統合 #2-#6（SELECTION 5/5） |
   | defenseCombinedDrive | 守備統合ドライブ #7-#8（SELECTION+RUNTIME 2/2） |
   | mainNpcAttack | Main NPC Pass/Shoot + パス後サポート（result.txt に PASS:） |

   部分実行:
   ./scripts/ci/run-goap-docker.sh editmode
   ./scripts/ci/run-goap-docker.sh batch-main-npc-attack
   ./scripts/ci/run-goap-docker.sh batch-cf-drive

   詳細: docs/goap-ci.md

5) 失敗時は Artifacts から Logs/ をダウンロード

   よくある失敗:
   - serial invalid (20110) → UNITY_SERIAL / UNITY_LICENSE の混在（SERIAL 優先時は LICENSE を削除）
   - activation failed → UNITY_EMAIL / UNITY_PASSWORD の誤り、またはパスワードの特殊文字
   - cfDrive フレーク → GoapDiag_cf_drive_latest.txt の SELECTION_/RUNTIME_ を確認

Docker image: ${GOAP_DOCKER_IMAGE}
Unity version: ${GOAP_UNITY_VERSION}

Docker について:
- 最新版 Docker Desktop は macOS 14 以降が必要（Ventura 13.7 では更新できない）
- ローカル Docker が使えない場合は上記「方法 A」（GitHub Actions）を使う

EOF

if command -v docker >/dev/null 2>&1; then
  if docker info >/dev/null 2>&1; then
    echo "Docker: running"
  else
    echo "Docker: installed but daemon not running"
  fi
else
  echo "Docker: not found (generate-ci-unity-license.sh には Docker が必要)"
fi

if command -v gh >/dev/null 2>&1; then
  echo "gh CLI: available"
else
  echo "gh CLI: not found (Web UI から Secret 登録で可)"
fi
