export const getAvatarStorageKey = (employeeId: number) => `hrops-avatar-${employeeId}`;

export const getStoredAvatar = (employeeId?: number | null) => {
    if (!employeeId) return null;
    return localStorage.getItem(getAvatarStorageKey(employeeId));
};

export const removeStoredAvatar = (employeeId: number) => {
    localStorage.removeItem(getAvatarStorageKey(employeeId));
};

export const resizeAvatarFile = (file: File): Promise<string> =>
    new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onerror = () => reject(new Error("Не удалось прочитать файл"));
        reader.onload = () => {
            const img = new Image();
            img.onerror = () => reject(new Error("Не удалось загрузить изображение"));
            img.onload = () => {
                const size = 360;
                const canvas = document.createElement("canvas");
                canvas.width = size;
                canvas.height = size;
                const ctx = canvas.getContext("2d");

                if (!ctx) {
                    reject(new Error("Canvas недоступен"));
                    return;
                }

                const side = Math.min(img.width, img.height);
                const sx = (img.width - side) / 2;
                const sy = (img.height - side) / 2;

                ctx.drawImage(img, sx, sy, side, side, 0, 0, size, size);
                resolve(canvas.toDataURL("image/jpeg", 0.86));
            };
            img.src = String(reader.result);
        };
        reader.readAsDataURL(file);
    });

export const saveAvatarFile = async (employeeId: number, file: File) => {
    const dataUrl = await resizeAvatarFile(file);
    localStorage.setItem(getAvatarStorageKey(employeeId), dataUrl);
    return dataUrl;
};
