# Validator Rejection Tests (10)

Fixture: `ValidatorRejectionTests.cs`

## 13. IsEmail rejects invalid value

- **Template:** `Email: {Email:IsEmail}`
- **Input:** `Email: notanemail`
- **Expected:** Token `Email` missed. Issue: ValidatorRejection with hint about missing `@`. Events include ValidatorFailed with DecoratorName "IsEmail".

## 14. IsEmail accepts valid value

- **Template:** `Email: {Email:IsEmail}`
- **Input:** `Email: user@example.com`
- **Expected:** Token `Email` matched. Events include ValidatorPassed. No issues.

## 15. IsNumeric rejects text

- **Template:** `Count: {Count:IsNumeric}`
- **Input:** `Count: twelve`
- **Expected:** Token `Count` missed. Issue: ValidatorRejection with DecoratorName "IsNumeric".

## 16. IsPhoneNumber rejects gibberish

- **Template:** `Phone: {Phone:IsPhoneNumber}`
- **Input:** `Phone: abc123`
- **Expected:** Token `Phone` missed. Issue: ValidatorRejection with DecoratorName "IsPhoneNumber".

## 17. IsDomainName rejects invalid

- **Template:** `Host: {Host:IsDomainName}`
- **Input:** `Host: not a domain`
- **Expected:** Token `Host` missed. Issue: ValidatorRejection with DecoratorName "IsDomainName".

## 18. Validator rejects but preamble was found — must NOT say "preamble never found"

**This is the key bug case.** The preamble `"Email: "` IS found in the input. The validator rejected the value. The diagnostic MUST report ValidatorRejection, NOT PreambleNeverFound.

- **Template:** `Email: {Email:IsEmail}`
- **Input:** `Email: bad`
- **Expected:**
  - Issue type: ValidatorRejection (NOT PreambleNeverFound)
  - RenderAlignment output says "validator rejected" (NOT "preamble never found")
  - Events contain PreambleMatched AND ValidatorFailed

## 19. Multiple validators on one token — first passes, second rejects

- **Template:** Token with two validators where value passes first but fails second
- **Input:** Value that satisfies first validator but not second
- **Expected:** ValidatorFailed event for the second validator. Issue: ValidatorRejection naming the second validator. First validator's ValidatorPassed event also present.

## 20. Same token (repeating) rejected at some occurrences, accepted at others

- **Template:** `Item: {Item*:IsNumeric}`
- **Input:** `Item: 1\nItem: two\nItem: 3`
- **Expected:** Token matched with values `["1", "3"]` (or `["1"]` if repeating stops at first failure). Events show ValidatorPassed for "1", ValidatorFailed for "two", and (if engine continues) ValidatorPassed for "3". Document actual behaviour.

## 21. Validator rejects every occurrence — token ends up missed

- **Template:** `Email: {Email:IsEmail}`
- **Input:** `Email: bad1\nEmail: bad2`
- **Expected:** Token `Email` missed. Multiple ValidatorFailed events visible. Issue: ValidatorRejection (NOT PreambleNeverFound — preamble was found twice).

## 22. Validator rejects with null/empty value

- **Template:** `Name: {Name:IsEmail}`
- **Input:** `Name: `
- **Expected:** Token `Name` missed. ValidatorFailed event with empty/null value. Issue: ValidatorRejection.
