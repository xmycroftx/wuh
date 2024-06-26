# wuh
Windows Update Helper - a CLI for interacting with wuapi

WUH is a command line utility for installing windows updates, and interacts directly with the wuapi library.

> usage: wuh.exe [install\|\|show-available\|\|show-updated\|\|help] [options]

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
