# Ticket 02: 百分比折扣券支援

- 狀態: Done
- Blocked by: Ticket 01

## What to build
在公開接縫 `calculateDiscount(cart, coupons)` 中擴充支援 `PERCENTAGE` 類型優惠券：
- 當 `cart.subtotal >= coupon.threshold` 時，計算 `discount = Math.round(cart.subtotal * (coupon.rate / 100))`。
- 折抵後正確計算 `finalPayableAmount` 與 `totalDiscount`。
- 保留 0 元防護機制。

## Acceptance Criteria
- [x] 傳入單一百分比折價券且達門檻時，依比例計算折扣金額（例如 1000 元 10% 折扣為 100 元，實付 900 元）。
- [x] 未達門檻時不予折抵。
- [x] 百分比折抵金額進行四捨五入整數處理。
