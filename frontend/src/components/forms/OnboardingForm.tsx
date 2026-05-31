import React, { useState } from 'react';
import api from '../../api';

interface OnboardingFormProps {
  employeeId: number;
  onComplete: () => void;
}

const DEPARTMENTS = ['Разработка', 'Маркетинг', 'Продажи', 'HR', 'Финансы', 'Операции', 'Дизайн', 'Юридический'];

export const OnboardingForm: React.FC<OnboardingFormProps> = ({ employeeId, onComplete }) => {
  const [department, setDepartment] = useState('');
  const [customDept, setCustomDept] = useState('');
  const [position, setPosition] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const effectiveDept = department === '__custom__' ? customDept : department;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!effectiveDept || !position) {
      setError('Пожалуйста, заполните все поля.');
      return;
    }
    setLoading(true);
    setError(null);
    try {
      await api.post('/tma/onboarding', { employeeId, department: effectiveDept, position });
      onComplete();
    } catch (err: any) {
      setError(err.message || 'Произошла ошибка');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{ minHeight: '100vh', background: 'var(--bg-base)', display: 'flex', flexDirection: 'column' }}>
      {/* Green hero header */}
      <div style={{
        background: 'var(--gradient-hero)',
        padding: '40px 24px 32px',
        color: '#fff',
        textAlign: 'center',
      }}>
        <div style={{ fontSize: '3rem', marginBottom: '12px' }}>👋</div>
        <h1 style={{ color: '#fff', fontSize: '1.6rem', fontFamily: 'Outfit, sans-serif', marginBottom: '8px' }}>
          Добро пожаловать!
        </h1>
        <p style={{ opacity: 0.9, fontSize: '0.95rem', lineHeight: 1.5 }}>
          Давайте настроим ваш профиль, чтобы HROps мог лучше вам помогать
        </p>
      </div>

      <div style={{ padding: '24px 16px', flex: 1 }}>
        <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
          {/* Department */}
          <div>
            <div className="section-label">Ваш отдел</div>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '8px', marginBottom: '8px' }}>
              {DEPARTMENTS.map(dept => (
                <button
                  key={dept}
                  type="button"
                  onClick={() => setDepartment(dept)}
                  style={{
                    padding: '10px',
                    borderRadius: 'var(--radius-sm)',
                    border: `2px solid ${department === dept ? 'var(--accent-primary)' : 'var(--border-light)'}`,
                    background: department === dept ? 'var(--green-50)' : 'var(--bg-surface)',
                    cursor: 'pointer',
                    fontFamily: 'Inter, sans-serif',
                    fontSize: '0.82rem',
                    fontWeight: department === dept ? 700 : 400,
                    color: department === dept ? 'var(--green-800)' : 'var(--text-secondary)',
                    transition: 'all 0.2s',
                    textAlign: 'center',
                  }}
                >
                  {dept}
                </button>
              ))}
              <button
                type="button"
                onClick={() => setDepartment('__custom__')}
                style={{
                  padding: '10px',
                  borderRadius: 'var(--radius-sm)',
                  border: `2px solid ${department === '__custom__' ? 'var(--accent-primary)' : 'var(--border-light)'}`,
                  background: department === '__custom__' ? 'var(--green-50)' : 'var(--bg-surface)',
                  cursor: 'pointer',
                  fontFamily: 'Inter, sans-serif',
                  fontSize: '0.82rem',
                  fontWeight: department === '__custom__' ? 700 : 400,
                  color: department === '__custom__' ? 'var(--green-800)' : 'var(--text-secondary)',
                  transition: 'all 0.2s',
                  gridColumn: '1 / -1',
                }}
              >
                ✏️ Другой отдел
              </button>
            </div>
            {department === '__custom__' && (
              <input
                className="input-field"
                type="text"
                placeholder="Введите название отдела"
                value={customDept}
                onChange={e => setCustomDept(e.target.value)}
                autoFocus
              />
            )}
          </div>

          {/* Position */}
          <div>
            <div className="section-label">Ваша должность</div>
            <input
              className="input-field"
              type="text"
              placeholder="Например: Frontend Developer, HR Manager..."
              value={position}
              onChange={e => setPosition(e.target.value)}
            />
          </div>

          {error && <div className="toast error">{error}</div>}

          <button
            type="submit"
            className="btn btn-primary btn-full"
            disabled={loading || !effectiveDept || !position}
            style={{ marginTop: '8px', padding: '16px', fontSize: '1rem' }}
          >
            {loading ? 'Сохранение...' : '🚀 Начать работу'}
          </button>
        </form>
      </div>
    </div>
  );
};
