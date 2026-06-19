#!/usr/bin/env bash
# GitHub Actions 用 Unity ライセンスのセットアップ手順を表示する。
# 実行: ./scripts/ci/setup-github-ci.sh
set -euo pipefail

UNITY_VERSION="${UNITY_VERSION:-6000.2.7f2}"
IMAGE="unityci/editor:ubuntu-${UNITY_VERSION}-base-3"

cat <<EOF
=== GitHub CI セットアップ ===

1) リポジトリを push する
   git remote add origin https://github.com/<user>/<repo>.git
   git push -u origin main

2) GitHub Secrets に Unity 認証情報を登録する（Personal・Unity 6）

   | Secret 名 | 必須 | 内容 |
   |-----------|------|------|
   | UNITY_EMAIL | ✅ | Unity ID のメールアドレス |
   | UNITY_PASSWORD | ✅ | Unity ID のパスワード（特殊文字は避けると安定） |
   | UNITY_LICENSE | 不要 | 本 CI はメール/パスワード活性化を使用 |

   ⚠️ 作らないこと: UNITY_SERIAL（あると serial invalid になる）

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
   - editmode-geometry: 13 tests
   - combined-batch-verify: SELECTION_TOTAL 11/11

5) 失敗時は Artifacts から Logs/ をダウンロード

   よくある失敗:
   - serial invalid (20110) → game-ci を使っている / UNITY_SERIAL が混在（本 CI では game-ci 不使用）
   - activation failed → UNITY_EMAIL / UNITY_PASSWORD の誤り、またはパスワードの特殊文字

Docker image (batch): ${IMAGE}

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
