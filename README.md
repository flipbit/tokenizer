 
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

Code:

    var tokenizer = new Tokenizer();

    var letter = tokenizer.Parse<Letter>(pattern, input);

JSON Output:

    {
        "letter": {
            "Date": "2014-01-14",
            "To": "Alice",
            "Subject": "the meeting next week",
            "From": "Bob"
        }
    }


