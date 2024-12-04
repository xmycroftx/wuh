using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using WUApiLib;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Win32;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics.Eventing.Reader;

namespace wuh
{
    class StatusChecker
    {
        public static bool pendingReboot(bool security, bool feature) {
            const string pendUpdateRegPath = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\WindowsUpdate\\Auto Update\\RebootRequired";
            //const string pendUpdateRegPath = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\WindowsUpdate\\Auto Update";

            if ( null != Registry.GetValue(pendUpdateRegPath, "foo",  "false" ) ) { 
                //Console.WriteLine("Reboot Pending...");
                return true;
                };

           //string regval = (string) Registry.GetValue(pendUpdateRegPath, "RebootRequire", "false");
            //Console.WriteLine("Regvalues for " + pendUpdateRegPath + "RebootRequired\n" + regval);
            return false; 
        }
    }
    class Updater
    {
            public static int notifyUser(bool notifyBubble, bool notifyWindow)
        {

            if (notifyBubble == false) { Console.Write("NotifyBubbleOff"); } else
            {
                var item = new NotifyIcon(); 
                item.Visible = true;
                item.Icon = System.Drawing.SystemIcons.Information;
                item.ShowBalloonTip(30000, "Title", "Contents", ToolTipIcon.Info);
            }
            if (notifyWindow == false) { Console.Write("NotifyWindowoff"); }
            else
            {
                var formPopup = new Form();
                formPopup.MinimumSize = new Size(1920, 1080);
                formPopup.MinimizeBox = false;
                formPopup.WindowState = FormWindowState.Maximized;
                // quick and dirty "add stuff to form"
                Label fielda = new Label()
                { Text = "Text Box Label", Location = new Point(10, 10), TabIndex = 10 };
                formPopup.Controls.Add(fielda);
                //breaks threading here, it's waiting on the dialog to be closed.
                //formPopup.ShowDialog();
                //MessageBox.Show("HEY UPDATE YOUR MACHINE!","The Update Police");
            }
            return 0;
        }
        public static string makeSearchString(bool showinstalled, bool showavailable, bool showpending, bool showhidden, bool showjson, bool showoptional, bool showassigned)
        {
            if (showpending == true) { showinstalled = true; }
            if (showinstalled == true)
            {
                string searchStr = "IsInstalled=1 And ";

                if (showhidden == true)

                {
                    searchStr = searchStr + "IsHidden=1";
                }
                else
                {
                    searchStr = searchStr + "IsHidden=0";
                }
                if (showoptional == true)
                {
                    searchStr = "BrowseOnly=1";
                }
                if (showassigned == true)
                {
                    searchStr = "isAssigned=1";
                }
                return searchStr;
            } else { return "";}
        }
            public static int showUpdates(bool showinstalled, bool showavailable, bool showpending, bool showhidden, bool showjson, bool showoptional, bool showassigned)
            { 
            string txtPendingUpdates = "";
            UpdateSession uSession = new UpdateSession();
            IUpdateSearcher uSearcher = uSession.CreateUpdateSearcher();
            uSearcher.Online = true;
            string searchStr=makeSearchString(showinstalled, showavailable, showpending, showhidden, showjson, showoptional, showassigned);
            try
            {
                    string txtAllUpdates = "";
                    string jsonAllUpdates = "{\"windowsUpdates\": { ";
                    UpdateSession updateSession = new UpdateSession();
                    IUpdateSearcher updateSearcher = updateSession.CreateUpdateSearcher();
                    int count = updateSearcher.GetTotalHistoryCount();
                    IUpdateHistoryEntryCollection history = updateSearcher.QueryHistory(0, count);
                    string kb2267602 = "";
                    int afterFilter = 0;

                    for (int i = count - 1; i >= 0; --i)
                    {
                        if (history[i].HResult == 0 || true)
                        {
                            if (!history[i].Title.Contains("KB2267602"))
                            {
                                if (i != count - 1) 
                                {
                                    jsonAllUpdates += ",";
                                }
                                jsonAllUpdates += "\"" + history[i].UpdateIdentity.UpdateID + "\": {";
                                jsonAllUpdates += "\"" + "Result" + "\": \"" + history[i].HResult.ToString() + "\",";
                                jsonAllUpdates += "\"" + "Title" + "\": \"" + history[i].Title.ToString() + "\",";
                                jsonAllUpdates += "\"" + "Date" + "\": \"" + history[i].Date.ToString() + "\"";
                                jsonAllUpdates += "}";
                                //result code returns [orcInProgress,orcFailed,orcSucceed]
                                //Console.WriteLine(history[i].ResultCode.ToString());
                                if (history[i].ResultCode.ToString().Contains("orcSucceeded"))
                                {
                                    txtAllUpdates += "\t" + history[i].Title + " " + "\n";
                                }
                                else if (history[i].ResultCode.ToString().Contains("orcInProgress"))
                                {
                                    txtPendingUpdates += txtPendingUpdates += "\t" + history[i].Title + "\n";
                                }
                                ++afterFilter;
                            }
                            else
                            {
                                kb2267602 = "\t" + history[i].Title +" "+ history[i].Date.ToString() + "\n";
                            }


                        }

                    }
                    jsonAllUpdates += "}}";
                    ++afterFilter;
                    
                    if (showjson == true) { Console.Write(jsonAllUpdates); return 0; }
                    else if (showinstalled == true) { Console.Write(txtAllUpdates); }              
                    Console.WriteLine("Total Update History Count :" + count);
                    Console.WriteLine("Last Defender Signature: \n"+ kb2267602);
                    Console.WriteLine( "Filtered Updates :" + afterFilter);
                    if (showpending == true)
                    {
                        Console.WriteLine("Pending Updates:\n");
                        Console.Write(txtPendingUpdates);
                    }

                    if (showavailable == true)

                    {
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

        public static int installMatching(string searchStr)
        {
            UpdateSession uSession = new UpdateSession();
            IUpdateSearcher uSearcher = uSession.CreateUpdateSearcher();
            UpdateCollection updatesToInstall = new UpdateCollection();
            uSearcher.Online = true;
            try
            {
                ISearchResult sResult = uSearcher.Search(searchStr);
                foreach (IUpdate update in sResult.Updates)
                {
                    if (enableall == true)
                    {
                        updatesToInstall.Add(update);
                        continue;
                    }
                    if (update.Title.Contains("Defender") | update.Title.Contains("Malicious"))
                    {
                        Console.Write("Defender/MalSoftTool: " + update.Title + Environment.NewLine);
                        updatesToInstall.Add(update);
                        continue;
                    }
                    if (update.Title.Contains("Security"))
                    {
                        Console.Write("Security Update: " + update.Title + Environment.NewLine);
                        updatesToInstall.Add(update);
                        continue;
                    }

                    if (update.Title.Contains("Update for Windows") & !update.Title.Contains("Cumulative"))
                    {
                        Console.Write("WinUpdate:" + update.Title + Environment.NewLine);
                        updatesToInstall.Add(update);
                        continue;
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
                            Console.Write("Failed to Download: " + updatesToInstall[i].Title + Environment.NewLine);
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
            catch (Exception ex)
            {
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
        public static int installDownloaded(bool installDownloaded, bool download, bool enablepreview, bool enablecumulative, bool enableall)

        {
            {




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
                        if (update.Title.Contains("Defender")|update.Title.Contains("Malicious"))
                        {
                            Console.Write("Defender/MalSoftTool: " + update.Title + Environment.NewLine);
                            updatesToInstall.Add(update);
                            continue;
                        }                        
                        if (update.Title.Contains("Security"))
                        {
                            Console.Write("Security Update: " + update.Title + Environment.NewLine);
                            updatesToInstall.Add(update);
                            continue;
                        }
                        
                        if (update.Title.Contains("Update for Windows") & !update.Title.Contains("Cumulative"))
                        {
                            Console.Write("WinUpdate:" + update.Title + Environment.NewLine);
                            updatesToInstall.Add(update);
                            continue;
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
        static int Main(string[] args)
        {
            bool showavailable = false;
            bool showinstalled = false;
            bool showpending = false;
            bool enablehidden = false;
            bool enablepreview = false;
            bool enablecumulative = false;
            bool download = false;
            bool installDownloaded = false;
            bool enableall = false;
            bool enablejson = false;
            bool enableoptional = false;
            bool enableassigned = false;
            //Updater.notifyUser(true, true);
            //Console.WriteLine("Windows Update Helper\r");
            //Console.WriteLine("------------------------\n");
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
                    if (obj.ToString().Contains("install")) { installDownloaded = true; }
                    if (obj.ToString().Contains("show-updated"))
                    {
                        Console.WriteLine("Showing updates successfully installed");
                        showinstalled = true;
                    }
                    if (obj.ToString().Contains("show-pending"))
                    {
                        Console.WriteLine("Showing Pending Updates...");
                        showpending = true; 
                    }
                    if (obj.ToString().Contains("show-available"))
                    {
                        Console.WriteLine("Showing updates available to Download/Install");
                        showavailable = true;
                    }

                    indexer++;
                    if (obj.ToString().Contains("help")) { Console.WriteLine("Help Menu:\n usage: wuh.exe [install||show-available||show-updated||help] [options] \n options: \n --download\n --all\n --enable-hidden\n --enable-previews\n --enable-cumulative\n --security-only \n ex install security updates: wuh install --download --security-only"); }
                    if (obj.ToString().Contains("--all")) { enableall = true; }
                    if (obj.ToString().Contains("--download")) { download = true; Console.WriteLine("Downloading...\n"); }
                    if (obj.ToString().Contains("--enable-hidden")) { enablehidden = true;  Console.WriteLine("Revealing Hidden Updates..."); }
                    if (obj.ToString().Contains("--enable-previews")) { enablepreview = true; Console.WriteLine("Enabled Preview Updates."); }
                    if (obj.ToString().Contains("--enable-cumulative")) { enablecumulative = true; Console.WriteLine("Enabled Cumulative Updates."); }
                    if (obj.ToString().Contains("--json")) { enablejson = true; }
                    if (obj.ToString().Contains("--enable-optional")) { enableoptional = true; Console.WriteLine("optional updates only..."); }
                    if (obj.ToString().Contains("--enable-assigned")) { enableassigned = true; Console.WriteLine("assigned updates only..."); }
                    if (obj.ToString().Contains("--security-only"))
                    {
                        enablehidden = false;
                        enablepreview = false;
                        enablecumulative = false;
                        enableoptional = false;
                        break;
                    }
                    if ((installDownloaded == true | download == true ) & (showavailable == true  | showinstalled == true)) { Console.WriteLine("Error: cannot have show and install or download directives."); return 1; }
                }
            }

            int result = 0;
            if (download == true | installDownloaded == true) 
            {
                if (StatusChecker.pendingReboot(true, true) == true)
                {
                    if (showpending == true) { result = Updater.showUpdates(showinstalled, showavailable, showpending, enablehidden, enablejson, enableoptional, enableassigned); return 0; }
                    Console.WriteLine("Machine is Pending Reboots... reboot before installing.\nexiting.");
                    return (-1);
                }
                result = Updater.installDownloaded(installDownloaded, download, enablepreview, enablecumulative, enableall);
                return 0; 
            }
            if (showavailable == true || showinstalled == true ||showpending == true) { result = Updater.showUpdates(showinstalled, showavailable,showpending, enablehidden,enablejson,enableoptional,enableassigned); return 0;}
            return 0;  
        }   
     }

}
