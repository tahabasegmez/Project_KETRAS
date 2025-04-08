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


namespace SARTEK_WPF.ViewModels.Livecharts2
{
    public partial class AltitudeViewModel : INotifyPropertyChanged
    {
        public ISeries[] ScrollbarSeries
        {
            get; set;
        }
        public ISeries[] Series
        {
            get; set;
        }
        public Axis[] ScrollableAxes
        {
            get; set;
        }
        public Axis[] InvisibleX
        {
            get; set;
        }
        public Axis[] InvisibleY
        {
            get; set;
        }
        public LiveChartsCore.Measure.Margin Margin
        {
            get; set;
        }
        public RectangularSection[] Thumbs
        {
            get; set;
        }
        private bool _isDown = false;
        private readonly ObservableCollection<ObservablePoint> _altitudeValues = [];

        private readonly DateTime _startTime = DateTime.Now; // Programin baslangic zamani


        public Func<DateTime, string> TimeFormatter => value => value.ToString("HH:mm:ss");

        public AltitudeViewModel()
        {



            Series = [
            new LineSeries<ObservablePoint>
            {
                Values = _altitudeValues,
                GeometryStroke = null,
                GeometryFill = null,
                Fill = null,
                //DataPadding = new(0, 1)
            }
            ];
            ScrollbarSeries = [ new LineSeries<ObservablePoint>
            {
                Values = _altitudeValues,
                GeometryStroke = null,
                DataPadding = new(0, 1),
                Stroke = new LinearGradientPaint(new[] { new SKColor(255, 140, 148), new SKColor(220, 237, 194) }),
                Fill = null,
                LineSmoothness = 0.1
            }
            ];
            ScrollableAxes = [new Axis()];

            Thumbs = [
                new RectangularSection
            {
                Fill = new SolidColorPaint(new SKColor(255, 205, 210, 100))
            }
            ];

            InvisibleX = [new Axis { IsVisible = false }];
            InvisibleY = [new Axis { IsVisible = false }];

            // force the left margin to be 100 and the right margin 50 in both charts, this will
            // align the start and end point of the "draw margin",
            // no matter the size of the labels in the Y axis of both chart.
            var auto = LiveChartsCore.Measure.Margin.Auto;
            Margin = new(100, auto, 50, auto);
        }

        [RelayCommand]
        public void ChartUpdated(ChartCommandArgs args)
        {
            var cartesianChart = (ICartesianChartView)args.Chart;

            var x = cartesianChart.XAxes.First();

            // update the scroll bar thumb when the chart is updated (zoom/pan)
            // this will let the user know the current visible range
            var thumb = Thumbs[0];

            thumb.Xi = x.MinLimit;
            thumb.Xj = x.MaxLimit;
        }

        [RelayCommand]
        public void PointerDown(PointerCommandArgs args) =>
            _isDown = true;

        [RelayCommand]
        public void PointerMove(PointerCommandArgs args)
        {
            if (!_isDown) return;

            var chart = (ICartesianChartView)args.Chart;
            var positionInData = chart.ScalePixelsToData(args.PointerPosition);

            var thumb = Thumbs[0];
            var currentRange = thumb.Xj - thumb.Xi;

            // update the scroll bar thumb when the user is dragging the chart
            thumb.Xi = positionInData.X - currentRange / 2;
            thumb.Xj = positionInData.X + currentRange / 2;

            // update the chart visible range
            ScrollableAxes[0].MinLimit = thumb.Xi;
            ScrollableAxes[0].MaxLimit = thumb.Xj;
        }

        [RelayCommand]
        public void PointerUp(PointerCommandArgs args) =>
            _isDown = false;


        public void UpdateAltitude(string data)
        {
            Debug.WriteLine("UpdateAltitude çağrıldı: " + data);

            try
            {
                // Programın başlangıcından beri geçen süreyi saniye cinsinden hesapla
                double elapsed = (DateTime.Now - _startTime).TotalSeconds;

                // SerialPortManager tarafından parse edilmiş altitude değerini direkt alıyoruz (örnek olarak data'nın altitude olduğunu varsayıyorum)
                // Gerçek uygulamada bu, SerialPortManager'dan gelen hazır bir değer olmalı
                if (double.TryParse(data, out double altitude)) // Parsing burada sadece örnek, gerçekte SerialPortManager'dan gelecek
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _altitudeValues.Add(new ObservablePoint(elapsed, altitude));
                        Debug.WriteLine($"_altitudeValues güncellendi: {elapsed:F3}s, {altitude}");
                        OnPropertyChanged(nameof(Series));
                    });
                }
                else
                {
                    Debug.WriteLine("Geçersiz altitude verisi.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Veri işleme hatası: {ex.Message}");
            }
        }



        public event PropertyChangedEventHandler? PropertyChanged = delegate { };

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
