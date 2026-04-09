# MZ Automation libs

As already described on Readme file, this project uses [Mz Automation`s Iec61850 libraries](https://github.com/mz-automation/libiec61850).

## libiec61850

To compile their libraries follow their instructions over https://github.com/mz-automation/libiec61850 and https://support.mz-automation.de/doc/libiec61850/net/latest/ pages.

## SCLParser

To compile SCL Parser, clone or download their repository. SCL parser code can be found at https://github.com/mz-automation/libiec61850/tree/v1.6/tools/model_generator_dotnet .

Simply run dotnet build tool.

E.g.:

`dotnet build .\SCLParser\SCLParser.csproj`


# ThirdPartyRefs folder

The external dependencies were set on c# projects to be found at `./ThirdPartyRefs` folder.
After compiling, copy all the files to that folder.
