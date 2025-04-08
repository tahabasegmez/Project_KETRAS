using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

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
    public class RocketSimulation
    {
        private readonly SerialPortManager _serialPortManager;
        private readonly Random random = new();

        // Roket uçuş parametreleri
        private double altitude = 0f; // İrtifa (m)
        private double velocity = 0f; // Hız (m/s)
        private double acceleration = 0f; // İvme (m/s²)
        private double time = 0f; // Zaman (s)
        private byte counter = 0; // Paket sayacı
        private byte state = 0; // Durum bilgisi (0: Uçuş, 1: Drogue, 2: Ana paraşüt)

        // Sabitler
        private readonly double gravity = 9.81f; // Yerçekimi ivmesi (m/s²)
        private readonly double thrust = 200f; // İtki (N)
        private readonly double mass = 5f; // Roket kütlesi (kg)
        private readonly double drogueDrag = 0.5f; // Drogue paraşüt sürtünme katsayısı
        private readonly double mainDrag = 2f; // Ana paraşüt sürtünme katsayısı

        public RocketSimulation(SerialPortManager serialPortManager)
        {
            _serialPortManager = serialPortManager ?? throw new ArgumentNullException(nameof(serialPortManager));
        }

        public void StartSimulation()
        {
            Thread simulationThread = new Thread(SimulateRocketFlight)
            {
                IsBackground = true // Uygulama kapanırken thread de kapansın
            };
            simulationThread.Start();
        }

        private void SimulateRocketFlight()
        {
            while (time < 60) // 60 saniyelik uçuş süresi
            {
                // Fiziksel hesaplamalar
                if (time < 5) // İlk 5 saniye: Motor çalışıyor
                {
                    acceleration = (thrust / mass) - gravity;
                    velocity += acceleration * 0.1f;
                    altitude += velocity * 0.1f;
                    state = 0; // Uçuş durumu
                }
                else if (altitude > 1500 && state == 0) // 1500m'de drogue paraşüt açılır
                {
                    acceleration = -gravity - (drogueDrag * velocity * velocity / mass);
                    velocity += acceleration * 0.1f;
                    altitude += velocity * 0.1f;
                    state = 1; // Drogue paraşüt açıldı
                }
                else if (altitude < 500 && state == 1) // 500m'de ana paraşüt açılır
                {
                    acceleration = -gravity - (mainDrag * velocity * velocity / mass);
                    velocity += acceleration * 0.1f;
                    altitude += velocity * 0.1f;
                    state = 2; // Ana paraşüt açıldı
                }
                else if (altitude <= 0) // Yere iniş
                {
                    altitude = 0;
                    velocity = 0;
                    acceleration = 0;
                    state = 2;
                    break;
                }
                else // Serbest düşüş veya paraşütlü iniş
                {
                    acceleration = (state == 0) ? -gravity : -gravity - ((state == 1 ? drogueDrag : mainDrag) * velocity * velocity / mass);
                    velocity += acceleration * 0.1f;
                    altitude += velocity * 0.1f;
                }

                // Dummy veri paketi oluştur ve gönder
                string packet = GeneratePacket();
                if (_serialPortManager.IsOpen)
                {
                    _serialPortManager.SendData(packet);
                }

                // Konsola yazdır (debug için)
                //Debug.WriteLine($"Zaman: {time:F1}s, İrtifa: {altitude:F2}m, Hız: {velocity:F2}m/s, İvme: {acceleration:F2}m/s², Durum: {state}");
                Thread.Sleep(500); // 100ms aralıklarla veri üret
                time += 0.1f;
                counter++;
            }
            Console.WriteLine("Simülasyon tamamlandı.");
        }

        private string GeneratePacket()
        {
            byte[] packet = new byte[78];

            // Sabit başlıklar
            packet[0] = 0xFF;
            packet[1] = 0xFF;
            packet[2] = 0x54;
            packet[3] = 0x52;

            // Takım ID ve sayaç
            packet[4] = 0; // Takım ID
            packet[5] = counter; // Sayacı artır

            // İrtifa (altitude)
            Buffer.BlockCopy(BitConverter.GetBytes(altitude), 0, packet, 6, 4);

            // Roket GPS İrtifa (örnek olarak altitude + küçük bir fark)
            Buffer.BlockCopy(BitConverter.GetBytes(altitude + (float)random.NextDouble()), 0, packet, 10, 4);

            // Roket Enlem
            float latitude = 39.925019f + (float)(random.NextDouble() - 0.5) * 0.001f;
            Buffer.BlockCopy(BitConverter.GetBytes(latitude), 0, packet, 14, 4);

            // Roket Boylam
            float longitude = 32.836954f + (float)(random.NextDouble() - 0.5) * 0.001f;
            Buffer.BlockCopy(BitConverter.GetBytes(longitude), 0, packet, 18, 4);

            // Görev Yükü GPS İrtifa, Enlem, Boylam
            Buffer.BlockCopy(BitConverter.GetBytes(altitude - 100f), 0, packet, 22, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(41.104593f + (float)(random.NextDouble() - 0.5) * 0.001f), 0, packet, 26, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(29.024411f + (float)(random.NextDouble() - 0.5) * 0.001f), 0, packet, 30, 4);

            // Kademe GPS İrtifa, Enlem, Boylam
            Buffer.BlockCopy(BitConverter.GetBytes(altitude + 166f), 0, packet, 34, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(41.091485f + (float)(random.NextDouble() - 0.5) * 0.001f), 0, packet, 38, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(29.061412f + (float)(random.NextDouble() - 0.5) * 0.001f), 0, packet, 42, 4);

            // Jiroskop verileri
            Buffer.BlockCopy(BitConverter.GetBytes(1.51f + (float)(random.NextDouble() - 0.5) * 0.1f), 0, packet, 46, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(0.49f + (float)(random.NextDouble() - 0.5) * 0.1f), 0, packet, 50, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(0.61f + (float)(random.NextDouble() - 0.5) * 0.1f), 0, packet, 54, 4);

            // İvme verileri
            Buffer.BlockCopy(BitConverter.GetBytes(acceleration * 0.1f), 0, packet, 58, 4); // X ekseni
            Buffer.BlockCopy(BitConverter.GetBytes(acceleration * 0.05f), 0, packet, 62, 4); // Y ekseni
            Buffer.BlockCopy(BitConverter.GetBytes(acceleration), 0, packet, 66, 4); // Z ekseni

            // Açı
            Buffer.BlockCopy(BitConverter.GetBytes(5.08f + (float)(random.NextDouble() - 0.5) * 0.5f), 0, packet, 70, 4);

            // Durum bilgisi
            packet[74] = state;

            // Checksum hesaplama
            byte checksum = 0;
            for (int i = 4; i < 75; i++)
            {
                checksum = (byte)((checksum + packet[i]) % 256);
            }
            packet[75] = checksum;

            // Sabit sonlandırma
            packet[76] = 0x0D;
            packet[77] = 0x0A;

            // Bayt dizisini string'e çevir (SerialPortManager SendData string kabul ediyor)
            return Encoding.ASCII.GetString(packet);
        }
    }
}