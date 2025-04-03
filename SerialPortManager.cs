using System;
using System.Diagnostics;
using System.Windows;
using System.IO.Ports;
using System.Threading;
using System.Text;

namespace SARTEK_WPF
{
    public struct RocketData
    {
        public byte TeamId
        {
            get; set;
        }
        public byte Counter
        {
            get; set;
        }
        public float Altitude
        {
            get; set;
        } // İrtifa
        public float RocketGpsAltitude
        {
            get; set;
        } // Roket GPS İrtifa
        public float RocketLatitude
        {
            get; set;
        } // Roket Enlem
        public float RocketLongitude
        {
            get; set;
        } // Roket Boylam
        public float PayloadGpsAltitude
        {
            get; set;
        } // Görev Yükü GPS İrtifa
        public float PayloadLatitude
        {
            get; set;
        } // Görev Yükü Enlem
        public float PayloadLongitude
        {
            get; set;
        } // Görev Yükü Boylam
        public float StageGpsAltitude
        {
            get; set;
        } // Kademe GPS İrtifa
        public float StageLatitude
        {
            get; set;
        } // Kademe Enlem
        public float StageLongitude
        {
            get; set;
        } // Kademe Boylam
        public float GyroX
        {
            get; set;
        } // Jiroskop X
        public float GyroY
        {
            get; set;
        } // Jiroskop Y
        public float GyroZ
        {
            get; set;
        } // Jiroskop Z
        public float AccelX
        {
            get; set;
        } // İvme X
        public float AccelY
        {
            get; set;
        } // İvme Y
        public float AccelZ
        {
            get; set;
        } // İvme Z
        public float Angle
        {
            get; set;
        } // Açı
        public byte State
        {
            get; set;
        } // Durum
        public byte Checksum
        {
            get; set;
        } // Kontrol toplamı

        public RocketData(byte[] bytes)
        {
            if (bytes.Length < 78) throw new ArgumentException("Veri paketi 78 bayt olmalı.");

            TeamId = bytes[4];
            Counter = bytes[5];
            Altitude = BitConverter.ToSingle(bytes, 6);
            RocketGpsAltitude = BitConverter.ToSingle(bytes, 10);
            RocketLatitude = BitConverter.ToSingle(bytes, 14);
            RocketLongitude = BitConverter.ToSingle(bytes, 18);
            PayloadGpsAltitude = BitConverter.ToSingle(bytes, 22);
            PayloadLatitude = BitConverter.ToSingle(bytes, 26);
            PayloadLongitude = BitConverter.ToSingle(bytes, 30);
            StageGpsAltitude = BitConverter.ToSingle(bytes, 34);
            StageLatitude = BitConverter.ToSingle(bytes, 38);
            StageLongitude = BitConverter.ToSingle(bytes, 42);
            GyroX = BitConverter.ToSingle(bytes, 46);
            GyroY = BitConverter.ToSingle(bytes, 50);
            GyroZ = BitConverter.ToSingle(bytes, 54);
            AccelX = BitConverter.ToSingle(bytes, 58);
            AccelY = BitConverter.ToSingle(bytes, 62);
            AccelZ = BitConverter.ToSingle(bytes, 66);
            Angle = BitConverter.ToSingle(bytes, 70);
            State = bytes[74];
            Checksum = bytes[75];
        }
    }

    public class SerialPortManager
    {
        #region VariableInitializations

        private SerialPort? _serialPort;
        private readonly bool _simulationMode; // Simülasyon modu kontrolü

        public bool IsOpen => _simulationMode || (_serialPort != null && _serialPort.IsOpen);
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

        public event Action<string> DataReceived;

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
                    ReadTimeout = 500,
                    WriteTimeout = 500,
                    NewLine = "\n"
                };

                _serialPort.DataReceived += SerialPort_DataReceived;
                _serialPort.Open();
                _serialPort.DiscardInBuffer();

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
                    _serialPort.Dispose();
                    _serialPort = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Port kapatma hatası: {ex.Message}");
            }
        }

        private string _buffer = string.Empty; // Sınıf seviyesinde bir tampon

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string incomingData = _serialPort.ReadExisting(); // Mevcut veriyi oku
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
                                DataReceived?.Invoke(data);
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