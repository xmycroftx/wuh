using System;
using WUApiLib;

namespace wuh
{
    class Updater
    {
        public static int showUpdates(int showinstalled, int showavailable, int showhidden)
        {
            UpdateSession uSession = new UpdateSession();
            IUpdateSearcher uSearcher = uSession.CreateUpdateSearcher();
            uSearcher.Online = true;
            try
            {
                if (showinstalled == 1)
                {
                    string searchStr = "IsInstalled=1 And ";
                    if (showhidden == 1)
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
                    for (int i = count - 1; i >= 0; --i)
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

                if (showavailable == 1)
                {
                    string searchStr = "IsInstalled=0 And ";
                    if (showhidden == 1)
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
        public static int installDownloaded(int installDownloaded, int download, int enablepreview, int enablecumulative, int enableall)
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
                        //Console.Write(update.Title + Environment.NewLine);
                        if (enableall == 1)
                        {
                            updatesToInstall.Add(update);
                            continue;
                        }

                        if (update.Title.Contains("Security")|update.Title.Contains("Defender"))
                        {
                            Console.Write("Security Update:" + update.Title + Environment.NewLine);
                            updatesToInstall.Add(update);
                        }

                        if (enablecumulative == 1)
                        {
                            if (enablepreview == 1)
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

                    if (download == 1) 
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
                    if (installDownloaded == 1) 
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
                //return true;

            }
        }
    }

    class Program
    {

        static int Main(string[] args)
        {
            int showavailable = 0;
            int showinstalled = 0;
            int enablehidden = 0;
            int enablepreview = 0;
            int enablecumulative = 0;
            int download = 0;
            int installDownloaded = 0;
            int enableall = 0;
            Console.WriteLine("Windows Update Helper\r");
            Console.WriteLine("------------------------\n");
            if (args.Length > 0)
            {
                //Console.WriteLine("Arguments Passed by the Programmer:");
                // To print the command line 
                // arguments using foreach loop
                int indexer = 0;
                foreach (Object obj in args)
                {
                    //Perform operations based on line index above indexer.
                    //Console.WriteLine(args[indexer]);
                    
                    //Console.WriteLine(obj);
                    if (obj.ToString().Contains("install")) { installDownloaded = 1; }
                    if (obj.ToString().Contains("show")) 
                    {
                        try
                        {
                            if (args[indexer + 1].ToString().Contains("available"))
                            {
                                Console.WriteLine("Showing updates available to Download/Install");
                                showavailable = 1;
                            }
                            if (args[indexer + 1].ToString().Contains("updated"))
                            {
                                Console.WriteLine("Showing updates successfully installed");
                                showinstalled = 1;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("We got an error!: " + ex.Message);
                            return 1;
                        }
                    }
                    indexer++;
                    if (obj.ToString().Contains("help")) { Console.WriteLine("Help Menu:\n usage: wuh.exe [install||show-available||show-updated||help] [options] \n options: \n --download\n --all\n --enable-hidden\n --enable-previews\n --enable-cumulative\n --security-only \n ex install security updates: wuh install --download --security-only"); }
                    if (obj.ToString().Contains("--all")) { enableall = 1; Console.WriteLine("Downloading and Installing allthethings."); }
                    if (obj.ToString().Contains("--download")) { download = 1; Console.WriteLine("Downloading...\n"); }
                    if (obj.ToString().Contains("--enable-hidden")) { enablehidden = 1;  Console.WriteLine("Revealing Hidden Updates..."); }
                    if (obj.ToString().Contains("--enable-previews")) { enablepreview = 1; Console.WriteLine("Enabled Preview Updates."); }
                    if (obj.ToString().Contains("--enable-cumulative")) { enablecumulative = 1; Console.WriteLine("Enabled Cumulative Updates."); }
                    if (obj.ToString().Contains("--security-only"))
                    { 
                        enablehidden = 0;
                        enablepreview = 0;
                        enablecumulative = 0;
                        break;
                    }
                    if ((installDownloaded == 1 | download == 1 ) & (showavailable == 1  | showinstalled ==1)) { Console.WriteLine("Error: cannot have show and install or download directives."); return 1; }
                }
            }

            int result = 0;
            if (download == 1 | installDownloaded == 1) { result = Updater.installDownloaded(installDownloaded, download, enablepreview, enablecumulative, enableall); ;  return 0;}
            if (showavailable == 1 | showinstalled == 1) { result = Updater.showUpdates(showinstalled, showavailable,enablehidden); return 0;}
            return 0;  
        }   
     }
}