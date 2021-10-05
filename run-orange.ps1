$CurrentPath = Get-Location
$DotnetPath = Join-Path -Path $CurrentPath -ChildPath "dotnet"
$Env:DOTNET_ROOT = $DotnetPath
$Env:Path += $DotnetPath
Invoke-Expression "&'.\Citrus\Orange\Launcher\bin\Win\Release\Launcher.exe'"