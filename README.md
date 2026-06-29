# driverSSD1306-nanoFramework-ESP32-
driver for oled display, which ready for use on ESP32
Самописный драйвер для OLED дисплея SSD1306 на nanoFramework (ESP32).
Написан с нуля без использования стандартных библиотек IoT.

## Возможности

- Вывод текста с поддержкой кириллицы (CP1251)
- Примитивы: линия, прямоугольник, круг, треугольник
- Отрисовка монохромных битмапов
- Двойная буферизация
- Алгоритм Брезенхема для линий и окружностей

## Подключение

| Дисплей | ESP32 |
|---------|-------|
| SDA     | GPIO 21 |
| SCL     | GPIO 22 |
| VCC     | 3.3V |
| GND     | GND |

## Быстрый старт

```csharp
var display = new _display(128, 64);
display.Drawstr(0, 0, "Привет!");
display.Render();
```

## Требования

- nanoFramework
- ESP32

## Лицензия

MIT
