﻿using R5R_Downloader.Properties;
using SuRGeoNix;
using SuRGeoNix.BitSwarmLib;
using SuRGeoNix.BitSwarmLib.BEP;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace R5R_Downloader
{
    public partial class Form1 : Form
    {
        static Torrent torrent;
        static BitSwarm bitSwarm;
        static Options opt;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            panel1.Location = new Point(12, 40);
            downloadpanel.Location = new Point(12, 40);
            this.Size = new Size(597, 356);
            CenterToScreen();

            if(Settings.Default.DownloadPath != "")
            {
                if (!Directory.Exists(Settings.Default.DownloadPath + "/R5R-Downloading-Temp/"))
                {
                    MessageBox.Show("Can not find previously downloaded files, restarting download!");

                    Settings.Default.DownloadPath = "";
                    Settings.Default.Save();

                    if (Directory.Exists(Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Temp\BitSwarm")))
                        Directory.Delete(Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Temp\BitSwarm"), true);
                }
                else
                {
                    panel1.Visible = false;
                    downloadpanel.Visible = true;
                    guna2Button2.Enabled = true;
                }
            }
            else
            {
                panel1.Visible = true;
                downloadpanel.Visible = false;
                guna2Button2.Enabled = false;
            }

            guna2AnimateWindow1.SetAnimateWindow(this, Guna.UI2.WinForms.Guna2AnimateWindow.AnimateWindowType.AW_BLEND);

            Refresh();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (Settings.Default.DownloadPath != "")
            {
                if (button1.Text == "Start")
                {
                    output.Text = "";
                    guna2Button1.Enabled = false;
                    try
                    {
                        opt = new Options();

                        opt.FolderComplete = @Settings.Default.DownloadPath;
                        opt.FolderIncomplete = @Settings.Default.DownloadPath + "/R5R-Downloading-Temp/";

                        opt.MaxTotalConnections = 120;
                        opt.MaxNewConnections = 300;
                        opt.PeersFromTracker = -1;
                        opt.ConnectionTimeout = 4000;
                        opt.HandshakeTimeout = 4000;
                        opt.PieceTimeout = 5500;
                        opt.MetadataTimeout = 1900;

                        opt.Verbosity = 0;
                        opt.LogDHT = false;
                        opt.LogStats = false;
                        opt.LogTracker = false;
                        opt.LogPeer = false;

                        output.Text = "Started at " + DateTime.Now.ToString("G", DateTimeFormatInfo.InvariantInfo) + "\r\n";
                        button1.Text = "Stop";

                        bitSwarm = new BitSwarm(opt);

                        bitSwarm.StatsUpdated += BitSwarm_StatsUpdated;
                        bitSwarm.MetadataReceived += BitSwarm_MetadataReceived;
                        bitSwarm.StatusChanged += BitSwarm_StatusChanged;

                        bitSwarm.Open("magnet:?xt=urn:btih:KCQJQT6DV2V4XWCOKCRM4EJELRLHQKI5&dn=R5pc_r5launch_N1094_CL456479_2019_10_30_05_20_PM&tr=udp%3A%2F%2Fwambo.club%3A1337%2Fannounce");
                        bitSwarm.Start();
                    }
                    catch (Exception e1)
                    {
                        MessageBox.Show("Can not find previously downloaded files, restarting download!");

                        if (Directory.Exists(Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Temp\BitSwarm")))
                            Directory.Delete(Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Temp\BitSwarm"), true);

                        button1.Text = "Start";
                        button1.PerformClick();
                    }
                }
                else
                {
                    bitSwarm.Dispose();
                    button1.Text = "Start";
                    guna2Button1.Enabled = true;
                }
            }
            else
            {
                MessageBox.Show("Please select a path to download to!");
            }
        }

        private void BitSwarm_MetadataReceived(object source, BitSwarm.MetadataReceivedArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => BitSwarm_MetadataReceived(source, e)));
                return;
            }
            else
            {
                torrent = e.Torrent;
                output.Text += bitSwarm.DumpTorrent().Replace("\n", "\r\n");
            }
        }
        private void BitSwarm_StatusChanged(object source, BitSwarm.StatusChangedArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => BitSwarm_StatusChanged(this, e)));
                return;
            }

            button1.Text = "Start";

            if (e.Status == 0)
            {
                string fileName = "";
                if (torrent.file.name != null) fileName = torrent.file.name;
                if (torrent != null) { torrent.Dispose(); torrent = null; }

                output.Text += "\r\n\r\nFinished at " + DateTime.Now.ToString("G", DateTimeFormatInfo.InvariantInfo);
                MessageBox.Show("Downloaded successfully!\r\n" + "Starting detours and scripts install.");
                StartR5RDetoursAndScripts();
            }
            else
            {
                output.Text += "\r\n\r\nStopped at " + DateTime.Now.ToString("G", DateTimeFormatInfo.InvariantInfo);

                if (e.Status == 2)
                {
                    output.Text += "\r\n\r\n" + "An error occurred :(\r\n\t" + e.ErrorMsg;
                    MessageBox.Show("An error occured :( \r\n" + e.ErrorMsg);
                }
            }

            if (torrent != null) torrent.Dispose();
        }
        private void BitSwarm_StatsUpdated(object source, BitSwarm.StatsUpdatedArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => BitSwarm_StatsUpdated(source, e)));
                return;
            }
            else
            {
                downRate.Text = String.Format("{0:n0}", (e.Stats.DownRate / 1024)) + " KB/s";
                downRateAvg.Text = String.Format("{0:n0}", (e.Stats.AvgRate / 1024)) + " KB/s";
                eta.Text = TimeSpan.FromSeconds((e.Stats.ETA + e.Stats.AvgETA) / 2).ToString(@"hh\:mm\:ss");
                bDownloaded.Text = Utils.BytesToReadableString(e.Stats.BytesDownloaded + e.Stats.BytesDownloadedPrevSession);
                dpeers.Text = e.Stats.PeersTotal.ToString();

                if (torrent != null && torrent.data.totalSize != 0)
                    progress.Value = e.Stats.Progress;
            }

        }

        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }


        private void StartR5RDetoursAndScripts()
        {
            if (Directory.Exists(Settings.Default.DownloadPath + "/R5pc_r5launch_N1094_CL456479_2019_10_30_05_20_PM/"))
            {
                Thread thread = new Thread(() =>
                {
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        bDownloaded.Text = "Downloading Detours";
                    });

                    string randomestring = RandomString(10);

                    WebClient scriptsdownload = new WebClient();

                    string downloadString = scriptsdownload.DownloadString("https://api.r5rmodmanager.com/v1.php?data=detours");
                    scriptsdownload.DownloadFile(new Uri(downloadString), @Settings.Default.DownloadPath + "/R5pc_r5launch_N1094_CL456479_2019_10_30_05_20_PM/detours-" + randomestring + ".zip");

                    Thread.Sleep(1000);

                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        bDownloaded.Text = "Installing Detours";
                    });

                    var detoursextract = ZipFile.Open(Settings.Default.DownloadPath + "/R5pc_r5launch_N1094_CL456479_2019_10_30_05_20_PM/detours-" + randomestring + ".zip", ZipArchiveMode.Read);
                    ZipArchiveExtensions.ExtractToDirectory(detoursextract, Settings.Default.DownloadPath + "/R5pc_r5launch_N1094_CL456479_2019_10_30_05_20_PM/", true);
                    detoursextract.Dispose();

                    File.Delete(Settings.Default.DownloadPath + "/R5pc_r5launch_N1094_CL456479_2019_10_30_05_20_PM/detours-" + randomestring + ".zip");

                    Thread.Sleep(1000);

                    if (!Directory.Exists(Settings.Default.DownloadPath + "/R5pc_r5launch_N1094_CL456479_2019_10_30_05_20_PM/platform"))
                    {
                        Directory.CreateDirectory(Settings.Default.DownloadPath + "/R5pc_r5launch_N1094_CL456479_2019_10_30_05_20_PM/platform");
                    }

                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        bDownloaded.Text = "Downloading Scripts";
                    });

                    scriptsdownload.DownloadFile(new Uri("https://github.com/Mauler125/scripts_r5/archive/refs/heads/S3_N1094.zip"), @Settings.Default.DownloadPath + "/R5pc_r5launch_N1094_CL456479_2019_10_30_05_20_PM/platform/newscripts.zip");

                    Thread.Sleep(1000);

                    if (Directory.Exists(Settings.Default.DownloadPath + "/R5pc_r5launch_N1094_CL456479_2019_10_30_05_20_PM/platform/scripts"))
                    {
                        Directory.Delete(Settings.Default.DownloadPath + "/R5pc_r5launch_N1094_CL456479_2019_10_30_05_20_PM/platform/scripts", true);
                    }

                    var scriptszip = ZipFile.Open(Settings.Default.DownloadPath + "/R5pc_r5launch_N1094_CL456479_2019_10_30_05_20_PM/platform/newscripts.zip", ZipArchiveMode.Read);
                    ZipArchiveExtensions.ExtractToDirectory(scriptszip, Settings.Default.DownloadPath + "/R5pc_r5launch_N1094_CL456479_2019_10_30_05_20_PM/platform/", true);
                    scriptszip.Dispose();

                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        bDownloaded.Text = "Installing Scripts";
                    });

                    Thread.Sleep(1000);

                    File.Delete(Settings.Default.DownloadPath + "/R5pc_r5launch_N1094_CL456479_2019_10_30_05_20_PM/platform/newscripts.zip");

                    Directory.Move(Settings.Default.DownloadPath + "/R5pc_r5launch_N1094_CL456479_2019_10_30_05_20_PM/platform/scripts_r5-S3_N1094", Settings.Default.DownloadPath + "/R5pc_r5launch_N1094_CL456479_2019_10_30_05_20_PM/platform/scripts");

                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        MessageBox.Show("Scripts and detours have been installed!");
                        bDownloaded.Text = "Installing Complete";
                    });
                });
                thread.Start();
            }
            else
            {
                MessageBox.Show("Somthing went wrong and cant continue!");
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (bitSwarm != null) bitSwarm.Dispose();
        }

        private void guna2Panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void guna2Button1_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    Settings.Default.DownloadPath = fbd.SelectedPath;
                    Settings.Default.Save();
                    guna2TextBox1.Text = Settings.Default.DownloadPath;
                    UpdateContinue();
                }
            }
        }
        
        private void UpdateContinue()
        {
            if (Settings.Default.DownloadPath != "")
            {
                guna2Button2.Enabled = true;
            }
            else
            {
                guna2Button2.Enabled = false;
            }
        }

        private void guna2Button2_Click(object sender, EventArgs e)
        {
            if(Settings.Default.DownloadPath != "")
            {
                guna2Transition1.Hide(panel1);
                guna2Transition1.Show(downloadpanel);
            }
            else
            {
                MessageBox.Show("Please select a path to download to!");
            }
        }
    }

    public static class ZipArchiveExtensions
    {
        public static void ExtractToDirectory(this ZipArchive archive, string destinationDirectoryName, bool overwrite)
        {
            if (!overwrite)
            {
                archive.ExtractToDirectory(destinationDirectoryName);
                return;
            }
            foreach (ZipArchiveEntry file in archive.Entries)
            {
                string completeFileName = Path.Combine(destinationDirectoryName, file.FullName);
                string directory = Path.GetDirectoryName(completeFileName);

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                if (file.Name != "")
                    file.ExtractToFile(completeFileName, true);
            }
        }
    }
}
