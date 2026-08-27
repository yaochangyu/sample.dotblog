#!/usr/bin/env bash
set -euo pipefail

# 1. 確認 Docker 已安裝並在跑
if ! command -v docker &>/dev/null; then
  echo "找不到 docker，請先安裝 Docker 並確認可執行 'docker ps'" >&2
  exit 1
fi
docker ps &>/dev/null || { echo "docker daemon 沒在跑，請先啟動 Docker" >&2; exit 1; }

# 2. 安裝 kubectl（若未安裝）— 裝到 ~/.local/bin，不需要 sudo
mkdir -p "$HOME/.local/bin"
export PATH="$HOME/.local/bin:$PATH"

if ! command -v kubectl &>/dev/null; then
  echo "安裝 kubectl..."
  curl -LO "https://dl.k8s.io/release/$(curl -L -s https://dl.k8s.io/release/stable.txt)/bin/linux/amd64/kubectl"
  chmod +x kubectl
  mv kubectl "$HOME/.local/bin/kubectl"
fi

# 3. 安裝 kind（若未安裝）
if ! command -v kind &>/dev/null; then
  echo "安裝 kind..."
  curl -Lo ./kind "https://kind.sigs.k8s.io/dl/v0.24.0/kind-linux-amd64"
  chmod +x ./kind
  mv ./kind "$HOME/.local/bin/kind"
fi

# 4. 建立 cluster（1 control-plane + 2 worker，設定檔見 kind-config.yaml）
kind create cluster --config "$(dirname "$0")/kind-config.yaml"

# 5. 驗證
kubectl cluster-info --context kind-lab-cluster
kubectl get nodes -o wide
