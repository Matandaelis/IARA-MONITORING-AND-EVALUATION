---
name: typescript-unit-testing
description: Write, structure, and review unit tests in TypeScript using Jasmine, Jest, Vitest, and Angular TestBed. Use when asked to "write unit tests", "test component/service", "add unit tests", "fix failing tests", "mock dependencies", or improve TypeScript test coverage following AAA patterns.
category: testing
tags: [typescript, unit-testing, jasmine, jest, vitest, angular, karma, testbed, mocking, aaa]
version: 1.0.0
---

# TypeScript Unit Testing

## Table of Contents

- [Overview](#overview)
- [When to Use](#when-to-use)
- [When NOT to Use](#when-not-to-use)
- [Core Directives](#core-directives)
- [Quick Start: AAA Pattern](#quick-start-aaa-pattern)
- [Workflow Selection](#workflow-selection)
- [Reference Guides](#reference-guides)
- [Best Practices](#best-practices)

---

## Overview

Produce robust, maintainable, and strictly type-safe unit tests for TypeScript applications and Angular components/services. Enforce the **Arrange-Act-Assert (AAA)** pattern, strict mock discipline without `any`, behavior-focused assertions, and comprehensive edge-case coverage.

---

## When to Use

- Writing new unit tests for TypeScript classes, pure functions, services, and utilities.
- Testing Angular components (standalone and classic), directives, pipes, and interceptors.
- Testing reactive state (Angular Signals, RxJS Observables, subjects).
- Creating type-safe test doubles, spies, and mock service implementations.
- Diagnosing and repairing failing unit tests or flaky asynchronous tests.
- Auditing existing test suites for anti-patterns, missing boundary conditions, or poor test isolation.

---

## When NOT to Use

- End-to-end (E2E) browser automation (use Cypress, Playwright, or Puppeteer skills).
- High-level integration tests requiring live databases or running external HTTP backends.
- Visual regression testing or screenshot comparison.

---

## Core Directives

1. **Zero `any` in Test Code:** Always provide strict TypeScript types for test fixtures, mock return values, and parameters. Use utility types such as `Partial<T>`, `Pick<T>`, or `jasmine.SpyObj<T>` / `jest.Mocked<T>`.
2. **Pure Unit Isolation:** Mock all external boundaries (HTTP requests, local storage, browser APIs, timers, third-party libraries). Tests must run in-memory and deterministically in any order.
3. **Behavior Over Implementation:** Test the public API contract, inputs, and outputs. Never spy on private methods or assert internal implementation states.
4. **Single Logical Assertion:** Each test case (`it` or `test`) should verify one cohesive behavior or transition. If a test fails, its name must immediately indicate what broke.
5. **Strict AAA Structure:** Clearly delineate `// Arrange`, `// Act`, and `// Assert` phases in every test.

---

## Quick Start: AAA Pattern

### 1. Pure TypeScript Service / Function

```typescript
describe('CalculatorService', () => {
  let service: CalculatorService;

  beforeEach(() => {
    service = new CalculatorService();
  });

  describe('calculatePercentage', () => {
    it('should return correct percentage when valid inputs provided', () => {
      // Arrange
      const reached = 25;
      const target = 100;

      // Act
      const result = service.calculatePercentage(reached, target);

      // Assert
      expect(result).toBe(25);
    });

    it('should throw an error when target is zero', () => {
      // Arrange
      const reached = 50;
      const target = 0;

      // Act & Assert
      expect(() => service.calculatePercentage(reached, target)).toThrowError('Division by zero');
    });
  });
});
```

### 2. Angular 19+ Service with HTTP Testing

```typescript
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { IndicatorService } from './indicator.service';
import { IndicatorDTO } from '../models/indicator.dto';

describe('IndicatorService', () => {
  let service: IndicatorService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        IndicatorService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(IndicatorService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should fetch indicator details by id', () => {
    // Arrange
    const mockIndicator: IndicatorDTO = {
      id: 42,
      name: 'Beneficiaries Trained',
      type: 'Collected'
    };

    // Act
    service.getById(42).subscribe((data) => {
      // Assert
      expect(data).toEqual(mockIndicator);
    });

    // Assert HTTP request
    const req = httpTesting.expectOne('/api/indicators/42');
    expect(req.request.method).toBe('GET');
    req.flush(mockIndicator);
  });
});
```

---

## Workflow Selection

Identify the current engineering task and follow the matching workflow:

| Workflow | Trigger | Focus Area |
| :--- | :--- | :--- |
| **Write Tests** | User asks to add tests or implement TDD | Review public interfaces, list happy & edge paths, write AAA blocks. |
| **Mocking & Isolation** | External dependencies need stubbing | Setup `jasmine.createSpyObj`, `provideHttpClientTesting`, or mock providers. |
| **Fix Failing Tests** | Asynchronous timeouts, null references | Audit `fakeAsync`/`tick`, verify mock return values, inspect teardown. |
| **Signal / Reactivity** | Signals, computed, or RxJS observables | Test signal emissions, `TestBed.flushEffects()`, marble/async timing. |

---

## Reference Guides

Detailed technical specifications and copy-pasteable patterns in `references/`:

| Guide | Description |
| :--- | :--- |
| [Test Structure & AAA Pattern](references/test-structure-aaa-pattern.md) | Standard block nesting, naming templates, AAA separation, assertion hygiene. |
| [Mocking & Test Doubles](references/mocking-and-test-doubles.md) | Type-safe spies, stub factories, `jasmine.createSpyObj`, avoiding `any`. |
| [Angular Testing Patterns](references/angular-testing-patterns.md) | Angular 19+ TestBed, standalone components, Signals, `HttpTestingController`. |
| [Async Testing & Observables](references/async-and-observables.md) | `fakeAsync`, `tick`, `flush`, Promises, RxJS `firstValueFrom`, error handling. |
| [Edge Cases & Boundary Testing](references/edge-cases-and-boundary-testing.md) | Null/undefined, boundaries, off-by-one, empty collections, malformed payloads. |

---

## Best Practices

### ✅ DO
- Structure test suites with nested `describe()` blocks reflecting unit names and method names.
- Name tests using the pattern: `it('should [expected outcome] when [condition]')`.
- Mock all injectables using strongly-typed test doubles.
- Call `httpTesting.verify()` in `afterEach()` to prevent unasserted HTTP calls.
- Test both the happy path and negative/boundary conditions (null, zero, empty string, errors).
- Clean up subscriptions or rely on `firstValueFrom` in async tests.

### ❌ DON'T
- Never cast mocks to `as any` — declare explicit mock shapes or use `jasmine.SpyObj<T>`.
- Never perform actual HTTP or socket calls in a unit test.
- Do not assert multiple unrelated behaviors inside a single `it()` block.
- Do not write tests that rely on the state or execution order of preceding tests.
- Avoid over-mocking the system under test; test its real public logic.
