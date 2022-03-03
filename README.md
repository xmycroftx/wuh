# wuh
Windows Update Helper - a CLI for interacting with wuapi

WUH is a command line utility for installing windows updates, and interacts directly with the wuapi library.

> usage: wuh.exe [install\|\|show-available\|\|show-updated\|\|help] [options]
> 
> options:

>--download

>--enable-hidden

>--enable-previews

>--enable-cumulative

>--security-only

Example usage:

> wuh show-available 

The above statement will show a list of any windows updates available to download from the update catalog

> wuh show-updated 

This shows a list of the "current" patches on the system, does not show history -- only "isInstalled" items.

> wuh install --download --enable-cumulative 

This will download, then install all security and cumulative updates.

> wuh install --download --security-only

This downloads and installs only updates with the word "Security" in the title.

