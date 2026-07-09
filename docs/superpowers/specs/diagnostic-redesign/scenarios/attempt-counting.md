# Attempt Counting Tests (3)

Fixture: `AttemptCountingTests.cs`

These tests validate the token consideration/rejection history that becomes the primary diagnostic view in Phase 4 (TokenDiagnostic.Attempts). In Phase 0 they document the raw events that will later be aggregated.

## 53. Token considered 3 times, rejected twice, matched once

- **Template:** `Email: {Email:IsEmail}`
- **Input:** `Email: bad1\nEmail: bad2\nEmail: a@b.com`
- **Expected:** Token `Email` matched with value `"a@b.com"`. Raw events show: PreambleMatched (x3), ValidatorFailed (x2 — for "bad1" and "bad2"), ValidatorPassed (x1), TokenAssigned (x1). In Phase 4: 3 attempts — 2 rejected, 1 assigned.

## 54. Token considered multiple times, never matched

- **Template:** `Email: {Email:IsEmail}`
- **Input:** `Email: x\nEmail: y\nEmail: z`
- **Expected:** Token `Email` missed. Raw events show: PreambleMatched (x3), ValidatorFailed (x3). Issue: ValidatorRejection (NOT PreambleNeverFound). In Phase 4: 3 attempts, all rejected.

## 55. Token with multiple candidates at same position

- **Template:** Template where multiple tokens share the same preamble or the engine considers multiple candidates at one position
- **Input:** Input matching the shared preamble
- **Expected:** Document which candidate is tried first, whether all are tried, and what events are recorded for each. TokenAssignmentAttempted should list all candidate names.
