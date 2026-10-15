# language: zh-TW
@real-storage @redis-replay
功能: 業務 API Redis 防重放與防重放紀元

  為了在多個業務 API 執行個體部署時確保簽章防重放的一致性，並在 Redis 重啟或被清空時避免舊請求被重放
  作為業務 API 服務端
  我需要透過 Redis 原子的檢查與登錄 nonce，並在防重放紀錄遺失時以防重放紀元拒絕舊簽章

  背景:
    假設 業務 API 執行環境已啟動並連線到真實 Redis
    並且 第二個業務 API 執行個體已啟動並連線到相同 Redis

  場景: 兩個執行個體併發提交同一份簽章請求只有一個通過防重放
    當 簽章呼叫端以 "orders-client" 併發送出同一份已簽章的建立訂單請求 10 次，交替送往兩個執行個體
    那麼 通過的請求數為 1
    並且 被拒絕的請求回應 401
    並且 兩個執行個體建立訂單總數為 1

  場景: Redis 被清空或重啟後早於紀元的簽章被拒絕且無業務副作用
    假設 簽章呼叫端以 "orders-client" 送出已簽章的建立訂單請求，Idempotency-Key 為 "order-key-before-flush"
    並且 第一次送出的回應為 201
    當 清空 Redis
    並且 同一份已簽章請求於第二個執行個體重送
    那麼 最近一次請求回應 401
    並且 最近一次請求被記錄為早於防重放紀元拒絕
    並且 兩個執行個體建立訂單總數為 1

  場景: 被紀元拒絕的呼叫端以新時間與新 nonce 重簽後可以通過
    假設 簽章呼叫端以 "orders-client" 送出已簽章的建立訂單請求，Idempotency-Key 為 "order-key-epoch-retry"
    並且 第一次送出的回應為 201
    當 清空 Redis
    並且 同一份已簽章請求於第二個執行個體重送
    那麼 最近一次請求回應 401
    當 簽章呼叫端 "orders-client" 以新時間與新 nonce 重新簽署並重試建立訂單
    那麼 最近一次請求回應 201
    並且 兩個執行個體建立訂單總數為 1

  場景: Redis 不可用時請求被拒絕並回報 503 且無業務副作用
    當 停止 "redis"
    並且 簽章呼叫端以 "orders-client" 送出已簽章的建立訂單請求，Idempotency-Key 為 "order-key-redis-down"
    那麼 最近一次請求回應 503
    並且 最近一次請求的錯誤代碼為 "replay_state_unavailable"
    並且 業務 API 建立訂單數為 0
