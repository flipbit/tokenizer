 
## .NET Tokenizer

.NET Tokenizer is a library written in C# that extracts values from text.  The library creates structured objects by overlaying patterns onto blocks of text.

##Installation

Installation:

Enter the following into the Package Manager Console window in Visual Studio:

    Install-Package Tokenizer

##Basic Example

The Tokenizer library was originally developed in order to parse information from WHOIS records.  A typical WHOIS record will contain free-form text such as the example below:

```
Domain Name: LATIMES.COM
Registry Domain ID: 510925_DOMAIN_COM-VRSN
Registrar WHOIS Server: whois.godaddy.com
Registrar URL: http://www.godaddy.com
Update Date: 2013-12-02 10:38:01
Creation Date: 1990-12-12 00:00:00
Registrar Registration Expiration Date: 2015-12-11 00:00:00
Registrar: GoDaddy.com, LLC
Registrar IANA ID: 146
Registrar Abuse Contact Email: abuse@godaddy.com
Registrar Abuse Contact Phone: +1.480-624-2505
Registrant Name: TRIBUNE COMPANY
Registrant Organization: Tribune Technology LLC
Registrant Street: 435 N. Michigan Ave
Registrant City: Chicago
Registrant State/Province: Illinois
Registrant Postal Code: 60611
Registrant Country: United States
Registrant Phone: +1.3122229100
Registrant Email: tis-dnsadmin@tribune.com
```

The Tokenizer will create an object with the information from the text extract and set onto it's properties.  A JSON representation of the information extracted from the above would be:

```json
{
    "WhoisRecord": {
        "DomainName": "LATIMES.COM",
        "RegistryDomainId": "510925_DOMAIN_COM-VRSN",
        "Registrar": {
            "WhoisHostName": "whois.godaddy.com",
            "URL": "http://www.godaddy.com",
            "Name": "GoDaddy.com, LLC",
            "IanaId": "146"                
        },
        "CreatedDate": "1990-12-12",
        "ModifiedDate": "2013-12-03",
        "ExpirationDate": "2015-12-11",
        "AbuseEmail": "abuse@godaddy.com",
        "AbusePhoneNumber": "+1.480-624-2505",
        "Registrant": {
            "Name": "TRIBUNE COMPANY",
            "Organization": "Tribune Technology LLC",
            "Street": "435 N. Michigan Ave",
            "City": "Chicago",
            "State": "Illinois",
            "PostalCode": "60611",
            "Country": "United States",
            "Phone": "+1.3122229100",
            "Email": "tis-dnsadmin@tribune.com",
        },
    }
}
```

The coding required to extract the information in the above example is simple.  First create a simple class to hold all the information you'd like to extract from the source text:

```c#
public class WhoisRecord
{
    public string DomainName { get; set; }

    ...
}
```

In order to populate the class with values, create an instance of the Tokenizer and call the Parse() method.  This instantiates a new object and parses the input text and reflects it's content onto the object.  The TokenResult object contains a list of all tokens extracted, as well as a Value property with the new object assigned to.

```c#
public WhoisRecord Parse(string input)
{
    var tokenizer = new Tokenizer();

    var result = tokenizer.Parse<WhoisRecord>(pattern, input);

    return result.Value;
}
```

Before you can call the Tokenizer, you need to supply it with a pattern first.  For the example above, the pattern would look something like:

```
Domain Name: #{WhoisRecord.DomainName}
Registry Domain ID: #{WhoisRecord.DomainId}
Registrar WHOIS Server: #{WhoisRecord.Registrar.WhoisHostName}
Registrar URL: #{WhoisRecord.Registrar.Url}
Updated Date: #{WhoisRecord.UpdatedDate}
Creation Date: #{WhoisRecord.CreatedDate}
Registrar Registration Expiration Date: #{WhoisRecord.ExpirationDate}
Registrar: #{WhoisRecord.Registrar.Name}
Registrar IANA ID: #{WhoisRecord.RegistrarIanaId}
Registrar Abuse Contact Email: #{WhoisRecord.AbuseEmail}
Registrar Abuse Contact Phone: #{WhoisRecord.AbusePhoneNumber}
Registrant Name: #{WhoisRecord.Registrant.Name}
Registrant Organization: #{WhoisRecord.Registrant.Organization}
Registrant Street: #{WhoisRecord.Registrant.Street}
Registrant City: #{WhoisRecord.Registrant.City}
Registrant State/Province: #{WhoisRecord.Registrant.State}
Registrant Postal Code: #{WhoisRecord.Registrant.PostalCode}
Registrant Country: #{WhoisRecord.Registrant.Country}
Registrant Phone: #{WhoisRecord.Registrant.PhoneNumber}
Registrant Email: #{WhoisRecord.Registrant.Email}
```

Each placeholder in the pattern refers to a property on the object.  The library will walk the object graph, instantiating properties as it encounters them, to set the values specified in the placeholder.

## Transforming Input

Sometimes the data you're processing requires preprocessing before it can be mapped onto your object.  The Tokenizer library contains a number of built-in functions that enable this to save writing additional code.

### Transforming Dates

Sometimes a dates in input text can't automatically be parsed by the .NET framework.  In this case, you can add a ToDateTime() transform to tell the Tokenizer to parse the date in an exact format:

```
Creation Date: 4 Dec 1990 14:32
```

Pattern with transform:

```
Creation Date: #{WhoisRecord.CreationDate:ToDateTime('d MMM yyyy HH:mm')}
```

## Limitations

The Tokenizer currently works on line-by-line.  You cannot currently write multi-line placeholders.

## Extending Tokenizer

The Tokenizer is easily extensible by adding new Token Operators and Token Validators.