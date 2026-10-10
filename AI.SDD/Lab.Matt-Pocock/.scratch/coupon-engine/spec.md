# Spec: 購物車折價券折扣計算模組 (Coupon Discount Engine)

## Problem Statement
目前的購物車結帳流程缺乏在結帳階段依條件計算複合折扣券並保護實付金額邊界的能力。

## Solution
提供一個純運算深模組函式 `calculateDiscount(cart, coupons)`，負責驗證門檻、計算折扣順序，並確保最終金額不為負數。

## User Stories
1. As a shopper, I want to apply a fixed amount coupon (e.g. $100 off when reaching $800), so that I get the discount if my cart meets the threshold.
2. As a shopper, I want to apply a 10% off coupon to my $1,000 cart, so that I only pay $900.
3. As a shopper, I want to apply both a 10% coupon and a $100 off coupon, so that the 10% is calculated first ($900), followed by the $100 reduction ($800).
4. As a shopper with an $80 cart applying a $100 coupon, I want the payable amount to be $0 instead of negative.
5. As a shopper, when my cart total does not reach the coupon threshold, I want the coupon not to be applied.

## Implementation Decisions
- 核心運算為無狀態純函式（Pure Function）。
- 輸入格式：`CartSnapshot` 物件與 `Coupon[]` 陣列。
- 輸出格式：包含折扣明細（`breakdowns`）、總折扣額（`totalDiscount`）與實付金額（`finalPayableAmount`）的運算結果物件。
- 套用順序遵循 ADR 0001：先計算百分比折扣券（PERCENTAGE），再扣減固定金額券（FIXED）。
- 不在此模組直接存取資料庫或外部 API，外部狀態於上游組裝。

## Testing Decisions (Seams)
- 測試接縫（Seam）：以公開介面 `calculateDiscount` 作為唯一觀測邊界。
- 嚴禁針對內部輔助函式單獨撰寫單元測試，避免重構時破壞測試。

## Out of Scope
- 折價券序號（Coupon Code）的領取限制與領取次數檢查（由領券服務負責）。
- 金流付款閘道串接。
