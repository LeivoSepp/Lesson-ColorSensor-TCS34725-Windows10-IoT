using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;
using Windows.UI;

namespace LessonColorSensorTCS34725
{
    class TCS34725
    {
        private const byte TCS34725_ADDRESS = 0x29;
        private const byte TCS34725_ID = 0x12; // 0x44 = TCS34721/TCS34725, 0x4D = TCS34723/TCS34727
        private const byte TCS34725_COMMAND_BIT = 0x80;
        private const byte TCS34725_CDATAL = 0x14; // Clear channel data
        private const byte TCS34725_RDATAL = 0x16; // Red channel data
        private const byte TCS34725_GDATAL = 0x18; // Green channel data
        private const byte TCS34725_BDATAL = 0x1A; // Blue channel data
        private const byte TCS34725_ENABLE = 0x00;
        private const byte TCS34725_ENABLE_AIEN = 0x10; // RGBC Interrupt Enable
        private const byte TCS34725_ENABLE_WEN = 0x08; // Wait enable - Writing 1 activates the wait timer
        private const byte TCS34725_ENABLE_AEN = 0x02; // RGBC Enable - Writing 1 actives the ADC, 0 disables it
        private const byte TCS34725_ENABLE_PON = 0x01; // Power on - Writing 1 activates the internal oscillator, 0 disables it
        private const byte TCS34725_CONTROL = 0x0F; // Set the gain level for the sensor
        private const byte TCS34725_ATIME = 0x01; // Integration time
        private const byte TCS34725_INTEGRATIONTIME_2_4MS = 0xFF;  //	2.4ms	- 1 Cycle		(Max Count: 1024)
        private const byte TCS34725_INTEGRATIONTIME_24MS = 0xF6;   //	24ms	- 10 Cycles		(Max Count: 10240)
        private const byte TCS34725_INTEGRATIONTIME_50MS = 0xEB;   //	50ms	- 20 Cycles		(Max Count: 20480)
        private const byte TCS34725_INTEGRATIONTIME_101MS = 0xD5;  //	101ms	- 42 Cycles		(Max Count: 43008)
        private const byte TCS34725_INTEGRATIONTIME_154MS = 0xC0;  //	154ms	- 64 Cycles		(Max Count: 65535)
        private const byte TCS34725_INTEGRATIONTIME_700MS = 0x00;  //	700ms	- 256 Cycles	(Max Count: 65535)
        private const byte TCS34725_GAIN_1X = 0x00;   //	No Gain
        private const byte TCS34725_GAIN_4X = 0x01;    //	2x Gain
        private const byte TCS34725_GAIN_16X = 0x02;   //	16x Gain
        private const byte TCS34725_GAIN_60X = 0x03;    //	60x	Gain

        // I2C Device
        private I2cDevice I2C;
        private int I2C_ADDRESS { get; set; } = TCS34725_ADDRESS;
        public TCS34725(int i2cAddress = TCS34725_ADDRESS)
        {
            I2C_ADDRESS = i2cAddress;
        }
        public static bool IsInitialised { get; private set; } = false;
        private void Initialise()
        {
            if (!IsInitialised)
            {
                EnsureInitializedAsync().Wait();
            }
        }
        private async Task EnsureInitializedAsync()
        {
            if (IsInitialised) { return; }
            try
            {
                var settings = new I2cConnectionSettings(I2C_ADDRESS);
                settings.BusSpeed = I2cBusSpeed.FastMode;
                settings.SharingMode = I2cSharingMode.Shared;
                string aqs = I2cDevice.GetDeviceSelector("I2C1");         /* Find the selector string for the I2C bus controller */
                var dis = await DeviceInformation.FindAllAsync(aqs);      /* Find the I2C bus controller device with our selector string           */
                I2C = await I2cDevice.FromIdAsync(dis[0].Id, settings);   /* Create an I2cDevice with our selected bus controller and I2C settings */

                InitialiseSensor();
                knownColorList();
                IsInitialised = true;
            }
            catch (Exception ex)
            {
                throw new Exception("I2C Initialization Failed", ex);
            }
        }
        private void InitialiseSensor()
        {
            // Turn on
            write8(TCS34725_ENABLE, TCS34725_ENABLE_PON);
            Task.Delay(3).Wait();
            write8(TCS34725_ENABLE, TCS34725_ENABLE_PON | TCS34725_ENABLE_AEN);
            // Integration Time
            write8(TCS34725_ATIME, TCS34725_INTEGRATIONTIME_700MS);
            // Gain
            write8(TCS34725_CONTROL, TCS34725_GAIN_60X);
        }
        private Color ConvertToColor(int[] rgbc)
        {
            double fr = rgbc[0], fg = rgbc[1], fb = rgbc[2], fc = rgbc[3];
            fr /= fc; fr *= 255;
            fg /= fc; fg *= 255;
            fb /= fc; fb *= 255;
            return Color.FromArgb(255, Convert.ToByte(fr), Convert.ToByte(fg), Convert.ToByte(fb));
        }
        private int[] readRGBC()
        {
            int[] rgbc = new int[4];
            byte[] write_buffer = new byte[1];
            byte[] read_buffer = new byte[1];
            int r, g, b, c;
            rgbc[0] = I2CRead16(TCS34725_RDATAL);
            rgbc[1] = I2CRead16(TCS34725_GDATAL);
            rgbc[2] = I2CRead16(TCS34725_BDATAL);
            rgbc[3] = I2CRead16(TCS34725_CDATAL);
            return rgbc;
        }
        public Color ColorRGBC()
        {
            Initialise();
            return ConvertToColor(readRGBC());
        }
        private Dictionary<String, Color> colors;
        private void knownColorList()
        {
            colors = typeof(Colors).GetProperties(BindingFlags.Static | BindingFlags.Public).ToDictionary(p => p.Name, p => (Color)p.GetValue(null, null));
        }
        public string ColorName()
        {
            Color color = ColorRGBC();
            double minDistance = double.MaxValue;
            string minColor = "Black";

            foreach (var colorProperty in colors)
            {
                var colorPropertyValue = (Color)colorProperty.Value;
                if (colorPropertyValue.R == color.R
                        && colorPropertyValue.G == color.G
                        && colorPropertyValue.B == color.B)
                {
                    return colorProperty.Key;
                }
                double distance = Math.Pow(colorPropertyValue.R - color.R, 2) +
                                Math.Pow(colorPropertyValue.G - color.G, 2) +
                                Math.Pow(colorPropertyValue.B - color.B, 2);
                distance = (int)Math.Sqrt(distance);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    minColor = colorProperty.Key;
                }
            }
            return minColor;
        }
        private void write8(byte addr, byte cmd)
        {
            byte[] Command = new byte[] { (byte)((addr) | TCS34725_COMMAND_BIT), cmd };

            I2C.Write(Command);
        }
        // Read byte
        private byte I2CRead8(byte addr)
        {
            byte[] aaddr = new byte[] { (byte)((addr) | TCS34725_COMMAND_BIT) };
            byte[] data = new byte[1];

            I2C.WriteRead(aaddr, data);

            return data[0];
        }
        // Read integer
        private ushort I2CRead16(byte addr)
        {
            byte[] aaddr = new byte[] { (byte)((addr) | TCS34725_COMMAND_BIT) };
            byte[] data = new byte[2];

            I2C.WriteRead(aaddr, data);

            return (ushort)((data[1] << 8) | (data[0]));
        }
    }
}
