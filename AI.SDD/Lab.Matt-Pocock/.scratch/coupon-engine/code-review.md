# 雙軸代碼審查報告 (Two-Axis Code Review)

- 目標分支/變更：`feat/coupon-discount-engine` (Seam: `calculateDiscount`)
- 審查時間：2026-10-10
- 規格參照：`.scratch/coupon-engine/spec.md` 及 ADR `docs/adr/0001-coupon-calculation-order.md`

---

## 1. Standards 軸（編碼規範與壞味道審查）

依據 Martin Fowler 12 種經典壞味道及 TypeScript 規範檢視：

- **Primitive Obsession (基本型別偏執)**:
  - 觀察：金額目前以原生 `number` 型別表示。在電商複雜金流環境下，建議未來可考慮引入專屬貨幣值物件（Value Object），以避免浮點運算邊界問題。
  - 現況評估：當前計算已包含 `Math.round` 處理，目前純運算範圍內可接受。
- **Feature Envy (依戀情結)**:
  - [Pass] 無。邏輯高度內聚於 `calculateDiscount`，未過度存取外部物件私有狀態。
- **Deep Module 檢驗**:
  - [Pass] 公開介面僅有一個函式 `calculateDiscount(cart, coupons)`，封裝了排序、門檻判斷、順序折抵、溢出防護等複雜度。
  - [Pass] 測試全數透過公開接縫（Seam）進行，無針對私有輔助邏輯撰寫脆化測試。
- **Mysterious Name / 註解整潔度**:
  - [Pass] 型別定義明確（`CartSnapshot`, `Coupon`, `CouponBreakdown`, `CalculationResult`），命名貼合 `GLOSSARY.md` 領域名詞。

---

## 2. Spec 軸（規格符合度與範疇審查）

比對 `.scratch/coupon-engine/spec.md` 驗收條件與工單內容：

- **User Story 1 & Ticket 01 (滿額現折)**:
  - [Pass] 滿千折百、未達門檻不折抵測試通過。
- **User Story 2 & Ticket 02 (百分比折扣)**:
  - [Pass] 百分比折扣計算與四捨五入邏輯測試通過。
- **User Story 3 & Ticket 03 (複合折抵順序與明細)**:
  - [Pass] 依 ADR 0001 先折百分比再扣固定金額，回傳完整 `breakdowns` 明細。
- **User Story 4 (0元下限防護)**:
  - [Pass] 超額折抵時實付金額下限為 0，且總折扣額不超過原始小計。
- **Scope Creep (範疇蔓延檢查)**:
  - [Pass] 無任何未經授權的外部 I/O、資料庫操作或額外依賴引入，完全維持純函式實作。

---

## 審查結論
- **Standards 軸**：通過（1 項建議：未來可將貨幣提煉為 Value Object）。
- **Spec 軸**：通過（100% 符合 Spec 與工單驗收條件）。
- **核准狀態**：Approved (可合併/交付)。
