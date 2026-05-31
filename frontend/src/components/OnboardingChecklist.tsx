import React, { useEffect, useState } from 'react';
import api from '../api';

interface OnboardingChecklistProps {
  employeeId: number;
  employeeName: string;
}

interface Progress {
  docsSubmitted: boolean;
  accessGranted: boolean;
  equipmentReceived: boolean;
  materialsRead: boolean;
  firstTasksDone: boolean;
  buddyMet: boolean;
  hr1on1Done: boolean;
  progressPercent: number;
  startedAt: string;
}

const STEPS = [
  {
    key: 'docs',
    field: 'docsSubmitted',
    icon: '📄',
    title: 'Документы',
    desc: 'Подписать трудовой договор, НДА и прочие документы',
  },
  {
    key: 'access',
    field: 'accessGranted',
    icon: '🔑',
    title: 'Доступы к системам',
    desc: 'Получить доступ к Jira, Slack, корпоративной почте',
  },
  {
    key: 'equipment',
    field: 'equipmentReceived',
    icon: '💻',
    title: 'Техника',
    desc: 'Получить ноутбук, монитор и рабочее оборудование',
  },
  {
    key: 'materials',
    field: 'materialsRead',
    icon: '📚',
    title: 'Вводные материалы',
    desc: 'Изучить регламенты, культуру компании и процессы',
  },
  {
    key: 'buddy',
    field: 'buddyMet',
    icon: '🤝',
    title: 'Знакомство с ментором',
    desc: 'Провести встречу с назначенным бадди/ментором',
  },
  {
    key: 'tasks',
    field: 'firstTasksDone',
    icon: '✅',
    title: 'Первые задачи',
    desc: 'Выполнить задачи первой недели от руководителя',
  },
  {
    key: 'hr1on1',
    field: 'hr1on1Done',
    icon: '💬',
    title: 'Встреча 1-на-1 с HR',
    desc: 'Пройти первую встречу с HR для обратной связи',
  },
];

export const OnboardingChecklist: React.FC<OnboardingChecklistProps> = ({ employeeId, employeeName }) => {
  const [progress, setProgress] = useState<Progress | null>(null);
  const [updating, setUpdating] = useState<string | null>(null);

  useEffect(() => {
    api.get(`/tma/onboarding-progress/${employeeId}`)
      .then(res => setProgress(res.data))
      .catch(console.error);
  }, [employeeId]);

  const toggleStep = async (stepKey: string, currentValue: boolean) => {
    if (updating) return;
    setUpdating(stepKey);
    try {
      const res = await api.post(`/tma/onboarding-progress/${employeeId}/step`, {
        step: stepKey,
        value: !currentValue,
      });
      setProgress(prev => prev ? { ...prev, [STEPS.find(s => s.key === stepKey)!.field]: !currentValue, progressPercent: res.data.progressPercent } : prev);
    } catch (e) {
      console.error(e);
    } finally {
      setUpdating(null);
    }
  };

  if (!progress) return <div className="flex-center" style={{ height: '100px' }}><div className="spinner" /></div>;

  const done = STEPS.filter(s => progress[s.field as keyof Progress] as boolean).length;

  return (
    <div className="glass-card no-hover animate-fade-in">
      <div className="section-header">
        <div>
          <div className="section-title">🚀 Онбординг</div>
          <div className="text-subtitle" style={{ fontSize: '0.8rem', marginTop: '2px' }}>
            {done} из {STEPS.length} шагов выполнено
          </div>
        </div>
        <span className={`badge ${progress.progressPercent === 100 ? 'success' : progress.progressPercent > 50 ? 'info' : 'warning'}`}>
          {progress.progressPercent}%
        </span>
      </div>

      <div className="progress-bar-bg" style={{ marginBottom: '20px' }}>
        <div className="progress-bar-fill" style={{ width: `${progress.progressPercent}%` }} />
      </div>

      {progress.progressPercent === 100 ? (
        <div className="empty-state">
          <div className="empty-state-icon">🎉</div>
          <div className="empty-state-text" style={{ color: 'var(--green-700)', fontWeight: 600 }}>
            Онбординг завершён! Добро пожаловать в команду, {employeeName}!
          </div>
        </div>
      ) : (
        <div>
          {STEPS.map(step => {
            const isDone = progress[step.field as keyof Progress] as boolean;
            return (
              <div
                key={step.key}
                className="checklist-item"
                onClick={() => toggleStep(step.key, isDone)}
                style={{ opacity: updating === step.key ? 0.6 : 1 }}
              >
                <div className={`checklist-check ${isDone ? 'done' : ''}`}>
                  {isDone ? '✓' : ''}
                </div>
                <div className="checklist-text">
                  <div className="checklist-title" style={{ textDecoration: isDone ? 'line-through' : 'none', color: isDone ? 'var(--text-muted)' : undefined }}>
                    {step.icon} {step.title}
                  </div>
                  <div className="checklist-desc">{step.desc}</div>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};
