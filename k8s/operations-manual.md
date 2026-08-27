# K8s Cluster 操作手冊

適用環境：`k8s/kind-config.yaml` 建立的 kind cluster（1 control-plane + 2 worker，context: `kind-lab-cluster`）。

所有指令請先確保 PATH 有 kind/kubectl：

```bash
export PATH="$HOME/.local/bin:$PATH"
```

## 1. 建立 Cluster

```bash
./k8s/install.sh
```

腳本會依序：檢查 Docker → 安裝 kubectl/kind（裝到 `~/.local/bin`，不需要 sudo）→ 用 `kind-config.yaml` 建立 3 節點 cluster → 驗證。

確認節點都 Ready：

```bash
kubectl wait --for=condition=Ready nodes --all --timeout=90s
kubectl get nodes
```

預期結果：

```
NAME                        STATUS   ROLES           VERSION
lab-cluster-control-plane   Ready    control-plane   v1.31.0
lab-cluster-worker          Ready    <none>          v1.31.0
lab-cluster-worker2         Ready    <none>          v1.31.0
```

## 2. 部署測試用 Deployment + Service

manifest：`k8s/test-app.yaml`（nginx，3 replica + ClusterIP Service）。

```bash
kubectl apply -f k8s/test-app.yaml
kubectl wait --for=condition=Ready pod -l app=nginx-test --timeout=90s
kubectl get pods -l app=nginx-test -o wide
```

觀察重點：3 個 Pod 會被 scheduler 分散到兩個 worker 節點上，**不會**排到 control-plane（預設有 `NoSchedule` taint）。

驗證 Service 是否能跨節點把流量導到不同 Pod：

```bash
kubectl run curl-test --image=curlimages/curl --rm -i --restart=Never -- sh -c '
for i in 1 2 3 4; do
  curl -s -o /dev/null -w "request $i -> HTTP %{http_code}\n" http://nginx-test
done'
```

預期每次都回 `HTTP 200`，證明 kube-proxy 的轉發規則正常運作。

## 3. Scale 測試

### 放大（觀察 scheduler 怎麼分配新 Pod）

```bash
kubectl scale deployment nginx-test --replicas=6
kubectl wait --for=condition=Ready pod -l app=nginx-test --timeout=60s

# 各節點 Pod 數量統計
kubectl get pods -l app=nginx-test -o jsonpath='{range .items[*]}{.spec.nodeName}{"\n"}{end}' | sort | uniq -c
```

觀察結果：scheduler 會盡量讓兩個 worker 負載平均（本例為 3:3）。

### 縮小（觀察 controller 怎麼決定砍哪些 Pod）

```bash
kubectl scale deployment nginx-test --replicas=2
kubectl get pods -l app=nginx-test -o wide
```

觀察結果：ReplicaSet controller 縮容時**優先刪除較晚建立的 Pod**，保留較早、較穩定的那批。

## 4. Cordon + Drain（節點下線流程）

模擬「要對某台 worker 做維護/升級」的標準流程：先擋新流量、再清空現有負載，才能安全動它。

先確認兩個 worker 上都有 Pod（若不夠可先 `kubectl scale deployment nginx-test --replicas=4`）：

```bash
kubectl get pods -l app=nginx-test -o wide
```

### Cordon：禁止該節點被排新 Pod（現有 Pod 不受影響）

```bash
kubectl cordon lab-cluster-worker
kubectl get nodes
```

預期節點狀態變成 `Ready,SchedulingDisabled`。

### Drain：強制驅逐該節點上現有的 Pod

```bash
kubectl drain lab-cluster-worker --ignore-daemonsets --delete-emptydir-data --force
```

- `--ignore-daemonsets`：跳過 DaemonSet 管理的 Pod（如 `kindnet`、`kube-proxy`），這些本來就該每個節點各跑一份，不該被驅逐
- `--delete-emptydir-data` / `--force`：允許刪除使用 emptyDir 的 Pod（測試用途才這樣做，正式環境要注意資料是否會遺失）

驗證 Pod 是否被重新排到其他節點：

```bash
kubectl wait --for=condition=Ready pod -l app=nginx-test --timeout=60s
kubectl get pods -l app=nginx-test -o wide
kubectl get nodes
```

觀察結果：原本在該節點上的 Pod 全部被驅逐，controller 立刻在唯一可排程的節點上補回 desired replica 數，被 drain 的節點本身維持 `Ready` 但不再承載 Pod。

### Uncordon：維護完成，恢復該節點可排程

```bash
kubectl uncordon lab-cluster-worker
kubectl get nodes
```

節點恢復 `Ready`，但已經在其他節點跑穩的 Pod **不會自動搬回來**，除非重建（刪除 Pod、rolling restart 等）。

## 5. Taint / Toleration（節點排斥 Pod）

Taint 是「節點主動拒絕 Pod」——沒有對應 toleration 的 Pod 排不上去（或會被趕走）。

### NoSchedule：擋新 Pod，不影響現有的

```bash
kubectl taint nodes lab-cluster-worker2 dedicated=special:NoSchedule
kubectl describe node lab-cluster-worker2 | grep -A1 Taints

# 現有 Pod 不受影響
kubectl get pods -l app=nginx-test -o wide

# 刪掉 Pod 讓 controller 重建，觀察新 Pod 只會排到沒被 taint 的節點
kubectl delete pod -l app=nginx-test
kubectl wait --for=condition=Ready pod -l app=nginx-test --timeout=60s
kubectl get pods -l app=nginx-test -o wide
```

### NoExecute：連現有的 Pod 都主動驅逐

```bash
kubectl taint nodes lab-cluster-worker dedicated=special:NoExecute
kubectl get pods -l app=nginx-test -o wide
```

若此時所有節點都被 taint 擋住（例如兩個 worker 都打了 `dedicated=special`），Pod 會卡在 `Pending`，可用以下指令看排程失敗原因：

```bash
kubectl describe pod -l app=nginx-test | grep -A3 Events
```

會看到類似 `0/3 nodes are available: 1 node(s) had untolerated taint {node-role.kubernetes.io/control-plane}, 2 node(s) had untolerated taint {dedicated: special}` 的訊息。

### 清除 taint

```bash
kubectl taint nodes lab-cluster-worker dedicated=special:NoExecute-
kubectl taint nodes lab-cluster-worker2 dedicated=special:NoSchedule-
```

taint key 後面加 `-` 代表移除該 taint。

觀察：確認 taint 真的清空、卡住的 Pod 恢復排程

```bash
kubectl describe node lab-cluster-worker lab-cluster-worker2 | grep Taints
kubectl wait --for=condition=Ready pod -l app=nginx-test --timeout=60s
kubectl get pods -l app=nginx-test -o wide
```

預期兩個節點的 `Taints` 都變回 `<none>`，Pending 的 Pod 全部變成 `Running`。

## 6. Node Affinity（Pod 主動指定節點）

跟 taint 方向相反：Pod 主動要求「我只想排到有特定標籤的節點」。

manifest：`k8s/test-app-affinity.yaml`（要求節點必須有 `disktype=ssd` 標籤）。

```bash
# 先幫其中一個節點貼標籤
kubectl label node lab-cluster-worker2 disktype=ssd
kubectl get nodes --show-labels | grep worker2

# 套用有 nodeAffinity 限制的 Deployment
kubectl apply -f k8s/test-app-affinity.yaml
kubectl wait --for=condition=Ready pod -l app=nginx-ssd --timeout=60s
kubectl get pods -l app=nginx-ssd -o wide
```

觀察結果：所有 Pod 都只會排到有 `disktype=ssd` 標籤的節點，其他節點即使有空資源也不會被排入。

清理：

```bash
kubectl delete -f k8s/test-app-affinity.yaml
kubectl label node lab-cluster-worker2 disktype-
```

觀察：確認 Pod 刪除、標籤移除

```bash
kubectl get pods -l app=nginx-ssd
kubectl get nodes --show-labels | grep worker2
```

預期第一條回傳 `No resources found`，第二條 `disktype=ssd` 不再出現在標籤清單。

### Taint vs Affinity 對照

| | Taint | Affinity |
|---|---|---|
| 誰主動 | 節點排斥 Pod | Pod 主動要求節點 |
| 效果類型 | `NoSchedule`（擋新的）/ `NoExecute`（連現有的都趕走）/ `PreferNoSchedule`（軟性）| `required...`（硬性）/ `preferred...`（軟性偏好）|
| 要配對什麼 | Pod 要有對應 `tolerations` 才能無視 taint | 節點要有對應 `label`，Pod 才排得上去 |
| 典型用途 | 專屬節點（GPU 機、正式環境節點）擋掉不該去的 Pod | 把 Pod 導向有特定資源/特性的節點（SSD、特定 zone）|

## 7. 觀察 Scheduler 怎麼做決策

| 方法 | 指令 | 看得到什麼 |
|---|---|---|
| Pod 排程事件 | `kubectl describe pod <pod>` 看 `Events` 區塊 | 排到哪個節點、或排程失敗的具體原因（哪些節點被什麼條件擋掉） |
| 叢集層級事件 | `kubectl get events --sort-by=.lastTimestamp` | 全叢集所有排程/驅逐事件的時間序列，適合抓某個時間點發生了什麼 |
| 只看排程相關事件 | `kubectl get events --field-selector reason=Scheduled` | 過濾出「成功排程」事件，快速看每個 Pod 被排到哪 |
| scheduler 本體的 log | `kubectl logs -n kube-system -l component=kube-scheduler` | scheduler 自己的運作日誌（過濾、打分、選節點的內部過程），預設 log level 較簡略 |
| 即時監看 Pod → Node 對應 | `kubectl get pods -o wide --watch` | 排程當下即時看 Pod 從 `Pending` 變成有 `NODE` 欄位的過程 |

實務上最常用的組合是：`kubectl get pods -o wide` 看結果 + `kubectl describe pod` 看單一 Pod 為什麼被排到那裡（或為什麼排不上去）。scheduler 的完整決策邏輯（打分細節）在 kind 這種預設 log level 下不會太詳細，若要深入看 filter/score 的每一步，需要調高 scheduler 的 `--v` verbosity（進階選項，一般練習不需要）。

## 8. 清理

```bash
kubectl delete -f k8s/test-app.yaml
```

觀察：確認測試資源已清空

```bash
kubectl get pods -l app=nginx-test
kubectl get svc nginx-test
```

預期兩條都回傳 `No resources found`。

刪除整個 cluster：

```bash
kind delete cluster --name lab-cluster
```

觀察：確認 cluster 真的被刪除

```bash
kind get clusters
kubectl config get-contexts | grep kind-lab-cluster
```

預期 `kind get clusters` 不再列出 `lab-cluster`，且 `kind-lab-cluster` context 也一併被移除。
