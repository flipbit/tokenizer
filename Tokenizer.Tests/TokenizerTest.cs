using System;
using System.Collections.Generic;
using NUnit.Framework;
using Tokens.Exceptions;

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

        private class Employee
        {
            public string FirstName { get; set; }

            public string MiddleName { get; set; }

            public string LastName { get; set; }

            public DateTime StartDate { get; set; }

            public int Number { get; set; }
        }

        [SetUp]
        public void SetUp()
        {
            tokenizer = new Tokenizer();
        }

        [Test]
        public void TestExtractSingleValue()
        {
            const string pattern = @"First Name: {Employee.FirstName}";
            const string input = @"First Name: Alice";

            var employee = tokenizer.Parse<Employee>(pattern, input);

            Assert.AreEqual("Alice", employee.FirstName);
        }

        [Test]
        public void TestExtractTwoValues()
        {
            const string pattern = @"First Name: {Employee.FirstName}, Last Name: {Employee.LastName}";
            const string input = @"First Name: Alice, Last Name: Smith";

            var employee = tokenizer.Parse<Employee>(pattern, input);

            Assert.AreEqual("Alice", employee.FirstName);
            Assert.AreEqual("Smith", employee.LastName);
        }

        [Test]
        public void TestExtractThreeValues()
        {
            const string pattern = @"First Name: {Employee.FirstName}, Middle Name: {Employee.MiddleName}, Last Name: {Employee.LastName}";
            const string input = @"First Name: Alice, Middle Name: Roberta, Last Name: Smith";

            var employee = tokenizer.Parse<Employee>(pattern, input);

            Assert.AreEqual("Alice", employee.FirstName);
            Assert.AreEqual("Roberta", employee.MiddleName);
            Assert.AreEqual("Smith", employee.LastName);
        }

        [Test]
        public void TestExtractThreeValuesWithLineBreaks()
        {
            const string pattern = @"First Name: {Employee.FirstName}
Middle Name: {Employee.MiddleName}
Last Name: {Employee.LastName}";
            const string input = @"First Name: Alice
Middle Name: Roberta
Last Name: Smith";

            var employee = tokenizer.Parse<Employee>(pattern, input);

            Assert.AreEqual("Alice", employee.FirstName);
            Assert.AreEqual("Roberta", employee.MiddleName);
            Assert.AreEqual("Smith", employee.LastName);
        }

        [Test]
        public void TestExtractValueWithTrailingTemplate()
        {
            const string pattern = @"First Name: {Employee.FirstName}, Role: Programmer";
            const string input = @"First Name: Alice, Role: Programmer";

            var employee = tokenizer.Parse<Employee>(pattern, input);

            Assert.AreEqual("Alice", employee.FirstName);
        }

        [Test]
        public void TestExtractValueWithChangesType()
        {
            const string pattern = @"First Name: {Employee.FirstName}, Number: {Employee.Number}";
            const string input = @"First Name: Bob, Number: 12345";

            var employee = tokenizer.Parse<Employee>(pattern, input);

            Assert.AreEqual("Bob", employee.FirstName);
            Assert.AreEqual(12345, employee.Number);
        }

        [Test]
        public void TestExtractValueWithValidatorWhenValid()
        {
            const string pattern = @"First Name: {Employee.FirstName}, Number: {Employee.Number:IsNumeric}";
            const string input = @"First Name: Bob, Number: 12345";

            var employee = tokenizer.Parse<Employee>(pattern, input);

            Assert.AreEqual("Bob", employee.FirstName);
            Assert.AreEqual(12345, employee.Number);
        }

        [Test]
        public void TestExtractValueWithValidatorWhenInvalid()
        {
            const string pattern = @"First Name: {Employee.FirstName}, Number: {Employee.Number:IsNumeric}";
            const string input = @"First Name: Bob, Number: Not a number";

            var employee = tokenizer.Parse<Employee>(pattern, input);

            Assert.AreEqual("Bob", employee.FirstName);
            Assert.AreEqual(0, employee.Number);
        }

        [Test]
        public void TestExtractValueWithValidatorWhenInvalidPicksNextValidMatch()
        {
            const string pattern = @"First Name: {Employee.FirstName}, Number: {Employee.Number:IsNumeric}";
            const string input = @"First Name: Bob, Number: (not a number), Number: 67890";

            var employee = tokenizer.Parse<Employee>(pattern, input);

            Assert.AreEqual("Bob", employee.FirstName);
            Assert.AreEqual(67890, employee.Number);
        }

        [Test]
        public void TestGetMultipleTokensExtractsDataOnlyOnce()
        {
            const string patternOne = @"{TestClass.Message}
{TestClass.Counter}";
            const string input = @"1234
5678";

            var result = tokenizer.Parse<TestClass>(patternOne, input);

            Assert.AreEqual("1234", result.Message);
            Assert.AreEqual(5678, result.Counter);
        }

        [Test]
        public void TestGetMultipleTokensRespectsTokenOrder()
        {
            const string patternOne = @"{TestClass.Message} 
{TestClass.Counter}";
            const string patternTwo = @"{TestClass.Counter} 
{TestClass.Message}";
            const string input = @"1234 
5678";

            var resultOne = tokenizer.Parse<TestClass>(patternOne, input);
            var resultTwo = tokenizer.Parse<TestClass>(patternTwo, input);

            Assert.AreEqual("1234", resultOne.Message);
            Assert.AreEqual(5678, resultOne.Counter);

            Assert.AreEqual("5678", resultTwo.Message);
            Assert.AreEqual(1234, resultTwo.Counter);
        }

        [Test]
        public void TestParseInputWithMissingOperators()
        {
            const string pattern = "Hello {TestClass.Message:UnknownFunction} World";
            const string input = "Hello ... World";

            Assert.Throws<TokenizerException>(() => tokenizer.Parse<TestClass>(pattern, input));            
        }

        [Test]
        public void TestExtracsWhenNotPresentInInput()
        {
            const string pattern = "Hello {TestClass.Message} World";
            const string input = "Goodbye!";

            var result = tokenizer.Parse<TestClass>(pattern, input);

            Assert.AreEqual(result.Message, null);
        }

        [Test]
        public void TestExtraValuesAppendToList()
        {
            const string pattern = "List: {TestClass.List}";
            const string input = "List: One\r\nList: Two";

            var result = tokenizer.Parse<TestClass>(pattern, input);

            Assert.AreEqual(2, result.List.Count);
            Assert.AreEqual("One", result.List[0]);
            Assert.AreEqual("Two", result.List[1]);            
        }

        [Test]
        public void TestExtracsAppendToNestedList()
        {
            const string pattern = "List: {TestClass.Nested.List}";
            const string input = "List: One\r\nList: Two";

            var result = tokenizer.Parse<TestClass>(pattern, input);

            Assert.AreEqual(2, result.Nested.List.Count);
            Assert.AreEqual("One", result.Nested.List[0]);
            Assert.AreEqual("Two", result.Nested.List[1]);

        }

        [Test]
        public void TestExtracsAppendToNestedListStopsAfterUnMatchedLine()
        {
            const string pattern = "List: #{TestClass.Nested.List}";
            const string input = "List: One\r\nList: Two\r\nBreak\r\nList: Three";

            var result = tokenizer.Parse<TestClass>(pattern, input);

            Assert.AreEqual(2, result.Nested.List.Count);
            Assert.AreEqual("One", result.Nested.List[0]);
            Assert.AreEqual("Two", result.Nested.List[1]);

        }

        [Test]
        public void TestExtracsDoesntThrowErrorWhenOptionsSetToFalse()
        {
            const string pattern = "Hello #{TestClass.MissingPropertyName}";
            const string input = "Hello World";

            tokenizer.Options.ThrowExceptionOnMissingProperty = false;

            var result = tokenizer.Parse<TestClass>(pattern, input);

            Assert.IsNotNull(result);
        }

        [Test]
        public void TestExtracsThrowsAnErrorWhenOptionsSetToTrue()
        {
            const string pattern = "Hello #{TestClass.MissingPropertyName}";
            const string input = "Hello World";

            tokenizer.Options.ThrowExceptionOnMissingProperty = true;

            Assert.Throws<TokenizerException>(() => tokenizer.Parse<TestClass>(pattern, input));
        }

        [Test]
        public void TestExtractMulitplsStopsExtractingOnEmptyLine()
        {
            const string pattern = @"
Name servers:
        #{TestClass.List}

    WHOIS lookup made at 10:35:59 22-Oct-2014";
            const string input = @"
Name servers:
        ns1.rbsov.bbc.co.uk       212.58.241.67
        ns1.tcams.bbc.co.uk       212.72.49.3
        ns1.thdow.bbc.co.uk       212.58.240.163

    WHOIS lookup made at 10:35:59 22-Oct-2014";

            var result = tokenizer.Parse<TestClass>(pattern, input);

            Assert.AreEqual(3, result.List.Count);
        }
    }
}

