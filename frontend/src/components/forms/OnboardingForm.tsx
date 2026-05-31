import React, { useState } from 'react';
import api from '../../api';

interface OnboardingFormProps {
  employeeId: number;
  onComplete: () => void;
}

export const OnboardingForm: React.FC<OnboardingFormProps> = ({ employeeId, onComplete }) => {
  const [department, setDepartment] = useState('');
  const [position, setPosition] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!department || !position) {
      setError("Пожалуйста, заполните все поля.");
      return;
    }

    setLoading(true);
    setError(null);
    try {
      await api.post('/tma/onboarding', { employeeId, department, position });
      onComplete();
    } catch (err: any) {
      setError(err.message || 'Произошла ошибка');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex-center animate-fade-in" style={{ height: '100vh', padding: '20px' }}>
      <div className="glass-card" style={{ width: '100%', maxWidth: '400px', padding: '30px 20px' }}>
        <h2 className="text-title text-gradient" style={{ textAlign: 'center', marginBottom: '10px' }}>Добро пожаловать!</h2>
        <p className="text-subtitle" style={{ textAlign: 'center', marginBottom: '24px' }}>
          Давайте настроим ваш профиль, чтобы бот мог лучше вам помогать.
        </p>

        <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
          <div>
            <label style={{ display: 'block', marginBottom: '8px', fontWeight: 500, fontSize: '0.9rem' }}>Ваш отдел</label>
            <input 
              type="text" 
              className="input-field" 
              placeholder="Например, Разработка" 
              value={department}
              onChange={e => setDepartment(e.target.value)}
              style={{ width: '100%', padding: '12px', borderRadius: '12px', border: '1px solid rgba(255,255,255,0.1)', background: 'rgba(255,255,255,0.05)', color: '#fff', outline: 'none' }}
            />
          </div>

          <div>
            <label style={{ display: 'block', marginBottom: '8px', fontWeight: 500, fontSize: '0.9rem' }}>Ваша должность</label>
            <input 
              type="text" 
              className="input-field" 
              placeholder="Например, Frontend Developer" 
              value={position}
              onChange={e => setPosition(e.target.value)}
              style={{ width: '100%', padding: '12px', borderRadius: '12px', border: '1px solid rgba(255,255,255,0.1)', background: 'rgba(255,255,255,0.05)', color: '#fff', outline: 'none' }}
            />
          </div>

          {error && <div style={{ color: '#ef4444', fontSize: '0.85rem', textAlign: 'center' }}>{error}</div>}

          <button 
            type="submit" 
            disabled={loading}
            style={{ 
              marginTop: '10px',
              padding: '14px', 
              borderRadius: '12px', 
              border: 'none',
              background: 'var(--gradient-primary)',
              color: '#fff',
              fontWeight: 600,
              fontSize: '1rem',
              cursor: loading ? 'not-allowed' : 'pointer',
              opacity: loading ? 0.7 : 1,
              transition: 'transform 0.2s',
            }}
            onMouseOver={(e) => !loading && (e.currentTarget.style.transform = 'scale(1.02)')}
            onMouseOut={(e) => !loading && (e.currentTarget.style.transform = 'scale(1)')}
          >
            {loading ? <span className="spinner" style={{width: '20px', height: '20px', display: 'inline-block', borderWidth: '2px'}}></span> : "Сохранить профиль"}
          </button>
        </form>
      </div>
    </div>
  );
};
