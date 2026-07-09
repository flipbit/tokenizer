# Edge Case Tests (9)

Fixture: `EdgeCaseTests.cs`

## 44. Empty input

- **Template:** `Name: {Name}`
- **Input:** `""`
- **Expected:** Token `Name` missed. Issue: PreambleNeverFound.

## 45. Whitespace-only input

- **Template:** `Name: {Name}`
- **Input:** `"   \n  "`
- **Expected:** Token `Name` missed. Issue: PreambleNeverFound.

## 46. Single character input

- **Template:** `Name: {Name}`
- **Input:** `"X"`
- **Expected:** Token `Name` missed. Issue: PreambleNeverFound.

## 47. Very long value

- **Template:** `Name: {Name}`
- **Input:** `Name: ` followed by 10,000 character string
- **Expected:** Token `Name` matched with the full long value. No issues. Diagnostics don't truncate or corrupt the value.

## 48. Value contains preamble text of another token

- **Template:** `Name: {Name}\nAge: {Age}`
- **Input:** `Name: Age: 30\nAge: 25`
- **Expected:** Document actual behaviour. Does `Name` consume `"Age: 30"` and `Age` get `"25"`? Or does the engine handle this differently? The diagnostic should accurately reflect whatever the engine does.

## 49. Unicode in preamble and value

- **Template:** `Nom: {Name}`
- **Input:** `Nom: Jose\u0301` (Jose with combining acute accent)
- **Expected:** Token `Name` matched with unicode value. Diagnostics handle unicode correctly in events, issues, and rendered output.

## 50. Newline-terminated token

- **Template:** Template using newline-terminated token syntax
- **Input:** Multi-line input where value ends at newline
- **Expected:** NewlineTerminatedTokenProcessed event in raw events. Token matched with value up to the newline.

## 51. Single-use token fails and is removed

- **Template:** Template with a single-use token (non-repeating, fails to match)
- **Input:** Input where the single-use token's preamble is found but value is rejected
- **Expected:** SingleUseTokenRemoved event. Token missed. Document what issue type is raised.

## 52. Optional token not present — no issue raised

- **Template:** Template with an optional token
- **Input:** Input without the optional token's content
- **Expected:** Optional token is not matched but no issue is raised (it's optional). Verdict counts only required tokens. No PreambleNeverFound for the optional token.
