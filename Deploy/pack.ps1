$root = (split-path -parent $MyInvocation.MyCommand.Definition) + '\..'
$version = [System.Reflection.Assembly]::LoadFile("$root\Spike.Box.Runtime\bin\Release\Spike.Box.Runtime.dll").GetName().Version
$versionStr = "{0}.{1}.{2}" -f ($version.Major, $version.Minor, $version.Build)

Write-Host "Setting .nuspec version tag to $versionStr"

$content = (Get-Content $root\Deploy\Spike.Box.nuspec) 
$content = $content -replace '\$version\$',$versionStr

$content | Out-File $root\Deploy\Spike.Box.compiled.nuspec

& $root\Deploy\NuGet.exe pack $root\Deploy\Spike.Box.compiled.nuspec
