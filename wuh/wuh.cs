using System;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Parsing;
using System.Threading.Tasks;
using WUApiLib;

namespace wuh
{
    class Updater
    {
        public static int showUpdates(bool showinstalled, bool showavailable, bool showhidden)
        {
            UpdateSession uSession = new UpdateSession();
            IUpdateSearcher uSearcher = uSession.CreateUpdateSearcher();
            uSearcher.Online = true;
            try
            {
                if (showinstalled == true)
                {
                    string searchStr = "IsInstalled=1 And ";
                    if ( showhidden == true )
                    {
                        searchStr = searchStr + "IsHidden=1";
                    }
                    else 
                    {
                        searchStr = searchStr + "IsHidden=0";
                    }
                    string txtAllUpdates = "";
                    UpdateSession updateSession = new UpdateSession();
                    IUpdateSearcher updateSearcher = updateSession.CreateUpdateSearcher();
                    int count = updateSearcher.GetTotalHistoryCount();
                    Console.WriteLine("Total Count = " + count);
                    IUpdateHistoryEntryCollection history = updateSearcher.QueryHistory(0, count);
                    string kb2267602 = "";
                    int afterFilter = 0;
                    for (int i = count-1; i >= 0; --i)
                    {
                        if (history[i].HResult == 0) 
                        {
                            if (!history[i].Title.Contains("KB2267602"))
                            {
                                txtAllUpdates += "\t" + history[i].Title + "\n";
                                ++afterFilter;


                            }
                            else 
                            { 
                                kb2267602 = "\t" + history[i].Title + "\n";
                                
                            }
                            
                        }

                    }
                    ++afterFilter;
                    Console.Write(txtAllUpdates);
                    Console.Write(kb2267602);
                    Console.WriteLine("After Filter Count = " + afterFilter);
                }

                if (showavailable == true) 
                {
                    string searchStr = "IsInstalled=0 And ";
                    if (showhidden == true)
                    {
                        searchStr = searchStr + "IsHidden=1";
                    }
                    else
                    {
                        searchStr = searchStr + "IsHidden=0";
                    }
                    ISearchResult sResult = uSearcher.Search(searchStr);
                    Console.WriteLine("Found " + sResult.Updates.Count + " update(s) available." + Environment.NewLine);
                    foreach (IUpdate update in sResult.Updates)
                    {
                        Console.WriteLine(update.Title);
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("0x80240024"))
                    {
                    Console.WriteLine("No updates found");
                        return 0;
                }
                else { Console.WriteLine("We got an error!: " + ex.Message); }
                return 1;
            }
        }
        public static int installDownloaded(bool installDownloaded, bool download, bool enablepreview, bool enablecumulative, bool enableall)
        {
            {


                UpdateSession uSession = new UpdateSession();
                IUpdateSearcher uSearcher = uSession.CreateUpdateSearcher();
                UpdateCollection updatesToInstall = new UpdateCollection();
                uSearcher.Online = true;

                try
                {
                    ISearchResult sResult = uSearcher.Search("IsInstalled=0 And IsHidden=0");
                    foreach (IUpdate update in sResult.Updates)
                    {
                        if (enableall == true)
                        {
                            updatesToInstall.Add(update);
                            continue;
                        }

                        if (update.Title.Contains("Security")|update.Title.Contains("Defender"))
                        {
                            Console.Write("Security Update:" + update.Title + Environment.NewLine);
                            updatesToInstall.Add(update);
                        }

                        if (enablecumulative == true)
                        {
                            if (enablepreview == true)
                            {
                                if (update.Title.Contains("Cumulative Update"))
                                {
                                    Console.Write("Cumulative Update:" + update.Title + Environment.NewLine);
                                    updatesToInstall.Add(update);
                                }
                            }
                            else
                            {
                                if (update.Title.Contains("Cumulative Update") & !update.Title.Contains("Preview"))
                                {
                                    Console.Write("Cumulative Update:" + update.Title + Environment.NewLine);
                                    updatesToInstall.Add(update);
                                }
                            }
                        }
                    }

                    if (download == true) 
                    { 
                        Console.WriteLine("Downloading " + updatesToInstall.Count + " eligible (security or cumulative) update(s)" + Environment.NewLine);
                        IUpdateDownloader downloader = uSession.CreateUpdateDownloader();
                        downloader.Updates = updatesToInstall;
                        IDownloadResult downloaderRes = downloader.Download();
                        for (int i = 0; i < updatesToInstall.Count; i++)
                        {
                            if (downloaderRes.GetUpdateResult(i).HResult == 0)
                            {
                                Console.Write("Downloaded : " + updatesToInstall[i].Title + Environment.NewLine);
                            }
                            else
                            {
                                Console.Write("Failed : " + updatesToInstall[i].Title + Environment.NewLine);
                            }
                        }
                    }
                    if (installDownloaded == true) 
                    {
                        Console.WriteLine("Installing pending updates...");
                        IUpdateInstaller installer = uSession.CreateUpdateInstaller();
                        installer.Updates = updatesToInstall;
                        IInstallationResult installationRes = installer.Install();
                        for (int i = 0; i < updatesToInstall.Count; i++)
                        {
                            if (installationRes.GetUpdateResult(i).HResult == 0)
                            {
                                Console.Write("Installed : " + updatesToInstall[i].Title + Environment.NewLine);
                            }
                            else
                            {
                                Console.Write("Failed : " + updatesToInstall[i].Title + Environment.NewLine);
                            }
                        }
                    }
                    return 0;
                }
                catch (Exception ex) {
                    //https://docs.microsoft.com/en-us/windows/deployment/update/windows-update-error-reference exception codes
                    if (ex.Message.Contains("0x80240024"))
                    {
                        Console.WriteLine("No updates found");
                        return 0;
                    }
                    else { Console.WriteLine("We got an error!: " + ex.Message); }
                    
                    return 1; 
                }
            }
        }
    }

    class Program
    {


            public static async Task<int> Main(params string[] args)
            {
            var help = new Command("help", description: "Shows help dialog.") { new Option<bool>("--all") };
            var install = new Command("install", description: "Initiates install of matching updates.")
                {
                    new Option<bool>("--all","Installs (and downloads if enabled) all non-optional updates available."),
                    new Option<bool>("--download","Downloads prior to installing non-cumulative updates."),
                    new Option<bool>("--enable-cumulative","includes cumulative updates in the update set."),
                    new Option<bool>("--enable-previews","includes PREVIEW cumulative updates in the update set."),
                    new Option<bool>("--security-only","Only updates to Security, Anti-Malware and Defender related keywords."),
                };
            var shUpdated = new Command("updated", description: "Shows all successfully installed windows updates."){
                        new Option<bool>("--all","Shows all updates installed (including ALL Security Intelligence updates)")
                    };
            var shAvailable = new Command("available", description: "Shows all available windows updates ready to install.")
                    {
                        new Option<bool>("--all","Shows all updates available (including hidden, preview etc.)")
                    };
            var shPending = new Command("pending", description: "Shows all pending windows updates waiting for reboot/install.");
            var show = new Command("show", description: "show commands (available,updated,pending)")
                {
                    shUpdated,
                    shAvailable,
                    shPending
                };
            var command = new RootCommand
            {
                help,
                install,
                show
            };

            help.Handler = CommandHandler.Create((bool all) =>
            {
                Console.WriteLine(all);
            });

            install.Handler = CommandHandler.Create((bool all, bool download, bool enableCumulative, bool enablePreviews, bool securityOnly) =>
            {
                Console.WriteLine(all);
                Console.WriteLine(download);
                Console.WriteLine(enableCumulative);
                Console.WriteLine(securityOnly);
                return Updater.installDownloaded(true, download, enablePreviews, enableCumulative, all);
            });
            shAvailable.Handler = CommandHandler.Create((bool all) =>
            {
                return Updater.showUpdates(false, true, all);
                //Console.WriteLine(all);
            });
            shUpdated.Handler = CommandHandler.Create((bool all) =>
            {
                return Updater.showUpdates(true, false, all);
                //Console.WriteLine(all);
            });
            shPending.Handler = CommandHandler.Create((ParseResult parseResult) =>
            {
                Console.WriteLine("Pending Handler Placeholder, not yet implemented");
            });
            
            return await command.InvokeAsync(args);
        }
    }
}