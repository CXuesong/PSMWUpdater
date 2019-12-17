[![PowerShell Gallery](https://img.shields.io/powershellgallery/v/PSMWUpdater?style=flat-square)](https://www.powershellgallery.com/packages/PSMWUpdater/) | [![Gitter](https://badges.gitter.im/CXuesong/PSMWUpdater.svg)](https://gitter.im/CXuesong/PSMWUpdater?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)

# PSMWUpdater

This PowerShell Core Module provides helper cmdlets helping you to upgrade MediaWiki and its extensions.

PowerShell Gallery: [PSMWUpdater](https://www.powershellgallery.com/packages/PSMWUpdater/) ![PowerShell Gallery](https://img.shields.io/powershellgallery/dt/PSMWUpdater?style=flat-square)

## Usage

You need to install PowerShell Core (`pwsh`) to use this module. See [Installing various versions of PowerShell#PowerShell Core](https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell?view=powershell-6#powershell-core).

Install/Update this module with

```powershell
PS> Install-Module -Name PSMWUpdater
# OR
PS> Update-Module -Name PSMWUpdater
```

Then import the module

```powershell
PS> Import-Module PSMWUpdater
```

You can use the following command to explore the module and its cmdlets

```powershell
# Show all cmdlets
PS> Get-Command -Module PSMWUpdater
# Show help for a cmdlet
PS> Help Get-MwExtension
```

### Query for extensions and skins

You may query for your current installed extension and skins with `Get-MwExtension`. For extensions downloaded from MediaWiki [`Special:ExtensionDistributor`](https://www.mediawiki.org/wiki/Special:ExtensionDistributor), you can see the branch and (SVC) revision information.

```powershell
PS> Get-MwExtension /mnt/x/mediawiki-1.33.0/

Name                           Branch   Version  Revision RevisionTime              LocalPath
----                           ------   -------  -------- ------------              ---------
Extension:CategoryTree                                                              /mnt/x/mediawiki-1.33.0/extensions/CategoryTree
Extension:Cite                          1.0.0                                       /mnt/x/mediawiki-1.33.0/extensions/Cite
Extension:CiteThisPage                                                              /mnt/x/mediawiki-1.33.0/extensions/CiteThisPage
Extension:CodeEditor                                                                /mnt/x/mediawiki-1.33.0/extensions/CodeEditor
Extension:ConfirmEdit                   1.6.0                                       /mnt/x/mediawiki-1.33.0/extensions/ConfirmEdit
Extension:Gadgets                                                                   /mnt/x/mediawiki-1.33.0/extensions/Gadgets
Extension:ImageMap                                                                  /mnt/x/mediawiki-1.33.0/extensions/ImageMap
Extension:InputBox                      0.3.0                                       /mnt/x/mediawiki-1.33.0/extensions/InputBox
Extension:intersection         REL1_33  1.7.0    05edc37  2019/6/17 PM7:24:11       /mnt/x/mediawiki-1.33.0/extensions/intersection
Extension:Interwiki                     3.1 201…                                    /mnt/x/mediawiki-1.33.0/extensions/Interwiki
Extension:LocalisationUpdate            1.4.0                                       /mnt/x/mediawiki-1.33.0/extensions/LocalisationUpdate
Extension:MultimediaViewer                                                          /mnt/x/mediawiki-1.33.0/extensions/MultimediaViewer
Extension:Nuke                          1.3.0                                       /mnt/x/mediawiki-1.33.0/extensions/Nuke
Extension:OATHAuth                      0.2.2                                       /mnt/x/mediawiki-1.33.0/extensions/OATHAuth
Extension:ParserFunctions               1.6.0                                       /mnt/x/mediawiki-1.33.0/extensions/ParserFunctions
Extension:PdfHandler                                                                /mnt/x/mediawiki-1.33.0/extensions/PdfHandler
Extension:Poem                                                                      /mnt/x/mediawiki-1.33.0/extensions/Poem
Extension:Renameuser                                                                /mnt/x/mediawiki-1.33.0/extensions/Renameuser
Extension:Replace Text                  1.4.1                                       /mnt/x/mediawiki-1.33.0/extensions/ReplaceText
Extension:SpamBlacklist                                                             /mnt/x/mediawiki-1.33.0/extensions/SpamBlacklist
Extension:SyntaxHighlight               2.0                                         /mnt/x/mediawiki-1.33.0/extensions/SyntaxHighlight_GeSHi
Extension:TitleBlacklist                1.5.0                                       /mnt/x/mediawiki-1.33.0/extensions/TitleBlacklist
Extension:WikiEditor                    0.5.2                                       /mnt/x/mediawiki-1.33.0/extensions/WikiEditor
Skin:MonoBook                                                                       /mnt/x/mediawiki-1.33.0/skins/MonoBook
Skin:Timeless                           0.8.1                                       /mnt/x/mediawiki-1.33.0/skins/Timeless
Skin:Vector                                                                         /mnt/x/mediawiki-1.33.0/skins/Vector
```

You may also query for a list of known extensions from [WMF MediaWiki site](https://www.mediawiki.org/).

```powershell
PS> Get-MwExtension

Name
----
Extension:3D
Extension:AJAXPoll
Extension:AbsenteeLandlord
Extension:AbuseFilter
Extension:AbuseFilterBypass
Extension:AccessControl
Extension:AccountInfo
Extension:ActiveAbstract
Extension:AdManager
Extension:AddHTMLMetaAndTitle
Extension:AddMessages
Extension:AddPersonalUrls
Extension:AddThis
Extension:AdminLinks
Extension:AdvancedMeta
Extension:AdvancedSearch
Extension:AkismetKlik
Extension:AllTimeZones
...
```

You can append `-Type Extension` or `-Type Skin` to filter for the extensions or skins only.

### Query for extension branches

You can query for the extension branches to retreive their download URL. Note that extension names are case-sensitive.

```powershell
PS> Get-MwExtensionBranch -Name Vector, Echo

ExtensionName  BranchName Url
-------------  ---------- ---
Extension:Echo REL1_33    https://extdist.wmflabs.org/dist/extensions/Echo-REL1_33-f106596.tar.gz
Skin:Vector    REL1_33    https://extdist.wmflabs.org/dist/skins/Vector-REL1_33-878c1e8.tar.gz
```

If there is ambiguity on whether the extension is a skin, prefix the name in `-Name` parameter with `Extension:` or `Skin:`.

You can ask for a specific branch instead of latest `REL` branch.

```powershell
PS> Get-MwExtensionBranch -Name Vector, Echo -Branch master

ExtensionName  BranchName Url
-------------  ---------- ---
Extension:Echo master     https://extdist.wmflabs.org/dist/extensions/Echo-master-4c991af.tar.gz
Skin:Vector    master     https://extdist.wmflabs.org/dist/skins/Vector-master-bf365aa.tar.gz
```

You can also ask for all the known branches.

```powershell
PS> Get-MwExtensionBranch -Name Vector, Echo -AllBranches

ExtensionName  BranchName Url
-------------  ---------- ---
Extension:Echo REL1_31    https://extdist.wmflabs.org/dist/extensions/Echo-REL1_31-b56ec9b.tar.gz
Extension:Echo REL1_32    https://extdist.wmflabs.org/dist/extensions/Echo-REL1_32-335389f.tar.gz
Extension:Echo REL1_33    https://extdist.wmflabs.org/dist/extensions/Echo-REL1_33-f106596.tar.gz
Extension:Echo master     https://extdist.wmflabs.org/dist/extensions/Echo-master-4c991af.tar.gz
Extension:Echo source     https://gerrit.wikimedia.org/r/mediawiki/extensions/Echo.git
Skin:Vector    REL1_31    https://extdist.wmflabs.org/dist/skins/Vector-REL1_31-f0327dc.tar.gz
Skin:Vector    REL1_32    https://extdist.wmflabs.org/dist/skins/Vector-REL1_32-d3ed21a.tar.gz
Skin:Vector    REL1_33    https://extdist.wmflabs.org/dist/skins/Vector-REL1_33-878c1e8.tar.gz
Skin:Vector    master     https://extdist.wmflabs.org/dist/skins/Vector-master-bf365aa.tar.gz
Skin:Vector    source     https://gerrit.wikimedia.org/r/mediawiki/skins/Vector.git
```

Then you can use `Invoke-WebRequest` or `wget` to download the `tar.gz` files.

```powershell
PS> Get-MwExtensionBranch -Name Vector, Echo | % { wget $_.Url }
```

Or use the following expression to download all the latest REL extensions for your local MediaWiki installation.

```powershell
PS> Get-MwExtension ./mediawiki-1.33.0/ -NoEmpty -BareName | Get-MwExtensionBranch
```

