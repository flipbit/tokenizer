 
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

The coding required to extract the information in the above example is simple.  First create:

```c#
public class WhoisRecord
{
    public string DomainName { get; set; }

    ...
}

public WhoisRecord Parse(string input)
{
    var tokenizer = new Tokenizer();

    var result = tokenizer.Parse<WhoisRecord>(pattern, input);

    return result.Value;
}
```


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


## Transforming Input

## Validating Input


## Limitations


## Extending Tokenizer