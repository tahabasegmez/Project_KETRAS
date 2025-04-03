using System;
using System.Windows;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.Defaults;
using SkiaSharp;
using LiveChartsCore.SkiaSharpView.Painting;
using System.Diagnostics;
using System.Text.RegularExpressions;
using LiveChartsCore.Kernel.Events;
using LiveChartsCore.Kernel.Sketches;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Controls;


namespace SARTEK_WPF
{
    public partial class AltitudeView : UserControl
    {
        public AltitudeView()
        {
            InitializeComponent();
            // ViewModel'i DataContext olarak ayarlıyoruz
            this.DataContext = new AltitudeViewModel();
        }
    }
}
