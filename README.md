 
# .NET Tokenizer

.NET Tokenizer is a library written in C# that extracts values from text.  By overlaying patterns on blocks of text, the library can create structured objects.  Object propeties are populated with data extracted from placeholder tokens within the patterns.

##Installation

Installation:

##Basic Example

Pattern:

                                                        Date: ${Letter.Date}
    Dear #{Letter.To},

    I am writing to you regarding #{Letter.Subject}.

    From,

    #{Letter.From}

Input Text:

                                                        Date: Jan 14 2014
    Dear Alice,

    I am writing to you regarding the meeting next week.

    From,

    Bob

Example code:

    var tokenizer = new Tokenizer();

    var letter = tokenizer.Parse<Letter>(pattern, input);

    Assert.AreEqual(new DateTime(2014, 1, 14), letter.Date);
    Assert.AreEqual("Alice", letter.To);
    Assert.AreEqual("the meeting next week", letter.Subject);
    Assert.AreEqual("Bob", letter.From);

