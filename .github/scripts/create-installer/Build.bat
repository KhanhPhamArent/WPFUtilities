@echo off
set PATH=C:\Program Files (x86)\Inno Setup 6;.\PreHandleApp\GenerateInnoSetupScript;%PATH%;

dotnet run --no-launch-profile --project .\ConsoleAppSupport\GenerateInnoSetupScript\GenerateInnoSetupScript.csproj --configuration Release -- -cfg ./conf/config.json -tmp ./conf/templateScript.iss -o ./