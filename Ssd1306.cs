using System;
using System.Device.I2c;
using mathutils;

namespace Iot.Device.Ssd13xx
{
    public class Ssd1306 : Ssd13xx
    {
        public const byte DefaultI2cAddress = 0x3C;
        private readonly byte[] _buffer;
        private readonly int _width;
        private readonly int _height;

        public Ssd1306(I2cDevice i2cDevice, DisplayResolution resolution) : base(i2cDevice)
        {
            _width = resolution == DisplayResolution.OLED128x32 ? 128 : 128;
            _height = resolution == DisplayResolution.OLED128x32 ? 32 : 64;
            _buffer = new byte[(_width * _height) / 8];
            Initialize();
        }

        private void Initialize()
        {
            byte[] initCmds = new byte[] {
                0x00, 0xAE,             // Display OFF
                0xD5, 0x80,             // Set Display Clock Divide Ratio
                0xA8, (byte)(_height - 1), // Set Multiplex Ratio
                
                // Возвращаем в чистый ноль, чтобы софтверный фикс работал от края до края
                0xD3, 0x00,             // Set Display Offset в 0
                0x40,                   // Set Display Start Line в 0
                
                0x8D, 0x14,             // Set Charge Pump (Enable)
                0x20, 0x00,             // Set Memory Addressing Mode (Horizontal)
                
                0xA1,                   // Column Address 127 mapped to SEG0
                0xC0,                   // Set COM Output Scan Direction (твои ровные буквы)
                
                0xDA, (byte)(_height == 32 ? 0x02 : 0x12), // COM Pins Hardware Configuration
                0x81, 0xCF,             // Set Contrast Control
                0xD9, 0xF1,             // Set Pre-charge Period
                0xDB, 0x40,             // Set VCOMH Deselect Level
                0xA4,                   // Entire Display ON
                0xA6,                   // Set Normal Display
                0xAF                    // Display ON
            };
            _i2cDevice.Write(initCmds);
        }

        public override void ClearScreen() => Array.Clear(_buffer, 0, _buffer.Length);

        public override void Display()
        {
            byte[] pageCmd = new byte[3];
            for (byte page = 0; page < (byte)(_height / 8); page++)
            {
                pageCmd[0] = 0x00; pageCmd[1] = (byte)(0xB0 + page); pageCmd[2] = 0x00;
                _i2cDevice.Write(pageCmd);
                byte[] chunk = new byte[_width + 1];
                chunk[0] = 0x40;
                Array.Copy(_buffer, page * _width, chunk, 1, _width);
                _i2cDevice.Write(chunk);
            }
        }

        public override void DrawString(int x, int y, string text, byte scale)
        {
            if (Font == null || text == null || text == "") return;
            int currentX = x;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                int code = (int)c; // Получаем код, который выдал компилятор Mono

                // ДЕБАГ-ХАК: Ловим искаженные компилятором русские буквы.
                // Так как компилятор сдвинул всё на +1, 'Т' (0x0411) превращаем обратно в 'С' (0x0410).
                // Диапазон сдвинутых заглавных и строчных букв теперь 0x0411 - 0x0450
                if (code >= 0x0411 && code <= 0x0450)
                {
                    code = code - 1; // Возвращаем букву на её законное место!
                }
                // Корректируем букву 'Ё', если она тоже уплыла (в Юникоде 0x0401)
                else if (code == 0x0402) // Если уплыла на +1
                {
                    code = 0x0401;
                }
                // Корректируем букву 'ё' (в Юникоде 0x0451)
                else if (code == 0x0452) // Если уплыла на +1
                {
                    code = 0x0451;
                }

                char finalChar = (char)code;

                // Переводим очищенный Юникод в индекс нашей таблицы CP1251
                if (code >= 0x0410 && code <= 0x044F)
                {
                    finalChar = (char)(code - 0x0410 + 192);
                }
                else if (code == 0x0401) finalChar = (char)168; // Ё
                else if (code == 0x0451) finalChar = (char)184; // ё

                // Отрисовка символа
                byte[] bitmap = Font[finalChar];
                for (int col = 0; col < Font.Width; col++)
                {
                    if (currentX >= _width) break;
                    byte fontByte = bitmap[col];

                    for (int bit = 0; bit < Font.Height; bit++)
                    {
                        if (((fontByte >> bit) & 1) == 1)
                        {
                            DrawPixel(currentX, y + bit, true);
                        }
                    }
                    currentX++;
                }
                currentX += 1;
            }
        }

        // 1. Отрисовка базового пикселя
        public void DrawPixel(int x, int y, bool color)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height) return;

            // Глобальный разворот осей для всех примитивов и текста
            y = (_height - 1) - y;

            int index = (y / 8) * _width + x;

            if (color)
                _buffer[index] |= (byte)(1 << (y % 8));
            else
                _buffer[index] &= (byte)~(1 << (y % 8));
        }


        // 2. Линия (Целочисленный алгоритм Брезенхема)
        public void DrawLine(int x0, int y0, int x1, int y1, bool color)
        {
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                DrawPixel(x0, y0, color);
                if (x0 == x1 && y0 == y1) break;
                
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx)  { err += dx; y0 += sy; }
            }
        }

        // 3. Контурный прямоугольник
        public void DrawRectangle(int x, int y, int width, int height, bool color)
        {
            DrawLine(x, y, x + width - 1, y, color);
            DrawLine(x, y + height - 1, x + width - 1, y + height - 1, color);
            DrawLine(x, y, x, y + height - 1, color);
            DrawLine(x + width - 1, y, x + width - 1, y + height - 1, color);
        }

        // 4. Закрашенный прямоугольник
        public void FillRectangle(int x, int y, int width, int height, bool color)
        {
            for (int i = 0; i < height; i++)
            {
                DrawLine(x, y + i, x + width - 1, y + i, color);
            }
        }

        // 5. Окружность (Алгоритм Брезенхема / Андреса)
        public void DrawCircle(int xc, int yc, int r, bool color)
        {
            int x = 0;
            int y = r;
            int d = 3 - 2 * r;

            while (y >= x)
            {
                DrawPixel(xc + x, yc + y, color);
                DrawPixel(xc - x, yc + y, color);
                DrawPixel(xc + x, yc - y, color);
                DrawPixel(xc - x, yc - y, color);
                DrawPixel(xc + y, yc + x, color);
                DrawPixel(xc - y, yc + x, color);
                DrawPixel(xc + y, yc - x, color);
                DrawPixel(xc - y, yc - x, color);

                if (d < 0)
                {
                    d += 4 * x + 6;
                }
                else
                {
                    d += 4 * (x - y) + 10;
                    y--;
                }
                x++;
            }
        }

        // 6. Контурный треугольник
        public void DrawTriangle(int x0, int y0, int x1, int y1, int x2, int y2, bool color)
        {
            DrawLine(x0, y0, x1, y1, color);
            DrawLine(x1, y1, x2, y2, color);
            DrawLine(x2, y2, x0, y0, color);
        }
        // 7. Отрисовка монохромного изображения (Битмапа)
        public void DrawBitmap(int x, int y, byte[] bitmap, int width, int height, bool color)
        {
            // Вычисляем, сколько байт занимает одна горизонтальная строка картинки
            int bytesPerRow = (width + 7) / 8;

            for (int pageY = 0; pageY < height; pageY++)
            {
                for (int pixelX = 0; pixelX < width; pixelX++)
                {
                    // Находим конкретный байт в массиве картинки
                    int byteIndex = pageY * bytesPerRow + (pixelX / 8);
                    
                    // Вытаскиваем нужный бит из этого байта
                    byte b = bitmap[byteIndex];
                    int bitShift = 7 - (pixelX % 8);

                    // Если бит равен 1 — зажигаем пиксель в нашем буфере
                    if (((b >> bitShift) & 1) == 1)
                    {
                        // color определяет, рисуем мы картинку белым (true) или стираем (false)
                        DrawPixel(x + pixelX, y + pageY, color);
                    }
                }
            }
        }

    }
}
