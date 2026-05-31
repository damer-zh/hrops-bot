import React, { useEffect, useState } from 'react';
import { useTelegramUser } from './hooks/useTelegramUser';
import { AdminDashboard } from './components/AdminDashboard';
import { EmployeeDashboard } from './components/EmployeeDashboard';
import { OnboardingForm } from './components/forms/OnboardingForm';
import api from './api';
import * as signalR from '@microsoft/signalr';

type Role = 'employee' | 'hr';

export const App: React.FC = () => {
  const { user, isReady } = useTelegramUser();
  const [employee, setEmployee] = useState<any>(null);
  const [notifications, setNotifications] = useState<string[]>([]);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [roleOverride, setRoleOverride] = useState<Role | null>(null);

  useEffect(() => {
    if (user?.id) {
      api.post('tma/auth', user)
         .then(res => setEmployee(res.data))
         .catch(err => {
            console.error(err);
            setErrorMsg(err.message + " | URL: " + err.config?.url);
         });

      const connection = new signalR.HubConnectionBuilder()
        .withUrl(`${import.meta.env.VITE_API_URL || ''}/notifications?telegramId=${user.id}`)
        .withAutomaticReconnect()
        .build();

      connection.on('ReceiveNotification', (message: string) => {
        setNotifications(prev => [message, ...prev.slice(0, 4)]);
        if (window.Telegram?.WebApp) {
          window.Telegram.WebApp.showAlert(message);
        }
      });

      connection.start().catch(console.error);
      return () => { connection.stop(); };
    }
  }, [user]);

  // ---------- LOADING / ERROR STATES ----------

  if (errorMsg) {
    return (
      <div className="loading-page">
        <div style={{ fontSize: '2.5rem' }}>⚠️</div>
        <div style={{ color: '#dc2626', fontWeight: 700 }}>Ошибка загрузки</div>
        <div style={{ wordBreak: 'break-all', fontSize: '0.8rem', color: 'var(--text-secondary)', textAlign: 'center' }}>{errorMsg}</div>
      </div>
    );
  }

  if (!isReady || (user && !employee)) {
    return (
      <div className="loading-page">
        <div className="spinner" />
        <div className="text-subtitle animate-fade-in">Загружаем профиль...</div>
      </div>
    );
  }

  if (!user || !employee) {
    return (
      <div className="loading-page animate-fade-in">
        <div style={{ fontSize: '3rem' }}>🤖</div>
        <h2 style={{ color: 'var(--accent-primary)' }}>HROps Bot</h2>
        <p className="text-subtitle" style={{ textAlign: 'center' }}>
          Пожалуйста, откройте приложение внутри Telegram.
        </p>
      </div>
    );
  }

  // ---------- ONBOARDING (profile setup) ----------
  if (!employee.department) {
    return <OnboardingForm employeeId={employee.id} onComplete={() => window.location.reload()} />;
  }

  // ---------- ROLE LOGIC ----------
  const effectiveRole: Role = roleOverride ?? (employee.isHrAdmin ? 'hr' : 'employee');
  const isNewEmployee = (Date.now() - new Date(employee.hiredAt).getTime()) < 90 * 24 * 60 * 60 * 1000;

  return (
    <div style={{ paddingBottom: '24px' }}>
      {/* ===== HEADER ===== */}
      <div className="app-header animate-fade-in" style={{ marginBottom: '16px' }}>
        <div className="flex-between" style={{ position: 'relative', zIndex: 1 }}>
          <div>
            <div style={{ fontSize: '0.75rem', opacity: 0.8, fontWeight: 500, textTransform: 'uppercase', letterSpacing: '0.06em' }}>
              {effectiveRole === 'hr' ? '🏢 HR-Панель' : '👤 Сотрудник'}
            </div>
            <div style={{ fontWeight: 700, fontSize: '1.15rem', marginTop: '2px' }}>{employee.nameRu}</div>
            <div style={{ fontSize: '0.82rem', opacity: 0.85, marginTop: '2px' }}>
              {employee.position} · {employee.department}
            </div>
          </div>
          <div className="avatar">{employee.nameRu.charAt(0)}</div>
        </div>

        {/* Test Role Switcher */}
        <div style={{ marginTop: '16px', position: 'relative', zIndex: 1 }}>
          <div style={{ fontSize: '0.68rem', opacity: 0.75, marginBottom: '6px', textTransform: 'uppercase', letterSpacing: '0.06em' }}>
            🧪 Тест-режим: переключить вид
          </div>
          <div className="role-switcher" style={{ background: 'rgba(255,255,255,0.15)' }}>
            <button
              className={`role-tab ${effectiveRole === 'employee' ? 'active' : ''}`}
              onClick={() => setRoleOverride('employee')}
              style={effectiveRole !== 'employee' ? { color: 'rgba(255,255,255,0.8)', background: 'transparent' } : {}}
            >
              👤 Сотрудник
            </button>
            <button
              className={`role-tab ${effectiveRole === 'hr' ? 'active' : ''}`}
              onClick={() => setRoleOverride('hr')}
              style={effectiveRole !== 'hr' ? { color: 'rgba(255,255,255,0.8)', background: 'transparent' } : {}}
            >
              🏢 HR
            </button>
          </div>
        </div>
      </div>

      {/* ===== NOTIFICATIONS ===== */}
      {notifications.length > 0 && (
        <div className="animate-fade-in delay-100" style={{ marginBottom: '16px' }}>
          {notifications.slice(0, 2).map((msg, i) => (
            <div key={i} className="toast success" style={{ marginBottom: '8px' }}>
              <span>🔔</span>
              <span>{msg}</span>
              <button
                onClick={() => setNotifications(prev => prev.filter((_, idx) => idx !== i))}
                style={{ marginLeft: 'auto', background: 'none', border: 'none', cursor: 'pointer', color: 'var(--green-700)' }}
              >✕</button>
            </div>
          ))}
        </div>
      )}

      {/* ===== DASHBOARD ===== */}
      {effectiveRole === 'hr' ? (
        <AdminDashboard />
      ) : (
        <EmployeeDashboard
          employeeId={employee.id}
          employeeName={employee.nameRu}
          isNewEmployee={isNewEmployee}
        />
      )}
    </div>
  );
};

export default App;
