#!/usr/bin/env bash
# GitHub Actions 用 UNITY_LICENSE の取得手順を表示する。
# 実行: ./scripts/ci/setup-github-ci.sh
set -euo pipefail

UNITY_VERSION="${UNITY_VERSION:-6000.2.7f2}"
IMAGE="unityci/editor:${UNITY_VERSION}-linux-1"

cat <<EOF
=== GitHub CI セットアップ ===

1) GitHub リポジトリを作成し push する
   git remote add origin https://github.com/<user>/<repo>.git
   git push -u origin main

2) GitHub Secrets に Unity 認証情報を登録する（Personal は3つすべて必須）

   | Secret 名 | 内容 |
   |-----------|------|
   | UNITY_LICENSE | .ulf の base64 文字列 |
   | UNITY_EMAIL | Unity Hub ログイン用メール |
   | UNITY_PASSWORD | Unity Hub ログイン用パスワード（特殊文字は避けると安定） |

   UNITY_LICENSE の取得:
   - 方法 A: base64 -i "/Library/Application Support/Unity/Unity_lic.ulf" | pbcopy
   - 方法 B: https://license.unity3d.com/manual で CI 用 .ulf を取得して base64 化

   方法 B (game-ci Docker で取得 — 要 Unity アカウント):
   docker run --rm -it \\
     -e UNITY_EMAIL \\
     -e UNITY_PASSWORD \\
     -e UNITY_SERIAL \\
     ${IMAGE} \\
     unity-editor -batchmode -nographics -quit \\
       -username "\$UNITY_EMAIL" \\
       -password "\$UNITY_PASSWORD" \\
       -serial "\$UNITY_SERIAL"

   生成されたライセンスを base64 化して UNITY_LICENSE に登録。
   詳細: https://game.ci/docs/github/activation

3) Actions を手動実行 (workflow_dispatch) または push で確認
   - editmode-geometry: 13 tests
   - combined-batch-verify: SELECTION_TOTAL 11/11

4) 失敗時は Artifacts から Logs/ をダウンロード

EOF

if command -v docker >/dev/null 2>&1; then
  echo "Docker: available"
else
  echo "Docker: not found (GitHub Actions 上では game-ci が Docker を使用)"
fi

if command -v gh >/dev/null 2>&1; then
  echo "gh CLI: available (gh secret set UNITY_LICENSE で登録可能)"
else
  echo "gh CLI: not found (brew install gh 推奨)"
fi
