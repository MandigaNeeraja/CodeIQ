import { useEffect, useMemo, useState } from 'react';
import { api, ApiError, difficultyLabel, languageLabel } from '../../api/client';
import type { QuestionBankDetail, QuestionBankSummary } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';

export function QuestionBankPage() {
  const { auth } = useAuth();
  const [items, setItems] = useState<QuestionBankSummary[]>([]);
  const [preview, setPreview] = useState<QuestionBankDetail | null>(null);
  const [search, setSearch] = useState('');
  const [category, setCategory] = useState('');
  const [difficulty, setDifficulty] = useState('');
  const [error, setError] = useState<string | null>(null);

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
      }
    })();
  }, [auth, search, category, difficulty]);

  const categories = useMemo(
    () => [...new Set(items.map((x) => x.category))].sort(),
    [items],
  );

  async function openPreview(id: string) {
    if (!auth) return;
    try {
      setPreview(await api.getQuestionBankItem(id, auth.token));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Failed to load question');
    }
  }

  return (
    <div className="page admin-page">
      <h1>Question bank</h1>
      <p className="muted">
        Pre-built coding problems with test cases. Add them to any assessment from the test editor.
      </p>

      <div className="form-row panel filters">
        <input
          placeholder="Search…"
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

      {error && <p className="error">{error}</p>}

      <div className="bank-grid">
        {items.map((item) => (
          <article key={item.id} className="card bank-card">
            <div className="card-title-row">
              <h2>{item.title}</h2>
              <span className="badge">{difficultyLabel(item.difficulty)}</span>
            </div>
            <p className="muted">
              {item.category} · {item.defaultPoints} pts · {item.testCaseCount} tests ·{' '}
              {languageLabel(item.defaultLanguage)}
            </p>
            <p className="small muted">{item.tags}</p>
            <button type="button" className="btn" onClick={() => void openPreview(item.id)}>
              Preview
            </button>
          </article>
        ))}
      </div>

      {preview && (
        <div className="modal-backdrop" onClick={() => setPreview(null)} role="presentation">
          <div className="modal panel wide-modal" onClick={(e) => e.stopPropagation()} role="dialog">
            <h2>{preview.title}</h2>
            <pre className="problem-text">{preview.problemStatement}</pre>
            <h3>Sample tests</h3>
            {preview.testCases
              .filter((t) => t.isSample)
              .map((t, i) => (
                <div key={i} className="sample-box">
                  <div>
                    <strong>Input</strong>
                    <pre>{t.input || '(empty)'}</pre>
                  </div>
                  <div>
                    <strong>Output</strong>
                    <pre>{t.expectedOutput}</pre>
                  </div>
                </div>
              ))}
            <button type="button" className="btn" onClick={() => setPreview(null)}>
              Close
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
