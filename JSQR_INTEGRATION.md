# jsQR Integration Guide

## Когда npm install заработает

Это краткая инструкция по добавлению jsQR библиотеки для автоматического сканирования QR-кодов.

### Шаг 1: Установить jsQR

```bash
cd frontend
npm install jsqr
npm install --save-dev @types/jsqr
```

### Шаг 2: Обновить ScanQR.tsx

Найдите функцию `captureFrame()` в [ScanQR.tsx](frontend/src/components/ScanQR.tsx) и замените на:

```typescript
import jsQR from "jsqr";

// ... существующий код ...

private async captureFrame(): Promise<void> {
    if (!this.videoRef.current || !this.canvasRef.current) return;

    try {
        const video = this.videoRef.current;
        const canvas = this.canvasRef.current;
        const context = canvas.getContext("2d");

        if (!context) return;

        // Рисуем кадр из видео на canvas
        context.drawImage(video, 0, 0, canvas.width, canvas.height);

        // Получаем данные пикселей
        const imageData = context.getImageData(0, 0, canvas.width, canvas.height);

        // Сканируем QR-код
        const code = jsQR(imageData.data, imageData.width, imageData.height);

        if (code) {
            console.log("✅ QR-код найден:", code.data);
            this.props.onTokenFound(code.data);
            // Остановить дальнейшее сканирование
            this.stopCamera();
            return;
        }
    } catch (error) {
        console.error("Ошибка при сканировании QR:", error);
    }

    // Продолжить сканирование, если QR не найден
    setTimeout(() => this.captureFrame(), 500);
}
```

### Шаг 3: Протестировать

```bash
cd frontend
npm run dev
```

Перейти на `http://localhost:5173/?scan=true` и сканировать QR-код.

### Результаты

- ✅ Автоматическое сканирование будет работать
- ✅ Fallback ввод всё ещё будет доступен как резервный вариант
- ✅ Система перенаправит на `/?verify={token}` после обнаружения QR

### Файл для отслеживания

Когда npm заработает, выполните команды выше и удалите этот файл.

---

## Причины, почему jsQR не установлена сейчас

1. **npm install ETIMEOUT** - Проблема с сетью/proxy в окружении
2. **Fallback работает** - Ручной ввод токена работает как временное решение
3. **Не блокирует систему** - Вся остальная функциональность готова

## Когда Всё Заработает

Система полностью готова к использованию даже без jsQR:
- 🪪 Сотрудник может генерировать пропуск с QR
- 📱 Сотрудник видит красивую ID-карточку с QR-кодом
- 👮 Охранник может вводить токен вручную (fallback)
- ✅ Система проверяет токен и показывает результат

jsQR просто сделает сканирование **автоматическим** вместо **ручного ввода**.

