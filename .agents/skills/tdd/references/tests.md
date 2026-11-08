# Good and Bad Tests

## Good Tests

**Integration-style**: Test through real interfaces, not mocks of internal parts.

```typescript
// GOOD: Tests observable behavior
test("user can checkout with valid cart", async () => {
  const cart = createCart();
  cart.add(product);
  const result = await checkout(cart, paymentMethod);
  expect(result.status).toBe("confirmed");
});
```

Characteristics:

- Tests behavior users/callers care about
- Uses public API only
- Survives internal refactors
- Describes WHAT, not HOW
- One logical assertion per test

## Bad Tests

### Do not bypass the transformation that can lose the requirement

For values carried through several stages, exercise the production path from their actual input to
the observable consumer. A launch-mode test that injects an already-selected group can pass while
the real plan-to-selected-group projection drops the mode. Start from the declared/planned input,
use the real selection/projection, and assert the resulting launch arguments or admission outcome.
Keep unrelated process/game work outside that focused test; test real transformations, not a copied
implementation. Cover the distinct selection routes actually supported by the changed contract.

For validation-before-side-effects assertions, identify the intended rejection by its public error
code or actionable reason, not only a broad exception type. A later lease, file or transport failure
can throw that same type and let a bypassed validator pass the test. When review exposes this gap,
prove the corrected assertion rejects the bypass; label a deliberate mutation check separately from
a product behavior RED.

For compatibility fallbacks, invoke the affected public behavior after denial. Checking an admission
flag alone misses emitted code that still changes native results. Reproduce review findings against
the actual prior implementation, then apply the minimum correction; compiler/setup failures are not
behavior REDs. This complements vertical TDD rather than requiring an exhaustive matrix in advance.

**Implementation-detail tests**: Coupled to internal structure.

```typescript
// BAD: Tests implementation details
test("checkout calls paymentService.process", async () => {
  const mockPayment = jest.mock(paymentService);
  await checkout(cart, payment);
  expect(mockPayment.process).toHaveBeenCalledWith(cart.total);
});
```

Red flags:

- Mocking internal collaborators
- Testing private methods
- Asserting on call counts/order
- Test breaks when refactoring without behavior change
- Test name describes HOW not WHAT
- Verifying through external means instead of interface

```typescript
// BAD: Bypasses interface to verify
test("createUser saves to database", async () => {
  await createUser({ name: "Alice" });
  const row = await db.query("SELECT * FROM users WHERE name = ?", ["Alice"]);
  expect(row).toBeDefined();
});

// GOOD: Verifies through interface
test("createUser makes user retrievable", async () => {
  const user = await createUser({ name: "Alice" });
  const retrieved = await getUser(user.id);
  expect(retrieved.name).toBe("Alice");
});
```
