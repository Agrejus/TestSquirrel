$version = $args[0]
$config = $args[1]
$msBuildLocation = $args[2]
$msbuildDefaultLocation = "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\msbuild.exe"

$ProjectName = "ReleaseTest";
$projectLocation = "C:\Repos\TestSquirrel\$ProjectName\"

$majorVersion = 1
$minorVersion = 0
$buildVersion = 0
$revisionVersion = 1

try {

    # Functions
    function Get-IsVersionValid {
        param (
            [string]$v
        )

        if ([string]::IsNullOrEmpty($v)) {
            return false
        }

        $split = $v.Split('.')

        return $split.Length -eq 4
    }

    function Get-CurrentVersion {
        param (
            [string]$v
        )
        
        $currentVersionFound = $v -match '(".*")'

        if ($currentVersionFound) {
            return $matches[1]
        }

        return ""
    }

    function Get-Config {
        if ([string]::IsNullOrEmpty($config)) {
            return "Release"
        }

        return $config
    }

    function Get-ParameterValue {
        param (
            [string]$line
        )

        $value = $line -replace '\<.*?\>', ''
        $value = $value -replace " ", ""
    
        return $value.trimstart(" ")
    }

    function Get-AssemblyVersion {
        param (
            [string]$value
        )

        $result = New-Object PsObject -Property @{assembly="" ; semVer=""}

        if (![string]::IsNullOrEmpty($version)) {

            if (!(Get-IsVersionValid -v $version)) {

                $result.assembly = ""
                $result.semVer = ""

                return $result
            }

            $split = $version.Split('.')
            $major = $split[0] -replace '"', ""
            $minor = $split[1]
            $build = $split[2]
            $revision = $split[3] -replace '"', ""

            $majorVersion = $major -as [int]
            $minorVersion = $minor -as [int]
            $buildVersion = $build -as [int]
            $revisionVersion = $revision -as [int]

            $result.assembly = $version
            $result.semVer = "$majorVersion.$minorVersion.$revisionVersion"

            return $result
        }
        
        $currentVersionFound = $value -match '(".*")'

        if ($currentVersionFound) {
            $split = $matches[1].Split('.')
            
            $major = $split[0] -replace '"', ""
            $minor = $split[1]
            $build = $split[2]
            $revision = $split[3] -replace '"', ""

            $majorVersion = $major -as [int]
            $minorVersion = $minor -as [int]
            $buildVersion = $build -as [int]
            $revisionVersion = $revision -as [int]

            $revisionVersion = $revisionVersion + 1

            $result.assembly = "$majorVersion.$minorVersion.$buildVersion.$revisionVersion"
            $result.semVer = "$majorVersion.$minorVersion.$revisionVersion"

            return $result
        }
    }

    # Set CS Proj Path
    $file = Get-Childitem -Path $projectLocation -Filter *.csproj
    $csprojLocation = Join-Path -Path $projectLocation -ChildPath $file.Name
    $assemblyInfoLocation = Join-Path -Path $projectLocation -ChildPath "Properties/AssemblyInfo.cs"
    $nuspecLocation = Join-Path -Path $projectLocation -ChildPath "ReleaseTest.csproj.nuspec"
    $nugetPackLocation = Join-Path -Path $projectLocation -ChildPath "Nuget"

    # Change Assembly Version
    $releaseVersion = ""
    $semVer = ""
    foreach ($line in Get-Content $assemblyInfoLocation) {
        if ($line -like "*AssemblyVersion*" -and !$line.StartsWith("//")) {
            $currentVersion = Get-CurrentVersion -v $line
            $assemblyVersion = Get-AssemblyVersion -value $line
            $releaseVersion = $assemblyVersion.assembly
            $semVer = $assemblyVersion.semVer

            $replace = $currentVersion -replace '"', ""
            $with = $releaseVersion

            ((Get-Content -path $assemblyInfoLocation) -replace $replace, $with) | Set-Content $assemblyInfoLocation
        }

        if ($line -like "*AssemblyFileVersion*" -and !$line.StartsWith("//")) {
            $currentVersion = Get-CurrentVersion -v $line
            $releaseVersion = Get-AssemblyVersion -value $line
            $releaseVersion = $assemblyVersion.assembly
            $semVer = $assemblyVersion.semVer

            $replace = $currentVersion -replace '"', ""
            $with = $releaseVersion

            ((Get-Content -path $assemblyInfoLocation) -replace $replace, $with) | Set-Content $assemblyInfoLocation
        }
    }

    # Change Nuspec File
    foreach ($line in Get-Content $nuspecLocation) {
        if ($line -like "*<version>*") {
            $currentNuspecVersion = Get-ParameterValue -line $line

            $replace = $currentNuspecVersion
            $with = $semVer

            ((Get-Content -path $nuspecLocation) -replace $replace, $with) | Set-Content $nuspecLocation
        }
    }

    # Build
    $buildConfiguration = Get-Config

    Invoke-Expression "& '$msbuildDefaultLocation' '$csprojLocation' /p:Configuration='$buildConfiguration'"

    # Nuget Pack
    Invoke-Expression "& nuget pack $nuspecLocation -OutputDirectory $nugetPackLocation -Exclude bin/$buildConfiguration/*.exe"

    $nugetFile = Join-Path -Path $nugetPackLocation -ChildPath "$ProjectName.$semVer.nupkg"

    Write-Output "Release PM Console Command: Squirrel --releasify $nugetFile"
}
catch {
    Write-Error $_
}
