# Repeating Token Tests (5)

Fixture: `RepeatingTokenTests.cs`

## 29. Repeating token — all match

- **Template:** `Item: {Item*}`
- **Input:** `Item: A\nItem: B\nItem: C`
- **Expected:** Token `Item` matched 3 times with values `["A", "B", "C"]`. No issues.

## 30. Repeating token cut short by validator

- **Template:** `Item: {Item*:IsNumeric}`
- **Input:** `Item: 1\nItem: two\nItem: 3`
- **Expected:** Issue: RepeatingTokenCutShort. RepeatingTokenHintGenerator explains which repetition failed and why. Document whether "3" is captured (does the engine retry after a failed repetition?).

## 31. Repeating token cut short by line gap

- **Template:** `Item: {Item*}`
- **Input:** `Item: A\n\n\nItem: B`
- **Expected:** Document actual behaviour. If line gap detection disables the repeating token: Issue: RepeatingTokenCutShort. If not: both matched.

## 32. Repeating token — zero matches (preamble never found)

- **Template:** `Item: {Item*}`
- **Input:** `Nothing here`
- **Expected:** Token `Item` missed. Issue: PreambleNeverFound. No RepeatingTokenCutShort (it was never started).

## 33. Repeating token — one match then disabled

- **Template:** `Item: {Item*:IsNumeric}`
- **Input:** `Item: 1\nItem: nope`
- **Expected:** Token `Item` matched once with value `["1"]`. Issue: RepeatingTokenCutShort explaining that the second repetition was rejected by IsNumeric.
