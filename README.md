# wuh
Windows Update Helper - a CLI for interacting with wuapi

WUH is a command line utility for installing windows updates, and interacts directly with the wuapi library.

> usage: wuh.exe [install\|\|install-kb\|\|uninstall-kb\|\|show-available\|\|show-updated\|\|show-pending\|\|help] [options]

> actions:
> 
> install
use the install action to install any available Security updates

> show available
The show-available action lists any updates that are ready to download or install

> show updated
Using the show-updated action, we can see the isInstalled list for the user.

> options:

>--download
Download before trying to run an install, can also be passwed with no actions to download any available Security updates.

>--all 
Enables downloading and installing of all (non-optional) updates. 

>--enable-hidden
Really only useful if you're looking for WSUS installed packages that are marked "hidden" with show-updated.

>--enable-previews
Cumulative updates have previews, as do other feature packs, this disables the filter that prevents previews from installing normally.

>--enable-cumulative
Cumulative updates are large, and onerous this option enables downloading/installing these update types.

>--security-only
Disables cumulative and previews and runs security updates only.

Example usage:

> wuh show available 

The above statement will show a list of any windows updates available to download from the update catalog

> wuh show updated
 
This shows a list of the "current" patches on the system, does not show history -- only "isInstalled" items.
## Installs/Downloads need to be run in the Administrator context



> wuh install --download --enable-cumulative 

This will download, then install all security and cumulative updates.

> wuh install --download --security-only

This downloads and installs only updates with the word "Security" in the title.

### Example scheduled task in powershell
The following task would check for security updates every day, and installs it as soon as it identifies one.

> $action = New-ScheduledTaskAction -Execute 'wuh.exe' -Argument 'install —download —security-only'

> $trigger =  New-ScheduledTaskTrigger -Daily -At 9am

> Register-ScheduledTask -Action $action -Trigger $trigger -TaskName "SecurityUpdates" -Description "Daily security updates"

### Todo:
> ~~add --enable-all option that enumerates and installs all updates found~~
>  
> add --kb=kbnumber option that allows for selecting only a specific KB for installation
>  
> add --isInstalled=kbnumber to verify only a specific KB is installed
>  
> add --optional-only option that specifically installs optional updates
>  
> add --reboot option that will automatically reboot if any applied update has RebootRequired set to true.
.


### install-kb / uninstall-kb

Surgical, single-KB operations. These deliberately **bypass** the
security/cumulative heuristics used by `install` -- if you named a KB, you meant
that KB.

```
wuh.exe install-kb KB5129195 --download
wuh.exe uninstall-kb KB5129195
wuh.exe uninstall-kb 5129195 --force
```

The KB may be given as `KB5129195` or `5129195`. Matching is on
`IUpdate.KBArticleIDs`, falling back to the title for driver and Store updates,
which frequently carry no KB article ID at all.

**Uninstall has a hard limit worth understanding.** `wuapi` can only remove an
update whose `IUpdate.IsUninstallable` is true. For most cumulative and security
updates that is **false** -- they are servicing-stack/CBS packages, and the COM
API simply will not remove them. In that case `uninstall-kb` reports the fact and
stops. Passing `--force` makes it fall back to
`wusa.exe /uninstall /kb:<n> /quiet /norestart`, which handles some of the
remainder; `wusa` exit code `3010` means success-but-reboot-required, and
`2359303` means the package is not removable at all. Nothing here can remove a
package Windows has marked permanent.

Exit codes: `0` success, `1` error, `2` no matching update found, `3` not
uninstallable and `--force` was not given.
