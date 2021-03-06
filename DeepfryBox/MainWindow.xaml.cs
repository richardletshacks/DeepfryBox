﻿using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Xabe.FFmpeg;

namespace DeepfryBox
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string ffmpegPath = "./ffmpeg-binaries";
        bool running = false;

        public MainWindow()
        {
            InitializeComponent();

            FFmpeg.SetExecutablesPath(ffmpegPath);
        }

        private void CloseLabel_Clicked(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void BrowseInputButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.DefaultExt = ".mp4";
            dlg.Filter = "Video Files|*.mp4;*.avi;*.mov";
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                InputPathBox.Text = dlg.FileName;
            }
        }

        private void Input_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                string filePath = files[0];
                InputPathBox.Text = filePath;
            }
        }

        private void BrowseOutputButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.DefaultExt = ".mp4";
            if (IsValidPath(InputPathBox.Text))
                dlg.FileName = Path.GetFileNameWithoutExtension(InputPathBox.Text) + "_deepfried.mp4";
            dlg.Filter = "Output File|*.mp4";
            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                OutputPathBox.Text = dlg.FileName;
            }
        }

        private void DeepfryButton_Click(object sender, RoutedEventArgs e)
        {
            string inputPath = InputPathBox.Text;
            string outputPath = OutputPathBox.Text;

            if (!IsValidPath(inputPath) || !File.Exists(inputPath))
            {
                NotificationBar.MessageQueue.Enqueue("Input path is not valid!");
                return;
            }

            if (!IsValidPath(outputPath))
            {
                NotificationBar.MessageQueue.Enqueue("Output path is not valid!");
                return;
            }

            if(!IsFFmpegInstalled())
            {
                NotificationBar.MessageQueue.Enqueue("FFmpeg binaries not found!");
                return;
            }

            if (running)
                NotificationBar.MessageQueue.Enqueue("Already running!");
            else
                Deepfry(inputPath, outputPath);
        }

        private async void Deepfry(String inputPath, String outputPath)
        {
            running = true;

            IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(inputPath).ConfigureAwait(true);
            IStream videoStream = mediaInfo.VideoStreams.FirstOrDefault();
            IStream audioStream = mediaInfo.AudioStreams.FirstOrDefault();

            IConversion c = FFmpeg.Conversions.New();
            c.AddStream(audioStream, videoStream);
            c.SetOutput(outputPath);
            c.SetOverwriteOutput(true);
            c.SetVideoBitrate((long)VideoBitrateSlider.Value);
            c.SetAudioBitrate((long)AudioBitrateSlider.Value);
            c.AddParameter("-filter:v fps=fps=" + FramerateSlider.Value);
            c.OnProgress += (sender, args) => { this.Dispatcher.Invoke(() => {
                DoubleAnimation anim = new DoubleAnimation(args.Percent, TimeSpan.FromSeconds(0.5));
                ConversionProgressBar.BeginAnimation(ProgressBar.ValueProperty, anim);
            }); };
            
            await c.Start().ConfigureAwait(true);

            running = false;
            NotificationBar.MessageQueue.Enqueue("Complete!", "OPEN", new Action(() => Process.Start("explorer.exe", outputPath)));
        }

        private bool IsValidPath(String path)
        {
            if (string.IsNullOrEmpty(path))
                return false;
            switch (Path.GetExtension(path)) {
                case ".mp4":
                    break;
                case ".avi":
                    break;
                case ".mov":
                    break;
                default:
                    return false;
            }
            try
            {
                System.Security.AccessControl.DirectorySecurity ds = Directory.GetAccessControl(Directory.GetParent(path).FullName);
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            return true;
        }

        private bool IsFFmpegInstalled()
        {
            if (!File.Exists(ffmpegPath + "/ffmpeg.exe")) return false;
            if (!File.Exists(ffmpegPath + "/ffprobe.exe")) return false;
            if (!File.Exists(ffmpegPath + "/ffplay.exe")) return false;
            return true;
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
        }
    }
}
