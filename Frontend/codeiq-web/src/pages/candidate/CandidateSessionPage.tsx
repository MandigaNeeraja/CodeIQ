import Editor from '@monaco-editor/react';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { api, ApiError, languageLabel, monacoLanguage } from '../../api/client';
import { TestResultsTable } from '../../components/TestResultsTable';
import { parseUtcMs } from '../../utils/utc';
import {
  AttemptStatus,
  SupportedLanguage,
  type SupportedLanguage as SupportedLanguageType,
  type CandidateQuestion,
  type CandidateSession,
  type RunCodeResponse,
  type CandidateSubmitQuestionResponse,
} from '../../api/types';

const LANGUAGE_OPTIONS = [
  SupportedLanguage.Python3,
  SupportedLanguage.JavaScript,
  SupportedLanguage.Cpp17,
  SupportedLanguage.C,
];

type ConsoleTab = 'tests' | 'output';

export function CandidateSessionPage() {
  const { accessToken } = useParams();
  const navigate = useNavigate();
  const [session, setSession] = useState<CandidateSession | null>(null);
  const [activeQuestionId, setActiveQuestionId] = useState<string | null>(null);
  const [language, setLanguage] = useState<SupportedLanguageType>(SupportedLanguage.Python3);
  const [sourceCode, setSourceCode] = useState('');
  const [runResult, setRunResult] = useState<RunCodeResponse | null>(null);
  const [submitResult, setSubmitResult] = useState<CandidateSubmitQuestionResponse | null>(null);
  const [consoleTab, setConsoleTab] = useState<ConsoleTab>('tests');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState<string | null>(null);
  const [now, setNow] = useState(Date.now());

  const loadSession = useCallback(async () => {
    if (!accessToken) return null;
    const data = await api.getSession(accessToken);
    setSession(data);
    return data;
  }, [accessToken]);

  useEffect(() => {
    if (!accessToken) return;
    void loadSession().catch((err) =>
      setError(err instanceof ApiError ? err.message : 'Failed to load session'),
    );
  }, [accessToken, loadSession]);

  useEffect(() => {
    if (!session?.questions.length) return;
    setActiveQuestionId((prev) => {
      if (prev && session.questions.some((q) => q.id === prev)) return prev;
      return session.questions[0].id;
    });
  }, [session?.questions]);

  const activeQuestion: CandidateQuestion | undefined = useMemo(
    () => session?.questions.find((q) => q.id === activeQuestionId),
    [session, activeQuestionId],
  );

  useEffect(() => {
    if (!activeQuestion) return;
    setLanguage(activeQuestion.progress?.language ?? activeQuestion.defaultLanguage);
    setSourceCode(activeQuestion.progress?.sourceCode ?? activeQuestion.starterCode);
    setRunResult(null);
    setSubmitResult(null);
  }, [activeQuestion?.id]);

  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), 1000);
    return () => window.clearInterval(timer);
  }, []);

  function selectQuestion(q: CandidateQuestion) {
    setActiveQuestionId(q.id);
  }

  const remainingSeconds = useMemo(() => {
    const expiresMs = parseUtcMs(session?.expiresAtUtc);
    if (expiresMs === null) return null;
    return Math.max(0, Math.floor((expiresMs - now) / 1000));
  }, [session?.expiresAtUtc, now]);

  const timeExpired = remainingSeconds !== null && remainingSeconds <= 0;
  const timerClass =
    timeExpired || (remainingSeconds !== null && remainingSeconds < 300)
      ? 'hr-timer danger'
      : 'hr-timer';

  useEffect(() => {
    if (!timeExpired || session?.status !== AttemptStatus.InProgress) return;
    void loadSession();
  }, [timeExpired, session?.status, loadSession]);

  async function startTest() {
    if (!accessToken) return;
    setBusy('start');
    setError(null);
    try {
      setSession(await api.startSession(accessToken));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Could not start test');
    } finally {
      setBusy(null);
    }
  }

  async function runSamples() {
    if (!accessToken || !activeQuestion) return;
    if (timeExpired) {
      setError('Time is up. You cannot run code on an expired attempt.');
      return;
    }
    setBusy('run');
    setError(null);
    setConsoleTab('tests');
    try {
      setSubmitResult(null);
      setRunResult(await api.runSamples(accessToken, activeQuestion.id, language, sourceCode));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Run failed');
    } finally {
      setBusy(null);
    }
  }

  async function submit(finalize: boolean) {
    if (!accessToken || !activeQuestion) return;
    setBusy(finalize ? 'submit-final' : 'submit');
    setError(null);
    setConsoleTab('tests');
    try {
      const result = await api.submitQuestion(
        accessToken,
        activeQuestion.id,
        language,
        sourceCode,
        finalize,
      );
      setRunResult(null);
      setSubmitResult(result);
      await loadSession();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Submit failed');
    } finally {
      setBusy(null);
    }
  }

  async function finalizeAttempt() {
    if (!accessToken) return;
    if (!window.confirm('Submit the entire test? You cannot make changes after this.')) return;
    setBusy('finalize');
    try {
      setSession(await api.finalizeAttempt(accessToken));
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Finalize failed');
    } finally {
      setBusy(null);
    }
  }

  if (!session) {
    return (
      <div className="hr-loading">
        <div className="hr-spinner" />
        <p>Loading assessment…</p>
      </div>
    );
  }

  if (session.status === AttemptStatus.Registered) {
    return (
      <div className="hr-prestart">
        <div className="hr-prestart-card">
          <h1>{session.assessmentTitle}</h1>
          <p className="muted">Duration: {session.durationMinutes} minutes · {session.questions.length} question(s)</p>
          {session.candidateInstructions && (
            <div className="instructions-box">
              <strong>Instructions</strong>
              <pre>{session.candidateInstructions}</pre>
            </div>
          )}
          {session.questions.length === 0 && (
            <p className="error">This test has no questions yet. Please contact the administrator.</p>
          )}
          {error && <p className="error">{error}</p>}
          <button
            type="button"
            className="btn hr-btn-submit"
            onClick={() => void startTest()}
            disabled={busy === 'start' || session.questions.length === 0}
          >
            {busy === 'start' ? 'Starting…' : 'Start test'}
          </button>
        </div>
      </div>
    );
  }

  if (session.status === AttemptStatus.Submitted || session.status === AttemptStatus.Expired) {
    return (
      <div className="hr-prestart">
        <div className="hr-prestart-card">
          <h1>Test complete</h1>
          <p>{session.status === AttemptStatus.Expired ? 'Time limit reached.' : 'Your answers were submitted.'}</p>
          <p className="muted">
            Your responses have been recorded. Final scores are shared by the assessment administrator after review.
          </p>
          {session.status === AttemptStatus.Expired && (
            <p className="muted">
              To try again, open the original test invite link and register with the same email — your timer will reset.
            </p>
          )}
          <button type="button" className="btn primary" onClick={() => navigate('/')}>
            Done
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="hr-test">
      <header className="hr-topbar">
        <div className="hr-topbar-left">
          <span className="hr-logo">CodeIQ</span>
          <span className="hr-test-title">{session.assessmentTitle}</span>
        </div>
        <div className={timerClass}>
          {remainingSeconds !== null
            ? `${String(Math.floor(remainingSeconds / 60)).padStart(2, '0')}:${String(remainingSeconds % 60).padStart(2, '0')}`
            : '--:--'}
        </div>
        <button
          type="button"
          className="btn hr-btn-end"
          onClick={() => void finalizeAttempt()}
          disabled={busy === 'finalize'}
        >
          End test
        </button>
      </header>

      <nav className="hr-q-tabs">
        {session.questions.length === 0 ? (
          <span className="muted">No questions available</span>
        ) : (
          session.questions.map((q, idx) => (
            <button
              key={q.id}
              type="button"
              className={`hr-q-tab ${q.id === activeQuestionId ? 'active' : ''} ${q.progress?.isFinal ? 'done' : ''}`}
              onClick={() => selectQuestion(q)}
            >
              {idx + 1}. {q.title}
              {q.progress?.isFinal && ' ✓'}
            </button>
          ))
        )}
      </nav>

      {timeExpired && session.status === AttemptStatus.InProgress && (
        <div className="hr-time-banner">
          Time is up — syncing attempt status. Run / Submit are disabled.
        </div>
      )}

      {activeQuestion ? (
        <div className="hr-workspace">
          <section className="hr-description">
            <div className="hr-desc-header">
              <h2>{activeQuestion.title}</h2>
              <span className="hr-points">{activeQuestion.points} pts</span>
            </div>
            <div className="hr-desc-body">
              <pre className="problem-text hr-problem-doc">{activeQuestion.problemStatement}</pre>
              {activeQuestion.sampleTestCases.length > 0 && (
                <div className="hr-samples">
                  <h3>Sample I/O (also shown above)</h3>
                  {activeQuestion.sampleTestCases.map((tc, i) => (
                    <div key={i} className="hr-sample-card">
                      <div>
                        <span className="hr-sample-label">Input</span>
                        <pre>{tc.input || '(empty)'}</pre>
                      </div>
                      <div>
                        <span className="hr-sample-label">Output</span>
                        <pre>{tc.expectedOutput}</pre>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </section>

          <section className="hr-code-pane">
            <div className="hr-editor-toolbar">
              <label className="hr-lang-select">
                Language
                <select
                  value={language}
                  onChange={(e) => setLanguage(Number(e.target.value) as SupportedLanguageType)}
                >
                  {LANGUAGE_OPTIONS.map((lang) => (
                    <option key={lang} value={lang}>
                      {languageLabel(lang)}
                    </option>
                  ))}
                </select>
              </label>
              <div className="hr-toolbar-actions">
                <button
                  type="button"
                  className="btn hr-btn-save"
                  onClick={() => {
                    setSourceCode(activeQuestion.starterCode);
                    setRunResult(null);
                    setSubmitResult(null);
                  }}
                  title="Restore the original template for this question"
                >
                  Reset template
                </button>
                {session.allowCodeRun && (
                  <button
                    type="button"
                    className="btn hr-btn-run"
                    onClick={() => void runSamples()}
                    disabled={busy === 'run' || timeExpired}
                  >
                    {busy === 'run' ? 'Running…' : 'Run tests'}
                  </button>
                )}
                <button
                  type="button"
                  className="btn hr-btn-save"
                  onClick={() => void submit(false)}
                  disabled={busy === 'submit' || timeExpired}
                >
                  Save draft
                </button>
                <button
                  type="button"
                  className="btn hr-btn-submit"
                  onClick={() => void submit(true)}
                  disabled={busy === 'submit-final' || timeExpired}
                >
                  Submit code
                </button>
              </div>
            </div>

            <div className="hr-editor-wrap">
              <Editor
                height="100%"
                language={monacoLanguage(language)}
                value={sourceCode}
                onChange={(v) => setSourceCode(v ?? '')}
                theme="vs-dark"
                options={{
                  minimap: { enabled: false },
                  fontSize: 14,
                  lineNumbers: 'on',
                  scrollBeyondLastLine: false,
                  automaticLayout: true,
                  padding: { top: 12 },
                }}
              />
            </div>

            <div className="hr-console">
              <div className="hr-console-tabs">
                <button
                  type="button"
                  className={consoleTab === 'tests' ? 'active' : ''}
                  onClick={() => setConsoleTab('tests')}
                >
                  Test results
                </button>
                <button
                  type="button"
                  className={consoleTab === 'output' ? 'active' : ''}
                  onClick={() => setConsoleTab('output')}
                >
                  Console
                </button>
              </div>
              <div className="hr-console-body">
                {error && <p className="error">{error}</p>}
                {consoleTab === 'tests' && (
                  <>
                    {!runResult && !submitResult && (
                      <p className="muted">
                        Run tests checks sample cases only. Submit code runs all sample and hidden tests. You will
                        not see your final score here.
                      </p>
                    )}
                    {runResult && (
                      <div className="hr-result-block">
                        <p className="hr-result-summary">
                          Sample tests: <strong>{runResult.passed}/{runResult.total}</strong> passed
                        </p>
                        <TestResultsTable results={runResult.results} mode="sample" />
                      </div>
                    )}
                    {submitResult && (
                      <div className="hr-result-block">
                        <p className={`hr-result-summary ${submitResult.isFinal ? 'success-msg' : ''}`}>
                          {submitResult.isFinal ? 'Submitted' : 'Draft saved'} —{' '}
                          {submitResult.passedTests}/{submitResult.totalTests} test cases passed
                        </p>
                        {submitResult.results?.length > 0 && (
                          <TestResultsTable results={submitResult.results} mode="submit" />
                        )}
                      </div>
                    )}
                  </>
                )}
                {consoleTab === 'output' && (
                  <pre className="hr-console-log">
                    {runResult?.results.some((r) => r.stderr)
                      ? runResult.results.map((r) => r.stderr).filter(Boolean).join('\n')
                      : 'No stderr output.'}
                  </pre>
                )}
              </div>
            </div>
          </section>
        </div>
      ) : (
        <div className="hr-empty-workspace">
          <p>Select a question from the tabs above.</p>
          {session.questions.length === 0 && (
            <p className="error">This assessment has no questions. Contact your administrator.</p>
          )}
        </div>
      )}
    </div>
  );
}
