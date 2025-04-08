using System;
using System.Diagnostics;
using System.Windows;
using System.IO.Ports;
using System.Threading;
using System.Text;

namespace SARTEK_WPF
{

    public class SerialPortManager
    {
        #region VariableInitializations

        private SerialPort? _serialPort;
        private readonly bool _simulationMode; // simulation mode control, must be controlled in the constructor

        public bool IsOpen => _simulationMode || (_serialPort != null && _serialPort.IsOpen); // port open control
        public string PortName
        {
            get; private set;
        }
        public int BaudRate
        {
            get; private set;
        }
        public Parity Parity
        {
            get; private set;
        }
        public int DataBits
        {
            get; private set;
        }
        public StopBits StopBits
        {
            get; private set;
        }

        public event Action<string> DataReceived; // event to notify when data is received

        private string _buffer = string.Empty; // Sınıf seviyesinde bir tampon

        #endregion VariableInitializations  

        public SerialPortManager(bool simulationMode = false)
        {
            _simulationMode = simulationMode;
            PortName = "COM3";
            BaudRate = 9600;
            Parity = Parity.None;
            DataBits = 8;
            StopBits = StopBits.One;
        }

        public void Start()
        {
            if (_simulationMode)
            {
                Debug.WriteLine("Simülasyon modu aktif, fiziksel port açılmadı.");
                return;
            }

            if (_serialPort != null && _serialPort.IsOpen) return;

            try
            {
                Stop(); // Önceki bağlantıyı temizle

                _serialPort = new SerialPort(PortName, BaudRate, Parity, DataBits, StopBits)
                {
                    ReadTimeout = 500, // 2hz
                    WriteTimeout = 500,
                    NewLine = "\n" 
                };

                _serialPort.DataReceived += SerialPort_DataReceived;
                _serialPort.Open();
                _serialPort.DiscardInBuffer(); // empty buffer: 

                Debug.WriteLine($"Port {PortName} başarıyla açıldı.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Port Hatası: {ex.Message}");
                throw new InvalidOperationException($"Port Hatası: {ex.Message}");
            }
        }

        public void Stop()
        {
            if (_simulationMode) return;

            try
            {
                if (_serialPort != null)
                {
                    if (_serialPort.IsOpen)
                    {
                        _serialPort.Close();
                    }
                    _serialPort.DataReceived -= SerialPort_DataReceived;
                    _serialPort.Dispose(); // dispose method to release resources
                    _serialPort = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Port kapatma hatası: {ex.Message}");
            }
        }

        public void SetPortSettings(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            try
            {
                Stop(); // Eski bağlantıyı güvenli şekilde kapat

                PortName = portName;
                BaudRate = baudRate;
                Parity = parity;
                DataBits = dataBits;
                StopBits = stopBits;

                Debug.WriteLine($"Port Ayarları Güncellendi: PortName={portName}, BaudRate={baudRate}, Parity={parity}, DataBits={dataBits}, StopBits={stopBits}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Port ayarı değiştirme hatası: {ex.Message}");
            }
        }


        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string incomingData = _serialPort.ReadExisting(); // read existing data
                ProcessData(incomingData);
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show("Port başka bir uygulama tarafından kullanılıyor!");
            }
            catch (System.IO.IOException ex)
            {
                MessageBox.Show("Port bağlantısı kesildi!");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Veri okuma hatası: {ex.Message}");
            }
        }

        // Simülasyon modunda veri işlemek için yeni metot
        public void SimulateDataReceived(string data)
        {
            if (_simulationMode)
            {
                ProcessData(data);
            }
            else
            {
                Debug.WriteLine("Simülasyon modu kapalı, bu metot yalnızca simülasyon modunda çalışır.");
            }
        }

        private void ProcessData(string incomingData)
        {
            _buffer += incomingData;

            int delimiterIndex = _buffer.IndexOf("\r\n");
            while (delimiterIndex >= 0)
            {
                string completePacket = _buffer.Substring(0, delimiterIndex);
                _buffer = _buffer.Substring(delimiterIndex + 2);

                if (!string.IsNullOrWhiteSpace(completePacket))
                {
                    try
                    {
                        byte[] packetBytes = Encoding.ASCII.GetBytes(completePacket);
                        if (packetBytes.Length >= 78 && packetBytes[0] == 0xFF && packetBytes[1] == 0xFF && packetBytes[2] == 0x54 && packetBytes[3] == 0x52)
                        {
                            RocketData data = new RocketData(packetBytes);
                            byte calculatedChecksum = 0;
                            for (int i = 4; i < 75; i++)
                            {
                                calculatedChecksum = (byte)((calculatedChecksum + packetBytes[i]) % 256);
                            }
                            if (calculatedChecksum == data.Checksum)
                            {
                                //DataReceived?.Invoke(data);
                            }
                            else
                            {
                                Debug.WriteLine("Checksum hatası!");
                            }
                        }
                        else
                        {
                            Debug.WriteLine("Geçersiz paket formatı veya boyutu!");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Paket ayrıştırma hatası: {ex.Message}");
                    }
                }

                delimiterIndex = _buffer.IndexOf("\r\n");
            }
        }

        

        public void SendData(string data)
        {
            try
            {
                if (_simulationMode)
                {
                    SimulateDataReceived(data); // Simülasyon modunda veriyi direkt işler
                }
                else if (_serialPort != null && _serialPort.IsOpen)
                {
                    _serialPort.WriteLine(data);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Veri gönderme hatası: {ex.Message}");
            }
        }
    }
}