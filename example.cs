//небольшая обертка для легкого использования
using System;
using System.Device.I2c;
using Iot.Device.Ssd13xx;
using nanoFramework.Hardware.Esp32;

namespace DisplayDriver
{
    public class _display
    {
        private int width, height;
        private Ssd1306 display;

        public _display(int width, int height)
        {
            this.width = width;
            this.height = height;
            // СТРОКА 1: Жестко привязываем физический пин 21 к шине данных I2C
            Configuration.SetPinFunction(21, DeviceFunction.I2C1_DATA);
            
            // СТРОКА 2: Жестко привязываем физический пин 22 к шине тактирования I2C
            Configuration.SetPinFunction(22, DeviceFunction.I2C1_CLOCK);

            // 1. Инициализация I2C соединения
            I2cConnectionSettings settings = new I2cConnectionSettings(1, 0x3C);
            I2cDevice i2cDevice = I2cDevice.Create(settings);

            // 2. Создание объекта дисплея (выбираем разрешение на основе переданных параметров)
            var resolution = (width == 128 && height == 32) 
                ? Ssd13xx.DisplayResolution.OLED128x32 
                : Ssd13xx.DisplayResolution.OLED128x64;

            display = new Ssd1306(i2cDevice, resolution);

            // 3. Очистка экрана по стандарту nanoFramework
            display.ClearScreen();
            
            // 4. Установка шрифта по умолчанию для вывода текста
            display.Font = new BasicFont();
            
            // 5. Отрисовка буфера на физический экран
            display.Display();
            Console.WriteLine("Блок init прошел");
        }
        public void Render() => display.Display();

        public void Drawstr(int x, int y, string value)
        {
            // Вывод строки: координаты X, Y, сам текст и размер шрифта (1 — стандартный, 2 — крупный)
            display.DrawString(x, y, value, 2);
        }
        public void DrawLn(int x,int y,int x2,int y2)
        {
            display.DrawLine(x,y,x2,y2,true);
        }

        public void clear()
        {
            display.ClearScreen();
        }
        public void DrawBitmap(int x, int y, byte[] bitmap, int w, int h)
        {
            display.DrawBitmap(x, y, bitmap, w, h, true);
        }

    }
}
