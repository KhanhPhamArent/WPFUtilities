<#
    .DESCRIPTION
    Execute build.

    .PARAMETER SolutionPath
    The solution path.

    .PARAMETER Configuration
    The configuration setting.

    .PARAMETER Platform
    The platform setting.

    .PARAMETER Version
    Optional. The version string(x.x.x.x style).
#>
Param(
    [parameter(mandatory=$true)][String]$SolutionPath,
    [parameter(mandatory=$true)][String]$Configuration,
    [parameter(mandatory=$true)][String]$Platform,
    [String]$Version
)
dotnet restore "$SolutionPath" -p:Configuration="$Configuration" -p:Platform="$Platform"
if ("$Version" -eq "") {
    dotnet build "$SolutionPath" --no-restore -p:Configuration="$Configuration" -p:Platform="$Platform" -p:ExecCI=true
} else {
    dotnet build "$SolutionPath" --no-restore -p:Configuration="$Configuration" -p:Platform="$Platform" -p:ExecCI=true -p:InformationalVersion="$Version" -p:AssemblyVersion="$Version" -p:FileVersion="$Version" -p:Version="$Version"
}
