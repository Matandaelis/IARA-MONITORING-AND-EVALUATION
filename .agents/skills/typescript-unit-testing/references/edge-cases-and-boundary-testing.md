# Edge Cases & Boundary Testing in TypeScript

## Overview

A robust unit test suite covers not only the happy path, but also defensive boundaries, off-by-one limits, nullability, and empty inputs.

---

## 1. The Checklist of Critical Edge Cases

Whenever authoring unit tests for a function, component, or calculation engine:

1. **Empty State:**
   - Empty array: `[]`
   - Empty string: `""`
   - Empty object: `{}`
2. **Boundary Values:**
   - Zero: `0`, `-0`
   - Negative values: `-1`, `-999`
   - Minimum / Maximum safe integers: `Number.MIN_SAFE_INTEGER`, `Number.MAX_SAFE_INTEGER`
   - Precision / Floating point decimals: `0.1 + 0.2 === 0.30000000000000004`
3. **Nullability & Undefined:**
   - `null` passed where an object is expected
   - `undefined` optional parameter
   - Missing nested properties: `user?.address?.city`
4. **Malformed or Extreme Inputs:**
   - Strings with leading/trailing whitespaces: `"  admin  "`
   - Strings with special characters or SQL/XSS tokens: `"<script>alert(1)</script>"`
   - Long strings / payloads exceeding usual thresholds

---

## 2. Example: Testing Edge Cases in Calculation Engine

```typescript
describe('CalculationService - Edge Cases', () => {
  let calculation: CalculationService;

  beforeEach(() => {
    calculation = new CalculationService();
  });

  describe('aggregateSum', () => {
    it('should return 0 when input array is empty', () => {
      expect(calculation.aggregateSum([])).toBe(0);
    });

    it('should handle array with a single element', () => {
      expect(calculation.aggregateSum([42])).toBe(42);
    });

    it('should correctly sum negative and positive numbers', () => {
      expect(calculation.aggregateSum([-10, 20, -5])).toBe(5);
    });

    it('should avoid floating-point inaccuracies using rounding', () => {
      const result = calculation.aggregateSum([0.1, 0.2]);
      expect(result).toBeCloseTo(0.3, 5);
    });

    it('should filter out null or NaN values gracefully if configured', () => {
      const inputs = [10, null as unknown as number, NaN, 20];
      expect(calculation.aggregateSumSafe(inputs)).toBe(30);
    });
  });
});
```

---

## 3. Boundary & Off-by-One Assertions

```typescript
describe('Pagination Helper', () => {
  it('should return totalPages = 1 when totalItems is exactly pageSize', () => {
    expect(calculateTotalPages(20, 20)).toBe(1);
  });

  it('should return totalPages = 2 when totalItems is pageSize + 1', () => {
    expect(calculateTotalPages(21, 20)).toBe(2);
  });

  it('should return totalPages = 0 when totalItems is 0', () => {
    expect(calculateTotalPages(0, 20)).toBe(0);
  });
});
```
