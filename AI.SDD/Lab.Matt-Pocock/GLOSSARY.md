# 領域辭典 (Domain Glossary)

## CartTotal (購物車總計)
購物車中所有項目未扣除任何折扣前的原始小計金額（Subtotal）。

## FixedAmountCoupon (滿額現折券)
當 CartTotal 達到指定門檻（Threshold）時，扣除固定金額（DiscountAmount）的優惠券。若未達門檻則不可折抵。

## PercentageCoupon (百分比折扣券)
針對符合門檻資格的商品總額，給予特定比例（Rate，0 ~ 1 之間或百分比整數）折扣的優惠券。折抵金額四捨五入至整數。

## FinalPayableAmount (實付金額)
扣除所有適用折扣後，買方最終必須支付的金額。計算下限固定為 0 元，不可為負數（Negative Total Protection）。

## TotalDiscount (總折扣金額)
購物車所獲得的實際折抵總額。當折扣總額大於購物車總計時，總折扣金額上限為購物車總計金額。

## CalculationResult (折扣計算結果)
包含實付金額（FinalPayableAmount）、總折扣金額（TotalDiscount）與各張券折抵明細（Breakdown）的完整結果物件。
