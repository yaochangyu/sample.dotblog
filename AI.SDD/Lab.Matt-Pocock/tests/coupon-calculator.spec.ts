import { describe, it, expect } from "vitest";
import { calculateDiscount, CartSnapshot, Coupon } from "../src/coupon-calculator";

describe("Coupon Discount Engine (Seam: calculateDiscount)", () => {
  describe("Ticket 01: 基礎滿額現折 (Fixed Amount)", () => {
    it("01_滿額現折達到門檻應正確扣除金額", () => {
      const cart: CartSnapshot = { subtotal: 1000 };
      const coupons: Coupon[] = [
        { id: "c1", type: "FIXED", amount: 100, threshold: 800 }
      ];

      const result = calculateDiscount(cart, coupons);

      expect(result.finalPayableAmount).toBe(900);
      expect(result.totalDiscount).toBe(100);
    });

    it("02_未達門檻時不扣除金額", () => {
      const cart: CartSnapshot = { subtotal: 500 };
      const coupons: Coupon[] = [
        { id: "c1", type: "FIXED", amount: 100, threshold: 800 }
      ];

      const result = calculateDiscount(cart, coupons);

      expect(result.finalPayableAmount).toBe(500);
      expect(result.totalDiscount).toBe(0);
    });

    it("03_折扣大於購物車金額時實付金額下限為零", () => {
      const cart: CartSnapshot = { subtotal: 80 };
      const coupons: Coupon[] = [
        { id: "c2", type: "FIXED", amount: 100, threshold: 50 }
      ];

      const result = calculateDiscount(cart, coupons);

      expect(result.finalPayableAmount).toBe(0);
      expect(result.totalDiscount).toBe(80);
    });
  });

  describe("Ticket 02: 百分比折扣 (Percentage)", () => {
    it("04_單一百分比折價券達到門檻應正確計算折扣", () => {
      const cart: CartSnapshot = { subtotal: 1000 };
      const coupons: Coupon[] = [
        { id: "p1", type: "PERCENTAGE", rate: 10, threshold: 500 }
      ];

      const result = calculateDiscount(cart, coupons);

      expect(result.finalPayableAmount).toBe(900);
      expect(result.totalDiscount).toBe(100);
    });

    it("05_百分比折價券未達門檻不予折抵", () => {
      const cart: CartSnapshot = { subtotal: 300 };
      const coupons: Coupon[] = [
        { id: "p1", type: "PERCENTAGE", rate: 10, threshold: 500 }
      ];

      const result = calculateDiscount(cart, coupons);

      expect(result.finalPayableAmount).toBe(300);
      expect(result.totalDiscount).toBe(0);
    });

    it("06_百分比計算應四捨五入至整數", () => {
      const cart: CartSnapshot = { subtotal: 333 };
      const coupons: Coupon[] = [
        { id: "p2", type: "PERCENTAGE", rate: 15, threshold: 100 }
      ];

      // 333 * 0.15 = 49.95 -> 50
      const result = calculateDiscount(cart, coupons);

      expect(result.totalDiscount).toBe(50);
      expect(result.finalPayableAmount).toBe(283);
    });
  });

  describe("Ticket 03: 複合折扣順序與明細彙整 (Compound Discounts)", () => {
    it("07_複合折扣時應先依小計扣百分比再扣固定金額", () => {
      const cart: CartSnapshot = { subtotal: 1000 };
      const coupons: Coupon[] = [
        { id: "c1", type: "FIXED", amount: 100, threshold: 500 },
        { id: "p1", type: "PERCENTAGE", rate: 10, threshold: 500 }
      ];

      // 10% of 1000 = 100 -> remain 900; then fixed 100 -> remain 800
      const result = calculateDiscount(cart, coupons);

      expect(result.finalPayableAmount).toBe(800);
      expect(result.totalDiscount).toBe(200);
      expect(result.breakdowns).toEqual([
        { couponId: "p1", applied: true, discountAmount: 100 },
        { couponId: "c1", applied: true, discountAmount: 100 }
      ]);
    });

    it("08_多券疊加超過總額時實付為0且各券折抵不超額", () => {
      const cart: CartSnapshot = { subtotal: 150 };
      const coupons: Coupon[] = [
        { id: "p1", type: "PERCENTAGE", rate: 50, threshold: 100 }, // 75
        { id: "c1", type: "FIXED", amount: 100, threshold: 100 }    // 100 (超過剩餘 75)
      ];

      const result = calculateDiscount(cart, coupons);

      expect(result.finalPayableAmount).toBe(0);
      expect(result.totalDiscount).toBe(150);
      expect(result.breakdowns).toEqual([
        { couponId: "p1", applied: true, discountAmount: 75 },
        { couponId: "c1", applied: true, discountAmount: 75 }
      ]);
    });
  });
});
