import React, { useEffect, useState } from 'react';
import {
  PieChart, Pie, Cell, ResponsiveContainer, Tooltip,
  BarChart, Bar, XAxis, YAxis, CartesianGrid
} from 'recharts';
import api from '../api';

const COLORS = ['#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6'];

export const AdminDashboard: React.FC = () => {
  const [stats, setStats] = useState<any>(null);

  useEffect(() => {
    api.get('/tma/admin/stats').then(res => setStats(res.data)).catch(console.error);
  }, []);

  if (!stats) return <div className="flex-center" style={{height: '50vh'}}><div className="spinner"></div></div>;

  return (
    <div className="animate-fade-in">
      <h2 className="text-title text-gradient">HR Admin Dashboard</h2>
      <p className="text-subtitle" style={{marginBottom: '20px'}}>
        Аналитика и метрики удовлетворенности
      </p>

      <div className="grid-cols-2">
        <div className="glass-card flex-center" style={{flexDirection: 'column', padding: '24px 16px'}}>
          <div style={{fontSize: '2.5rem', fontWeight: 700, color: '#f59e0b'}}>{stats.csatScore.toFixed(1)}</div>
          <div className="text-subtitle">CSAT Score ({stats.csatTotal} оценок)</div>
        </div>
        <div className="glass-card flex-center" style={{flexDirection: 'column', padding: '24px 16px'}}>
          <div style={{fontSize: '2.5rem', fontWeight: 700, color: '#3b82f6'}}>{stats.avgResponseTimeMs}ms</div>
          <div className="text-subtitle">Avg Response Time</div>
        </div>
      </div>

      <div className="glass-card" style={{marginTop: '16px'}}>
        <h3 style={{marginBottom: '16px', fontSize: '1.1rem'}}>Распределение запросов</h3>
        <div style={{height: '250px'}}>
          <ResponsiveContainer width="100%" height="100%">
            <PieChart>
              <Pie
                data={stats.intents}
                cx="50%"
                cy="50%"
                innerRadius={60}
                outerRadius={80}
                paddingAngle={5}
                dataKey="value"
                stroke="none"
              >
                {stats.intents.map((_: any, index: number) => (
                  <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                ))}
              </Pie>
              <Tooltip 
                contentStyle={{backgroundColor: 'rgba(15, 23, 42, 0.9)', border: '1px solid rgba(255,255,255,0.1)', borderRadius: '8px'}}
                itemStyle={{color: '#fff'}}
              />
            </PieChart>
          </ResponsiveContainer>
        </div>
        <div style={{display: 'flex', flexWrap: 'wrap', gap: '8px', justifyContent: 'center'}}>
          {stats.intents.map((intent: any, index: number) => (
            <div key={intent.name} style={{display: 'flex', alignItems: 'center', fontSize: '0.85rem'}}>
              <div style={{width: '10px', height: '10px', borderRadius: '50%', backgroundColor: COLORS[index % COLORS.length], marginRight: '4px'}}></div>
              {intent.name} ({intent.value})
            </div>
          ))}
        </div>
      </div>

      <div className="glass-card" style={{marginTop: '16px'}}>
        <h3 style={{marginBottom: '16px', fontSize: '1.1rem'}}>Топ запросов по объёму</h3>
        <div style={{height: '250px'}}>
          <ResponsiveContainer width="100%" height="100%">
            <BarChart data={stats.intents}>
              <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.05)" vertical={false} />
              <XAxis dataKey="name" stroke="#94a3b8" fontSize={12} tickLine={false} axisLine={false} />
              <YAxis stroke="#94a3b8" fontSize={12} tickLine={false} axisLine={false} />
              <Tooltip 
                cursor={{fill: 'rgba(255,255,255,0.05)'}}
                contentStyle={{backgroundColor: 'rgba(15, 23, 42, 0.9)', border: '1px solid rgba(255,255,255,0.1)', borderRadius: '8px'}}
              />
              <Bar dataKey="value" fill="#3b82f6" radius={[4, 4, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>
      </div>
    </div>
  );
};
