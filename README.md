# AzureFunction Swagger Definition Generator
| Author | Twitter
--- | ---
| Mikael Svenson | @mikaelsvenson



Azure function to generate a proper Swagger definition for the other Azure functions in your project.

Maybe a bit meta ;)

## Notes and reflection on my part
* The Swagger generator will only create _200_ return codes. If you have other return codes you need to specify them manually.
    * As an example, you might want to return _201_ (Created) if your operation creates something.
* It's recommended to use objects as both input and output parameters, as they are most descriptive
* It's recommended to use _POST_ over _GET_ for complex input, as chanses of encoding issues etc. are smaller.
* See _Templates.cs_ for common useful Function signatures.
* I don't fine the default in parameter _HttpRequestMessage_ all that useful, as generating Swagger from random input is quite hard. Better to pass in a typed request object.

