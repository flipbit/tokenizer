## .NET Tokenizer
Build Status: [![Build status](https://ci.appveyor.com/api/projects/status/0wfoj83bi9x8cvpx?svg=true)](https://ci.appveyor.com/project/flipbit/tokenizer)

.NET Tokenizer is a library written in C# that extracts values from text.  By overlaying patterns on blocks of text, the library can create structured objects.  Object propeties are populated with data extracted from placeholder tokens within the patterns.

##Installation

To install into your project, enter the following at the Package Manager prompt in Visual Studio:

    Install-Package Tokenizer

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

