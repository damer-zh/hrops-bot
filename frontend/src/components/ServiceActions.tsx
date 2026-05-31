import React, { useState } from 'react';
import api from '../api';

interface ServiceActionsProps {
  employeeId: number;
}

export const ServiceActions: React.FC<ServiceActionsProps> = ({ employeeId }) => {
  const [activeModal, setActiveModal] = useState<string | null>(null);

  // Forms State
  const [certType, setCertType] = useState('1');
  const [delivery, setDelivery] = useState('digital');
  const [equipType, setEquipType] = useState('1');
  const [regQuery, setRegQuery] = useState('');
  const [regResults, setRegResults] = useState<any[]>([]);
  
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

  const handleAction = async (action: () => Promise<any>) => {
    setLoading(true);
    setMessage(null);
    try {
      await action();
      setMessage("Успешно выполнено!");
      setTimeout(() => {
        setActiveModal(null);
        setMessage(null);
      }, 2000);
    } catch (err: any) {
      setMessage("Ошибка: " + err.message);
    } finally {
      setLoading(false);
    }
  };

  const requestCertificate = () => handleAction(() => 
    api.post('/tma/certificate', { employeeId, type: parseInt(certType), deliveryMethod: delivery })
  );

  const requestEquipment = () => handleAction(() => 
    api.post('/tma/equipment', { employeeId, type: parseInt(equipType) })
  );

  const searchRegulations = async () => {
    if (!regQuery) return;
    setLoading(true);
    try {
      const res = await api.get(`/tma/regulations?q=${encodeURIComponent(regQuery)}`);
      setRegResults(res.data);
    } catch (e) {
      console.error(e);
    } finally {
      setLoading(false);
    }
  };

  const actionButtons = [
    { id: 'cert', icon: '📄', label: 'Справка' },
    { id: 'equip', icon: '💻', label: 'Оборудование' },
    { id: 'reg', icon: '🔍', label: 'Регламенты' },
    { id: 'hr', icon: '🗓️', label: 'К HR' },
    { id: 'faq', icon: '❓', label: 'FAQ' },
  ];

  return (
    <div style={{ marginTop: '24px' }}>
      <h3 style={{ fontSize: '1.1rem', marginBottom: '12px' }}>Действия</h3>
      
      <div style={{ 
        display: 'grid', 
        gridTemplateColumns: 'repeat(auto-fill, minmax(100px, 1fr))', 
        gap: '12px' 
      }}>
        {actionButtons.map(btn => (
          <div 
            key={btn.id}
            className="glass-card flex-center"
            style={{ 
              flexDirection: 'column', 
              padding: '16px 8px', 
              cursor: 'pointer',
              transition: 'transform 0.2s',
            }}
            onClick={() => setActiveModal(btn.id)}
            onMouseOver={(e) => e.currentTarget.style.transform = 'scale(1.05)'}
            onMouseOut={(e) => e.currentTarget.style.transform = 'scale(1)'}
          >
            <div style={{ fontSize: '2rem', marginBottom: '8px' }}>{btn.icon}</div>
            <div style={{ fontSize: '0.8rem', fontWeight: 500, textAlign: 'center' }}>{btn.label}</div>
          </div>
        ))}
      </div>

      {/* MODALS */}
      {activeModal && (
        <div style={{
          position: 'fixed', top: 0, left: 0, right: 0, bottom: 0,
          background: 'rgba(0,0,0,0.6)', backdropFilter: 'blur(4px)',
          display: 'flex', alignItems: 'center', justifyContent: 'center',
          zIndex: 1000, padding: '20px'
        }}>
          <div className="glass-card animate-fade-in" style={{ width: '100%', maxWidth: '400px', padding: '24px', position: 'relative' }}>
            <button 
              onClick={() => setActiveModal(null)}
              style={{ position: 'absolute', top: '12px', right: '12px', background: 'transparent', border: 'none', color: '#fff', fontSize: '1.2rem', cursor: 'pointer' }}
            >
              ✕
            </button>

            {activeModal === 'cert' && (
              <div>
                <h3 style={{ marginBottom: '16px' }}>Заказ справки</h3>
                <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
                  <select className="input-field" value={certType} onChange={e => setCertType(e.target.value)} style={{ padding: '10px', borderRadius: '8px', background: 'rgba(255,255,255,0.1)', color: '#fff', border: 'none' }}>
                    <option value="1" style={{color: '#000'}}>С места работы</option>
                    <option value="2" style={{color: '#000'}}>О доходах</option>
                    <option value="3" style={{color: '#000'}}>Для визы</option>
                  </select>
                  <select className="input-field" value={delivery} onChange={e => setDelivery(e.target.value)} style={{ padding: '10px', borderRadius: '8px', background: 'rgba(255,255,255,0.1)', color: '#fff', border: 'none' }}>
                    <option value="digital" style={{color: '#000'}}>Электронная копия (Telegram)</option>
                    <option value="paper" style={{color: '#000'}}>Бумажный оригинал (Офис)</option>
                  </select>
                  <button onClick={requestCertificate} disabled={loading} style={{ padding: '12px', borderRadius: '8px', background: 'var(--gradient-primary)', color: '#fff', border: 'none', fontWeight: 'bold' }}>
                    {loading ? 'Отправка...' : 'Заказать'}
                  </button>
                </div>
              </div>
            )}

            {activeModal === 'equip' && (
              <div>
                <h3 style={{ marginBottom: '16px' }}>Запрос оборудования</h3>
                <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
                  <select className="input-field" value={equipType} onChange={e => setEquipType(e.target.value)} style={{ padding: '10px', borderRadius: '8px', background: 'rgba(255,255,255,0.1)', color: '#fff', border: 'none' }}>
                    <option value="1" style={{color: '#000'}}>Ноутбук</option>
                    <option value="2" style={{color: '#000'}}>Монитор</option>
                    <option value="3" style={{color: '#000'}}>Клавиатура</option>
                    <option value="4" style={{color: '#000'}}>Мышь</option>
                    <option value="5" style={{color: '#000'}}>Гарнитура</option>
                  </select>
                  <button onClick={requestEquipment} disabled={loading} style={{ padding: '12px', borderRadius: '8px', background: 'var(--gradient-primary)', color: '#fff', border: 'none', fontWeight: 'bold' }}>
                    {loading ? 'Отправка...' : 'Отправить запрос'}
                  </button>
                </div>
              </div>
            )}

            {activeModal === 'reg' && (
              <div>
                <h3 style={{ marginBottom: '16px' }}>Регламенты</h3>
                <div style={{ display: 'flex', gap: '8px', marginBottom: '16px' }}>
                  <input 
                    type="text" 
                    placeholder="Например, отпуск" 
                    value={regQuery} 
                    onChange={e => setRegQuery(e.target.value)}
                    style={{ flex: 1, padding: '10px', borderRadius: '8px', background: 'rgba(255,255,255,0.1)', color: '#fff', border: 'none' }} 
                  />
                  <button onClick={searchRegulations} disabled={loading} style={{ padding: '10px 16px', borderRadius: '8px', background: 'var(--gradient-primary)', color: '#fff', border: 'none' }}>
                    🔍
                  </button>
                </div>
                <div className="list" style={{ maxHeight: '200px', overflowY: 'auto' }}>
                  {regResults.map(r => (
                    <div key={r.id} className="list-item" style={{ padding: '8px' }}>
                      <div style={{ fontWeight: 500 }}>{r.titleRu}</div>
                      <div style={{ fontSize: '0.8rem', color: '#9ca3af' }}>{r.contentRu.substring(0, 50)}...</div>
                    </div>
                  ))}
                  {regResults.length === 0 && !loading && regQuery && (
                    <div style={{ textAlign: 'center', fontSize: '0.9rem', color: '#9ca3af' }}>Ничего не найдено</div>
                  )}
                </div>
              </div>
            )}

            {activeModal === 'hr' && (
              <div style={{ textAlign: 'center', padding: '20px 0' }}>
                <h3>Запись к HR</h3>
                <p style={{ marginTop: '10px', fontSize: '0.9rem', color: '#9ca3af' }}>Функция в разработке для Mini App. Пожалуйста, используйте бота для записи к HR.</p>
              </div>
            )}

            {activeModal === 'faq' && (
              <div style={{ textAlign: 'center', padding: '20px 0' }}>
                <h3>FAQ</h3>
                <p style={{ marginTop: '10px', fontSize: '0.9rem', color: '#9ca3af' }}>Часто задаваемые вопросы скоро появятся здесь.</p>
              </div>
            )}

            {message && (
              <div style={{ marginTop: '16px', padding: '10px', borderRadius: '8px', background: 'rgba(255,255,255,0.2)', textAlign: 'center', fontWeight: 'bold' }}>
                {message}
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
};
