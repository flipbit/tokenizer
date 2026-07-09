# Diagnostic Output Format Tests (6)

Fixture: `DiagnosticOutputFormatTests.cs`

## 56. RenderAlignment for clean match

- **Template:** `Name: {Name}\nAge: {Age}`
- **Input:** `Name: Alice\nAge: 30`
- **Expected:** Rendered output contains "Matched Tokens" section with both tokens. No "Failures" or "Unmatched Tokens" sections. Summary line shows "Matched: 2 | Missed: 0 | Failures: 0".

## 57. RenderAlignment for mixed results

- **Template:** `Name: {Name}\nEmail: {Email:IsEmail}\nAge: {Age}`
- **Input:** `Name: Alice\nEmail: notvalid\nAge: 30`
- **Expected:** Rendered output contains all three sections: Matched (Name, Age), Failures (Email — validator rejection), Unmatched (if Email ends up missed). Verify sections are populated correctly.

## 58. RenderAlignment for validator rejection — says "validator rejected"

**Verifies the Phase 2 bug fix.**

- **Template:** `Email: {Email:IsEmail}`
- **Input:** `Email: bad`
- **Expected (current, buggy):** Unmatched section says "preamble never found".
- **Expected (after Phase 2):** Failures section says "ValidatorFailed" or similar. Unmatched section either absent or does NOT say "preamble never found" for this token.

## 59. Verdict string for full match

- **Template:** `Name: {Name}\nAge: {Age}`
- **Input:** `Name: Alice\nAge: 30`
- **Expected:** Verdict is `"Matched 2 of 2 tokens."` (no "missed" clause).

## 60. Verdict string for partial match

- **Template:** `A: {A}\nB: {B}\nC: {C}`
- **Input:** `A: one\nC: three`
- **Expected:** Verdict is `"Matched 2 of 3 tokens (1 missed)."`.

## 61. Verdict string for zero matches

- **Template:** `A: {A}\nB: {B}`
- **Input:** `nothing`
- **Expected:** Verdict is `"Matched 0 of 2 tokens (2 missed)."`.
