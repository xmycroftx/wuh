using System;
using WUApiLib;

namespace wuh
{
    class Updater
    {
        public static Boolean showUpdates(int showinstalled, int showavailable, int showhidden)
        {
            UpdateSession uSession = new UpdateSession();
            IUpdateSearcher uSearcher = uSession.CreateUpdateSearcher();
            uSearcher.Online = true;
            try
            {
                if (showinstalled == 1)
                {
                    string searchStr = "IsInstalled=1 And ";
                    if ( showhidden == 1 )
                    {
                        searchStr = searchStr + "IsHidden=1";
                    }
                    else 
                    {
                        searchStr = searchStr + "IsHidden=0";
                    }
                    ISearchResult sResult = uSearcher.Search(searchStr);
                    Console.WriteLine("Found " + sResult.Updates.Count + " update(s) installed." + Environment.NewLine);
                    foreach (IUpdate update in sResult.Updates)
                    {
                        Console.WriteLine(update.Title);
                    }
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
                return true;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("0x80240024"))
                    {
                    Console.WriteLine("No updates found");
                        return true;
                }
                else { Console.WriteLine("We got an error!: " + ex.Message); }
                return false;
            }
        }
        public static Boolean installDownloaded(int installDownloaded, int download, int enablepreview, int enablecumulative, int enableall)
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
                    return true;
                }
                catch (Exception ex) {
                    //https://docs.microsoft.com/en-us/windows/deployment/update/windows-update-error-reference exception codes
                    if (ex.Message.Contains("0x80240024"))
                    {
                        Console.WriteLine("No updates found");
                        return true;
                    }
                    else { Console.WriteLine("We got an error!: " + ex.Message); }
                    
                    return false; 
                }
                //return true;

            }
        }
    }

    class Program
    {

        static void Main(string[] args)
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
                foreach (Object obj in args)
                {
                    //Console.WriteLine(obj);
                    if (obj.ToString().Contains("--all")) { enableall = 1; Console.WriteLine("Downloading and Installing allthethings."); }
                    if (obj.ToString().Contains("help")){ Console.WriteLine("Help Menu:\n usage: wuh.exe [install||show-available||show-updated||help] [options] \n options: \n --download\n --all\n --enable-hidden\n --enable-previews\n --enable-cumulative\n --security-only \n ex install security updates: wuh install --download --security-only"); }
                    if (obj.ToString().Contains("--download")) { download = 1; Console.WriteLine("Downloading...\n"); }
                    if (obj.ToString().Contains("install")) { installDownloaded = 1; }
                    if (obj.ToString().Contains("show-available")){ showavailable = 1; }
                    if (obj.ToString().Contains("show-updated")){ showinstalled = 1; }
                    if (obj.ToString().Contains("--enable-hidden")) { enablehidden = 1;  Console.WriteLine("Revealing Hidden Updates..."); }
                    if (obj.ToString().Contains("--enable-previews")) { enablepreview = 1; Console.WriteLine("Enabled Preview Updates."); }
                    if (obj.ToString().Contains("--enable-cumulative")) { enablecumulative = 1; }
                    if (obj.ToString().Contains("--security-only"))
                    { 
                        enablehidden = 0;
                        enablepreview = 0;
                        enablecumulative = 0;
                        break;
                    }
                   
                    

                    if ((installDownloaded == 1 | download == 1 ) & (showavailable == 1  | showinstalled ==1)) { Console.WriteLine("Error: cannot have show and install or download directives."); return; }
                    


                }
            }
            // Display title as the C# console calculator app.

            {
                bool result = true;
                if (download == 1 | installDownloaded == 1) { result = Updater.installDownloaded(installDownloaded, download, enablepreview, enablecumulative, enableall); ;  return;}
                if (showavailable == 1 | showinstalled == 1) { result = Updater.showUpdates(showinstalled, showavailable,enablehidden); return;}
                return;

            }

            //Console.ReadKey();
            
        }
            
        }
}