# Preamble Matching Tests (12)

Fixture: `PreambleMatchingTests.cs`

## 1. Simple match — happy path

- **Template:** `Name: {Name}`
- **Input:** `Name: Alice`
- **Expected:** Token `Name` matched with value `"Alice"`. No issues. Verdict: "Matched 1 of 1 tokens."

## 2. All tokens match

- **Template:** `Name: {Name}\nAge: {Age}`
- **Input:** `Name: Alice\nAge: 30`
- **Expected:** Both tokens matched. No issues. Verdict: "Matched 2 of 2 tokens."

## 3. Preamble not found at all

- **Template:** `Name: {Name}`
- **Input:** `Foo: Alice`
- **Expected:** Token `Name` missed. Issue: PreambleNeverFound. Verdict: "Matched 0 of 1 tokens (1 missed)."

## 4. Preamble case mismatch

- **Template:** `Name: {Name}`
- **Input:** `name: Alice`
- **Expected:** Token `Name` missed. Issue: PreambleNeverFound with near-miss hint suggesting case difference.

## 5. Preamble whitespace mismatch

- **Template:** `Name:  {Name}` (2 spaces after colon)
- **Input:** `Name: Alice` (1 space after colon)
- **Expected:** Token `Name` missed. Issue: PreambleNeverFound with near-miss hint suggesting whitespace difference.

## 6. Preamble partial match

- **Template:** `Username: {User}`
- **Input:** `User: Alice`
- **Expected:** Token `User` missed. Issue: PreambleNeverFound. Near-miss hint may suggest "User:" vs "Username:".

## 7. Out-of-order tokens (OutOfOrder disabled)

- **Template:** `A: {A}\nB: {B}`
- **Input:** `B: Two\nA: One`
- **Expected (default ordering):** Depends on engine behaviour — document what happens. Likely B missed (appears before A's preamble position), A matched.

## 8. Out-of-order tokens (OutOfOrder enabled)

- **Template:** `A: {A}\nB: {B}` with OutOfOrder option
- **Input:** `B: Two\nA: One`
- **Expected:** Both tokens matched regardless of input order.

## 9. Multiple tokens sharing same preamble prefix

- **Template:** `Email: {Email}\nEmail Address: {FullEmail}`
- **Input:** `Email: a@b.com`
- **Expected:** Shorter-preamble token matched. Longer-preamble token missed (or vice versa — document actual behaviour).

## 10. Preamble appears multiple times in input

- **Template:** `Name: {Name}` (non-repeating)
- **Input:** `Name: Alice\nName: Bob`
- **Expected:** Token `Name` matched with first occurrence value. Second occurrence ignored.

## 11. Empty preamble (token at start of input)

- **Template:** `{Name} is here`
- **Input:** `Alice is here`
- **Expected:** Token `Name` matched with value `"Alice"`.

## 12. Preamble found but value is empty

- **Template:** `A: {A}\nB: {B}`
- **Input:** `A: \nB: hello`
- **Expected:** Token `A` matched with empty string value. Token `B` matched. Document whether empty value triggers a ValueMismatch issue.
