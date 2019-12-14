param (
    $ModulePath = "../PSMWUpdater/bin/Debug/netstandard2.0/PSMWUpdater.dll"
)

Import-Module $ModulePath

Describe PSMWUpdater {
    It FetchRemoteExtensionNames {
        $Extensions = Get-MwExtension
        $Extensions.Length | Should -BeIn (500..2000)
        Write-Information "Extension count: $($Extensions.Length)"
        $Extensions | % { $_.Name.Type } | Should -Contain "Extension"
        $Extensions | % { $_.Name.Type } | Should -Contain "Skin"
    }

    It FetchRemoteExtensionBranches {
        $Branches = Get-MwExtensionBranch Echo
        $Branches.Length | Should -BeIn (3..6)
        $Branches | % { $_.BranchName } | Should -Contain "master"
        $Branches | % { $_.BranchName } | Should -Contain "source"
        $Branches | % { $_.BranchName.StartsWith("REL1_") } | Should -Contain $true
    }
}
