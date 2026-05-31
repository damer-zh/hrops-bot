import React, { useEffect, useState } from 'react';
import api from '../api';

interface EmployeeDashboardProps {
  employeeId: number;
}

export const EmployeeDashboard: React.FC<EmployeeDashboardProps> = ({ employeeId }) => {
  const [data, setData] = useState<any>(null);

  useEffect(() => {
    api.get(`/tma/dashboard/${employeeId}`).then(res => setData(res.data)).catch(console.error);
  }, [employeeId]);

  if (!data) return <div className="flex-center" style={{height: '50vh'}}><div className="spinner"></div></div>;

  const { vacation, tasks, equipment } = data;
  const overdueTasks = tasks.filter((t: any) => t.isOverdue).length;

  return (
    <div className="animate-fade-in delay-100">
      <h2 className="text-title text-gradient">Ваш профиль</h2>
      <p className="text-subtitle" style={{marginBottom: '20px'}}>
        Сводка по отпускам, задачам и заявкам
      </p>

      {/* Vacation Card */}
      <div className="glass-card" style={{marginBottom: '16px'}}>
        <div className="flex-between" style={{marginBottom: '12px'}}>
          <h3 style={{fontSize: '1.1rem'}}>🌴 Отпуск</h3>
          <span className="badge info">{vacation.remaining} дн. доступно</span>
        </div>
        <div style={{background: 'rgba(0,0,0,0.2)', borderRadius: '8px', height: '8px', overflow: 'hidden'}}>
          <div 
            style={{
              width: `${(vacation.used / vacation.total) * 100}%`, 
              height: '100%', 
              background: 'var(--gradient-primary)'
            }} 
          />
        </div>
        <div className="flex-between text-subtitle" style={{marginTop: '8px', fontSize: '0.8rem'}}>
          <span>Использовано: {vacation.used}</span>
          <span>Всего: {vacation.total}</span>
        </div>
      </div>

      {/* Tasks Card */}
      <div className="glass-card" style={{marginBottom: '16px'}}>
        <div className="flex-between" style={{marginBottom: '12px'}}>
          <h3 style={{fontSize: '1.1rem'}}>✅ Задачи</h3>
          {overdueTasks > 0 && <span className="badge danger">{overdueTasks} просрочено</span>}
        </div>
        <div className="list">
          {tasks.length === 0 ? (
            <div className="text-subtitle flex-center" style={{padding: '20px 0'}}>Нет активных задач 🎉</div>
          ) : (
            tasks.slice(0, 3).map((t: any) => (
              <div key={t.id} className="list-item">
                <div style={{flex: 1}}>
                  <div style={{fontWeight: 500, color: t.isOverdue ? '#f87171' : 'inherit'}}>{t.titleRu}</div>
                  <div className="text-subtitle" style={{fontSize: '0.8rem', marginTop: '4px'}}>
                    {t.priority === 1 ? '🔴 Critical' : t.priority === 2 ? '🟠 High' : '🟢 Normal'}
                  </div>
                </div>
              </div>
            ))
          )}
        </div>
      </div>

      {/* Equipment Card */}
      <div className="glass-card">
        <div className="flex-between" style={{marginBottom: '12px'}}>
          <h3 style={{fontSize: '1.1rem'}}>💻 Оборудование</h3>
          <span className="badge warning">{equipment.length} заявок</span>
        </div>
        <div className="list">
          {equipment.length === 0 ? (
            <div className="text-subtitle flex-center" style={{padding: '20px 0'}}>Нет активных заявок</div>
          ) : (
            equipment.map((e: any) => (
              <div key={e.id} className="list-item">
                <div style={{flex: 1}}>
                  <div style={{fontWeight: 500}}>Заявка {e.ticketNumber}</div>
                  <div className="text-subtitle" style={{fontSize: '0.8rem', marginTop: '4px'}}>
                    {e.type === 1 ? 'Ноутбук' : e.type === 2 ? 'Монитор' : 'Оборудование'}
                  </div>
                </div>
                <span className="badge warning">В обработке</span>
              </div>
            ))
          )}
        </div>
      </div>
    </div>
  );
};
