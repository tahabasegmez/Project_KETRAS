using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SARTEK_WPF
{
    public partial class MainWindow : Window
    {
        private readonly SerialPortManager _serialPortManager;
        private readonly AltitudeViewModel _altitudeViewModel;
        private readonly VelocityViewModel _velocityViewModel;
        private readonly RocketSimulation _rocketSimulation;

        // invoke yerine queue kullanıyoruz
        private Queue<string> dataQueue = new Queue<string>();
        private DispatcherTimer _queueTimer;

        public MainWindow()
        {
            InitializeComponent();
            _serialPortManager = new SerialPortManager(simulationMode: true);
            _altitudeViewModel = new AltitudeViewModel();
            _velocityViewModel = new VelocityViewModel();

            DataContext = _altitudeViewModel;
            _rocketSimulation = new RocketSimulation(_serialPortManager);
            

            this.SizeChanged += MainWindow_SizeChanged;

            InitializePortList();

            //_serialPortManager.DataReceived += (data) =>
            //{
            //    Application.Current.Dispatcher.Invoke(() => // Dispatcher ekleyin
            //    {
            //        _altitudeViewModel.UpdateAltitudeData(data);
            //    });
            //};
            _queueTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _queueTimer.Tick += ProcessQueue;
            _queueTimer.Start();

            _serialPortManager.DataReceived += (data) =>
            {
                lock (dataQueue)
                {
                    dataQueue.Enqueue(data);
                }
            };
        }
        private void ProcessQueue(object sender, EventArgs e)
        {
            lock (dataQueue)
            {
                while (dataQueue.Count > 0)
                {
                    string data = dataQueue.Dequeue();
                    _altitudeViewModel.UpdateAltitude(data);
                }
            }
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var aspectRatio = 16.0 / 9.0;
            var newWidth = e.NewSize.Width;
            var newHeight = e.NewSize.Height;

            if (newWidth / newHeight > aspectRatio)
            {
                newWidth = newHeight * aspectRatio;
            }
            else
            {
                newHeight = newWidth / aspectRatio;
            }

            this.Width = newWidth;
            this.Height = newHeight;
        }

        private void InitializePortList()
        {
            var ports = SerialPort.GetPortNames();

            foreach (var port in ports)
            {
                PortNameComboBox.Items.Add(new ComboBoxItem() { Content = port });
            }

            if (ports.Length > 0)
            {
                PortNameComboBox.SelectedItem = PortNameComboBox.Items[0];
            }
        }

        private void SerialPortSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SerialPortSettingsPopup.IsOpen = !SerialPortSettingsPopup.IsOpen;
            _rocketSimulation.StartSimulation();
        }

        private async void ApplySettingsButton_Click(object sender, RoutedEventArgs e)
        {
            ApplySettingsButton.IsEnabled = false;
            ApplySettingsButton.Content = "Applying...";

            try
            {
                // ComboBox'lardan ayarları al
                string? selectedPort = (PortNameComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                int selectedBaudRate = int.Parse((BaudRateComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "0");
                int selectedDataBits = int.Parse((DataBitsComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "0");
                Parity selectedParity = (Parity)Enum.Parse(typeof(Parity), (ParityComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "None");
                StopBits selectedStopBits = (StopBits)Enum.Parse(typeof(StopBits), (StopBitsComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "None");

                // Seri port ayarlarını uygula
                await Task.Run(() =>
                {
                    _serialPortManager.SetPortSettings(
                        selectedPort,
                        selectedBaudRate,
                        selectedParity,
                        selectedDataBits,
                        selectedStopBits
                    );
                    _serialPortManager.Start();
                });

                await Task.Delay(200); // Port stabilizasyonu için bekle

                // Başarı mesajını göster
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Ayarlar başarıyla uygulandı!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                });

                
            }
            catch (Exception ex)
            {
                // Hata mesajını göster
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            finally
            {
                // Butonu eski haline getir
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ApplySettingsButton.IsEnabled = true;
                    ApplySettingsButton.Content = "Apply Settings";
                });
            }
        }
    }
}
