﻿using UciGui.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Xceed.Wpf.Toolkit;
using System.Text.RegularExpressions;

namespace UciGui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public string Fen
        {
            get { return (string)GetValue(FenProperty); }
            set { SetValue(FenProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Fen.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty FenProperty =
            DependencyProperty.Register("Fen", typeof(string), typeof(MainWindow), new PropertyMetadata(null));


        public UIElement[] Options
        {
            get { return (UIElement[])GetValue(OptionsProperty); }
            set { SetValue(OptionsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Options.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OptionsProperty =
            DependencyProperty.Register("Options", typeof(UIElement[]), typeof(MainWindow), new PropertyMetadata(null));


        public string BestMove
        {
            get { return (string)GetValue(BestMoveProperty); }
            set { SetValue(BestMoveProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BestMove.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BestMoveProperty =
            DependencyProperty.Register("BestMove", typeof(string), typeof(MainWindow), new PropertyMetadata(null));


        public string Ponder
        {
            get { return (string)GetValue(PonderProperty); }
            set { SetValue(PonderProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Ponder.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PonderProperty =
            DependencyProperty.Register("Ponder", typeof(string), typeof(MainWindow), new PropertyMetadata(null));


        public bool IsBusy
        {
            get { return (bool)GetValue(IsBusyProperty); }
            set { SetValue(IsBusyProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsBusy.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsBusyProperty =
            DependencyProperty.Register("IsBusy", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));


        Process process;


        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create process start information
                var processStartInfo = new ProcessStartInfo(Settings.Default.UciEngineExe);
                processStartInfo.UseShellExecute = false;
                processStartInfo.ErrorDialog = false;
                processStartInfo.RedirectStandardError = true;
                processStartInfo.RedirectStandardInput = true;
                processStartInfo.RedirectStandardOutput = true;
                processStartInfo.CreateNoWindow = true;

                // Start process
                process = new Process();
                process.StartInfo = processStartInfo;
                bool processStarted = process.Start();

                if (processStarted)
                {
                    var optionLines = GetOptionLines(process);

                    UpdateGuiOptions(optionLines);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }

        private void UpdateGuiOptions(List<string> optionLines)
        {
            var options = optionLines.Select(f => new Option
            {
                Name = GetWord(f, "name", "type"),
                Type = GetWord(f, "type", "default"),
                Default = GetWord(f, "default", "min"),
                Minimum = Convert.ToInt32(GetWord(f, "min", "max")),
                Maximum = Convert.ToInt32(GetWord(f, "max", null)),
                Items = GetItems(f, "var"),
            });

            var o = new List<UIElement>();

            foreach (var option in options)
            {
                switch (option.Type)
                {
                    case "spin":
                        var dp = new DockPanel { Margin = new Thickness(2) };
                        dp.Children.Add(new TextBlock { Text = option.Name + ": " });
                        var sld = new Slider { Value = Convert.ToInt32(option.Default), Minimum = option.Minimum, Maximum = option.Maximum, SmallChange = 1, IsSnapToTickEnabled = true, AutoToolTipPlacement = AutoToolTipPlacement.TopLeft };
                        sld.ValueChanged += (s, e) =>
                        {
                            SetOption(option, sld.Value.ToString());
                        };
                        DockPanel.SetDock(sld, Dock.Right);
                        dp.Children.Add(sld);
                        o.Add(dp);
                        break;
                    case "check":
                        var cb = new CheckBox { Content = option.Name, IsChecked = Convert.ToBoolean(option.Default), Margin = new Thickness(2) };
                        cb.Checked += (s, e) =>
                        {
                            SetOption(option, "true");
                        };
                        cb.Unchecked += (s, e) =>
                        {
                            SetOption(option, "false");
                        };
                        o.Add(cb);
                        break;
                    case "string":
                        var wtb = new WatermarkTextBox { Watermark = option.Name, Text = option.Default, Margin = new Thickness(2) };
                        wtb.TextChanged += (s, e) =>
                        {
                            SetOption(option, wtb.Text);
                        };
                        o.Add(wtb);
                        break;
                    case "button":
                        var btn = new Button { Content = option.Name, Margin = new Thickness(2) };
                        btn.Click += (s, e) =>
                        {
                            SetOption(option, null);
                        };
                        o.Add(btn);
                        break;
                    case "combo":
                        var cbx = new ComboBox { ItemsSource = option.Items, Margin = new Thickness(2) };
                        cbx.SelectionChanged += (s, e) =>
                        {
                            SetOption(option, (string)cbx.SelectedItem);
                        };
                        o.Add(cbx);
                        break;
                }
            }

            Options = o.ToArray();
        }

        private string[] GetItems(string optionLine, string previousWord)
        {
            var items = new List<string>();

            while (optionLine.Contains(" var "))
            {
                items.Add(GetWord(optionLine, "var", "var"));
                var secondIndex = optionLine.IndexOf("var ", 0, 2);

                if (secondIndex != -1)
                {
                    optionLine = optionLine.Substring(secondIndex);
                }
            }

            return items.ToArray();
        }

        private List<string> GetOptionLines(Process process)
        {
            process.StandardInput.WriteLine("uci");

            var options = new List<string>();

            while (true)
            {
                var line = process.StandardOutput.ReadLine();

                if (line != null)
                {
                    if (line.StartsWith("option"))
                    {
                        options.Add(line);
                    }
                    else if (line.StartsWith("uciok"))
                    {
                        return options;
                    }
                }
            }
        }

        private void SetOption(Option option, string newValue)
        {
            if (newValue == null)
            {
                process.StandardInput.WriteLine("setoption name {0}", option.Name);
            }
            else
            {
                process.StandardInput.WriteLine("setoption name {0} value {1}", option.Name, newValue);
            }
        }

        private string GetWord(string optionLine, string previousWord, string nextWord)
        {
            var previousWordIndex = optionLine.IndexOf(previousWord);

            if (previousWordIndex < 0)
            {
                return null;
            }
            else
            {
                var startIndex = previousWordIndex + previousWord.Length + 1;

                if (nextWord == null)
                {
                    return optionLine.Substring(startIndex);
                }
                else
                {
                    var nextWordIndex = optionLine.IndexOf(nextWord);

                    if (nextWordIndex < 0)
                    {
                        return optionLine.Substring(startIndex);
                    }
                    else
                    {
                        var length = nextWordIndex - 1 - startIndex;

                        return optionLine.Substring(startIndex, length);
                    }
                }
            }
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                ((ToggleButton)sender).Content = "STOP";
                IsBusy = true;

                BestMove = null;
                Ponder = null;

                process.StandardInput.WriteLine("ucinewgame");

                Fen = Fen.Trim();

                if (string.IsNullOrWhiteSpace(Fen))
                {
                    process.StandardInput.WriteLine("position startpos");
                }
                else
                {
                    process.StandardInput.WriteLine("position fen {0}", Fen);
                }

                process.StandardInput.WriteLine("go infinite");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                ((ToggleButton)sender).Content = "GO";
                IsBusy = false;

                process.StandardInput.WriteLine("stop");

                while (true)
                {
                    var line = process.StandardOutput.ReadLine();

                    if (line != null && line.StartsWith("bestmove"))
                    {
                        BestMove = GetWord(line, "bestmove", "ponder");
                        Ponder = GetWord(line, "ponder", null);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }

        private void ParseFen(string fen)
        {
            if (!IsFenValid(fen))
                return;


        }

        private bool IsFenValid(string fen)
        {
            if (string.IsNullOrWhiteSpace(fen))
                return false;

            return Regex.IsMatch(fen, @"([rnbqkpRNBQKP1-8]+\/){7}([rnbqkpRNBQKP1-8]+)\s[bw-]\s(([a-hkqA-HKQ]{1,4})|(-))\s(([a-h][36])|(-))\s(0|[1-9][0-9]*)\s([1-9][0-9]*)");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine(IsFenValid(Fen));
        }
    }
}