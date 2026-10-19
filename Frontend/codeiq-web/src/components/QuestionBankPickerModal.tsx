import { useEffect, useMemo, useState } from 'react';
import { api, ApiError, difficultyLabel } from '../api/client';
import type { QuestionBankSummary } from '../api/types';
import { useAuth } from '../auth/AuthContext';

interface Props {
  assessmentId: string;
  existingBankIds: string[];
  onClose: () => void;
  onAdded: () => void;
}

export function QuestionBankPickerModal({ assessmentId, existingBankIds, onClose, onAdded }: Props) {
  const { auth } = useAuth();
  const [items, setItems] = useState<QuestionBankSummary[]>([]);
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [search, setSearch] = useState('');
  const [category, setCategory] = useState('');
  const [difficulty, setDifficulty] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (!auth) return;
    void (async () => {
      try {
        setItems(
          await api.listQuestionBank(auth.token, {
            search: search || undefined,
            category: category || undefined,
            difficulty: difficulty || undefined,
          }),
        );
      } catch (err) {
        setError(err instanceof ApiError ? err.message : 'Failed to load question bank');
      } finally {
        setLoading(false);
      }
    })();
  }, [auth, search, category, difficulty]);

  const categories = useMemo(
    () => [...new Set(items.map((x) => x.category))].sort(),
    [items],
  );

  function toggle(id: string) {
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  async function addSelected() {
    if (!auth || selected.size === 0) return;
    setSubmitting(true);
    setError(null);
    try {
      await api.addQuestionsFromBank(assessmentId, [...selected], auth.token);
      onAdded();
      onClose();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Failed to add questions');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="modal-backdrop" role="presentation" onClick={onClose}>
      <div className="modal panel" role="dialog" onClick={(e) => e.stopPropagation()}>
        <h2>Add from question bank</h2>
        <div className="form-row">
          <input
            placeholder="Search title, tags…"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
          <select value={category} onChange={(e) => setCategory(e.target.value)}>
            <option value="">All categories</option>
            {categories.map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </select>
          <select value={difficulty} onChange={(e) => setDifficulty(e.target.value)}>
            <option value="">All difficulties</option>
            <option value="Easy">Easy</option>
            <option value="Medium">Medium</option>
            <option value="Hard">Hard</option>
          </select>
        </div>
        {loading && <p>Loading…</p>}
        {error && <p className="error">{error}</p>}
        <ul className="bank-picker-list">
          {items.map((item) => {
            const already = existingBankIds.includes(item.id);
            return (
              <li key={item.id}>
                <label className="checkbox-row">
                  <input
                    type="checkbox"
                    disabled={already}
                    checked={selected.has(item.id)}
                    onChange={() => toggle(item.id)}
                  />
                  <span>
                    <strong>{item.title}</strong>
                    <span className="muted">
                      {' '}
                      · {item.category} · {difficultyLabel(item.difficulty)} · {item.defaultPoints}{' '}
                      pts · {item.testCaseCount} tests
                    </span>
                    {already && <span className="badge muted"> Already in test</span>}
                  </span>
                </label>
              </li>
            );
          })}
        </ul>
        <div className="modal-actions">
          <button type="button" className="btn" onClick={onClose}>
            Cancel
          </button>
          <button
            type="button"
            className="btn primary"
            disabled={submitting || selected.size === 0}
            onClick={() => void addSelected()}
          >
            {submitting ? 'Adding…' : `Add ${selected.size} question(s)`}
          </button>
        </div>
      </div>
    </div>
  );
}
