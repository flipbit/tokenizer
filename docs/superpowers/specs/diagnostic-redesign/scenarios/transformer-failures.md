# Transformer Failure Tests (6)

Fixture: `TransformerFailureTests.cs`

## 23. ToDateTime with wrong format

- **Template:** `Date: {Date:ToDateTime(yyyy-MM-dd)}`
- **Input:** `Date: 13/01/2024`
- **Expected:** Token `Date` missed. Issue: TransformerFailure. Hint suggests a matching date format (e.g. "dd/MM/yyyy").

## 24. ToDateTime with correct format

- **Template:** `Date: {Date:ToDateTime(yyyy-MM-dd)}`
- **Input:** `Date: 2024-01-13`
- **Expected:** Token `Date` matched. Events include TransformerSucceeded. No issues.

## 25. ToDateTime — hint suggests matching format

- **Template:** `Date: {Date:ToDateTime(yyyy-MM-dd)}`
- **Input:** `Date: 01/13/2024`
- **Expected:** Token `Date` missed. Issue: TransformerFailure. DateFormatHintGenerator suggests the format that would match (e.g. "MM/dd/yyyy"). Verify the hint is actionable.

## 26. Transformer fails but preamble was found — must NOT say preamble not found

**Same class of bug as test #18, but for transformers.**

- **Template:** `Date: {Date:ToDateTime(yyyy-MM-dd)}`
- **Input:** `Date: not-a-date`
- **Expected:**
  - Issue type: TransformerFailure (NOT PreambleNeverFound)
  - RenderAlignment output says "transformer failed" (NOT "preamble never found")
  - Events contain PreambleMatched AND TransformerFailed

## 27. Chained transformer + validator — transformer succeeds, validator fails

- **Template:** Token with a transformer followed by a validator, where the transformer succeeds but the validator rejects the transformed value
- **Input:** Value that transforms successfully but fails validation
- **Expected:** Events show TransformerSucceeded then ValidatorFailed. Issue: ValidatorRejection (not TransformerFailure). The transformer did its job.

## 28. Chained transformers — first succeeds, second fails

- **Template:** Token with two transformers in sequence
- **Input:** Value that the first transformer handles but the second cannot
- **Expected:** Events show TransformerSucceeded for first, TransformerFailed for second. Issue: TransformerFailure naming the second transformer.
