#!/usr/bin/env bash
# CI 用 Unity ライセンス (.alf) を unityci Docker 上で生成する。
# 生成した .alf を https://license.unity3d.com/manual にアップロードし、
# ダウンロードした .ulf の XML 全文を GitHub Secret UNITY_LICENSE に登録する。
#
# 実行: ./scripts/ci/generate-ci-unity-license.sh
set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
UNITY_VERSION="${UNITY_VERSION:-6000.2.7f2}"
IMAGE="${GOAP_UNITY_DOCKER_IMAGE:-unityci/editor:ubuntu-${UNITY_VERSION}-base-3}"
LOG_DIR="${PROJECT_ROOT}/Logs"
mkdir -p "${LOG_DIR}"

if ! command -v docker >/dev/null 2>&1; then
  echo "docker が見つかりません。Docker Desktop を起動してから再実行してください。" >&2
  exit 1
fi

if ! docker info >/dev/null 2>&1; then
  echo "Docker デーモンが起動していません。Docker Desktop を起動してから再実行してください。" >&2
  exit 1
fi

echo "[ci-license] image=${IMAGE}"
echo "[ci-license] unityci Docker 上で .alf を生成します（Mac ローカル用ではありません）..."

rm -f "${PROJECT_ROOT}"/Unity_v*.alf "${PROJECT_ROOT}"/*.alf

docker run --rm \
  -v "${PROJECT_ROOT}:/project" \
  -w /project \
  "${IMAGE}" \
  unity-editor \
    -batchmode \
    -nographics \
    -quit \
    -createManualActivationFile \
    -logFile /project/Logs/ci-alf-gen.log

alf_file="$(find "${PROJECT_ROOT}" -maxdepth 1 -name '*.alf' -print -quit || true)"
if [[ -z "${alf_file}" ]]; then
  echo "[ci-license] .alf が見つかりません。Logs/ci-alf-gen.log を確認してください。" >&2
  exit 1
fi

echo ""
echo "=== 生成完了 ==="
echo "  .alf : ${alf_file}"
echo "  log  : ${LOG_DIR}/ci-alf-gen.log"
echo ""
cat <<'EOF'
次の手順:

1) https://license.unity3d.com/manual を開く
2) 上記 .alf をアップロード
3) ライセンス種別で Personal を選択
   ※ Personal が表示されない場合:
      ページを右クリック → 検証(Inspect) → Personal 要素の style から display:none を削除
      (game.ci 公式の回避策: https://game.ci/docs/troubleshooting/common-issues/)
4) ダウンロードした .ulf をテキストエディタで開き、XML 全文をコピー
5) GitHub → Settings → Secrets → Actions → UNITY_LICENSE を更新
   ※ base64 ではなく XML 全文を貼り付ける（<?xml で始まる）
6) UNITY_EMAIL / UNITY_PASSWORD も登録済みであることを確認
7) Actions を Re-run

注意:
- Mac の /Library/Application Support/Unity/Unity_lic.ulf は CI では使えません
- UNITY_SERIAL Secret は作らないでください（Personal では不要。あると誤動作します）
EOF
