# Just Fucking Compile
This is my compiler project for EECE 5183 Compiler Theory. It is a fully working compiler as per the class specification. It is written in C#.
## Requirements & Building
This program targets .NET 5, and you therefore must have the .NET 5 SDK installed in order to build the project. To build it, simply enter the following commands:  
- Windows: `dotnet publish -r win-x64 -c Release --self-contained true -p:PublishSingleFile=true`
- Linux: `dotnet publish -r linux-x64 -c Release --self-contained true -p:PublishSingleFile=true`

This will build a single `jfc` executable that you can run like any other command-line application.
