# Hint Tests (3)

Fixture: `HintTests.cs`

## 34. Required hint present

- **Template:** Template with required hint `"Invoice"`
- **Input:** `Invoice #1234\nAmount: $50.00`
- **Expected:** HintMatched event. Normal tokenization proceeds. No HintMissing issue.

## 35. Required hint missing

- **Template:** Template with required hint `"Invoice"`
- **Input:** `Receipt #1234\nAmount: $50.00`
- **Expected:** HintMissing event. Tokenization skipped. Issue: HintMissing with description naming the missing hint. All tokens missed.

## 36. Hint case mismatch

- **Template:** Template with required hint `"Invoice"`
- **Input:** `invoice #1234\nAmount: $50.00`
- **Expected:** Document actual behaviour. If case-sensitive: HintMissing. If case-insensitive: HintMatched. Verify diagnostics accurately reflect what happened.
