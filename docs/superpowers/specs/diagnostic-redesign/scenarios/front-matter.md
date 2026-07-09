# Front Matter Tests (2)

Fixture: `FrontMatterTests.cs`

## 37. Front matter token matched

- **Template:** Template with front matter token definition
- **Input:** Input containing expected front matter section
- **Expected:** FrontMatterTokenAssigned event. Token matched with front matter value. No issues.

## 38. Front matter token failed

- **Template:** Template with front matter token definition
- **Input:** Input without expected front matter section
- **Expected:** FrontMatterTokenFailed event. Token missed. Appropriate issue raised.
