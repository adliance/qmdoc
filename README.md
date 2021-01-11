# Adliance QmDoc

[![Build Status](https://dev.azure.com/adliance/QmDoc/_apis/build/status/QmDoc?branchName=master)](https://dev.azure.com/adliance/QmDoc/_build/latest?definitionId=91&branchName=master)
[![NuGet](https://img.shields.io/nuget/v/Adliance.QmDoc.svg)](https://www.nuget.org/packages/Adliance.QmDoc/)

## What is QmDoc
QmDoc is used by Adliance GmbH to automatically convert Markdown files to nicely formatted PDF files including corporate design guidelines.

## Installation
QmDoc is a .NET Core global tool. You'll need .NET Core 3.1 installed in order to install and use QmDoc. 

Open your commandline and call

    dotnet tool install -g Adliance.QmDoc

to install QmDoc, or call

    dotnet tool update -g Adliance.QmDoc

to upgrade it to the latest version.

## Usage
Once QmDoc is installed, you can convert all Markdown (`*.md`) files in a directory by calling `QmDoc` in that directory. You may also specify files or directories by using the `--source` parameter:

    qmdoc --source "c:\users\me\project\documentation"

or

    qmdoc --source "c:\users\me\project\documentation\my_awesome_document.md"

Call

    qmdoc --help 
    
to see all commands and parameters.

## Themes
QmDoc supports multiple themes which can be used to easily create PDF documents that match the different corporate designs of clients or projects.

For example

    qmdoc set-theme --theme Adliance
    
to switch the the `Adliance` theme. Themes are automatically downloaded from the [GitHub repository](https://github.com/adliance/qmdoc/tree/master/themes) and cached locally in the `USERPROFILE\.qmdoc\themes` directory. Please note that it may take a few minutes until changes of the theme on GitHub are picked up by your local installation of QmDoc.

You can also set the theme for a single conversion by using the `--theme` parameter. For example

    qmdoc --theme ScientificDX

will convert all Markdown files in the current directory to PDF by using the "ScientificDX" theme.

Each theme must consist of 5 different files (`index.html`, `footer.html`, `header.html`, `styles.scss`, `options.json`) that specify the behaviour and look and feel of the resulting PDF files. If no theme is specified, a simple default theme will be used.

## Features
### Automatic numbering of headlines

```
# Headline1
## Headline2

Output
1 Headline1
1.1 Headline2
```



### Reference to other markdown-file in the same directory

Write in the main-document: 

```
[filename.md]
```

At the end of the document you can list all documents to wich you've created a reference:

```
{{LINKED_DOCUMENTS}}
```

### Link in the document

```
[#titel]
```



### Link to GIT commit history of the specific document

Display commit-messages:

```
{{GIT_VERSIONS}}
```


### Alert/Warning/Question-blocks

```
{!} Your alertmessage
{!!} Your warningmessage
{?} Your question
```

### Date

```
{{DATE}}
```

### View errors

Errors will be viewed in the command window.

