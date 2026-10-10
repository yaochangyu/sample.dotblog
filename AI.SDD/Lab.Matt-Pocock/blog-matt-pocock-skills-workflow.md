---
title: '[AI.SDD] 拆解 Matt Pocock 的 Engineering Skills：從環境配置到高階 TDD 與架構演進的完整工作流程'
abstract: <p>前面幾篇我們已經演練過 OpenSpec 與 Spec-Kit，體驗過規格驅動開發的威力。這次來試試 Matt Pocock 開源的 <a target="_blank" rel="noopener noreferrer" href="https://github.com/mattpocock/skills">skills</a>，這是一套將需求盤問、領域建模、規格提煉、垂直切片、多代理編排、TDD 紅綠循環、雙軸程式碼審查與架構深化完整封裝的 AI 工程技能庫。這裡我以「購物車折價券計算模組」為例，一步步把整套工作流程與 Prompt 拆解出來，看看這套規範如何實際落地。</p>
keywords: SDD,Vibe Coding,Specification-Driven Development
categories: Vibe Coding
weblogName: 余小章 @ 大內殿堂
postId: a7d16385-3160-40bb-9c62-5724d620ac28
postDate: 2026-10-10T15:10:08.0000000
postStatus: 
dontInferFeaturedImage: false
stripH1Header: true
---
# [AI.SDD] 拆解 Matt Pocock 的 Engineering Skills：從環境配置到高階 TDD 與架構演進的完整工作流程

前面幾篇我們已經演練過 OpenSpec 與 Spec-Kit，體驗過規格驅動開發的威力。這次來試試 Matt Pocock 開源的 [skills](https://github.com/mattpocock/skills)，這是一套將軟體工程規範（需求盤問、領域建模、規格提煉、垂直切片、多代理編排、TDD 紅綠循環、雙軸程式碼審查與架構深化）完整封裝起來的 AI 技能庫。這裡我以電商系統的「購物車折價券計算模組」為例，一步步把整套工作流程與 Prompt 拆解出來，看看這套規範如何實際落地。

---

## 開發環境

- 作業系統：Ubuntu 24.04 LTS（WSL2）
- 執行環境：Node.js v22.14.0 / npm 10.9.2
- 測試框架：Vitest 3.0.0 / TypeScript 5.7.0
- AI 輔助工具：Claude Code / Antigravity CLI
- 依賴套件庫：[mattpocock/skills](https://github.com/mattpocock/skills)  
  （註：此為建議版本，非強制）

---

## 核心概念：軟體工程全生命週期閉環

在動手實作前，先來看一下這套技能庫的架構邏輯。以往叫 AI 開發的盲點在於：需求給得太模糊，AI 只能憑空瞎猜；動手時又一口氣寫一大包程式碼，等到單元測試紅一片時才發現方向偏掉了；更別說程式碼上線後，缺乏系統性手段檢查架構是否腐化。

這套技能庫構建了一套涵蓋「功能交付內軌」、「架構演進外軌」以及「異常診斷中繼軌」的完整閉環：

```
flowchart TD
    subgraph OuterLoop ["外軌：持續架構演進迴圈 (Architecture Evolution)"]
        R0["專案落地運作與迭代"] --> R1["/improve-codebase-architecture<br>掃描熱點與淺模組，產出 HTML 報告"]
        R1 --> R2["挑選候選方案進行 /grilling 盤問"]
    end

    subgraph DiagLoop ["中繼軌：異常排查紀律 (Diagnosing Bugs)"]
        D0["線上例外 / 測試紅燈 / 效能衰退"] --> D1["/diagnosing-bugs<br>拒絕肉眼盲猜，建立秒級紅燈反饋迴圈"]
        D1 --> D2["最小化重現 ＋ 提出可證偽假說"]
        D2 --> D3["單變數插樁 ＋ 接縫回歸測試修復"]
    end

    subgraph InnerLoop ["內軌：高階功能交付流水線 (Feature Delivery Loop)"]
        F0["階段 0：環境配置<br>/setup-matt-pocock-skills"] --> F1["階段 1：邊界對齊<br>/grill-with-docs"]
        F1 --> F2["階段 2：規格提煉<br>/to-spec 與 /to-tickets"]
        F2 --> F3["階段 3：多代理編排實作<br>/implement-spec 驅動 /tdd"]
        F3 --> F4["階段 4：雙軸程式碼審查<br>/code-review"]
    end

    R2 --> F2
    D3 --> F4
    F4 --> R0
    R0 -.-> D0

    G["底層設計哲學：codebase-design<br>Deep Modules, Seam 測試接縫, Leverage, Locality"] -.-> F2
    G -.-> F3
    G -.-> R1
    G -.-> D3
```

這裡可以拆解為四個維度：  
- **底層哲學（codebase-design）**：追求「深模組 (Deep Module)」——以極簡的公開介面封裝大量內部複雜度，並堅持公開接縫 (Seam) 就是唯一的測試表面。  
- **內軌（功能交付鏈）**：從環境配置、邊界盤問、規格制定、工單切片，到背景子代理在獨立 Git Worktree 裡並行跑 TDD，最後以雙軸審查驗收。  
- **外軌（架構演進鏈）**：系統上線一段時間後，利用 `improve-codebase-architecture` 掃描程式碼摩擦力與淺模組，產出視覺化 HTML 報告，驅動下一輪重構。  
- **中繼軌（diagnosing-bugs）**：線上噴錯、偶發失敗或效能衰退時，嚴格遵守六階段科學診斷紀律，在建立可一鍵重現的紅燈迴圈前，絕對不看 code 盲猜。

接下來我們以「購物車折價券計算模組」為例，把每一步驟的操作走過一遍。

---

## 實作流程與 Prompt 實戰拆解

### 1. 初始化環境規範（/setup-matt-pocock-skills）

在任何工程技能運作前，首先必須讓 AI 知道「工單要記在哪裡」以及「領域文件放在哪裡」。`/setup-matt-pocock-skills` 是一個互動式的引導技能，它會主動檢查專案結構，確認使用 GitHub Issues、GitLab 還是本地 Markdown 當作追蹤器，並在專案中建立 `docs/agents/` 目錄。

#### 提示詞範例（Prompt）

直接在對話框中輸入指令：

```
/setup-matt-pocock-skills
```

執行後，AI 會檢查當前環境並列出建議設定：

```
我已檢查專案環境：
- Git Remote: 本地專案（尚未綁定 GitHub Remote）
- 專案說明檔: CLAUDE.md 已存在
- 領域文件: 尚未建立 GLOSSARY.md

請確認以下配置：
Section A (Issue tracker): 建議採用「Local markdown」（工單將存放於 .scratch/<feature>/issues/）。
Section C (Domain docs): 建議採用「單一上下文（single-context）」（根目錄維護 GLOSSARY.md 與 docs/adr/）。

是否同意此配置並寫入 docs/agents/？
```

若沒有問題就可以往下一步。使用者確認後，AI 會在 `CLAUDE.md` 寫入 `## Agent skills` 區塊，並建立設定檔：  
- `docs/agents/issue-tracker.md`：定義工單的儲存路徑與格式  
- `docs/agents/domain.md`：定義領域名詞與架構決策紀錄 (ADR) 的維護規則

NOTE：這個步驟在每個專案只需執行一次，之後所有的工程技能都會自動依循這些設定檔運作。至於團隊若有整合專案看板（例如 GitHub Projects、GitLab Issue Board、Jira，或是本地以 Markdown 維護的進度看板），非常建議直接在 `docs/agents/issue-tracker.md` 裡明確約定狀態推進的生命週期：  
- **開始實作時**：在動手前先將該張工單的狀態由「待處理 (Todo)」更新為「進行中 (In Progress)」（在 Markdown 看板中標記為 `🟦 進行中`）。  
- **測試通過後**：實作完成並經測試套件驗證全綠後，才將狀態推進為「完成 (Done)」（在 Markdown 看板中標記為 `✅ 完成`）。

這樣做能讓看板即時反映實作現況，避免工單永遠停在待辦、或是尚未通過測試就偷跑標記為完成的混亂狀況。

---

### 2. 需求磨礪與領域建模（/grill-with-docs）

當要開發新功能時，最忌諱直接叫 AI 寫扣。`/grill-with-docs` 結合了 `grilling`（無情盤問）與 `domain-modeling`（領域建模）。這裡 AI 會把需求拆解成「設計樹 (Design Tree)」，一次只針對當前已經能決策的「前沿問題 (Frontier)」發動輪次 (Rounds) 提問，並在釐清專有名詞後，即時寫入 `GLOSSARY.md` 與架構決策紀錄 (ADR)。

#### 提示詞範例（Prompt）

向 AI 提出新需求並要求進行邊界盤問：

```
/grill-with-docs 我要在購物車結帳流程中加入折價券折扣計算模組。
目前需要支援滿千折百券（Fixed Amount）以及全館折扣券（Percentage）。
請針對設計邊界與未決策的商業邏輯對我進行盤問。
```

AI 隨即會鎖定未釐清的邊界，整理出第一輪前沿問題：

```
❓ Q1 - 折扣計算順序 (Calculation Order):
當購物車同時套用「滿額現折券」與「百分比折扣券」時，兩者的套用順序為何？
選項 A: 先扣滿額現折，剩餘金額再套用百分比折扣。
選項 B: 先依原始商品總額計算百分比折扣，再扣除滿額現折。
➡️ 建議選擇 B。一般電商主流習慣先依原始金額打折，再折抵固定金額，能避免小數點精度反覆縮放的歧義。

---

❓ Q2 - 折扣溢出保護 (Negative Total):
若折價券折扣金額大於購物車商品金額時，該如何處理？
選項 A: 訂單總額最低折抵至 0 元，溢出金額直接作廢不找零。
選項 B: 拋出例外（Throw Exception），不允許套用超出商品總額的折價券。
➡️ 建議選擇 A。這符合消費者的結帳體驗，總計金額不可為負數。
```

回覆確認選項後，AI 會立刻更新根目錄下的 `GLOSSARY.md`，把模糊用語收斂成精確定義：

```
# 領域辭典 (Domain Glossary)

## CartTotal (購物車總計)
購物車中所有項目未扣除任何折扣前的原始小計金額（Subtotal）。

## FixedAmountCoupon (滿額現折券)
當 CartTotal 達到指定門檻（Threshold）時，扣除固定金額（DiscountAmount）的優惠券。

## PercentageCoupon (百分比折扣券)
針對符合資格的商品總額，給予特定比例（Rate）折扣的優惠券。

## FinalPayableAmount (實付金額)
扣除所有折扣後，買方最終必須支付的金額。計算下限固定為 0 元，不可為負數。
```

除此之外，由於折扣計算順序屬於不可逆的重大業務規則，AI 會在 `docs/adr/0001-coupon-calculation-order.md` 建立決策紀錄，明確記下選擇「先打折、後折現」的背後權衡。

---

### 3. 提煉規格與鎖定深接縫（/to-spec 結合 codebase-design）

盤問對齊完畢後，上下文已經累積了具體共識。這時候呼叫 `/to-spec`，AI 會終止盤問模式，純粹將討論提煉成結構化規格書。

這裡最關鍵的是融入了 `codebase-design` 的理念：**拒絕淺模組 (Shallow Module)，定義高槓桿的深接縫 (Deep Seam)**。

#### 淺模組（Shallow Module）反模式範例

如果沒有做好封裝，很容易寫出下面這種把內部細節全部裸露在外的淺模組，呼叫端必須自己處理排序與驗證，測試也非常容易脆裂：

```
// 反模式：淺模組（呼叫端負擔極重，測試容易碎裂）
export class CouponService {
  validateThreshold(coupon: Coupon, cartTotal: number): boolean { /* ... */ }
  sortCouponsByOrder(coupons: Coupon[]): Coupon[] { /* ... */ }
  computePercentage(rate: number, total: number): number { /* ... */ }
  computeFixed(amount: number): number { /* ... */ }
  clampNonNegative(total: number): number { /* ... */ }
}
```

#### 深模組（Deep Module）設計標準

依據 `codebase-design` 的原則，模組應該具備極小的介面，內部封裝完整的業務複雜度，並且這個公開介面就是唯一的測試表面（The interface is the test surface）：

```
// 正確模式：深模組（單一進入點，高內聚，內部複雜度完全封裝）
export function calculateDiscount(cart: CartSnapshot, coupons: Coupon[]): CalculationResult;
```

#### 提示詞範例（Prompt）

指示 AI 提煉規格並指明公開接縫：

```
/to-spec 請根據剛才討論出的折扣計算規則與 ADR 決策，
並遵照 codebase-design 的深模組原則，以 calculateDiscount 為唯一公開接縫產出規格文件。
```

AI 產出的規格書結構如下：

```
## Problem Statement
目前的購物車結帳流程僅支援單品特價，缺乏在結帳階段依條件計算複合折扣券並保護實付金額邊界的能力。

## Solution
提供一個純運算深模組函式 `calculateDiscount(cart, coupons)`，負責驗證門檻、計算折扣順序，並確保最終金額不為負數。

## User Stories
1. As a shopper, I want to apply a 10% off coupon to my $1,000 cart, so that I only pay $900.
2. As a shopper, I want to apply both a 10% coupon and a $100 off coupon, so that the 10% is calculated first ($900), followed by the $100 reduction ($800).
3. As a shopper with an $80 cart applying a $100 coupon, I want the payable amount to be $0 instead of negative.
4. As a shopper, when my cart total does not reach the coupon threshold, I want the coupon to be rejected with an explanatory code.

## Implementation Decisions
- 核心運算為無狀態純函式（Pure Function）。
- 輸入格式：CartSnapshot 物件與 Coupon 陣列。
- 輸出格式：包含折扣明細（breakdowns）與實付金額（finalPayableAmount）的運算結果物件。
- 不在此模組直接存取資料庫或外部 API，外部狀態於上游組裝。

## Testing Decisions (Seams)
- 測試接縫（Seam）：以公開介面 `calculateDiscount` 作為唯一觀測邊界。
- 嚴禁針對內部輔助函式單獨撰寫單元測試，避免重構時破壞測試。

## Out of Scope
- 折價券序號（Coupon Code）的領取限制與領取次數檢查（由領券服務負責）。
- 金流付款閘道串接。
```

規格生成後會存放在追蹤器中，若沒有問題就可以往下一步。

---

### 4. 垂直切片工單（/to-tickets）

規格書有了之後，傳統習慣往往會水平切分：一張工單開資料表、一張寫介面、一張寫商業邏輯、一張寫單元測試。這種水平切層（Horizontal Slicing）正是整合時常常噴錯的主因。

Matt Pocock 的 `/to-tickets` 強制要求採用貫穿切片（Tracer-bullet Vertical Slices）：每一張工單都必須貫穿輸入到輸出，完成時能獨立通過測試，並且必須明確宣告依賴阻擋鏈（Blocked by）。

#### 提示詞範例（Prompt）

指示 AI 拆解垂直工單：

```
/to-tickets 請將上述規格拆解成垂直切片工單。
```

AI 會列出工單拆解草案：

```
工單拆解提案：

1. Ticket 01: 基礎滿額現折計算（貫穿切片）
   - What to build: 傳入 CartSnapshot 與單一 FixedAmountCoupon，達門檻扣除金額，未達門檻回傳無折扣，支援 0 元下限防護。
   - Blocked by: None (可立即開工)

2. Ticket 02: 百分比折扣券支援
   - What to build: 支援單一 PercentageCoupon 折扣計算，包含小數點四捨五入邏輯。
   - Blocked by: Ticket 01

3. Ticket 03: 複合折扣順序與明細彙整
   - What to build: 支援多張優惠券同時套用，依 ADR 0001 規定先算百分比再折固定金額，並輸出折扣明細列表。
   - Blocked by: Ticket 02
```

確認後，AI 會在 `.scratch/coupon-engine/issues/` 目錄下建立獨立工單檔案，形成一張具備依賴關係的任務圖 (Task Graph)。

---

### 5. 編排多代理自動化實作（/implement-spec）

在拿到 Spec 與工單任務圖後，不需要人類手動一張一張工單餵給 AI。`/implement-spec` 是整個實作階段的**總指揮官（Orchestrator）**。

它的核心流程如下：  
1. 讀取 Spec 與 Tickets，解析整個 Task Graph。  
2. 開出一條專屬的整合分支（Integration Branch，例如 `feat/coupon-discount-engine`）。  
3. 找出目前未被阻塞的工單前沿 (Frontier)，在背景為每張工單建立獨立的 Git Worktree。  
4. 派出 **Implementer 子代理** 進入各自的 Worktree，並在內部自動掛載 `/tdd` 技能寫測試與寫 code。  
5. 工單完成後，由 **Merger 子代理** 合併回整合分支，推進 Frontier，自動解鎖下一批工單。  
6. 全部工單交付完畢後，自動在整合分支上觸發 `/code-review` 進行雙軸審查。

#### 提示詞範例（Prompt）

直接命令 AI 啟動整套編排實作：

```
/implement-spec 請依照 .scratch/coupon-engine/ 的 spec 與 tickets，在整合分支上實作整個功能。
```

AI 接收到指令後的背景調度流程：

```
[Orchestrator] 讀取任務相依圖，當前 Frontier: [Ticket 01]
[Orchestrator] 建立整合分支 integration/coupon-engine
[Orchestrator] 建立 Worktree: .worktrees/ticket-01，派發 Implementer-1 子代理
  └─ [Implementer-1] 載入 /tdd 技能開始實作 Ticket 01...
  └─ [Implementer-1] 紅燈測試 -> 綠燈實作 -> 驗證通過，提交 commit
[Merger] 將 .worktrees/ticket-01 合併回 integration/coupon-engine
[Orchestrator] Frontier 推進，解鎖 [Ticket 02]，建立 .worktrees/ticket-02...
```

透過這種架構，所有程式碼改動都在乾淨隔離的 Worktree 裡進行，主工作目錄不會被弄亂，並行交付非常俐落。

---

### 6. 深入單工單的測試驅動交付（/tdd）

在 `/implement-spec` 調度 Implementer 子代理實作每張工單時，底層嚴格執行的正是 `/tdd` 規約。這裡要求嚴格遵循紅綠循環（Red-Green Loop）：  
1. 在公開接縫 (Seam) 先寫失敗的測試（Red）。  
2. 只寫剛剛好能讓測試變綠的最小實作程式碼（Green）。  
3. 嚴禁在測試亮紅燈前憑空增加假設或預先編寫未被測試覆蓋的功能。

這裡以 Implementer 子代理在處理 Ticket 01 時的具體程式碼為例。

#### 步驟 1：紅燈測試

以下程式碼示範在公開接縫 `calculateDiscount` 建立規格測試：

```
// tests/coupon-calculator.spec.ts
import { describe, it, expect } from "vitest";
import { calculateDiscount } from "../src/coupon-calculator";

describe("Coupon Discount Engine (Seam: calculateDiscount)", () => {
  it("_01_滿額現折達到門檻應正確扣除金額()", () => {
    const cart = { subtotal: 1000 };
    const coupons = [
      { id: "c1", type: "FIXED", amount: 100, threshold: 800 }
    ];

    const result = calculateDiscount(cart, coupons);

    expect(result.finalPayableAmount).toBe(900);
    expect(result.totalDiscount).toBe(100);
  });

  it("_02_折扣大於購物車金額時實付金額下限為零()", () => {
    const cart = { subtotal: 80 };
    const coupons = [
      { id: "c2", type: "FIXED", amount: 100, threshold: 50 }
    ];

    const result = calculateDiscount(cart, coupons);

    expect(result.finalPayableAmount).toBe(0);
    expect(result.totalDiscount).toBe(80);
  });
});
```

執行測試，確定收到預期的紅燈錯誤：

```
FAIL tests/coupon-calculator.spec.ts
Error: Cannot find module '../src/coupon-calculator'
```

#### 步驟 2：綠燈最小實作

以下程式碼編寫最小實作，讓測試剛好通過，不寫任何多餘邏輯：

```
// src/coupon-calculator.ts
export interface CartSnapshot {
  subtotal: number;
}

export interface Coupon {
  id: string;
  type: "FIXED" | "PERCENTAGE";
  amount?: number;
  rate?: number;
  threshold: number;
}

export interface CalculationResult {
  finalPayableAmount: number;
  totalDiscount: number;
}

export function calculateDiscount(
  cart: CartSnapshot,
  coupons: Coupon[]
): CalculationResult {
  let discount = 0;

  for (const coupon of coupons) {
    if (coupon.type === "FIXED" && coupon.amount) {
      if (cart.subtotal >= coupon.threshold) {
        discount += coupon.amount;
      }
    }
  }

  const finalDiscount = Math.min(discount, cart.subtotal);
  const payable = Math.max(0, cart.subtotal - finalDiscount);

  return {
    finalPayableAmount: payable,
    totalDiscount: finalDiscount,
  };
}
```

再次執行測試確認結果：

```
PASS tests/coupon-calculator.spec.ts
✓ _01_滿額現折達到門檻應正確扣除金額 (2ms)
✓ _02_折扣大於購物車金額時實付金額下限為零 (1ms)

Test Files  1 passed (1)
Tests       2 passed (2)
```

測試通過後，Implementer 子代理即完成該工單任務，由 Merger 子代理將程式碼合併回整合分支。

---

### 7. 雙軸程式碼審查（/code-review）

當所有切片工單實作完畢並合併到整合分支後，流程會自動觸發 `/code-review` 進行最終驗收。

這個技能最獨特之處在於「雙軸獨立審查（Two-axis review）」。它會平行派出兩個子代理，分別針對兩個維度進行審查，避免相互掩蓋：  
1. **Standards 軸**：檢查程式碼是否符合專案編碼標準，並依據 Martin Fowler 的 12 種經典壞味道（Code Smells，例如 Primitive Obsession、Feature Envy、Shotgun Surgery）進行健檢。  
2. **Spec 軸**：比對規格書與 Git Diff，檢查是否有漏做的驗收條件，或者未經允許的額外功能（Scope Creep）。

#### 提示詞範例（Prompt）

審查整合分支相對於主幹的變更：

```
/code-review main...integration/coupon-engine
```

審查完成後，AI 會分別輸出兩份獨立報告：

```
## Standards (編碼規範與壞味道審查)
- [Heuristic - Primitive Obsession]: `src/coupon-calculator.ts` 中的金額與小數計算目前直接使用原生 `number` 型別。建議在後續重構中封裝為專屬的貨幣數值型別，避免浮點數精度誤差。
- [Pass]: 函式介面簡潔，符合高內聚與單一職責，未發現 Feature Envy 或 Mysterious Name。

## Spec (規格符合度審查)
- [Pass]: 驗收條件 1 至 4 皆已由公開接縫的單元測試覆蓋。
- [Pass]: 0 元下限防護與滿額門檻邏輯皆符合 ADR 0001 決策。
- [Scope Creep Check]: 檢查無多餘的未授權介面或資料庫存取，範圍維持在純運算模組。

---
審查總結：
- Standards: 1 個輕微建議（建議將 number 提煉為貨幣型別）
- Spec: 0 項違規（完全符合原定規格）
```

審查確認沒有阻礙問題後，清理 Worktrees，並將整合分支合併或建立 Pull Request。

NOTE：依照規範，Git 提交訊息嚴禁包含 Co-authored-by 標記。

---

### 8. 落地後的主動架構深化（/improve-codebase-architecture）

功能上線幾週後，隨著需求陸續增加，系統又追加了「全館免運券」、「會員點數折抵」以及「紅利折現」。這時很多專案的程式碼開始出現摩擦力——折價券邏輯寫在結帳模組，免運券寫在物流模組，點數折抵寫在會員模組，彼此之間產生了散彈式修改（Shotgun Surgery）。

這就是 `/improve-codebase-architecture` 派上用場的時候了。它不會瞎猜，而是：  
1. 掃描 Git Commit 歷史，找出近期變更最頻繁的程式碼熱點 (Hot spots)。  
2. 對模組進行「刪除測試 (Deletion Test)」：如果刪掉某個中介模組，複雜度只會分散到各處還是會集中？  
3. **在作業系統暫存區生成一份視覺化的 HTML 報告**（整合 Tailwind CSS 與 Mermaid），給出 Before / After 架構對比。

#### 提示詞範例（Prompt）

啟動架構健檢掃描：

```
/improve-codebase-architecture
```

執行後，AI 輸出報告路徑並在瀏覽器中開啟：

```
已完成程式碼庫熱點與架構摩擦力掃描！
報告已產生於: /tmp/architecture-review-20261010.html
正在為您開啟瀏覽器檢視...
```

#### HTML 報告中的診斷內容範例

報告中會精準列出候選重構項目：

```
### 候選項目 1：購物車優惠結算淺模組深化 (Recommendation: Strong)
- 涉及檔案: `src/coupon-calculator.ts`, `src/shipping-discount.ts`, `src/points-deduction.ts`
- 問題描述 (Problem):
  結帳流程呼叫端需要依序呼叫三個不同的淺模組，並手動在外部處理「點數與折價券互斥」以及「免運門檻與實付金額連動」的邊界邏輯。局部性 (Locality) 極差，任何優惠規則調整都會導致呼叫端破碎。
- 改善方案 (Solution):
  建立深模組 `CartCheckoutPricingEngine`，將折價券、運費券、點數折抵收斂至單一深接縫，外部只需傳入購物車與優惠清單，內部一次完成所有相依排程與折抵運算。
```

#### 結構對比圖（Before vs After）

```
flowchart LR
    subgraph BeforeMode ["Before: 淺模組分散呼叫 (高摩擦力)"]
        Caller["結帳呼叫端 (Checkout Controller)"]
        Caller --> M1["CouponCalculator (折價券)"]
        Caller --> M2["ShippingEngine (運費)"]
        Caller --> M3["PointsDeduction (點數)"]
        Caller -.-> Rule["呼叫端必須自行協調互斥與順序"]
    end

    subgraph AfterMode ["After: 深模組單一接縫 (高槓桿與局部性)"]
        Caller2["結帳呼叫端 (Checkout Controller)"] --> DeepEngine["CartCheckoutPricingEngine (深模組接縫)"]
        DeepEngine --> InnerLogic["內部私有封裝:<br>1. 折價券順序計算<br>2. 運費折抵規則<br>3. 點數抵扣與邊界防護"]
    end
```

在 HTML 報告中挑選候選方案後，AI 會無縫啟動 `/grilling` 盤問迴圈，引導敲定重構決策：

```
❓ Q1 - 優惠互斥權重 (Discount Priority):
在新的深模組中，若全館折扣券與點數折抵規則衝突，優先扣除哪一項？
➡️ 建議先扣折價券，剩餘金額再套用點數抵扣。

確認後將自動更新 GLOSSARY.md 並建立 ADR-0002，接著進入 /to-spec 產生重構規格！
```

如此一來，外軌的架構健檢便完美回流到內軌的規格與 TDD 交付鏈，形成了真正的永續架構演進。

---

### 9. 系統異常與效能衰退的科學排查（/diagnosing-bugs）

當系統上線運轉後，難免會遇到線上回報「偶發性計算錯誤 (Flaky Bug)」、「噴出例外 (Throwing)」或「效能衰退 (Performance Regression)」。面對這類棘手問題，傳統 AI 最常見的壞習慣就是：一拿到錯誤日誌，立刻開啟相關檔案，憑肉眼直覺「猜測」可能的原因並隨意修改程式碼。這種盲猜式除錯往往只會越改越糟。

Matt Pocock 體系中的 `/diagnosing-bugs` 是一套極度嚴謹的**科學診斷紀律**。它的核心鋼鐵準則是：**在尚未建立出「秒級、確定性、一鍵可跑」的紅燈重現指令 (Feedback Loop) 之前，嚴禁跳入程式碼盲猜假說！**

這裡以我們折價券模組在線上遇到的真實故障為例：  
線上回報當購物車遇到多張小額券與百分比券疊加時，實付金額偶發計算出小於 0 的極小浮點數殘留（例如 `-0.000000001`），結果實付金額算成負數，金流付款直接噴掉了啦!!!

#### 提示詞範例（Prompt）

指示 AI 啟動科學排查流程：

```
/diagnosing-bugs 線上回報購物車在特定多券抵扣時，calculateDiscount 傳出的實付金額偶發為小於 0 的微小負數。
請依照六階段診斷紀律進行排查，先建立反饋迴圈，嚴禁看 code 盲猜。
```

AI 隨即會依循六大階段有條不紊地推進：

#### 階段 1 與 2：建立秒級紅燈迴圈並最小化（Build a feedback loop & Minimise）

AI 不會直接改程式碼，而是先在測試接縫處建立一個確定能抓到此 Bug 的測試案例，並砍掉所有無關參數，只保留造成錯誤的最小承重牆資料：

```
// tests/regression-flaky-negative.spec.ts
import { describe, it, expect } from "vitest";
import { calculateDiscount } from "../src/coupon-calculator";

describe("Regression: 浮點數微小負數溢出重現迴圈", () => {
  it("_01_極限折扣組合不應產生負數浮點數殘留()", () => {
    // 最小重現資料（承重牆條件：小數點折抵剛好觸發 IEEE 754 精度誤差）
    const cart = { subtotal: 99.99 };
    const coupons = [
      { id: "c1", type: "PERCENTAGE", rate: 0.1, threshold: 0 },
      { id: "c2", type: "FIXED", amount: 89.991, threshold: 0 },
    ];

    const result = calculateDiscount(cart, coupons);

    // 嚴格斷言：實付金額必須為完全精確的 0，不能為微小負數
    expect(result.finalPayableAmount).toBeGreaterThanOrEqual(0);
    expect(Object.is(result.finalPayableAmount, -0)).toBe(false);
  });
});
```

執行此反饋迴圈，精準目睹紅燈報錯：

```
FAIL tests/regression-flaky-negative.spec.ts
AssertionError: expected -0 to not be -0 // 或 -1e-11 to be greater than or equal to 0
```

#### 階段 3：提出可證偽假說（Hypothesise）

在確認紅燈後，AI 會列出 3 個具備可證偽性的假說，並向開發者確認：  
1. **假說 1**：IEEE 754 浮點數相減誤差導致 `cart.subtotal - finalDiscount` 產生微小負數殘留，且原生 `Math.max(0, ...)` 未能攔截 `-0`。  
2. **假說 2**：折價券排序演算法在面對權重相同時順序漂移。  
3. **假說 3**：`Math.min` 上限保護取值順序發生時序競爭。

#### 階段 4 與 5：單變數插樁驗證與接縫修復（Instrument & Fix）

在關鍵邊界加入帶有唯一識別碼的臨時探針 `[DEBUG-b1c4]`，確認假說 1 完全成立。接著在不修改測試的前提下，於 `src/coupon-calculator.ts` 給出最小精準修復：

```
// src/coupon-calculator.ts 修復片段：以精確數值修正邊界
const rawPayable = cart.subtotal - finalDiscount;
// 消除 IEEE 754 負零 (-0) 與微小浮點數殘留
const payable = rawPayable <= 0.000001 ? 0 : Math.round(rawPayable * 100) / 100;
```

再次執行重現迴圈與完整測試套件：

```
PASS tests/regression-flaky-negative.spec.ts
✓ _01_極限折扣組合不應產生負數浮點數殘留 (1ms)

Test Files  2 passed (2)
Tests       9 passed (9)
```

#### 階段 6：清理還原（Cleanup）

單一指令搜尋並移除所有 `[DEBUG-b1c4]` 標記，確認原始重現腳本全綠，並將「假說 1 成立（IEEE 754 浮點數負零邊界修正）」作為關鍵紀錄寫入 Git 提交訊息。

---

## 心得

- 這套工作流程把傳統「隨意叫 AI 寫扣」的隨機性，收斂成一套具備工程約束的現代流水線。
- 從 `/setup-matt-pocock-skills` 固化環境與工單格式開始，到 `/grill-with-docs` 的多輪邊界盤問，可以在動手前先淘汰掉 80% 的理解偏差。
- `codebase-design` 提供了一套清晰的判斷準則，告別無意義的淺模組（Shallow Modules），堅持以深接縫（Deep Seams）作為唯一測試表面，解決了傳統單元測試容易脆弱碎裂的長年痛點。
- `/to-spec` 與 `/to-tickets` 將巨型需求拆成有依賴關係的任務圖（Task Graph），而 `/implement-spec` 則扮演自動化調度大腦，透過 Git Worktree 隔離並行驅動 `/tdd`，展現了現代多代理架構的威力。
- 落地後再搭配 `/improve-codebase-architecture` 定期主動體檢，生成 HTML Before/After 視覺化報告，讓架構重構不再憑直覺摸黑進行。
- 面對系統異常與效能衰退時，`/diagnosing-bugs` 的六階段紀律強迫 AI 在建立秒級紅燈迴圈前「絕對不准瞎猜看 code」，是保障大型系統穩定維運的終極安全網。

---

完整程式碼位置: https://github.com/yaochangyu/sample.dotblog/tree/master/AI.SDD/Lab.Matt-Pocock
