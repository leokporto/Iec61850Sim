# Testing Guidelines — Iec61850Sim

This document defines the testing strategy and rules for the project.

---

# Test Scope

Unit tests must focus on the project:

Iec61850Sim.Core

This project contains the core simulation logic and business rules.

Client applications must NOT contain unit tests:

- Iec61850Sim.Web
- Iec61850Sim.Desktop

These layers are considered infrastructure / UI.

---

# Test Project

Tests must be implemented in the project:

Iec61850Sim.Tests

This project references:

Iec61850Sim.Core

---

# Test Stack

The project uses the following testing libraries:

- xUnit v3 - Main unit testing framework
- NSubstitute - Used for mocking dependencies
- Bogus - Used for generating fake test data

---

# Testing Strategy

The project follows **Test Driven Development (TDD)**.

Rules:

1. Tests must be written before implementing new public methods.
2. New features are only considered complete when all tests pass.
3. If an existing public method is modified and does not have tests, a test must be created.


Definition of Done (DoD):

A feature is only complete when:

- All tests pass
- New behavior is covered by tests
- No existing test is broken

---

# Unit Test Coverage Rules

Tests must be written for:

- All public methods
- All domain logic
- Simulation logic

Avoid testing:

- UI
- framework configuration
- external libraries

---

# Test Design Guidelines

Tests should follow the AAA (Arrange, Act, Assert) pattern.

**Naming:**
- `<ClassName>Tests` for test class
- `<MethodName>_<Conditions>_<AssertedOutcome>` for test methods (never `Async` suffix)

Example:

```csharp
public class BreakerTests
{
    [Fact]
    public void Open_Should_Set_IsOpen_To_True()
    {
        // Arrange
        var breaker = new Breaker();

        // Act
        breaker.Open();

        // Assert
        Assert.True(breaker.IsOpen);
    }
}
```

---

# Mocking Rules

Use NSubstitute for mocking dependencies.

Mock external dependencies such as:

* repositories
* services
* infrastructure

Avoid mocking domain logic.

---

# Test Data Generation

Use Bogus to generate random data when appropriate.

Example:

```csharp
var faker = new Faker<DevicePoint>()
    .RuleFor(p => p.Reference, f => f.Random.Word())
    .RuleFor(p => p.Value, f => f.Random.Double());
```

# Continuous Improvement

If a bug is discovered:

1. Create a failing test that reproduces the bug.
2. Fix the implementation.
3. Ensure the test passes.


---
