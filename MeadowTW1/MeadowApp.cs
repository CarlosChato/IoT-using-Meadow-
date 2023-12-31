﻿using Meadow;
using Meadow.Foundation;
using Meadow.Foundation.Sensors.Temperature;
using Meadow.Foundation.Graphics;
using Meadow.Foundation.Displays;
using Meadow.Devices;
using Meadow.Hardware;
using Meadow.Gateway.WiFi;
using Meadow.Units;
using System;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using NewCode.Web;
using NETDuinoWar;


namespace NewCode {
    public class MeadowApp : App<F7FeatherV2> {

        //Temperature Sensor
        AnalogTemperature sensor;

        //Display
        St7789 display;
        MicroGraphics graphics;

        //Time Controller Values
        public static int total_time = 0;
        public static int total_time_in_range = 20;
        public static int total_time_out_of_range = 0;

        public int count = 0;

        public override async Task Run() {
            if (count == 0) {
                Console.WriteLine("Initialization...");

                //Temperature Sensor Configuration
                sensor = new AnalogTemperature(device: Device, analogPin: Device.Pins.A01, sensorType: AnalogTemperature.KnownSensorType.TMP36);
                sensor.TemperatureUpdated += AnalogTemperatureUpdated;

                //Display Configuration
                var config = new SpiClockConfiguration(new Frequency(48000, Frequency.UnitType.Kilohertz), SpiClockConfiguration.Mode.Mode3);
                var spiBus = Device.CreateSpiBus(Device.Pins.SCK, Device.Pins.COPI, Device.Pins.CIPO, config);
                display = new St7789(
                device: Device,
                spiBus: spiBus,
                chipSelectPin: null,
                dcPin: Device.Pins.D01,
                resetPin: Device.Pins.D00,
                width: 240, height: 240);
                graphics = new MicroGraphics(display);
                graphics.Rotation = RotationType._270Degrees;

                var wifi = Device.NetworkAdapters.Primary<IWiFiNetworkAdapter>();
                wifi.NetworkConnected += WiFiAdapter_ConnectionCompleted;

                //WiFi Channel
                WifiNetwork wifiNetwork = ScanForAccessPoints(Secrets.WIFI_NAME);

                wifi.NetworkConnected += WiFiAdapter_WiFiConnected;
                wifi.Connect(Secrets.WIFI_NAME, Secrets.WIFI_PASSWORD);

                string IPAddress = wifi.IpAddress.ToString();

                //Connnect to the WiFi network.
                Console.WriteLine($"IP Address: {IPAddress}");
                Data.IP = IPAddress;
                if (!string.IsNullOrWhiteSpace(IPAddress)) {
                    Data.IP = IPAddress;
                    WebServer webServer = new WebServer(wifi.IpAddress, Data.Port);
                    if (webServer != null) {
                        webServer.CommandReceived += WebServer_CommandReceived;
                        webServer.Start();
                    }
                }

                Display();

                new Thread(ReadTemp).Start();

                Console.WriteLine("Done.");

                count = count + 1;
            }
        }

        //TW Combat Round
        public static void StartRound() {

            Stopwatch timer = Stopwatch.StartNew();
            timer.Start();

            //Value to control the time for heating and cooling
            //First iteration is 100 for the time spend creating timecontroller and thread
            int sleep_time = 100;

            //Initialization of time controller
            TimeController timeController = new TimeController();

            //Configuration of differents ranges
            TemperatureRange[] temperatureRanges = new TemperatureRange[Data.round_time.Length];

            //Range configurations
            bool success;
            string error_message = null;
            Data.is_working = true;


            //define ranges
            temperatureRanges[0] = new TemperatureRange(double.Parse(Data.temp_min[0]), double.Parse(Data.temp_max[0]), int.Parse(Data.round_time[0]) * 1000);
            total_time += int.Parse(Data.round_time[0]);

            //Initialization of timecontroller with the ranges
            timeController.DEBUG_MODE = true;
            success = timeController.Configure(temperatureRanges, total_time * 1000, Data.refresh, out error_message);

            timeController.StartOperation();

            //Initialization of timer
            new Thread(Timer).Start();

            //THE TW START WORKING
            while (Data.is_working) {

                //This is the time refresh we did not do before
                Thread.Sleep(Data.refresh - sleep_time);

                Console.WriteLine(Data.temp_act);

                //Temperature registration
                timeController.RegisterTemperature(double.Parse(Data.temp_act));
                /*if (timeController.DEBUG_MODE)
                {
                    Debug.Print(Data.temp_act);
                }*/

            }
            Console.WriteLine("Round Finish");

            total_time_in_range += timeController.TimeInRangeInMilliseconds;
            total_time_out_of_range += timeController.TimeOutOfRangeInMilliseconds;
            Data.time_in_range_temp =(timeController.TimeInRangeInMilliseconds / 1000);

            Console.WriteLine("Tiempo dentro del rango " + total_time_in_range/1000 /*((timeController.TimeInRangeInMilliseconds / 1000))*/ + " s de " + total_time + " s");
            Console.WriteLine("Tiempo fuera del rango " + total_time_out_of_range / 1000 + " s de " + total_time + " s");
        }

        //Round Timer
        private static void Timer() {
            Data.is_working = true;
            for (int i = 0; i < Data.round_time.Length; i++) {
                Data.time_left = int.Parse(Data.round_time[i]);

                while (Data.time_left > 0) {
                    Data.time_left--;
                    Thread.Sleep(1000);
                }
            }
            Data.is_working = false;
        }

        //Display Theme
        void Display() {

            graphics.Clear(true);

            int radius = 225;
            int originX = graphics.Width / 2;
            int originY = graphics.Height / 2 + 130;

            graphics.Stroke = 3;
            for (int i = 1; i < 5; i++) {
                graphics.DrawCircle
                (
                    centerX: originX,
                    centerY: originY,
                    radius: radius,
                    color: Data.colors[i - 1],
                    filled: true
                );

                radius -= 20;
            }

            graphics.DrawLine(0, 220, 239, 220, Color.White);
            graphics.DrawLine(0, 230, 239, 230, Color.White);

            graphics.CurrentFont = new Font12x20();
            graphics.DrawText(54, 130, "TEMPERATURE", Color.White);
            graphics.DrawText(54, 160, Data.temp_act.ToString(), Color.White);

            graphics.Show();

        }

        //Temperature and Display Updated
        void AnalogTemperatureUpdated(object sender, IChangeResult<Meadow.Units.Temperature> e) {
            //Update Display with new temperature
            graphics.DrawRectangle(
                x: 48, y: 160,
                width: 144,
                height: 40,
                color: Data.colors[Data.colors.Length - 1],
                filled: true);

            graphics.DrawText(
                x: 48, y: 160,
                text: $"{e.New.Celsius:00.0}°C",
                color: Color.White,
                scaleFactor: ScaleFactor.X2);

            graphics.Show();
            //Update Display with new temperature
            Data.temp_act = Math.Round((Double)e.New.Celsius, 2).ToString();

        }

        //Read Temperature Function
        public void ReadTemp() {
            sensor.StartUpdating(TimeSpan.FromSeconds(2));
        }

        void WebServer_CommandReceived(object source, WebCommandEventArgs e) {
            if (source != null) {

            }
        }

        void WiFiAdapter_WiFiConnected(object sender, EventArgs e) {
            if (sender != null) {
                Console.WriteLine($"Connecting to WiFi Network {Secrets.WIFI_NAME}");
            }
        }

        void WiFiAdapter_ConnectionCompleted(object sender, EventArgs e) {
            Console.WriteLine("Connection request completed.");
        }

        protected WifiNetwork ScanForAccessPoints(string SSID) {
            WifiNetwork wifiNetwork = null;
            ObservableCollection<WifiNetwork> networks = new ObservableCollection<WifiNetwork>(Device.NetworkAdapters.Primary<IWiFiNetworkAdapter>().Scan()?.Result?.ToList()); //REVISAR SI ESTO ESTA BIEN
            wifiNetwork = networks?.FirstOrDefault(x => string.Compare(x.Ssid, SSID, true) == 0);
            return wifiNetwork;
        }
    }
}