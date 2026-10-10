# Ticket 03: 複合折扣順序與明細彙整

- 狀態: Done
- Blocked by: Ticket 02

## What to build
支援多張優惠券同時套用，並依 ADR 0001 規定：
- 先計算百分比折扣券（PERCENTAGE），再計算固定金額折價券（FIXED）。
- 回傳結構中包含明細列表（`breakdowns`），記錄每張優惠券的 `couponId`、`applied` 是否套用、以及實際折抵金額 `discountAmount`。
- 複合計算後實施 0 元下限防護。

## Acceptance Criteria
- [x] 同時傳入百分比券與固定現折券時，先依小計計算百分比，再扣固定金額。
- [x] 回傳結果包含 `breakdowns` 明細。
- [x] 多券累計折抵若超過購物車小計，實付金額仍為 0 元，總折抵額不超過購物車小計。
