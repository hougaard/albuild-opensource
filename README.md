# ALBuild

ALBuild is a tool for building AL applications in both pipeline and non-pipeline environments. It maintains a database of translations.

ALBuild supports the following types of operations:

* GIT operations
* File Copy operations
* Deploy app to Docker container with Basic authentication
* Deploy app to Business Central SaaS with service 2 service OAuth authentiation
* PowerShell operations
* App Signing (using signtool.exe)
* Run test codeunits on Docker container with Basic authentication
* Run test codeunits on Business Central SaaS with OAuth authentication 
* Translate XLF using Azure Cognitive Services
* Update version in app.json

The list of operatinos is defined in a .json file that describes the series of operations.

# ALBuild Translation Administartion 

The admin tool enables you to perform maintanence operations on the translation database, such as:

* Edit specific entries
* Bulk import from XLF files
* Ripper for BC artifacts - To reuse translations from Microsoft

# Standalone Translation Tool
The standalone translation tool enables you to add translations to an AL app (working in the /Translation folder)

# Configuration

Each app has a .config file where you configure:

* AzureKey for Azure Cognitive Services (translation)
* "App Name" for specifying in XLF where translation comes from
* Location of local translation database
* List of langauges supported
* Storage Account and key for Azure Table Storage

