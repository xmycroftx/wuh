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
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json.Nodes;

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
                    UpdateSession updateSession = new UpdateSession();
                    IUpdateSearcher updateSearcher = updateSession.CreateUpdateSearcher();
                    int count = updateSearcher.GetTotalHistoryCount();
                    IUpdateHistoryEntryCollection history = updateSearcher.QueryHistory(0, count);
                    string kb2267602 = "";
                    int afterFilter = 0;
                    var windowsUpdates = new JsonObject();

                    for (int i = count - 1; i >= 0; --i)
                    {
                        if (history[i].HResult == 0)
                        {
                            if (!history[i].Title.Contains("KB2267602"))
                            {
                                windowsUpdates[history[i].UpdateIdentity.UpdateID] = new JsonObject 
                                { 
                                    ["Result"] = history[i].HResult.ToString(),
                                    ["Title"] = history[i].Title.ToString(),
                                    ["Date"] = history[i].Date.ToString()
                                };
                                //result code returns [orcInProgress,orcFailed,orcSucceed]
                                //Console.WriteLine(history[i].ResultCode.ToString());
                                if (history[i].ResultCode.ToString().Contains("orcSucceeded"))
                                {
                                    txtAllUpdates += "\t" + history[i].Title + " " + "\n";
                                }
                                else if (history[i].ResultCode.ToString().Contains("orcInProgress"))
                                {
                                    txtPendingUpdates += "\t" + history[i].Title + "\n";
                                }
                                ++afterFilter;
                            }
                            else
                            {
                                kb2267602 = "\t" + history[i].Title +" "+ history[i].Date.ToString() + "\n";
                            }


                        }

                    }
                    var root = new JsonObject { ["windowsUpdates"] = windowsUpdates };
                    
                    if (showjson == true) { Console.Write(root.ToJsonString()); return 0; }
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

        private static bool ShouldInclude(IUpdate update, bool enableall, bool enablecumulative, bool enablepreview)
        {
            if (enableall == true)
            {
                return true;
            }
            if (update.Title.Contains("Defender") | update.Title.Contains("Malicious"))
            {
                Console.Write("Defender/MalSoftTool: " + update.Title + Environment.NewLine);
                return true;
            }
            if (update.Title.Contains("Security"))
            {
                Console.Write("Security Update: " + update.Title + Environment.NewLine);
                return true;
            }

            if (update.Title.Contains("Update for Windows") & !update.Title.Contains("Cumulative"))
            {
                Console.Write("WinUpdate:" + update.Title + Environment.NewLine);
                return true;
            }
            if (enablecumulative == true)
            {
                if (enablepreview == true)
                {
                    if (update.Title.Contains("Cumulative Update"))
                    {
                        Console.Write("Cumulative Update:" + update.Title + Environment.NewLine);
                        return true;
                    }
                }
                else
                {
                    if (update.Title.Contains("Cumulative Update") & !update.Title.Contains("Preview"))
                    {
                        Console.Write("Cumulative Update:" + update.Title + Environment.NewLine);
                        return true;
                    }
                }
            }
            return false;
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
                    if (ShouldInclude(update, enableall, enablecumulative, enablepreview))
                    {
                        updatesToInstall.Add(update);
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
            UpdateSession uSession = new UpdateSession();
            IUpdateSearcher uSearcher = uSession.CreateUpdateSearcher();
            UpdateCollection updatesToInstall = new UpdateCollection();
            uSearcher.Online = true;

            try
            {
                ISearchResult sResult = uSearcher.Search("IsInstalled=0 And IsHidden=0");
                foreach (IUpdate update in sResult.Updates)
                {
                    if (ShouldInclude(update, enableall, enablecumulative, enablepreview))
                    {
                        updatesToInstall.Add(update);
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

    class Program
    {
        // Flags gathered from the shared global options.
        private sealed class Flags
        {
            public bool Download, All, Hidden, Preview, Cumulative, Json, Optional, Assigned;
        }

        static int Main(string[] args)
        {
            var downloadOption = new Option<bool>("--download", "Download before installing (or download-only when no action is given).");
            var allOption = new Option<bool>("--all", "Enable downloading/installing of all non-optional updates.");
            var hiddenOption = new Option<bool>("--enable-hidden", "Include hidden (WSUS) updates.");
            var previewsOption = new Option<bool>("--enable-previews", "Include preview updates.");
            var cumulativeOption = new Option<bool>("--enable-cumulative", "Include cumulative updates.");
            var jsonOption = new Option<bool>("--json", "Emit machine-readable JSON.");
            var optionalOption = new Option<bool>("--enable-optional", "Optional (BrowseOnly) updates only.");
            var assignedOption = new Option<bool>("--enable-assigned", "Assigned updates only.");
            var securityOnlyOption = new Option<bool>("--security-only", "Security updates only (clears hidden/preview/cumulative/optional).");

            var root = new RootCommand("Windows Update Helper - a CLI for interacting with wuapi.");
            foreach (var opt in new Option[]
            {
                downloadOption, allOption, hiddenOption, previewsOption, cumulativeOption,
                jsonOption, optionalOption, assignedOption, securityOnlyOption
            })
            {
                root.AddGlobalOption(opt);
            }

            Flags ReadFlags(InvocationContext ctx)
            {
                var p = ctx.ParseResult;
                var f = new Flags
                {
                    Download = p.GetValueForOption(downloadOption),
                    All = p.GetValueForOption(allOption),
                    Hidden = p.GetValueForOption(hiddenOption),
                    Preview = p.GetValueForOption(previewsOption),
                    Cumulative = p.GetValueForOption(cumulativeOption),
                    Json = p.GetValueForOption(jsonOption),
                    Optional = p.GetValueForOption(optionalOption),
                    Assigned = p.GetValueForOption(assignedOption),
                };
                if (p.GetValueForOption(securityOnlyOption))
                {
                    f.Hidden = false;
                    f.Preview = false;
                    f.Cumulative = false;
                    f.Optional = false;
                }
                return f;
            }

            var installCommand = new Command("install", "Install available security updates.");
            installCommand.SetHandler(ctx =>
            {
                var f = ReadFlags(ctx);
                if (StatusChecker.pendingReboot(true, true))
                {
                    Console.WriteLine("Machine is Pending Reboots... reboot before installing.\nexiting.");
                    ctx.ExitCode = -1;
                    return;
                }
                ctx.ExitCode = Updater.installDownloaded(true, f.Download, f.Preview, f.Cumulative, f.All);
            });

            var showAvailableCommand = new Command("show-available", "List updates ready to download or install.");
            showAvailableCommand.SetHandler(ctx =>
            {
                var f = ReadFlags(ctx);
                ctx.ExitCode = Updater.showUpdates(false, true, false, f.Hidden, f.Json, f.Optional, f.Assigned);
            });

            var showUpdatedCommand = new Command("show-updated", "List installed updates (the isInstalled list).");
            showUpdatedCommand.SetHandler(ctx =>
            {
                var f = ReadFlags(ctx);
                ctx.ExitCode = Updater.showUpdates(true, false, false, f.Hidden, f.Json, f.Optional, f.Assigned);
            });

            var showPendingCommand = new Command("show-pending", "List updates that are in progress.");
            showPendingCommand.SetHandler(ctx =>
            {
                var f = ReadFlags(ctx);
                ctx.ExitCode = Updater.showUpdates(false, false, true, f.Hidden, f.Json, f.Optional, f.Assigned);
            });

            root.AddCommand(installCommand);
            root.AddCommand(showAvailableCommand);
            root.AddCommand(showUpdatedCommand);
            root.AddCommand(showPendingCommand);

            // No subcommand: --download downloads available security updates without installing.
            root.SetHandler(ctx =>
            {
                var f = ReadFlags(ctx);
                if (!f.Download)
                {
                    return;
                }
                if (StatusChecker.pendingReboot(true, true))
                {
                    Console.WriteLine("Machine is Pending Reboots... reboot before installing.\nexiting.");
                    ctx.ExitCode = -1;
                    return;
                }
                ctx.ExitCode = Updater.installDownloaded(false, true, f.Preview, f.Cumulative, f.All);
            });

            return root.Invoke(args);
        }
    }
}
