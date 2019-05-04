﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using TeknoParrotUi.Common;
using static TeknoParrotUi.MainWindow;
using Application = System.Windows.Application;

namespace TeknoParrotUi.Views
{
    /// <summary>
    /// Interaction logic for GitHubUpdates.xaml
    /// </summary>
    public partial class GitHubUpdates : Window
    {
        private readonly UpdaterComponent _componentUpdated;
        private readonly GithubRelease _latestRelease;
        private DownloadWindow downloadWindow;
        public GitHubUpdates(UpdaterComponent componentUpdated, GithubRelease latestRelease)
        {
            InitializeComponent();
            _componentUpdated = componentUpdated;
            labelUpdated.Content = componentUpdated.name;
            _latestRelease = latestRelease;
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnChangelog(object sender, RoutedEventArgs e)
        {
            var reponame = !string.IsNullOrEmpty(_componentUpdated.reponame) ? _componentUpdated.reponame : _componentUpdated.name;
            var baselink = $"https://github.com/teknogods/{reponame}/";
            Process.Start(baselink + (_componentUpdated.opensource ? "commits/master" : $"releases/{_componentUpdated.name}"));
        }

        private void afterDownload(object sender, EventArgs e)
        {
            bool isUI = _componentUpdated.name == "TeknoParrotUI";
            string destinationFolder = !string.IsNullOrEmpty(_componentUpdated.folderOverride) ? _componentUpdated.folderOverride : _componentUpdated.name;

            if (!isUI)
            {
                Directory.CreateDirectory(destinationFolder);
            }

            using (var memoryStream = new MemoryStream(downloadWindow.data))
            using (var zip = new ZipArchive(memoryStream, ZipArchiveMode.Read))
            {
                foreach (var entry in zip.Entries)
                {
                    var dest = isUI ? entry.FullName : Path.Combine(destinationFolder, entry.FullName);
                    Debug.WriteLine($"Updater file: {entry.FullName} extracting to: {dest}");

                    try
                    {
                        if (File.Exists(dest))
                            File.Delete(dest);
                    }
                    catch (UnauthorizedAccessException uae)
                    {
                        // couldn't delete, just move for now
                        File.Move(dest, dest + ".bak");
                    }

                    try
                    {
                        using (var entryStream = entry.Open())
                        using (var dll = File.Create(dest))
                        {
                            entryStream.CopyTo(dll);
                        }
                    }
                    catch
                    {
                        // ignore..?
                    }
                }
            }

            if (_componentUpdated.name == "TeknoParrotUI")
            {
                if (MessageBox.Show(
                        $"Would you like to restart me to finish the update? Otherwise, I will close TeknoParrotUi for you to reopen.",
                        "Update Complete", MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                {
                    string[] psargs = Environment.GetCommandLineArgs();
                    System.Diagnostics.Process.Start(Application.ResourceAssembly.Location, psargs[0]);
                    Application.Current.Shutdown();
                }
                else
                {
                    Application.Current.Shutdown();
                }
            }
        }

        private void ButtonUpdate_Click(object sender, RoutedEventArgs e)
        {
            downloadWindow = new DownloadWindow(_latestRelease.assets[0].browser_download_url);
            downloadWindow.Closed += afterDownload;
            downloadWindow.Show();
        }
    }
}
