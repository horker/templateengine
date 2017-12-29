# Horker Template Engine

The Horker Template Engine is a template engine for plain text empowered by PowerShell.

## Installation

The Horker Template Engine is available in the PowerShell Gallery.

```PowerShell
Install-Module HorkerTemplateEngine
```

## Cmdlet synopsis

The Horker Template Engine exports a single cmdlet, `Invoke-TemplateEngine`. It processes a document template that embeds PowerShell code and generates a plain text as an outcome to the standard output stream.

```PowerShell
Invoke-TemplateEngine [-Template] <string> [[-ProcessorFile] <string>] [<CommonParameters>]
```

- `-Template` A document template, given as a string or an array of strings through pipeline.  When given through pipeline, they are joined with newlines.
- `-ProcessorFile` generates a internal processing script for debugging.

## Syntax

- Text portions enclosed with `<%` and `%>` are executed as PowerShell code. The objects returned by the code into the standard output stream are written into a resultant document.
- Text portions enclosed with `<%` and `-%>` are processed as the same above, but the following newlines will not appear in the output.

Note that, unlike usual PowerShell output, objects are converted into strings by the ToString() method and no newlines are inserted between them. See Example section for details.

## Example

(TBD)

## Dependencies

On builid time, it depends the following modules:

```PowerShell
Install-Module pester -Scope CurrentUser
Install-Module InvokeBuild -Scope CurrentUser
Install-Module XmlDoc2CmdletDoc -Destination .
```

## License

The MIT License
