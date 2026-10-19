# Mocking & Test Doubles

## Overview

Mocking allows you to isolate the unit under test from its external dependencies (APIs, storage, routing, child services). In TypeScript, test doubles must be strictly typed to prevent mock drift (where a mock interface doesn't match the real interface).

---

## 1. Jasmine Spies & `createSpyObj`

In Jasmine/Karma (standard Angular test runners), use `jasmine.createSpyObj` with explicit generics:

```typescript
// Define strongly-typed spy
let authServiceSpy: jasmine.SpyObj<AuthService>;

beforeEach(() => {
  const spy = jasmine.createSpyObj<AuthService>('AuthService', [
    'getCurrentUser',
    'hasRole',
    'logout'
  ]);

  // Set default behaviors
  spy.getCurrentUser.and.returnValue(of({ id: 'u-1', username: 'auditor', role: 'Oversight' }));
  spy.hasRole.and.callFake((role: string) => role === 'Oversight');

  authServiceSpy = spy;
});
```

---

## 2. Preventing `any` in Test Fixtures

Never cast test doubles with `as any`. Use TypeScript utility types to construct minimal, valid test data:

```typescript
// ❌ Dangerous: hides missing properties or breaking type changes
const mockIndicator = { id: 1 } as any;

// ✅ Clean & Type-safe: Use Partial or Pick
const mockIndicator: Partial<IndicatorDTO> = {
  id: 1,
  name: 'Beneficiary Count',
  indicatorType: IndicatorType.Collected,
  frequency: FrequencyType.Quarterly
};

// Or use a typed factory function:
export function createTestIndicator(overrides: Partial<IndicatorDTO> = {}): IndicatorDTO {
  return {
    id: 1,
    name: 'Default Indicator',
    indicatorType: IndicatorType.Collected,
    frequency: FrequencyType.Quarterly,
    dataType: DataType.Number,
    sortOrder: 1,
    ...overrides
  };
}
```

---

## 3. Mocking Angular Router & ActivatedRoute

```typescript
import { ActivatedRoute, Router } from '@angular/router';
import { of } from 'rxjs';

let mockRouter: jasmine.SpyObj<Router>;
let mockActivatedRoute: Partial<ActivatedRoute>;

beforeEach(() => {
  mockRouter = jasmine.createSpyObj<Router>('Router', ['navigate']);
  mockActivatedRoute = {
    paramMap: of(new Map([['id', '101']])),
    snapshot: {
      paramMap: {
        get: (key: string) => (key === 'id' ? '101' : null)
      }
    } as any
  };
});
```

---

## 4. Spying on Methods vs Stubbing

- **Stubbing (`returnValue`)**: Replaces the method with a fixed response.
  ```typescript
  serviceSpy.fetchData.and.returnValue(of([1, 2, 3]));
  ```
- **Error Simulation (`throwError`)**:
  ```typescript
  serviceSpy.fetchData.and.returnValue(throwError(() => new Error('Server down')));
  ```
- **Call-through**: Allows the real implementation to run while recording invocations.
  ```typescript
  spyOn(realService, 'validateInput').and.callThrough();
  ```
