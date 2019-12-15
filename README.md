[![PowerShell Gallery](https://img.shields.io/powershellgallery/v/PSMWUpdater?style=flat-square)](https://www.powershellgallery.com/packages/PSMWUpdater/) | [![Gitter](https://badges.gitter.im/CXuesong/PSMWUpdater.svg)](https://gitter.im/CXuesong/PSMWUpdater?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)

# PSMWUpdater

This PowerShell Core Module provides helper cmdlets helping you to upgrade MediaWiki and its extensions.

PowerShell Gallery: [PSMWUpdater](https://www.powershellgallery.com/packages/PSMWUpdater/) ![PowerShell Gallery](https://img.shields.io/powershellgallery/dt/PSMWUpdater)

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

You may query for your current installed extension and skins with

```powershell
PS> Get-MwExtension ./mediawiki-1.33.0/

LastWriteTime              IsEmpty Name
-------------              ------- ----
6/8/19 1:02:17 AM +08:00     False Extension:CategoryTree
12/13/18 7:35:19 AM +08:00   False Extension:Cite
12/13/18 7:35:19 AM +08:00   False Extension:CiteThisPage
6/8/19 1:02:18 AM +08:00     False Extension:CodeEditor
12/13/18 7:35:21 AM +08:00   False Extension:ConfirmEdit
12/13/18 7:35:27 AM +08:00   False Extension:Gadgets
12/13/18 7:35:29 AM +08:00   False Extension:ImageMap
12/13/18 7:35:29 AM +08:00   False Extension:InputBox
12/13/18 7:35:29 AM +08:00   False Extension:Interwiki
12/13/18 7:35:32 AM +08:00   False Extension:LocalisationUpdate
6/8/19 1:02:20 AM +08:00     False Extension:MultimediaViewer
12/13/18 7:35:34 AM +08:00   False Extension:Nuke
6/8/19 1:02:20 AM +08:00     False Extension:OATHAuth
6/8/19 1:02:20 AM +08:00     False Extension:ParserFunctions
12/13/18 7:35:36 AM +08:00   False Extension:PdfHandler
12/13/18 7:35:36 AM +08:00   False Extension:Poem
12/13/18 7:35:38 AM +08:00   False Extension:Renameuser
6/8/19 1:02:21 AM +08:00     False Extension:ReplaceText
12/13/18 7:35:39 AM +08:00   False Extension:SpamBlacklist
12/13/18 7:35:40 AM +08:00   False Extension:SyntaxHighlight_GeSHi
12/13/18 7:35:41 AM +08:00   False Extension:TitleBlacklist
12/13/18 7:36:08 AM +08:00   False Extension:WikiEditor
12/13/18 7:36:23 AM +08:00   False Skin:MonoBook
7/2/19 9:32:51 PM +08:00     False Skin:Timeless
12/13/18 7:36:23 AM +08:00   False Skin:Vector
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

