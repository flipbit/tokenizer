using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Tokens
{
    [TestFixture]
    public class TokenizerTest
    {
        private Tokenizer tokenizer;

        private class TestClass
        {
            public string Message { get; set; }

            public string Name { get; set; }

            public int Counter { get; set; }

            public IList<string> List { get; set; }

            public TestClass Nested { get; set; }
        }

        [SetUp]
        public void SetUp()
        {
            tokenizer = new Tokenizer();
        }

        [Test]
        public void TestGetTokenFromMiddleOfString()
        {
            const string pattern = "test #{TestClass.Message:ToUpper()} string";

            var token = tokenizer.GetNextToken(pattern, string.Empty);

            Assert.AreEqual("test ", token.Prefix);
            Assert.AreEqual("TestClass.Message", token.Value);
            Assert.AreEqual("ToUpper()", token.Operation);
            Assert.AreEqual(" string", token.Suffix);
        }

        [Test]
        public void TestGetTokenFromStartOfString()
        {
            const string pattern = "#{TestClass.Message} test string";

            var token = tokenizer.GetNextToken(pattern, string.Empty);

            Assert.AreEqual(string.Empty, token.Prefix);
            Assert.AreEqual("TestClass.Message", token.Value);
            Assert.AreEqual(" test string", token.Suffix);
        }

        [Test]
        public void TestGetTokenFromEndOfString()
        {
            const string pattern = " test string #{TestClass.Message}";

            var token = tokenizer.GetNextToken(pattern, string.Empty);

            Assert.AreEqual(" test string ", token.Prefix);
            Assert.AreEqual("TestClass.Message", token.Value);
            Assert.AreEqual(string.Empty, token.Suffix);
        }

        [Test]
        public void TestGetMulitipleTokens()
        {
            const string pattern = "begining #{TestClass.Message} middle #{TestClass.Counter} end";

            var token = tokenizer.GetTokens(pattern);

            Assert.AreEqual(2, token.Count);
            Assert.AreEqual("begining ", token[0].Prefix);
            Assert.AreEqual("TestClass.Message", token[0].Value);
            Assert.AreEqual(" middle ", token[0].Suffix);
            Assert.AreEqual(" middle ", token[1].Prefix);
            Assert.AreEqual("TestClass.Counter", token[1].Value);
            Assert.AreEqual(" end", token[1].Suffix);
        }

        [Test]
        public void TestGetMulitipleTokensWithMulitpleLines()
        {
            const string pattern = @"
begining #{TestClass.Message} 
middle #{TestClass.Counter} 
end #{TestClass.Nested.Counter}";

            var token = tokenizer.GetTokens(pattern);

            Assert.AreEqual(3, token.Count);
            Assert.AreEqual("begining ", token[0].Prefix);
            Assert.AreEqual("TestClass.Message", token[0].Value);
            Assert.AreEqual("", token[0].Suffix);
            Assert.AreEqual("middle ", token[1].Prefix);
            Assert.AreEqual("TestClass.Counter", token[1].Value);
            Assert.AreEqual("", token[1].Suffix);
            Assert.AreEqual("end ", token[2].Prefix);
            Assert.AreEqual("TestClass.Nested.Counter", token[2].Value);
            Assert.AreEqual("", token[2].Suffix);
        }

        [Test]
        public void TestGetTokensWithPrerequisite()
        {
            const string pattern = "First Line\r\nbegining #{TestClass.Message} end";

            var token = tokenizer.GetTokens(pattern);

            Assert.AreEqual(1, token.Count);
            Assert.AreEqual("begining ", token[0].Prefix);
            Assert.AreEqual("TestClass.Message", token[0].Value);
            Assert.AreEqual(" end", token[0].Suffix);
            Assert.AreEqual("First Line", token[0].Prerequisite);
        }

        [Test]
        public void TestGetMulitipleTokensWithoutPrerequisites()
        {
            const string pattern = "First #{TestClass.Counter} Line\r\nbegining #{TestClass.Message} end";

            var token = tokenizer.GetTokens(pattern);

            Assert.AreEqual(2, token.Count);
            Assert.AreEqual("First ", token[0].Prefix);
            Assert.AreEqual("TestClass.Counter", token[0].Value);
            Assert.AreEqual(" Line", token[0].Suffix);
            Assert.AreEqual("", token[0].Prerequisite);
            Assert.AreEqual("begining ", token[1].Prefix);
            Assert.AreEqual("TestClass.Message", token[1].Value);
            Assert.AreEqual(" end", token[1].Suffix);
            Assert.AreEqual("", token[1].Prerequisite);
        }

        [Test]
        public void TestGetMultipleTokensExtractsDataOnlyOnce()
        {
            const string patternOne = @"#{TestClass.Message}
#{TestClass.Counter}";
            const string input = @"1234
5678";

            var result = tokenizer.Parse<TestClass>(patternOne, input);

            Assert.AreEqual("1234", result.Value.Message);
            Assert.AreEqual(5678, result.Value.Counter);
        }

        [Test]
        public void TestGetMultipleTokensRespectsTokenOrder()
        {
            const string patternOne = @"#{TestClass.Message} 
#{TestClass.Counter}";
            const string patternTwo = @"#{TestClass.Counter} 
#{TestClass.Message}";
            const string input = @"1234 
5678";

            var resultOne = tokenizer.Parse<TestClass>(patternOne, input);
            var resultTwo = tokenizer.Parse<TestClass>(patternTwo, input);

            Assert.AreEqual("1234", resultOne.Value.Message);
            Assert.AreEqual(5678, resultOne.Value.Counter);

            Assert.AreEqual("5678", resultTwo.Value.Message);
            Assert.AreEqual(1234, resultTwo.Value.Counter);
        }

        [Test]
        public void TestGetMultipleTokensSkipsMissingTokens()
        {
            const string patternOne = @"#{TestClass.Message} 
#{TestClass.Counter}";
            const string patternTwo = @"#{TestClass.Counter} 
#{TestClass.Message}";
            const string input = @"1234 
5678";

            var resultOne = tokenizer.Parse<TestClass>(patternOne, input);
            var resultTwo = tokenizer.Parse<TestClass>(patternTwo, input);

            Assert.AreEqual("1234", resultOne.Value.Message);
            Assert.AreEqual(5678, resultOne.Value.Counter);

            Assert.AreEqual("5678", resultTwo.Value.Message);
            Assert.AreEqual(1234, resultTwo.Value.Counter);
        }

        [Test]
        public void TestExtractString()
        {
            const string input = "test hello world string";
            const string pattern = "test #{TestClass.Message} string";

            var result = tokenizer.Parse<TestClass>(pattern, input);

            Assert.AreEqual(1, result.Replacements.Count);
            Assert.AreEqual("TestClass.Message", result.Replacements[0].Value);
            Assert.AreEqual(typeof(TestClass), result.Value.GetType());
            Assert.AreEqual("hello world", result.Value.Message);
        }

        [Test]
        [Ignore("Ignored for now..")]
        public void TestExtractMultipleStringOnSameLine()
        {
            const string input = "test hello world string bob end";
            const string pattern = "test #{TestClass.Message} string #{TestClass.Name} end";

            var result = tokenizer.Parse<TestClass>(pattern, input);

            Assert.AreEqual(2, result.Replacements.Count);
            Assert.AreEqual("TestClass.Message", result.Replacements[0].Value);
            Assert.AreEqual("TestClass.Name", result.Replacements[1].Value);
            Assert.AreEqual(typeof(TestClass), result.Value.GetType());
            Assert.AreEqual("hello world", result.Value.Message);
            Assert.AreEqual("bob", result.Value.Name);
        }

        [Test]
        public void TestExtractInteger()
        {
            const string input = "test 1234 string";
            const string pattern = "test #{TestClass.Counter} string";

            var result = tokenizer.Parse<TestClass>(pattern, input);

            Assert.AreEqual(1234, result.Value.Counter);
        }

        [Test]
        public void TestExtractNestedValue()
        {
            const string input = "test 1234 string";
            const string pattern = "test #{TestClass.Nested.Counter} string";

            var result = tokenizer.Parse<TestClass>(pattern, input);

            Assert.AreEqual(1234, result.Value.Nested.Counter);
        }

        [Test]
        public void TestExtractMultipleStrings()
        {
            const string input = "test hello world string";
            const string pattern = "test #{TestClass.Message} string";

            var result = tokenizer.Parse<TestClass>(pattern, input);

            Assert.AreEqual("hello world", result.Value.Message);
        }

        [Test]
        public void TestSetValueString()
        {
            var result = tokenizer.SetValue(new TestClass(), "TestClass.Message", "It Worked");

            Assert.AreEqual("It Worked", result.Message);
        }

        [Test]
        public void TestSetValueInteger()
        {
            var result = tokenizer.SetValue(new TestClass(), "TestClass.Counter", 1234);

            Assert.AreEqual(1234, result.Counter);
        }

        [Test]
        public void TestSetValueWhenPathTooShort()
        {
            Assert.Throws<ArgumentException>(() => tokenizer.SetValue(new TestClass(), "TestClass", 1234));
        }

        [Test]
        public void TestSetValueWhenPathOfIncorrectBaseClassType()
        {
            Assert.Throws<ArgumentException>(() => tokenizer.SetValue(new TestClass(), "WrongTestClass.Message", 1234));
        }

        [Test]
        public void TestSetValueIntegerToString()
        {
            var result = tokenizer.SetValue(new TestClass(), "TestClass.Message", 1234);

            Assert.AreEqual("1234", result.Message);
        }

        [Test]
        public void TestSetValueStringToInteger()
        {
            var result = tokenizer.SetValue(new TestClass(), "TestClass.Counter", "1234");

            Assert.AreEqual(1234, result.Counter);
        }

        [Test]
        public void TestSetValuePreservesExistingValues()
        {
            var input = new TestClass { Message = "Existing Message" };

            var result = tokenizer.SetValue(input, "TestClass.Counter", "1234");

            Assert.AreEqual(input, result);
            Assert.AreEqual(1234, result.Counter);
            Assert.AreEqual("Existing Message", result.Message);
        }

        [Test]
        public void TestSetNestedValueString()
        {
            var result = tokenizer.SetValue(new TestClass(), "TestClass.Nested.Message", "Nested Hello");

            Assert.AreEqual("Nested Hello", result.Nested.Message);
        }

        [Test]
        public void TestSetListValueString()
        {
            var result = tokenizer.SetValue(new TestClass(), "TestClass.List", "First");

            Assert.AreEqual(1, result.List.Count);
            Assert.AreEqual("First", result.List[0]);
        }

        [Test]
        public void TestSetMultipleListValueString()
        {
            var testClass = new TestClass();

            tokenizer.SetValue(testClass, "TestClass.List", "First");
            tokenizer.SetValue(testClass, "TestClass.List", "Second");
            tokenizer.SetValue(testClass, "TestClass.List", "Third");

            Assert.AreEqual(3, testClass.List.Count);
            Assert.AreEqual("First", testClass.List[0]);
            Assert.AreEqual("Second", testClass.List[1]);
            Assert.AreEqual("Third", testClass.List[2]);
        }

        [Test]
        public void TestMultipleLinePatterns()
        {
            const string pattern = "Hello #{TestClass.Message} World\r\nGoodbye #{TestClass.Counter} Everyone\r\n";
            const string input = "Hello 'They Said It Here!' World\nGoodbye 123456 Everyone";

            var result = tokenizer.Parse(new TestClass(), pattern, input);

            Assert.AreEqual(result.Value.Message, "'They Said It Here!'");
            Assert.AreEqual(result.Value.Counter, 123456);
        }

        [Test]
        public void TestValidateInputWhenValid()
        {
            const string pattern = "Hello #{TestClass.Message:IsNumeric()} Numbers";
            const string input = "Hello 123456.7 Numbers";

            var result = tokenizer.Parse(new TestClass(), pattern, input);

            Assert.AreEqual(result.Value.Message, "123456.7");
        }

        [Test]
        public void TestValidateInputWhenInvalid()
        {
            const string pattern = "Hello #{TestClass.Message:IsNumeric()} Numbers";
            const string input = "Hello World Not Numbers";

            var result = tokenizer.Parse(new TestClass(), pattern, input);

            Assert.AreEqual(null, result.Value.Message);
        }

        [Test]
        public void TestValidateInputWhenInvalidPicksNextValue()
        {
            const string pattern = "Hello #{TestClass.Message:IsNumeric()} Numbers";
            const string input = "Hello World Not Numbers\r\nHello 12345678.9 Numbers";

            var result = tokenizer.Parse(new TestClass(), pattern, input);

            Assert.AreEqual(result.Value.Message, "12345678.9");
        }

        [Test]
        public void TestExtractValueMustSeePrequisite()
        {
            const string pattern = "Hello First Line\r\nHello #{TestClass.Message} Line";
            const string input = "Hello First Line\r\nHello Second Line";

            var result = tokenizer.Parse(new TestClass(), pattern, input);

            Assert.AreEqual(result.Value.Message, "Second");
        }

        [Test]
        public void TestParseInputWithMissingOperators()
        {
            const string pattern = "Hello #{TestClass.Message:ToFabulous()} World";
            const string input = "Hello ... World";

            Assert.Throws<ArgumentException>(() => tokenizer.Parse(new TestClass(), pattern, input));            
        }

        [Test]
        public void TestExtractValuesWhenNotPresentInInput()
        {
            const string pattern = "Hello #{TestClass.Message} World";
            const string input = "Goodbye!";

            var result = tokenizer.Parse(new TestClass(), pattern, input);

            Assert.AreEqual(result.Value.Message, null);
        }
    }
}
