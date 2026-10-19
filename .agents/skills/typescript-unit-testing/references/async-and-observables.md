# Async Testing & RxJS Observables

## Overview

Asynchronous operations in TypeScript unit tests commonly involve Promises, RxJS Observables, and time-delayed actions (debouncing, polling). Testing them reliably requires proper synchronization to avoid flaky or uncompleted tests.

---

## 1. Promises with `async / await`

When testing async functions that return Promises:

```typescript
it('should load user profile data asynchronously', async () => {
  // Arrange
  mockAuthService.getUserProfile.and.resolveTo({ id: '1', name: 'Jane' });

  // Act
  const profile = await service.loadProfile();

  // Assert
  expect(profile.name).toBe('Jane');
});
```

---

## 2. Testing RxJS Observables with `firstValueFrom`

Prefer `firstValueFrom` from RxJS over manual `.subscribe()` callbacks when awaiting a single emission:

```typescript
import { firstValueFrom, of } from 'rxjs';

it('should transform raw data into presentation model', async () => {
  // Arrange
  mockApi.getData.and.returnValue(of({ count: 10 }));

  // Act
  const result = await firstValueFrom(service.getFormattedCount$());

  // Assert
  expect(result).toBe('Total: 10 items');
});
```

---

## 3. Using `fakeAsync` and `tick` for Timers and Debounces

For tests involving `setTimeout`, `setInterval`, or RxJS operators like `debounceTime`, use `fakeAsync`:

```typescript
import { fakeAsync, tick, flush } from '@angular/core/testing';

it('should debounce search input queries by 300ms', fakeAsync(() => {
  // Arrange
  const searchResults: string[] = [];
  component.results$.subscribe((r) => searchResults.push(r));

  // Act: Simulate typing
  component.onSearchInput('health');
  tick(100); // 100ms elapsed
  expect(searchResults.length).toBe(0); // Not emitted yet

  tick(200); // Now 300ms elapsed
  // Assert
  expect(searchResults.length).toBe(1);
  expect(searchResults[0]).toBe('health');

  flush(); // Flush any pending timers
}));
```

---

## 4. Testing Error Emission in Observables

Verify that observables properly forward error notifications:

```typescript
import { throwError } from 'rxjs';

it('should handle API failure gracefully', (done: DoneFn) => {
  // Arrange
  const errorResponse = { status: 500, message: 'Internal Server Error' };
  mockService.fetchRecords.and.returnValue(throwError(() => errorResponse));

  // Act
  component.loadRecords$().subscribe({
    next: () => {
      fail('Expected error, but next was called');
    },
    error: (err) => {
      // Assert
      expect(err.status).toBe(500);
      expect(component.hasError).toBeTrue();
      done();
    }
  });
});
```
