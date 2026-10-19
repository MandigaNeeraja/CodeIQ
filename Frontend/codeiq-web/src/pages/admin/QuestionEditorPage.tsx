import { useEffect, useState, type FormEvent } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { api, ApiError, languageLabel } from '../../api/client';
import { SupportedLanguage, type QuestionAdmin, type SupportedLanguage as SupportedLanguageType } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';

const LANGUAGE_OPTIONS = [
  SupportedLanguage.Python3,
  SupportedLanguage.JavaScript,
  SupportedLanguage.Cpp17,
  SupportedLanguage.C,
];

interface TestCaseDraft {
  id?: string;
  input: string;
  expectedOutput: string;
  isSample: boolean;
  orderIndex: number;
}

export function QuestionEditorPage() {
  const { assessmentId, questionId } = useParams();
  const isNew = questionId === 'new';
  const { auth } = useAuth();
  const navigate = useNavigate();

  const [loading, setLoading] = useState(!isNew && Boolean(questionId));
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [title, setTitle] = useState('');
  const [problemStatement, setProblemStatement] = useState('');
  const [defaultLanguage, setDefaultLanguage] = useState<SupportedLanguageType>(SupportedLanguage.Python3);
  const [starterCode, setStarterCode] = useState('print("Hello")');
  const [points, setPoints] = useState(100);
  const [timeLimitSeconds, setTimeLimitSeconds] = useState(5);
  const [orderIndex, setOrderIndex] = useState(0);
  const [testCases, setTestCases] = useState<TestCaseDraft[]>([
    { input: '', expectedOutput: 'Hello\n', isSample: true, orderIndex: 0 },
  ]);

  useEffect(() => {
    if (isNew || !auth || !assessmentId || !questionId) return;
    void (async () => {
      try {
        const assessment = await api.getAssessment(assessmentId, auth.token);
        const q = assessment.questions.find((x) => x.id === questionId);
        if (!q) {
          setError('Question not found');
          return;
        }
        applyQuestion(q);
      } catch (err) {
        setError(err instanceof ApiError ? err.message : 'Failed to load question');
      } finally {
        setLoading(false);
      }
    })();
  }, [assessmentId, auth, isNew, questionId]);

  function applyQuestion(q: QuestionAdmin) {
    setTitle(q.title);
    setProblemStatement(q.problemStatement);
    setDefaultLanguage(q.defaultLanguage);
    setStarterCode(q.starterCode);
    setPoints(q.points);
    setTimeLimitSeconds(q.timeLimitSeconds);
    setOrderIndex(q.orderIndex);
    setTestCases(
      q.testCases.map((tc) => ({
        id: tc.id,
        input: tc.input,
        expectedOutput: tc.expectedOutput,
        isSample: tc.isSample,
        orderIndex: tc.orderIndex,
      })),
    );
  }

  function addTestCase() {
    setTestCases((prev) => [
      ...prev,
      { input: '', expectedOutput: '', isSample: false, orderIndex: prev.length },
    ]);
  }

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    if (!auth || !assessmentId) return;
    setSaving(true);
    setError(null);
    try {
      const payload = {
        title,
        problemStatement,
        defaultLanguage,
        starterCode,
        points,
        timeLimitSeconds,
        orderIndex,
        testCases: testCases.map((tc, idx) => ({
          ...(tc.id ? { id: tc.id } : {}),
          input: tc.input,
          expectedOutput: tc.expectedOutput,
          isSample: tc.isSample,
          orderIndex: idx,
        })),
      };
      const saved = await api.upsertQuestion(
        assessmentId,
        isNew ? null : questionId!,
        payload,
        auth.token,
      );
      if (isNew) {
        navigate(`/admin/assessments/${assessmentId}/questions/${saved.id}`);
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Save failed');
    } finally {
      setSaving(false);
    }
  }

  if (loading) return <p>Loading…</p>;

  return (
    <div className="page">
      <Link to={`/admin/assessments/${assessmentId}`} className="back-link">
        ← Assessment
      </Link>
      <h1>{isNew ? 'New question' : title}</h1>

      <form onSubmit={onSubmit} className="panel stack">
        <label>
          Title
          <input value={title} onChange={(e) => setTitle(e.target.value)} required />
        </label>
        <label>
          Problem statement (Markdown supported in UI)
          <textarea
            value={problemStatement}
            onChange={(e) => setProblemStatement(e.target.value)}
            rows={8}
            required
          />
        </label>
        <div className="form-row">
          <label>
            Language
            <select
              value={defaultLanguage}
              onChange={(e) => setDefaultLanguage(Number(e.target.value) as SupportedLanguageType)}
            >
              {LANGUAGE_OPTIONS.map((v) => (
                  <option key={v} value={v}>
                    {languageLabel(v)}
                  </option>
                ))}
            </select>
          </label>
          <label>
            Points
            <input type="number" min={1} value={points} onChange={(e) => setPoints(Number(e.target.value))} />
          </label>
          <label>
            Time limit (sec)
            <input
              type="number"
              min={1}
              max={30}
              value={timeLimitSeconds}
              onChange={(e) => setTimeLimitSeconds(Number(e.target.value))}
            />
          </label>
          <label>
            Order
            <input type="number" value={orderIndex} onChange={(e) => setOrderIndex(Number(e.target.value))} />
          </label>
        </div>
        <label>
          Starter code
          <textarea value={starterCode} onChange={(e) => setStarterCode(e.target.value)} rows={8} className="code" />
        </label>

        <div>
          <div className="section-header">
            <h2>Test cases</h2>
            <button type="button" className="btn" onClick={addTestCase}>
              Add test case
            </button>
          </div>
          {testCases.map((tc, index) => (
            <div key={index} className="test-case-editor">
              <label className="checkbox-row">
                <input
                  type="checkbox"
                  checked={tc.isSample}
                  onChange={(e) =>
                    setTestCases((prev) =>
                      prev.map((item, i) =>
                        i === index ? { ...item, isSample: e.target.checked } : item,
                      ),
                    )
                  }
                />
                Visible sample to candidate
              </label>
              <label>
                Input (stdin)
                <textarea
                  value={tc.input}
                  onChange={(e) =>
                    setTestCases((prev) =>
                      prev.map((item, i) => (i === index ? { ...item, input: e.target.value } : item)),
                    )
                  }
                  rows={2}
                  className="code"
                />
              </label>
              <label>
                Expected output
                <textarea
                  value={tc.expectedOutput}
                  onChange={(e) =>
                    setTestCases((prev) =>
                      prev.map((item, i) =>
                        i === index ? { ...item, expectedOutput: e.target.value } : item,
                      ),
                    )
                  }
                  rows={2}
                  className="code"
                />
              </label>
            </div>
          ))}
        </div>

        {error && <p className="error">{error}</p>}
        <button type="submit" className="btn primary" disabled={saving}>
          {saving ? 'Saving…' : 'Save question'}
        </button>
      </form>
    </div>
  );
}
