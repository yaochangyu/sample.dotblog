# Ticket 01: 基礎滿額現折計算（貫穿切片）

- 狀態: Done
- Blocked by: None

## What to build
實作貫穿切片：透過公開接縫 `calculateDiscount(cart, coupons)`，傳入 `CartSnapshot` 與 `FIXED` 類型的 `FixedAmountCoupon`：
- 若 `cart.subtotal >= coupon.threshold`，扣除 `coupon.amount`。
- 若 `cart.subtotal < coupon.threshold`，不扣除任何金額。
- 支援 0 元下限防護：當折扣大於購物車總計時，`finalPayableAmount` 最低為 0，`totalDiscount` 不超過 `cart.subtotal`。

## Acceptance Criteria
- [x] 購物車金額達到門檻時，正確折抵固定金額。
- [x] 購物車金額未達門檻時，不折抵任何金額。
- [x] 折扣大於購物車金額時，實付金額下限為 0。
