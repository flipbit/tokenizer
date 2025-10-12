using System;
using Xunit;
using Tokens.Enumerators;
using Tokens.Validators;

namespace Tokens;

public class TokenTests
{
    private readonly Token token = new("Test");

    public class Person
    {
        public string Name { get; set; }

        public int Age { get; set; }

        public DateTime Birthday { get; set; }
    }

    [Fact]
    public void TestSetTokenValue()
    {
        var person = new Person();

        token.Name = "Person.Name";

        var assigned = token.Assign(person, "Sue", TokenizerOptions.Defaults, new FileLocation(), out var value);

        Assert.True(assigned);
        Assert.Equal("Sue", person.Name);
    }

    [Fact]
    public void TestSetTokenValueWithValidator()
    {
        var person = new Person();

        token.Name = "Person.Age";
        token.Decorators.Add(new TokenDecoratorContext(typeof(IsNumericValidator)));

        var assigned = token.Assign(person, "20", TokenizerOptions.Defaults, new FileLocation(), out var value);

        Assert.True(assigned);
        Assert.Equal(20, person.Age);
    }

    [Fact]
    public void TestSetTokenValueWithValidatorWhenInvalid()
    {
        var person = new Person();

        token.Name = "Person.Age";
        token.Decorators.Add(new TokenDecoratorContext(typeof(IsNumericValidator)));

        var assigned = token.Assign(person, "Twenty", TokenizerOptions.Defaults, new FileLocation(), out var value);

        Assert.False(assigned);
        Assert.Equal(0, person.Age);
    }

    [Fact]
    public void TestSetTokenValueWhenNull()
    {
        var person = new Person();

        token.Name = "Person.Name";

        var assigned = token.Assign(person, "Sue", TokenizerOptions.Defaults, new FileLocation(), out var value);

        Assert.True(assigned);
        Assert.Equal("Sue", person.Name);
    }
}