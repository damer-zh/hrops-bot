import React, { useEffect, useRef, useState } from "react";

interface ScanQRProps {
    onTokenFound: (token: string) => void;
    onClose: () => void;
}

/**
 * QR-сканер для охранника (или для быстрого теста)
 * Использует встроенную камеру браузера для сканирования QR кодов
 * jsQR загружается через CDN для распознавания QR-кодов
 */
export const ScanQR: React.FC<ScanQRProps> = ({ onTokenFound, onClose }) => {
    const videoRef = useRef<HTMLVideoElement>(null);
    const canvasRef = useRef<HTMLCanvasElement>(null);
    const [error, setError] = useState<string | null>(null);
    const [scanning, setScanning] = useState(true);
    const [brightness, setBrightness] = useState(100);
    const [jsQRLoaded, setJsQRLoaded] = useState(false);
    const scanStartTimeRef = useRef<number>(0);

    // Загружаем jsQR библиотеку через CDN
    useEffect(() => {
        const script = document.createElement("script");
        script.src = "https://cdn.jsdelivr.net/npm/jsqr@1.4.0/dist/jsQR.js";
        script.async = true;
        script.onload = () => {
            setJsQRLoaded(true);
        };
        script.onerror = () => {
            console.warn("jsQR не загружена - используется fallback ввод");
            setJsQRLoaded(false);
        };
        document.head.appendChild(script);
        return () => {
            document.head.removeChild(script);
        };
    }, []);

    useEffect(() => {
        if (!scanning) return;

        const startCamera = async () => {
            try {
                // Начинаем отсчёт времени сканирования
                scanStartTimeRef.current = Date.now();
                
                const stream = await navigator.mediaDevices.getUserMedia({
                    video: { facingMode: "environment", width: { ideal: 1280 }, height: { ideal: 720 } },
                });
                if (videoRef.current) {
                    videoRef.current.srcObject = stream;
                }
            } catch (err) {
                setError("Не удалось получить доступ к камере");
                setScanning(false);
            }
        };

        startCamera();
        return () => {
            if (videoRef.current?.srcObject) {
                const tracks = (videoRef.current.srcObject as MediaStream).getTracks();
                tracks.forEach(track => track.stop());
            }
        };
    }, [scanning]);

    // Захват кадра и распознавание QR-кода
    const captureFrame = () => {
        if (!videoRef.current || !canvasRef.current) return;

        const context = canvasRef.current.getContext("2d");
        if (!context) return;

        // Рисуем кадр из видео
        context.drawImage(videoRef.current, 0, 0, canvasRef.current.width, canvasRef.current.height);

        // Если jsQR загружена - используем её для распознавания QR
        if (jsQRLoaded && (window as any).jsQR) {
            try {
                const imageData = context.getImageData(0, 0, canvasRef.current.width, canvasRef.current.height);
                const qrCode = (window as any).jsQR(imageData.data, imageData.width, imageData.height);
                
                if (qrCode && qrCode.data) {
                    console.log("✅ QR найден:", qrCode.data);
                    // Извлекаем токен из URL параметра verify= если это URL
                    const url = qrCode.data;
                    const urlObj = new URL(url, window.location.origin);
                    const token = urlObj.searchParams.get("verify") || url;
                    
                    if (token && token.trim()) {
                        onTokenFound(token.trim());
                        setScanning(false);
                        return;
                    }
                }
            } catch (err) {
                console.error("Ошибка при распознавании QR:", err);
            }
        }

        // Проверяем таймер - если сканируем более 30 сек без результата → ошибка
        if (scanStartTimeRef.current && Date.now() - scanStartTimeRef.current > 30000) {
            setError("QR код не найден. Попробуйте ещё раз.");
            setScanning(false);
        }
    };

    useEffect(() => {
        if (!scanning) return;
        const interval = setInterval(captureFrame, 300); // Быстрее сканируем с jsQR
        return () => clearInterval(interval);
    }, [scanning, jsQRLoaded]);

    return (
        <div style={{
            position: "fixed",
            inset: 0,
            background: "rgba(0, 0, 0, 0.95)",
            zIndex: 2000,
            display: "flex",
            flexDirection: "column",
            alignItems: "center",
            justifyContent: "center",
            padding: "20px",
        }}>
            <style>{`
                @keyframes pulse { 0%, 100% { opacity: 1; } 50% { opacity: 0.5; } }
                .scanning-border { animation: pulse 1.5s ease-in-out infinite; }
            `}</style>

            {/* Заголовок */}
            <div style={{ color: "#fff", marginBottom: 24, textAlign: "center" }}>
                <div style={{ fontSize: "1.2rem", fontWeight: 700, marginBottom: 8 }}>
                    📱 Сканирование пропуска
                </div>
                <div style={{ fontSize: "0.85rem", color: "rgba(255,255,255,0.6)" }}>
                    {jsQRLoaded ? "✅ Автоматическое сканирование" : "⏳ Инициализация..."}
                </div>
                <div style={{ fontSize: "0.75rem", color: "rgba(255,255,255,0.4)", marginTop: 4 }}>
                    Направьте камеру на QR код
                </div>
            </div>

            {/* Видео + обрамление */}
            <div style={{
                position: "relative",
                width: "100%",
                maxWidth: 400,
                aspectRatio: "1",
                background: "#000",
                borderRadius: 16,
                overflow: "hidden",
                border: "3px solid #3b82f6",
                marginBottom: 20,
            }}>
                {error ? (
                    <div style={{
                        width: "100%",
                        height: "100%",
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "center",
                        color: "#ef4444",
                        fontSize: "0.9rem",
                        textAlign: "center",
                        padding: "20px",
                    }}>
                        ⚠️ {error}
                    </div>
                ) : (
                    <>
                        <video
                            ref={videoRef}
                            autoPlay
                            playsInline
                            style={{
                                width: "100%",
                                height: "100%",
                                objectFit: "cover",
                                filter: `brightness(${brightness}%)`,
                            }}
                        />
                        {/* Фокусирующая рамка */}
                        <div style={{
                            position: "absolute",
                            inset: "20%",
                            border: "2px solid rgba(59,130,246,0.8)",
                            borderRadius: 12,
                            pointerEvents: "none",
                            className: "scanning-border",
                        }} />
                        <div style={{
                            position: "absolute",
                            top: "50%",
                            left: "50%",
                            transform: "translate(-50%, -50%)",
                            color: "#3b82f6",
                            fontSize: "0.7rem",
                            opacity: 0.6,
                            pointerEvents: "none",
                        }}>
                            Поместите QR в центр
                        </div>
                    </>
                )}
                <canvas ref={canvasRef} width={400} height={400} style={{ display: "none" }} />
            </div>

            {/* Яркость */}
            {!error && (
                <div style={{
                    width: "100%",
                    maxWidth: 300,
                    marginBottom: 20,
                    color: "#fff",
                }}>
                    <div style={{ fontSize: "0.75rem", marginBottom: 8, opacity: 0.7 }}>
                        💡 Яркость: {brightness}%
                    </div>
                    <input
                        type="range"
                        min="50"
                        max="150"
                        value={brightness}
                        onChange={(e) => setBrightness(Number(e.target.value))}
                        style={{
                            width: "100%",
                            cursor: "pointer",
                        }}
                    />
                </div>
            )}

            {/* Кнопка закрытия */}
            <button
                onClick={onClose}
                style={{
                    background: "rgba(255,255,255,0.1)",
                    border: "1px solid rgba(255,255,255,0.3)",
                    color: "rgba(255,255,255,0.8)",
                    borderRadius: 12,
                    padding: "12px 20px",
                    fontSize: "0.9rem",
                    fontWeight: 600,
                    cursor: "pointer",
                    width: "100%",
                    maxWidth: 300,
                }}
            >
                ✕ Закрыть
            </button>

            <div style={{
                marginTop: 20,
                fontSize: "0.7rem",
                color: "rgba(255,255,255,0.3)",
                textAlign: "center",
            }}>
                🛡️ HROps Security · Сканер пропусков
            </div>
        </div>
    );
};
