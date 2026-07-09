# Multi-Token Interaction Tests (5)

Fixture: `MultiTokenInteractionTests.cs`

## 39. First token fails, second would match

- **Template:** `A: {A}\nB: {B}`
- **Input:** `B: hello`
- **Expected:** Token `A` missed (PreambleNeverFound). Token `B` — document whether it matches or is blocked by `A`'s failure. This test establishes baseline behaviour for Phase 6 (causality chains).

## 40. First token's validator fails, second token matches

- **Template:** `Email: {Email:IsEmail}\nName: {Name}`
- **Input:** `Email: Alice\nName: Bob`
- **Expected:** Token `Email` missed (ValidatorRejection — "Alice" is not a valid email). Token `Name` matched with value `"Bob"`. Verify that Email's failure doesn't prevent Name from matching.

## 41. All tokens fail

- **Template:** `A: {A}\nB: {B}\nC: {C}`
- **Input:** `completely unrelated text`
- **Expected:** All 3 tokens missed. 3 PreambleNeverFound issues. Verdict: "Matched 0 of 3 tokens (3 missed)."

## 42. Middle token fails, others match

- **Template:** `A: {A}\nB: {B}\nC: {C}`
- **Input:** `A: one\nC: three`
- **Expected:** Token `A` matched. Token `B` missed (PreambleNeverFound). Token `C` matched. Verdict: "Matched 2 of 3 tokens (1 missed)."

## 43. Token matched after backtracking

- **Template:** Template where the preamble text appears as a false positive earlier in the input, then as the real match later
- **Input:** Input with preamble text in a non-matching context first, then in the correct context
- **Expected:** BacktrackStarted event visible in raw events. Token eventually assigned. Document the backtrack count and positions.
