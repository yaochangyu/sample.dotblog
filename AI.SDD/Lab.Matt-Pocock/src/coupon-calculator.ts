export interface CartSnapshot {
  subtotal: number;
}

export type CouponType = "FIXED" | "PERCENTAGE";

export interface Coupon {
  id: string;
  type: CouponType;
  amount?: number;
  rate?: number;
  threshold: number;
}

export interface CouponBreakdown {
  couponId: string;
  applied: boolean;
  discountAmount: number;
}

export interface CalculationResult {
  finalPayableAmount: number;
  totalDiscount: number;
  breakdowns: CouponBreakdown[];
}

/**
 * 核心深模組公開接縫：計算購物車折價券折扣
 * 遵循 ADR 0001：先依原始金額套用百分比折價券，再折減固定金額券；下限 0 元防護
 */
export function calculateDiscount(
  cart: CartSnapshot,
  coupons: Coupon[]
): CalculationResult {
  const breakdowns: CouponBreakdown[] = [];
  let remainingPayable = cart.subtotal;

  // 排序依據 ADR 0001: 先 PERCENTAGE 後 FIXED
  const sortedCoupons = [...coupons].sort((a, b) => {
    if (a.type === "PERCENTAGE" && b.type === "FIXED") return -1;
    if (a.type === "FIXED" && b.type === "PERCENTAGE") return 1;
    return 0;
  });

  for (const coupon of sortedCoupons) {
    if (cart.subtotal < coupon.threshold) {
      breakdowns.push({
        couponId: coupon.id,
        applied: false,
        discountAmount: 0
      });
      continue;
    }

    let calculatedDiscount = 0;
    if (coupon.type === "PERCENTAGE" && typeof coupon.rate === "number") {
      calculatedDiscount = Math.round(cart.subtotal * (coupon.rate / 100));
    } else if (coupon.type === "FIXED" && typeof coupon.amount === "number") {
      calculatedDiscount = coupon.amount;
    }

    // 0 元防護：折抵額不可超過目前剩餘應付金額
    const actualDiscount = Math.min(calculatedDiscount, remainingPayable);
    remainingPayable -= actualDiscount;

    breakdowns.push({
      couponId: coupon.id,
      applied: actualDiscount > 0,
      discountAmount: actualDiscount
    });
  }

  const totalDiscount = cart.subtotal - remainingPayable;

  return {
    finalPayableAmount: remainingPayable,
    totalDiscount,
    breakdowns
  };
}
