# Test Structure & The AAA Pattern

## Overview

The **Arrange-Act-Assert (AAA)** pattern is the foundational structure for clean, readable, and maintainable unit tests. Adhering to this pattern ensures that anyone reading the test can immediately identify the preconditions, the action executed, and the expected result.

---

## 1. The AAA Anatomy

```typescript
describe('ComponentName / ClassName', () => {
  // Test suite setup (if shared)
  
  describe('methodName / behavior', () => {
    it('should do expected thing when given condition', () => {
      // 1. Arrange: Prepare the state, create fixtures, set up mock responses
      const initialCount = 10;
      const incrementStep = 2;
      const counter = new Counter(initialCount);

      // 2. Act: Trigger the single behavior being validated
      counter.increment(incrementStep);

      // 3. Assert: Verify the outcome or side effects
      expect(counter.currentValue).toBe(12);
    });
  });
});
```

---

## 2. Naming Conventions

Always use predictable, descriptive test descriptions that read like natural language specifications:

### Formula:
`it('should [expected result] when [condition or inputs]')`

### Examples:
- ✅ `it('should return empty list when no matching indicators exist')`
- ✅ `it('should disable submit button when form is invalid')`
- ✅ `it('should emit reviewApproved event when status changes to approved')`
- ❌ `it('test calculate')`
- ❌ `it('works')`
- ❌ `it('handles null')`

---

## 3. Suite Nesting Structure

Structure suites with logical hierarchy:

```typescript
describe('LogFrameService', () => {
  let service: LogFrameService;
  let mockHttp: jasmine.SpyObj<HttpClient>;

  beforeEach(() => {
    // Setup fresh instance per test
  });

  describe('getLogFrameById', () => {
    it('should return logframe with components on 200 success', () => { ... });
    it('should return fallback error model on 404 not found', () => { ... });
  });

  describe('calculateRollup', () => {
    it('should aggregate quarterly values into annual sum', () => { ... });
    it('should carry forward most recent value when aggregation type is MostRecent', () => { ... });
  });
});
```

---

## 4. Single Logical Assertion Principle

Avoid asserting multiple unrelated concepts in the same test block. Multiple assertions are acceptable **only** when asserting different properties of the exact same returned object or state transition:

### Allowed (Testing properties of the same result):
```typescript
const result = service.createUser(dto);
expect(result.id).toBeDefined();
expect(result.status).toBe('Active');
expect(result.roles).toContain('DataEntry');
```

### Discouraged (Testing multiple sequential actions in one test):
```typescript
// ❌ Anti-pattern: Chained action testing
counter.increment(1);
expect(counter.value).toBe(1);
counter.decrement(1); // Second act!
expect(counter.value).toBe(0);
```
Split into separate `it()` blocks so that when a failure occurs, the exact failed behavior is immediately identified.
