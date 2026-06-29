using System;
using System.Device.I2c;

namespace Iot.Device.Ssd13xx
{
    public abstract class Ssd13xx : IDisposable
    {
        protected I2cDevice _i2cDevice;
        public IFont Font { get; set; }

        public enum DisplayResolution
        {
            OLED128x32,
            OLED128x64
        }

        protected Ssd13xx(I2cDevice i2cDevice)
        {
            _i2cDevice = i2cDevice ?? throw new ArgumentNullException();
        }

        public abstract void ClearScreen();
        public abstract void Display();
        public abstract void DrawString(int x, int y, string text, byte scale);

        public void Dispose()
        {
            _i2cDevice?.Dispose();
        }
    }

    public abstract class IFont
    {
        public abstract byte Width { get; }
        public abstract byte Height { get; }
        public abstract byte[] this[char character] { get; }
    }
}
