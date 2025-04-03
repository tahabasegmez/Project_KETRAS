using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.Generic;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Extensions;
using LiveChartsCore.VisualElements;
using LiveChartsCore.SkiaSharpView.VisualElements;
using LiveChartsCore.Defaults;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using SkiaSharp;
using LiveChartsCore.SkiaSharpView.Painting;
using System.Diagnostics;
using System.Text.RegularExpressions;
using LiveChartsCore.Kernel.Events;
using LiveChartsCore.Kernel.Sketches;



namespace SARTEK_WPF
{
    public partial class VelocityViewModel : INotifyPropertyChanged
    {
        private readonly Random _random = new();

        public event PropertyChangedEventHandler? PropertyChanged = delegate { };

        public IEnumerable<ISeries> Series
        {
            get; set;
        }
        public IEnumerable<VisualElement> VisualElements
        {
            get; set;
        }
        public NeedleVisual Needle
        {
            get; set;
        }

        public VelocityViewModel()
        {
            var sectionsOuter = 130;
            var sectionsWidth = 20;

            Needle = new NeedleVisual
            {
                Value = 45
            };

            Series = GaugeGenerator.BuildAngularGaugeSections(
                new GaugeItem(60, s => SetStyle(sectionsOuter, sectionsWidth, s)),
                new GaugeItem(30, s => SetStyle(sectionsOuter, sectionsWidth, s)),
                new GaugeItem(10, s => SetStyle(sectionsOuter, sectionsWidth, s)));

            VisualElements =
            [
                new AngularTicksVisual
                {
                    Labeler = value => value.ToString("N1"),
                    LabelsSize = 16,
                    LabelsOuterOffset = 15,
                    OuterOffset = 65,
                    TicksLength = 20
                },
                Needle
            ];
        }

        [RelayCommand]
        public void DoRandomChange()
        {
            // modifying the Value property updates and animates the chart automatically
            Needle.Value = _random.Next(0, 100);
            OnPropertyChanged(nameof(Needle));
        }

        private static void SetStyle(
            double sectionsOuter, double sectionsWidth, PieSeries<ObservableValue> series)
        {
            series.OuterRadiusOffset = sectionsOuter;
            series.MaxRadialColumnWidth = sectionsWidth;
            series.CornerRadius = 0;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
