# ALBuild

ALBuild is an open-source tool for building AL applications in both pipeline and non-pipeline environments. It maintains a database of translations.

ALBuild supports the following types of operations:

* GIT operations
* File Copy and Delete operations
* Upload files to an HTTP endpoint
* Deploy app to Docker container with Basic authentication
* Deploy app to Business Central SaaS with service 2 service OAuth authentication
* Download Symbols from Docker container
* Download Symbols from SaaS sandbox
* Compile app with the AL compiler (alc.exe)
* PowerShell operations
* App Signing (using signtool.exe)
* Run test codeunits on Docker container with Basic authentication
* Run test codeunits on Business Central SaaS with OAuth authentication
* Translate XLF using Azure Cognitive Services
* Translate XLF using an LLM (Claude, ChatGPT, or a local Ollama model)
* Update version in app.json

The list of operations is defined in a .json file that describes the series of operations.

## Requirements

* .NET 10 (all projects target `net10.0`, TranslateAdmin targets `net10.0-windows7.0`)
* The AL Language extension for VS Code (ALBuild locates `alc.exe` in `%USERPROFILE%\.vscode\extensions\ms-dynamics-smb.al*`)
* `git.exe` on the PATH (for Git tasks)
* `signtool.exe` next to `ALBuild.exe` (for the Sign task)

## Running ALBuild

```
ALBuild <buildscript json file> [-offline]
```

`-offline` makes Git tasks ignore a non-zero exit code and makes the translation tasks use only the local translation database, without calling any translation service.

# ALBuild Translation Administration 

The admin tool enables you to perform maintenance operations on the translation database, such as:

* Edit specific entries
* Bulk import from XLF files
* Ripper for BC artifacts - To reuse translations from Microsoft

# Standalone Translation Tool
The standalone translation tool enables you to add translations to an AL app (working in the /Translation folder)

```
BCTranslateApp <input extension.g.xlf file> <App Name>
```

# Test Runner

The `TestSaaS` and `TestBasicDocker` tasks call the `ALBuild_runcodeunit` OData web service. Publish the app in the `/TestRunner` folder to the environment and register the web service (see `TestRunner/Webservice.xml`) before using those tasks.

# Configuration

Each app has a .config file where you configure:

* AzureKey for Azure Cognitive Services (translation)
* "App Name" for specifying in XLF where translation comes from
* Location of local translation database
* List of languages supported
* Storage Account and key for Azure Table Storage
* LLM provider, key and model (used by the `TranslateLLM` task)

```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <appSettings>
    <add key="AzureKey" value="<Azure Translation Cognitive Servivce key>" />
    <add key="Name" value="translate" />
    <add key="Database" value="<Path and Name of translation.db>"/>
    <add key="Languages" value="en-AU,de-AT,nl-BE,fr-BE,en-CA,fr-CA,da-DK,de-DE,fi-FI,fr-FR,is-IS,it-IT,es-MX,nl-NL,en-NZ,nb-NO,es-ES_tradnl,sv-SE,fr-CH,de-CH,it-CH,en-GB,en-US,et-EE,zh-HK,ja-JP,pl-PL,en-ZA,ko-KR,zh-TW,cs-CZ,ru-RU"/>

    <add key="storageaccount" value="<Azure Storage Account>"/>
    <add key="storageaccountkey" value="<Azure Storage Account Access Key>"/>

    <add key="LLMProvider" value="Claude"/>
    <add key="LLMSystemPrompt" value="<Optional extra context about the app, given to the LLM>"/>
    <add key="ClaudeKey" value="<Anthropic API key>"/>
    <add key="ClaudeModel" value="<Claude model, defaults to claude-sonnet-4-5 when empty>"/>
    <add key="OpenAIKey" value="<OpenAI API key>"/>
    <add key="OpenAIModel" value="<OpenAI model, defaults to gpt-4o-mini when empty>"/>
    <add key="OllamaEndpoint" value="http://localhost:11434/v1/chat/completions"/>
    <add key="OllamaModel" value="<Installed Ollama model name>"/>
    <add key="OllamaApiKey" value="<Optional API key, defaults to ollama when empty>"/>
  </appSettings>
</configuration>
```

`LLMProvider` must be `Claude`, `ChatGPT`, or `Ollama` (`OpenAI` is accepted as an alias); if it is empty, `ChatGPT` is used. Claude and ChatGPT run in offline mode when their selected API key is empty.

For a local Ollama server, set `LLMProvider` to `Ollama` and set `OllamaModel` to the name of an installed model. `OllamaEndpoint` is the complete OpenAI-compatible chat-completions URL and defaults to `http://localhost:11434/v1/chat/completions`. An empty `OllamaApiKey` uses `ollama` as a dummy Bearer token, so a standard local Ollama installation does not require credentials; the setting can also hold a real token for an authenticated proxy.

# Variables

The `Remember` task reads the `app.json` of an app and makes its values available to the following tasks. Any property from `app.json` can be used as `%PROPERTY%` (uppercase), plus `%APPPATH%` for the folder that was remembered:

* `%APPPATH%` - the path given to the `Remember` task
* `%NAME%` - the app name
* `%PUBLISHER%` - the publisher
* `%VERSION%` - the version (after `UpdateVersion` has run, the new version)
* `%ID%` - the app id

# Tasks

| Type | Settings |
| --- | --- |
| `Git` | `Path`, `Command` |
| `UpdateVersion` | `AppPath`, `VersionPartToIncrement`, `Increment`, `DateInVersionPartNo` (optional) |
| `Remember` | `AppPath` |
| `Compile` | `AppPath`, `RuleSet` (optional, uses `<AppPath>\.vscode\ruleset.json`) |
| `Translate` | `XLFPath`, `ProductName` |
| `TranslateLLM` | `XLFPath`, `ProductName`, `SystemPrompt` (optional, overrides `LLMSystemPrompt`) |
| `Sign` | `AppPath`, `Keyhash` (SHA1 thumbprint of the certificate) |
| `Copy` | `From`, `To` |
| `Delete` | `From` (wildcards supported) |
| `Upload` | `Path`, `Filter`, `Endpoint` (`%1` is replaced with the file name) |
| `PowerShell` | `Command` |
| `DeploySaaS` | `ClientId`, `ClientSecret`, `TenantId`, `Environment`, `SchemaUpdateMode`, `AppFile` |
| `DeployBasicDocker` | `BaseURL`, `User`, `Password`, `SchemaUpdateMode`, `AppFile` |
| `TestSaaS` | `ClientId`, `ClientSecret`, `TenantId`, `Environment`, `Company`, `TestCodeunit` |
| `TestBasicDocker` | `BaseURL`, `User`, `Password`, `Company`, `TestCodeunit` |
| `DownloadSymbolsSaaS` | `ClientId`, `ClientSecret`, `TenantId`, `Environment`, `AppPath` |
| `DownloadSymbolsDocker` | `BaseURL`, `User`, `Password`, `AppPath` |

A task that fails stops the build.

# Build File

The following is an example of a build json file:

```json
{
  "Project": "Demo",
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
        "DateInVersionPartNo": 3
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
      "Type": "TranslateLLM",
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
        "Path": "%APPPATH%",
        "Command": "add *"
      }
    },
    {
      "Type": "Git",
      "Settings": {
        "Path": "%APPPATH%",
        "Command": "commit -a -m \"ALBuild Version %VERSION%\""
      }
    },
    {
      "Type": "Git",
      "Settings": {
        "Path": "%APPPATH%",
        "Command": "push"
      }
    }
  ]
}
```
