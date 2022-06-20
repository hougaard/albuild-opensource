# ALBuild

ALBuild is an open-source tool for building AL applications in both pipeline and non-pipeline environments. It maintains a database of translations.

ALBuild supports the following types of operations:

* GIT operations
* File Copy operations
* Deploy app to Docker container with Basic authentication
* Deploy app to Business Central SaaS with service 2 service OAuth authentication
* Download Symbols from Docker container
* Download Symbols from SaaS sandbox
* PowerShell operations
* App Signing (using signtool.exe)
* Run test codeunits on Docker container with Basic authentication
* Run test codeunits on Business Central SaaS with OAuth authentication 
* Translate XLF using Azure Cognitive Services
* Update version in app.json

The list of operations is defined in a .json file that describes the series of operations.

# ALBuild Translation Administration 

The admin tool enables you to perform maintenance operations on the translation database, such as:

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
* List of languages supported
* Storage Account and key for Azure Table Storage



# Build File

The following is an example of a build json file:

```{
  "Project": "Demo",
  "Report" : "Email",
  "ReportDestination" : "appowner@company.com,
  "Tasks": [
    {
      "Type": "DeployBasicDocker",
      "Settings": {
        "AppFile": "c:\\projects\\albuild\\testrunner\\Hougaard_ALBuild TestRunner_1.0.0.0.app",
        "BaseURL": "http://bc20:7049/BC/",
        "User": "demo",
        "Password": "demo",
        "SchemaUpdateMode": "forcesync"
      }
    },
    {
      "Type": "Git",
      "Settings": {
        "Path": "c:\\projects\\youtube\\point of sale",
        "Command": "pull"
      }
    },
    {
      "Type": "UpdateVersion",
      "Settings": {
        "AppPath": "c:\\projects\\youtube\\point of sale",
        "VersionPartToIncrement": 4,
        "Increment": 1,
	      "DateInVersionPartNo":3
      }
    },
    {
      "Type": "Remember",
      "Settings": {
        "AppPath": "c:\\projects\\youtube\\point of sale"
      }
    },
    {
	"Type": "DownloadSymbolsDocker",
	"Settings": {
        "AppPath": "%APPPATH%",
        "BaseURL": "http://bc20:7049/BC/",
        "User": "demo",
        "Password": "demo"  	
	}
    },
    {
      "Type": "Compile",
      "Settings": {
        "AppPath": "%APPPATH%"
      }
    },
    {
      "Type": "Translate",
      "Settings": {
        "XLFPath": "%APPPATH%\\Translations\\%NAME%.g.xlf",
        "ProductName": "%NAME%"
      }
    },
    {
      "Type": "Compile",
      "Settings": {
        "AppPath": "%APPPATH%"
      }
    },
    {
      "Type": "Copy",
      "Settings": {
        "From": "%APPPATH%\\%PUBLISHER%_%NAME%_%VERSION%.app",
        "To": "C:\\Projects\\youtube\\ALbuild\\Release\\%PUBLISHER%_%NAME%_%VERSION%.app"
      }
    },
    {
      "Type": "Git",
      "Settings": {
        "Path": "c:\\projects\\youtube\\point of sale",
        "Command": "add *"
      }
    },
    {
      "Type": "Git",
      "Settings": {
        "Path": "c:\\projects\\youtube\\point of sale",
        "Command": "commit -a -m \"ALBuild Version %VERSION%\""
      }
    },
    {
      "Type": "Git",
      "Settings": {
        "Path": "c:\\projects\\youtube\\point of sale",
        "Command": "push"
      }
    }
  ]
}
```
