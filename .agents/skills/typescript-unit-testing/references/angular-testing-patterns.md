# Angular Testing Patterns (Angular 19+)

## Overview

Modern Angular testing emphasizes standalone components, signal reactivity, modern dependency injection via `inject()`, and `provideHttpClientTesting()`.

---

## 1. Testing Standalone Components

Standalone components declare their own dependencies and imports directly in test setup:

```typescript
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { IndicatorCardComponent } from './indicator-card.component';

describe('IndicatorCardComponent', () => {
  let component: IndicatorCardComponent;
  let fixture: ComponentFixture<IndicatorCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [IndicatorCardComponent], // Standalone components are imported, not declared
      providers: [
        // Provide mock services here
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(IndicatorCardComponent);
    component = fixture.componentInstance;
  });

  it('should render the indicator title in header', () => {
    // Arrange
    fixture.componentRef.setInput('title', 'Community Cleanliness Score');

    // Act
    fixture.detectChanges(); // Trigger change detection

    // Assert
    const titleEl = fixture.debugElement.query(By.css('.card-title')).nativeElement;
    expect(titleEl.textContent).toContain('Community Cleanliness Score');
  });
});
```

---

## 2. Testing Angular Signals & `computed()`

Angular Signals are synchronous and reactive. In unit tests, updating a signal immediately updates computed signals and templates upon change detection:

```typescript
describe('IndicatorProgressComponent with Signals', () => {
  it('should compute completion percentage correctly', () => {
    // Arrange
    const component = new IndicatorProgressComponent();

    // Act
    component.targetValue.set(200);
    component.actualValue.set(150);

    // Assert
    expect(component.percentComplete()).toBe(75);
  });

  it('should cap completion percentage at 100 when overachieved', () => {
    const component = new IndicatorProgressComponent();
    component.targetValue.set(100);
    component.actualValue.set(125);

    expect(component.cappedPercent()).toBe(100);
  });
});
```

---

## 3. Testing Reactive Forms

```typescript
import { ReactiveFormsModule } from '@angular/forms';

describe('SurveyFormComponent', () => {
  let fixture: ComponentFixture<SurveyFormComponent>;
  let component: SurveyFormComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SurveyFormComponent, ReactiveFormsModule]
    }).compileComponents();

    fixture = TestBed.createComponent(SurveyFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should mark form as invalid when required question response is empty', () => {
    // Arrange
    const questionControl = component.surveyForm.get('response');
    questionControl?.setValue('');

    // Act & Assert
    expect(component.surveyForm.valid).toBeFalse();
    expect(questionControl?.hasError('required')).toBeTrue();
  });
});
```

---

## 4. Testing HTTP Interceptors (Functional Interceptors)

Angular 19 utilizes functional interceptors (`HttpInterceptorFn`):

```typescript
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient, withInterceptors, HttpClient } from '@angular/common/http';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  let client: HttpClient;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting()
      ]
    });

    client = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  it('should attach Bearer Authorization header when token exists', () => {
    localStorage.setItem('auth_token', 'jwt-token-123');

    client.get('/api/projects').subscribe();

    const req = httpTesting.expectOne('/api/projects');
    expect(req.request.headers.get('Authorization')).toBe('Bearer jwt-token-123');
    req.flush([]);
  });
});
```
