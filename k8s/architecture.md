# Kubernetes 架構筆記

K8s 是一個「聲明式」的容器編排系統，分成 **Control Plane（控制平面）** 和 **Worker Node（工作節點）** 兩大塊。

## Control Plane（管理大腦，通常跑在 master node）

| 元件 | 職責 |
|---|---|
| **kube-apiserver** | 唯一的入口，所有元件（包括 kubectl）都透過它讀寫叢集狀態；做認證/授權 |
| **etcd** | 分散式 key-value store，存放整個叢集的「唯一真相來源」（所有物件的 desired state + 目前 state） |
| **kube-scheduler** | 決定新建立的 Pod 該被排到哪個 Node（依資源、affinity、taint/toleration 等） |
| **kube-controller-manager** | 跑各種 controller loop（Node controller、ReplicaSet controller、Job controller…），持續把「現況」拉向「期望狀態」 |
| **cloud-controller-manager**（選用）| 跟雲端 provider 互動（建 LB、掛 disk 等），雲上環境才需要 |

## Worker Node（實際跑容器的機器）

| 元件 | 職責 |
|---|---|
| **kubelet** | 每台 node 上的 agent，負責跟 apiserver 溝通、確保該 node 上的 Pod 按規格運行 |
| **kube-proxy** | 維護 Service 的網路規則（iptables/IPVS），讓 Service ClusterIP 能路由到正確的 Pod |
| **container runtime** | 實際跑容器的引擎，現在多是 containerd 或 CRI-O（Docker 已被移除為 runtime，但可透過 cri-dockerd） |

## 核心概念一次看

- **Pod**：最小部署單位，一個或多個共享 network/storage 的容器
- **Deployment / ReplicaSet**：管理 Pod 的數量與滾動更新
- **Service**：給一組 Pod 一個穩定的網路入口（ClusterIP / NodePort / LoadBalancer）
- **Namespace**：邏輯隔離
- **ConfigMap / Secret**：配置與敏感資料注入
- **Ingress**：七層路由，把外部流量導進叢集內的 Service
- **Volume / PV / PVC**：儲存抽象層

## 運作流程（一個請求怎麼跑過整個架構）

`kubectl apply` → apiserver 認證/校驗 → 寫入 etcd → scheduler 發現未排程 Pod → 分配 Node → 該 Node 的 kubelet 察覺並呼叫 container runtime 建容器 → kube-proxy 更新網路規則讓 Service 能導流進去。

## 下一步：安裝方式怎麼選

### minikube/kind vs kubeadm 差異

| 面向 | minikube / kind | kubeadm |
|---|---|---|
| **架構真實度** | 通常單節點模擬整個叢集（kind 可模擬多節點，但都是本機 Docker 容器） | 真正的多台實體機/VM，control plane 和 worker 分開 |
| **底層原理** | minikube 用 VM 或 Docker driver 跑完整 K8s；kind 是「Kubernetes in Docker」，每個 node 是一個 Docker container | 在每台機器上手動 `kubeadm init`（master）/ `join`（worker），走完整憑證簽發、etcd 部署、CNI 安裝流程 |
| **學習目的** | 快速上手 kubectl、部署應用 | 學叢集怎麼被組出來的（etcd HA、control plane 互信、CNI、跨機網路） |
| **安裝難度** | 低 | 高，要自己處理網路規劃、containerd、CNI plugin、join token |
| **適合情境** | 目標是「學怎麼用 K8s 部署東西」 | 目標是「學 K8s 底層怎麼運作、以後管理正式叢集」 |

## 學習路線決定：用 Docker 練 Cluster

決定用 **kind（Kubernetes in Docker）** 來練多節點 cluster，理由：
- kind 內部其實是用 **kubeadm** 在每個 Docker container 裡把它當一台獨立節點初始化，所以看到的是真實的 control plane + worker 架構，不是假的
- 純手動「kubeadm + Docker 容器扮演 VM」需要自己解決 systemd-in-docker、kernel module、cgroup 相容性問題，不建議走這條

### 3 節點配置怎麼選

| 配置 | 說明 | 適合學什麼 |
|---|---|---|
| **1 control-plane + 2 worker** | 最標準入門配置 | Pod 怎麼被排程到不同節點、Service 跨節點路由、node affinity/taint 等「多節點才有意義」的概念 |
| **3 control-plane（HA）+ 0/N worker** | etcd 變成 3 節點 quorum | control plane 故障轉移、etcd leader election、apiserver 前面要不要放 LB（進階議題） |

**決定**：先用 **1 control-plane + 2 worker**。理由是目前還在架構認識階段，HA 的複雜度（etcd quorum、apiserver LB）會在還沒搞懂基本排程/網路前把人搞混。等 3 節點基礎版玩熟了 Deployment 分散、Service 路由、kubectl drain，之後只要改 kind-config 就能升級成 HA 版練習。

已完成：`kind-config.yaml`（1 control-plane + 2 worker）與 `install.sh` 安裝腳本。

用法：
```bash
./k8s/install.sh
```
腳本會依序檢查 Docker、安裝 kubectl/kind、用 `kind-config.yaml` 建立 3 節點 cluster，最後跑 `kubectl get nodes` 驗證。

## Docker container 不等於實體機 / VM

| | 實體機 / VM | Docker container |
|---|---|---|
| **kernel** | 有自己獨立的 kernel | 共用 host 的 kernel，沒有自己的 |
| **systemd / init** | 有，kubelet 可以用 systemd 管理 | 預設沒有，一般 container 是單一 process，沒有完整的 init 系統 |
| **網路堆疊** | 獨立的網路介面卡、路由表 | 預設是 network namespace，跟 host 共用 kernel 網路模組 |
| **能不能跑 kubeadm** | 可以，這是它被設計來做的環境 | 不行（用一般 container 直接跑），除非用特製過的映像檔硬做出 systemd-in-container |

kind 能用 Docker container 模擬出「看起來像節點」的東西，是因為用了特製的 node image（裡面塞了偽 systemd、containerd、預先調好的 kernel module 掛載），本質上是繞過上述限制的工程解法，而且只能在同一台 Docker host 上運作，跨不了實體機。

## 失敗紀錄：手動用 Docker container 模擬新機器加入 kind cluster

**嘗試做的事**：不透過 `kind create cluster` 重建，而是手動 `docker run` 一個跟現有 worker 相同規格（`kindest/node` image、privileged、掛 `/lib/modules`、接到 `kind` network）的新 container，再從 control-plane 取得 `kubeadm join` 指令，讓這個新 container 用真正的 kubeadm join 流程動態加入現有 cluster——目的是體驗最貼近「真實新機器加入」的操作。

**結果：失敗，而且弄壞了整個 cluster 的網路**

1. 新 container 開起來後，systemd/containerd/kubeadm/kubelet 都正常
2. 執行 `kubeadm join` 卡在 `[discovery] Waiting for the cluster-info ConfigMap` 逾時
3. 診斷發現：不只新 container 連不到 apiserver（`172.19.0.4:6443`），連**原本已經在跑、正常運作中的 worker** 對 apiserver 的新連線也全部逾時（舊的、已建立的連線因為 conntrack 還撐著沒斷，所以 node 沒有立刻變 NotReady，但新連線一律不通）
4. 刪掉新 container 後問題沒有恢復
5. 用 `kind delete cluster` + 重新 `kind create cluster` 想靠重建 Docker network 修復，結果新 cluster 一樣卡在同樣的 `kubeadm join` 逾時——證實問題不是 `kind` network 本身壞掉，而是**宿主機層級**的東西被動到了
6. 沒有 sudo 權限，無法檢查/清除 iptables 規則做進一步診斷，只能靠 `sudo systemctl restart docker`（會重啟所有 container，包含這台機器上其他不相干的服務）才可能修復，但這需要使用者授權才能執行

**根因推測**：這台環境是 WSL2，container 之間共用同一個 host kernel。新 container 的 entrypoint 腳本在啟動時會動 iptables/cgroup/sysctl 等設定，這些操作在一般 Linux 上是被 network namespace 隔離的，但在 WSL2 這種簡化過的核心環境下，某些規則可能不是完全隔離，導致新 container 啟動的副作用波及到同一個 Docker network 上其他 container 的連線能力。

**結論／教訓**：
- **不要用「手動 docker run 複製 kind node 規格 + kubeadm join」的方式模擬新機器加入 cluster**——這是非官方支援的操作，在 WSL2 這類共用 kernel 的環境下風險很高，一出問題就是整個 cluster 網路壞掉，而且無法在沒有 sudo 的情況下自行修復
- 要真正練習「新機器加入 cluster」，建議用**真正獨立的 VM 或實體機**跑 kubeadm join，這樣才有真正的 kernel/network namespace 隔離，不會波及既有節點
- kind 的定位就是「單機模擬」，不要硬拗成「動態加節點」的教材，這不是它被設計來做的事

**目前 cluster 狀態**：因為上述問題，`lab-cluster` 目前無法正常運作，需要先執行 `sudo systemctl restart docker`（需人工授權/操作）才能重新建立乾淨的 cluster。
