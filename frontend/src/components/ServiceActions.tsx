import React, { useState, useEffect } from 'react';
import api from '../api';
import { ItRequestForm } from './ItRequestForm';
import { VacationRequestForm } from './VacationRequestForm';

interface ServiceActionsProps {
  employeeId: number;
  vacationBalance: { total: number; used: number; remaining: number };
  onRequestCreated?: () => void;
}

type ModalId = 'cert' | 'equip' | 'reg' | 'hr' | 'faq' | 'it' | 'vacation' | null;

const CERT_TYPE_MAP: Record<string, string> = {
  '0': 'С места работы',
  '1': 'О доходах',
  '2': 'ИПН/КПН',
  '3': 'Стаж',
};

export const ServiceActions: React.FC<ServiceActionsProps> = ({ employeeId, vacationBalance, onRequestCreated }) => {
  const [activeModal, setActiveModal] = useState<ModalId>(null);
  const [certType, setCertType] = useState('0');
  const [delivery, setDelivery] = useState('digital');
  const [equipType, setEquipType] = useState('1');
  const [regQuery, setRegQuery] = useState('');
  const [regResults, setRegResults] = useState<any[]>([]);
  const [slots, setSlots] = useState<string[]>([]);
  const [selectedSlot, setSelectedSlot] = useState<string>('');
  const [faqData, setFaqData] = useState<any[]>([]);
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  useEffect(() => {
    if (activeModal === 'hr' && slots.length === 0) {
      api.get('/tma/appointments/slots').then(res => setSlots(res.data)).catch(console.error);
    }
    if (activeModal === 'faq' && faqData.length === 0) {
      api.get('/tma/faq').then(res => setFaqData(res.data)).catch(console.error);
    }
  }, [activeModal]);

  const showSuccess = (text: string) => {
    setMessage({ type: 'success', text });
    setTimeout(() => { setActiveModal(null); setMessage(null); onRequestCreated?.(); }, 2000);
  };
  const showError = (text: string) => setMessage({ type: 'error', text });

  const handleAction = async (action: () => Promise<any>) => {
    setLoading(true);
    setMessage(null);
    try {
      await action();
      showSuccess('Заявка успешно отправлена!');
    } catch (err: any) {
      showError('Ошибка: ' + (err.response?.data?.error || err.message));
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

  const bookAppointment = () => handleAction(() =>
    api.post('/tma/appointments', { employeeId, slot: selectedSlot })
  );

  const searchRegulations = async () => {
    if (!regQuery) return;
    setLoading(true);
    try {
      const res = await api.get(`/tma/regulations?q=${encodeURIComponent(regQuery)}`);
      setRegResults(res.data);
    } catch (e) { console.error(e); }
    finally { setLoading(false); }
  };

  const closeModal = () => { setActiveModal(null); setMessage(null); setRegResults([]); };

  const actionButtons = [
    { id: 'vacation' as ModalId, icon: '🌴', label: 'Отпуск' },
    { id: 'cert'    as ModalId, icon: '📄', label: 'Справка' },
    { id: 'it'     as ModalId, icon: '🔧', label: 'IT-доступ' },
    { id: 'equip'  as ModalId, icon: '💻', label: 'Техника' },
    { id: 'hr'     as ModalId, icon: '🗓️', label: 'К HR' },
    { id: 'reg'    as ModalId, icon: '🔍', label: 'Регламенты' },
    { id: 'faq'    as ModalId, icon: '❓', label: 'FAQ' },
  ];

  const formatSlot = (s: string) => {
    const d = new Date(s);
    return d.toLocaleString('ru-RU', { weekday: 'short', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
  };

  return (
    <div>
      <div className="section-label" style={{ marginBottom: '10px' }}>Действия</div>
      <div className="grid-cols-3" style={{ gap: '10px' }}>
        {actionButtons.slice(0, 6).map(btn => (
          <button key={btn.id} className="action-btn" onClick={() => { setActiveModal(btn.id); setMessage(null); }}>
            <span className="action-icon">{btn.icon}</span>
            <span className="action-label">{btn.label}</span>
          </button>
        ))}
        <button key="faq" className="action-btn" onClick={() => { setActiveModal('faq'); setMessage(null); }}>
          <span className="action-icon">❓</span>
          <span className="action-label">FAQ</span>
        </button>
      </div>

      {/* ===== BOTTOM SHEET MODAL ===== */}
      {activeModal && (
        <div className="modal-overlay" onClick={closeModal}>
          <div className="modal-sheet" onClick={e => e.stopPropagation()}>
            <div className="modal-handle" />

            {/* VACATION */}
            {activeModal === 'vacation' && (
              <>
                <h3 style={{ marginBottom: '20px', fontSize: '1.2rem' }}>🌴 Заявка на отпуск</h3>
                <VacationRequestForm
                  employeeId={employeeId}
                  vacationBalance={vacationBalance}
                  onSuccess={() => { onRequestCreated?.(); setTimeout(closeModal, 2500); }}
                />
              </>
            )}

            {/* CERTIFICATE */}
            {activeModal === 'cert' && (
              <>
                <h3 style={{ marginBottom: '20px', fontSize: '1.2rem' }}>📄 Заказ справки</h3>
                <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
                  <div>
                    <div className="section-label">Тип справки</div>
                    <select className="input-field" value={certType} onChange={e => setCertType(e.target.value)}>
                      <option value="0">С места работы</option>
                      <option value="1">О доходах (2-НДФЛ)</option>
                      <option value="2">ИПН/КПН</option>
                      <option value="3">Стаж работы</option>
                    </select>
                  </div>
                  <div>
                    <div className="section-label">Способ получения</div>
                    <div style={{ display: 'flex', gap: '8px' }}>
                      {[['digital','📱 Электронно'],['paper','📋 Бумажный']].map(([v,l]) => (
                        <button key={v} onClick={() => setDelivery(v)}
                          style={{
                            flex: 1, padding: '10px', borderRadius: 'var(--radius-sm)',
                            border: `2px solid ${delivery === v ? 'var(--accent-primary)' : 'var(--border-light)'}`,
                            background: delivery === v ? 'var(--green-50)' : 'var(--bg-surface)',
                            cursor: 'pointer', fontFamily: 'Inter, sans-serif', fontSize: '0.85rem',
                            fontWeight: 600, color: 'var(--text-primary)', transition: 'all 0.2s',
                          }}>{l}</button>
                      ))}
                    </div>
                  </div>
                  {message && <div className={`toast ${message.type}`}>{message.text}</div>}
                  <button className="btn btn-primary btn-full" onClick={requestCertificate} disabled={loading}>
                    {loading ? 'Отправка...' : '📤 Заказать справку'}
                  </button>
                </div>
              </>
            )}

            {/* IT REQUEST */}
            {activeModal === 'it' && (
              <>
                <h3 style={{ marginBottom: '20px', fontSize: '1.2rem' }}>🔧 IT-запрос</h3>
                <ItRequestForm
                  employeeId={employeeId}
                  onSuccess={() => { onRequestCreated?.(); setTimeout(closeModal, 2500); }}
                />
              </>
            )}

            {/* EQUIPMENT */}
            {activeModal === 'equip' && (
              <>
                <h3 style={{ marginBottom: '20px', fontSize: '1.2rem' }}>💻 Запрос оборудования</h3>
                <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
                  <div>
                    <div className="section-label">Тип оборудования</div>
                    <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '8px' }}>
                      {[['1','💻 Ноутбук'],['2','🖥 Монитор'],['3','⌨️ Клавиатура'],['4','🖱 Мышь'],['5','🎧 Гарнитура'],['6','📱 Телефон']].map(([v,l]) => (
                        <button key={v} onClick={() => setEquipType(v)}
                          style={{
                            padding: '10px', borderRadius: 'var(--radius-sm)',
                            border: `2px solid ${equipType === v ? 'var(--accent-primary)' : 'var(--border-light)'}`,
                            background: equipType === v ? 'var(--green-50)' : 'var(--bg-surface)',
                            cursor: 'pointer', fontFamily: 'Inter, sans-serif', fontSize: '0.85rem',
                            fontWeight: 600, color: 'var(--text-primary)', transition: 'all 0.2s',
                          }}>{l}</button>
                      ))}
                    </div>
                  </div>
                  {message && <div className={`toast ${message.type}`}>{message.text}</div>}
                  <button className="btn btn-primary btn-full" onClick={requestEquipment} disabled={loading}>
                    {loading ? 'Отправка...' : '📤 Отправить заявку'}
                  </button>
                </div>
              </>
            )}

            {/* HR APPOINTMENT */}
            {activeModal === 'hr' && (
              <>
                <h3 style={{ marginBottom: '16px', fontSize: '1.2rem' }}>🗓️ Запись к HR</h3>
                <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
                  <div className="section-label">Выберите удобное время</div>
                  {slots.length === 0 ? (
                    <div className="flex-center" style={{ padding: '20px' }}><div className="spinner" /></div>
                  ) : (
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '6px', maxHeight: '250px', overflowY: 'auto' }}>
                      {slots.map(slot => (
                        <button key={slot} onClick={() => setSelectedSlot(slot)}
                          style={{
                            padding: '11px 14px', borderRadius: 'var(--radius-sm)', textAlign: 'left',
                            border: `2px solid ${selectedSlot === slot ? 'var(--accent-primary)' : 'var(--border-light)'}`,
                            background: selectedSlot === slot ? 'var(--green-50)' : 'var(--bg-surface)',
                            cursor: 'pointer', fontFamily: 'Inter, sans-serif', fontSize: '0.88rem',
                            fontWeight: selectedSlot === slot ? 600 : 400, color: 'var(--text-primary)', transition: 'all 0.2s',
                          }}>
                          📅 {formatSlot(slot)}
                        </button>
                      ))}
                    </div>
                  )}
                  {message && <div className={`toast ${message.type}`}>{message.text}</div>}
                  <button className="btn btn-primary btn-full" onClick={bookAppointment} disabled={loading || !selectedSlot}>
                    {loading ? 'Бронирование...' : '✓ Записаться'}
                  </button>
                </div>
              </>
            )}

            {/* REGULATIONS */}
            {activeModal === 'reg' && (
              <>
                <h3 style={{ marginBottom: '16px', fontSize: '1.2rem' }}>🔍 Регламенты</h3>
                <div style={{ display: 'flex', gap: '8px', marginBottom: '16px' }}>
                  <input className="input-field" type="text" placeholder="Введите запрос..." value={regQuery}
                    onChange={e => setRegQuery(e.target.value)}
                    onKeyDown={e => e.key === 'Enter' && searchRegulations()}
                    style={{ flex: 1 }} />
                  <button className="btn btn-primary" onClick={searchRegulations} disabled={loading}>🔍</button>
                </div>
                <div style={{ maxHeight: '280px', overflowY: 'auto' }}>
                  {regResults.length === 0 && !loading && regQuery && (
                    <div className="empty-state"><div className="empty-state-icon">📭</div><div className="empty-state-text">Ничего не найдено</div></div>
                  )}
                  {regResults.map(r => (
                    <div key={r.id} className="list-item" style={{ flexDirection: 'column', alignItems: 'flex-start' }}>
                      <div style={{ fontWeight: 600, fontSize: '0.9rem' }}>{r.titleRu}</div>
                      <div className="text-subtitle" style={{ fontSize: '0.8rem' }}>{(r.contentRu || '').substring(0, 80)}...</div>
                    </div>
                  ))}
                </div>
              </>
            )}

            {/* FAQ */}
            {activeModal === 'faq' && (
              <>
                <h3 style={{ marginBottom: '16px', fontSize: '1.2rem' }}>❓ Часто задаваемые вопросы</h3>
                {faqData.length === 0 ? (
                  <div className="flex-center" style={{ padding: '20px' }}><div className="spinner" /></div>
                ) : (
                  <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
                    {faqData.map((item, i) => (
                      <div key={i} style={{ padding: '14px', borderRadius: 'var(--radius-sm)', background: 'var(--green-50)', border: '1px solid var(--green-100)' }}>
                        <div style={{ fontWeight: 600, fontSize: '0.9rem', color: 'var(--green-800)', marginBottom: '6px' }}>Q: {item.question}</div>
                        <div style={{ fontSize: '0.85rem', color: 'var(--text-secondary)', lineHeight: 1.55 }}>A: {item.answer}</div>
                      </div>
                    ))}
                  </div>
                )}
              </>
            )}
          </div>
        </div>
      )}
    </div>
  );
};
