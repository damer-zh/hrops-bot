import { useEffect, useState } from 'react';

// Extend Window interface for Telegram WebApp
declare global {
  interface Window {
    Telegram?: {
      WebApp: any;
    };
  }
}

export interface TelegramUser {
  id: number;
  first_name: string;
  last_name?: string;
  username?: string;
  language_code?: string;
}

export function useTelegramUser() {
  const [user, setUser] = useState<TelegramUser | null>(null);
  const [isReady, setIsReady] = useState(false);

  useEffect(() => {
    const tg = window.Telegram?.WebApp;
    
    if (tg) {
      tg.ready();
      tg.expand();
      
      const initDataUnsafe = tg.initDataUnsafe;
      if (initDataUnsafe?.user) {
        setUser(initDataUnsafe.user);
      } else {
        // Fallback for local development
        if (import.meta.env.DEV) {
          setUser({ id: 100000003, first_name: 'Local', last_name: 'Admin' }); // 100000003 is HR Admin Dinara in mock DB
          // setUser({ id: 100000001, first_name: 'Local', last_name: 'Employee' });
        }
      }
      setIsReady(true);
    } else {
      // Fallback for regular browser without Telegram WebApp injected
      if (import.meta.env.DEV || true) { // allow testing outside telegram for now
        setUser({ id: 479526836, first_name: 'Local', last_name: 'Dev' }); // use real id
      }
      setIsReady(true);
    }
  }, []);

  return { user, isReady };
}
