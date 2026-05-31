import React, { useEffect, useState } from 'react';
import { useTelegramUser } from './hooks/useTelegramUser';
import { AdminDashboard } from './components/AdminDashboard';
import { EmployeeDashboard } from './components/EmployeeDashboard';
import { OnboardingForm } from './components/forms/OnboardingForm';
import api from './api';
import * as signalR from '@microsoft/signalr';

export const App: React.FC = () => {
  const { user, isReady } = useTelegramUser();
  const [employee, setEmployee] = useState<any>(null);
  const [notifications, setNotifications] = useState<string[]>([]);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  useEffect(() => {
    if (user?.id) {
      api.post('tma/auth', user)
         .then(res => setEmployee(res.data))
         .catch(err => {
            console.error(err);
            setErrorMsg(err.message + " | URL: " + err.config?.url + " | BaseURL: " + err.config?.baseURL);
         });

      // SignalR setup
      const connection = new signalR.HubConnectionBuilder()
        .withUrl(`${import.meta.env.VITE_API_URL || ''}/notifications?telegramId=${user.id}`)
        .withAutomaticReconnect()
        .build();

      connection.on('ReceiveNotification', (message: string) => {
        setNotifications(prev => [message, ...prev]);
        
        // Show Telegram native popup if available
        if (window.Telegram?.WebApp) {
          window.Telegram.WebApp.showAlert(message);
        }
      });

      connection.start().catch(console.error);

      return () => {
        connection.stop();
      };
    }
  }, [user]);

  if (errorMsg) {
    return (
      <div className="flex-center" style={{height: '100vh', flexDirection: 'column', padding: 20}}>
        <div style={{color: 'red', marginBottom: '16px'}}>Ошибка загрузки:</div>
        <div style={{wordBreak: 'break-all', fontSize: '0.8rem'}}>{errorMsg}</div>
        <div style={{marginTop: 20, fontSize: '0.8rem'}}>
           VITE_API_URL = {import.meta.env.VITE_API_URL}
        </div>
      </div>
    );
  }

  if (!isReady || (user && !employee)) {
    return (
      <div className="flex-center" style={{height: '100vh', flexDirection: 'column'}}>
        <div className="spinner" style={{marginBottom: '16px'}}></div>
        <div className="text-subtitle animate-fade-in">Загрузка профиля...</div>
      </div>
    );
  }

  if (!user || !employee) {
    return (
      <div className="flex-center animate-fade-in" style={{height: '100vh', flexDirection: 'column'}}>
        <h2 className="text-title text-gradient">HROps Bot</h2>
        <p className="text-subtitle" style={{textAlign: 'center'}}>
          Пожалуйста, откройте приложение внутри Telegram.
        </p>
      </div>
    );
  }

  // Intercept for Onboarding
  if (!employee.department) {
    return <OnboardingForm employeeId={employee.id} onComplete={() => window.location.reload()} />;
  }

  return (
    <div style={{paddingBottom: '24px'}}>
      {/* Header Profile Info */}
      <div className="glass-card flex-between animate-fade-in" style={{marginBottom: '20px', padding: '16px'}}>
        <div>
          <div style={{fontWeight: 600, fontSize: '1.1rem'}}>{employee.nameRu}</div>
          <div className="text-subtitle" style={{fontSize: '0.85rem'}}>{employee.position} • {employee.department}</div>
        </div>
        <div className="flex-center" style={{
          width: '40px', height: '40px', 
          borderRadius: '50%', background: 'var(--gradient-primary)',
          fontWeight: 'bold', fontSize: '1.2rem'
        }}>
          {employee.nameRu.charAt(0)}
        </div>
      </div>

      {/* Notifications Toast Area */}
      {notifications.length > 0 && (
        <div className="animate-fade-in delay-100" style={{marginBottom: '20px'}}>
          {notifications.map((msg, i) => (
            <div key={i} className="glass-card" style={{borderLeft: '4px solid var(--accent-primary)', marginBottom: '8px', padding: '12px 16px'}}>
              <div className="text-subtitle" style={{color: 'var(--text-primary)'}}>{msg}</div>
            </div>
          ))}
        </div>
      )}

      {/* Dashboard Router */}
      {employee.isHrAdmin ? (
        <AdminDashboard />
      ) : (
        <EmployeeDashboard employeeId={employee.id} />
      )}
    </div>
  );
};

export default App;
