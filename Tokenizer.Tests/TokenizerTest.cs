using System;
using System.Collections.Generic;
using NUnit.Framework;
using Tokens.Exceptions;
using Tokens.Parsers;

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

        private class Manager : Employee
        {
            public List<string> Manages { get; set; }
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
        public void TestExtractOptionalValueWhenNotPresent()
        {
            const string pattern = @"First Name: {Employee.FirstName}, Middle Name: {Employee.MiddleName?}, Last Name: {Employee.LastName}";
            const string input = @"First Name: Bob, Last Name: Smith";

            var employee = tokenizer.Parse<Employee>(pattern, input);

            Assert.AreEqual("Bob", employee.FirstName);
            Assert.AreEqual("Smith", employee.LastName);
        }

        [Test]
        public void TestExtractOptionalValueWhenPresent()
        {
            const string pattern = @"First Name: {Employee.FirstName}, Middle Name: {Employee.MiddleName?}, Last Name: {Employee.LastName}";
            const string input = @"First Name: Bob, Middle Name: Charles, Last Name: Smith";

            var employee = tokenizer.Parse<Employee>(pattern, input);

            Assert.AreEqual("Bob", employee.FirstName);
            Assert.AreEqual("Charles", employee.MiddleName);
            Assert.AreEqual("Smith", employee.LastName);
        }

        [Test]
        public void TestExtractWhenOperatorMissing()
        {
            const string pattern = "Hello {Employee.FirstName:UnknownFunction} World";
            const string input = "Hello ... World";

            Assert.Throws<TokenizerException>(() => tokenizer.Parse<Employee>(pattern, input));            
        }

        [Test]
        public void TestExtractWhenNotPresentInInput()
        {
            const string pattern = "First Name: {Employee.FirstName}";
            const string input = "David";

            var result = tokenizer.Parse<Employee>(pattern, input);

            Assert.AreEqual(result.FirstName, null);
        }

        [Test]
        public void TestExtractListValues()
        {
            const string pattern = "Employee: {Manager.Manages#$}";
            const string input = "Employee: Alice\r\nEmployee: Bob";

            var result = tokenizer.Parse<Manager>(pattern, input);

            Assert.AreEqual(2, result.Manages.Count);
            Assert.AreEqual("Alice", result.Manages[0]);
            Assert.AreEqual("Bob", result.Manages[1]);            
        }

        [Test]
        public void TestExtractListValuesOnNewLines()
        {
            const string pattern = "Name: {Manager.FirstName}\r\nEmployee: {Manager.Manages#$}\r\nNumber: {Manager.Number}";
            const string input = "Name: Sue\r\nEmployee: Alice\r\nEmployee: Bob\r\nEmployee: Charles\r\nNumber: 1234";

            var result = tokenizer.Parse<Manager>(pattern, input);

            Assert.AreEqual(3, result.Manages.Count);
            Assert.AreEqual("Alice", result.Manages[0]);
            Assert.AreEqual("Bob", result.Manages[1]);            
        }

        [Test]
        public void TestExtractEmbeddedListValues()
        {
            const string pattern = "Name: {Manager.FirstName}, Manages: {Manager.Manages#$}, Number: {Manager.Number}";
            const string input = "Name: Alice, Manages: Bob, Manages: Sue, Number: 1234";

            var result = tokenizer.Parse<Manager>(pattern, input);

            Assert.AreEqual("Alice", result.FirstName);
            Assert.AreEqual(2, result.Manages.Count);
            Assert.AreEqual("Bob", result.Manages[0]);
            Assert.AreEqual("Sue", result.Manages[1]);            
            Assert.AreEqual(1234, result.Number);
        }

        [Test]
        public void TestExtracsDoesntThrowErrorWhenOptionsSetToFalse()
        {
            const string pattern = "Hello {TestClass.MissingPropertyName}";
            const string input = "Hello World";

            tokenizer.Options.ThrowExceptionOnMissingProperty = false;

            var result = tokenizer.Parse<TestClass>(pattern, input);

            Assert.IsNotNull(result);
        }

        [Test]
        public void TestExtracsThrowsAnErrorWhenOptionsSetToTrue()
        {
            const string pattern = "Hello {TestClass.MissingPropertyName}";
            const string input = "Hello World";

            tokenizer.Options.ThrowExceptionOnMissingProperty = true;

            Assert.Throws<MissingMemberException>(() => tokenizer.Parse<TestClass>(pattern, input));
        }

        [Test]
        public void TestExtractMulitplsStopsExtractingOnEmptyLine()
        {
            const string pattern = @"
Name servers:
        {TestClass.List#}
        {TestClass.List#}

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

        [Test]
        public void TestExtractValueWithMismatchedNewLines()
        {
            const string pattern = "First Name:\n{Employee.FirstName}";
            const string input = "First Name:\r\nAlice";

            var employee = tokenizer.Parse<Employee>(pattern, input);

            Assert.AreEqual("Alice", employee.FirstName);
        }

        [Test]
        public void TestExtractValueWithAnyOrder()
        {
            const string pattern = @"---
OutOfOrder: true
---
First Name: {Employee.FirstName}
Middle Name: {Employee.MiddleName}
Last Name: {Employee.LastName}";
            const string input = @"Last Name: Smith
First Name: Bob
Middle Name: Charles";

            var employee = tokenizer.Parse<Employee>(pattern, input);

            Assert.AreEqual("Bob", employee.FirstName);
            Assert.AreEqual("Charles", employee.MiddleName);
            Assert.AreEqual("Smith", employee.LastName);
        }

        [Test]
        public void TestExtractPatternTwice()
        {
            const string pattern = @"First Name: {Employee.FirstName}, Last Name: {Employee.LastName}";
            const string input = @"First Name: Alice, Last Name: Smith";

            var template = new TokenParser().Parse(pattern);

            var one = tokenizer.Parse<Employee>(template, input);

            Assert.AreEqual("Alice", one.FirstName);
            Assert.AreEqual("Smith", one.LastName);

            var two = tokenizer.Parse<Employee>(template, input);

            Assert.AreEqual("Alice", two.FirstName);
            Assert.AreEqual("Smith", two.LastName);
        }
    }
}

