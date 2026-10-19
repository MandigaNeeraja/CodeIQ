import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react';
import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { api, ApiError } from '../../api/client';
import { AttemptStatus, type AssessmentDetail, type AssessmentLiveMonitor } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import { QuestionBankPickerModal } from '../../components/QuestionBankPickerModal';

type Tab = 'settings' | 'questions' | 'live';

function statusLabel(status: (typeof AttemptStatus)[keyof typeof AttemptStatus]): string {
  switch (status) {
    case AttemptStatus.Registered:
      return 'Registered';
    case AttemptStatus.InProgress:
      return 'In progress';
    case AttemptStatus.Submitted:
      return 'Submitted';
    case AttemptStatus.Expired:
      return 'Expired';
    default:
      return 'Unknown';
  }
}

export function AssessmentEditorPage() {
  const { id } = useParams();
  const isNew = id === 'new';
  const { auth } = useAuth();
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const tab = (searchParams.get('tab') as Tab) || 'settings';

  const [loading, setLoading] = useState(!isNew && Boolean(id));
  const [saving, setSaving] = useState(false);
  const [publishing, setPublishing] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [saveNotice, setSaveNotice] = useState<string | null>(null);
  const [detail, setDetail] = useState<AssessmentDetail | null>(null);
  const [showBank, setShowBank] = useState(false);
  const [live, setLive] = useState<AssessmentLiveMonitor | null>(null);

  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [slug, setSlug] = useState('');
  const [durationMinutes, setDurationMinutes] = useState(60);
  const [candidateInstructions, setCandidateInstructions] = useState('');
  const [shuffleQuestions, setShuffleQuestions] = useState(false);
  const [allowCodeRun, setAllowCodeRun] = useState(true);

  const loadDetail = useCallback(async () => {
    if (!auth || !id || isNew) return;
    const data = await api.getAssessment(id, auth.token);
    setDetail(data);
    setTitle(data.title);
    setDescription(data.description);
    setSlug(data.slug);
    setDurationMinutes(data.durationMinutes);
    setCandidateInstructions(data.candidateInstructions);
    setShuffleQuestions(data.shuffleQuestions);
    setAllowCodeRun(data.allowCodeRun);
  }, [auth, id, isNew]);

  useEffect(() => {
    if (isNew || !auth || !id) return;
    void loadDetail()
      .catch((err) => setError(err instanceof ApiError ? err.message : 'Failed to load'))
      .finally(() => setLoading(false));
  }, [auth, id, isNew, loadDetail]);

  useEffect(() => {
    if (tab !== 'live' || !auth || !id || isNew) return;
    const tick = () => {
      void api.getLiveMonitor(id, auth.token).then(setLive).catch(() => undefined);
    };
    tick();
    const handle = window.setInterval(tick, 5000);
    return () => window.clearInterval(handle);
  }, [tab, auth, id, isNew]);

  const settingsDirty = useMemo(() => {
    if (!detail || isNew) return isNew;
    return (
      title !== detail.title ||
      description !== detail.description ||
      slug !== detail.slug ||
      durationMinutes !== detail.durationMinutes ||
      candidateInstructions !== detail.candidateInstructions ||
      shuffleQuestions !== detail.shuffleQuestions ||
      allowCodeRun !== detail.allowCodeRun
    );
  }, [
    detail,
    isNew,
    title,
    description,
    slug,
    durationMinutes,
    candidateInstructions,
    shuffleQuestions,
    allowCodeRun,
  ]);

  const questionCount = detail?.questions.length ?? 0;
  const isPublished = detail?.isPublished ?? false;
  const canPublish = !isNew && !isPublished && !settingsDirty && questionCount > 0;

  function setTab(next: Tab) {
    setSearchParams({ tab: next });
  }

  async function onCreate(e: FormEvent) {
    e.preventDefault();
    if (!auth) return;
    setSaving(true);
    setError(null);
    try {
      const created = await api.createAssessment(
        { title, description, slug: slug || undefined, durationMinutes, candidateInstructions },
        auth.token,
      );
      navigate(`/admin/assessments/${created.id}?tab=questions`);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Create failed');
    } finally {
      setSaving(false);
    }
  }

  async function saveSettings(e?: FormEvent) {
    e?.preventDefault();
    if (!auth || !id || isNew || !detail) return;
    setSaving(true);
    setError(null);
    setSaveNotice(null);
    try {
      const updated = await api.updateAssessment(
        id,
        {
          title,
          description,
          slug,
          durationMinutes,
          opensAtUtc: null,
          closesAtUtc: null,
          isPublished: detail.isPublished,
          candidateInstructions,
          shuffleQuestions,
          allowCodeRun,
        },
        auth.token,
      );
      setDetail(updated);
      if (!updated.isPublished && updated.questions.length > 0) {
        setSaveNotice('Settings saved. You can publish the test when ready.');
      } else {
        setSaveNotice('Settings saved.');
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Save failed');
    } finally {
      setSaving(false);
    }
  }

  async function publishTest() {
    if (!auth || !id || !detail) return;
    if (settingsDirty) {
      setError('Save your changes before publishing.');
      return;
    }
    if (questionCount === 0) {
      setError('Add at least one question before publishing.');
      return;
    }
    setPublishing(true);
    setError(null);
    try {
      const updated = await api.updateAssessment(
        id,
        {
          title: detail.title,
          description: detail.description,
          slug: detail.slug,
          durationMinutes: detail.durationMinutes,
          opensAtUtc: null,
          closesAtUtc: null,
          isPublished: true,
          candidateInstructions: detail.candidateInstructions,
          shuffleQuestions: detail.shuffleQuestions,
          allowCodeRun: detail.allowCodeRun,
        },
        auth.token,
      );
      setDetail(updated);
      setSaveNotice('Test is live — share the invite link with candidates.');
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Publish failed');
    } finally {
      setPublishing(false);
    }
  }

  async function unpublishTest() {
    if (!auth || !id || !detail) return;
    if (!window.confirm('Unpublish this test? New registrations will stop until you publish again.')) return;
    setPublishing(true);
    setError(null);
    try {
      const updated = await api.updateAssessment(
        id,
        {
          title: detail.title,
          description: detail.description,
          slug: detail.slug,
          durationMinutes: detail.durationMinutes,
          opensAtUtc: null,
          closesAtUtc: null,
          isPublished: false,
          candidateInstructions: detail.candidateInstructions,
          shuffleQuestions: detail.shuffleQuestions,
          allowCodeRun: detail.allowCodeRun,
        },
        auth.token,
      );
      setDetail(updated);
      setSaveNotice('Test moved to draft.');
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Unpublish failed');
    } finally {
      setPublishing(false);
    }
  }

  async function deleteTest() {
    if (!auth || !id || isNew) return;
    if (
      !window.confirm(
        'Delete this test permanently? All candidate attempts and results for this test will be removed.',
      )
    ) {
      return;
    }
    setDeleting(true);
    setError(null);
    try {
      await api.deleteAssessment(id, auth.token);
      navigate('/admin');
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Delete failed');
    } finally {
      setDeleting(false);
    }
  }

  const inviteUrl =
    detail && typeof window !== 'undefined'
      ? `${window.location.origin}/test/${detail.slug}`
      : '';

  const bankIds =
    detail?.questions
      .map((q) => q.sourceQuestionBankItemId)
      .filter((x): x is string => x !== null) ?? [];

  if (loading) return <p className="admin-page">Loading…</p>;

  return (
    <div className="page admin-page">
      <div className="page-header">
        <div>
          <Link to="/admin" className="back-link">
            ← Dashboard
          </Link>
          <div className="title-row">
            <h1>{isNew ? 'Create assessment' : title}</h1>
            {!isNew && (
              <span className={`status-pill ${isPublished ? 'live' : 'draft'}`}>
                {isPublished ? 'Live' : 'Draft'}
              </span>
            )}
          </div>
        </div>
        {!isNew && id && (
          <div className="header-actions">
            <Link to={`/admin/assessments/${id}/results`} className="btn">
              Results
            </Link>
          </div>
        )}
      </div>

      {!isNew && (
        <div className="editor-action-bar panel">
          <div className="editor-action-hints">
            {settingsDirty && <span className="dirty-hint">Unsaved changes</span>}
            {saveNotice && !settingsDirty && <span className="success-msg">{saveNotice}</span>}
            {!isPublished && questionCount === 0 && (
              <span className="muted small">Add questions before publishing.</span>
            )}
          </div>
          <div className="editor-action-buttons">
            <button type="button" className="btn danger" onClick={() => void deleteTest()} disabled={deleting}>
              {deleting ? 'Deleting…' : 'Delete test'}
            </button>
            <button
              type="button"
              className="btn primary"
              onClick={() => void saveSettings()}
              disabled={saving || !settingsDirty}
            >
              {saving ? 'Saving…' : 'Save changes'}
            </button>
            {isPublished ? (
              <button type="button" className="btn" onClick={() => void unpublishTest()} disabled={publishing}>
                {publishing ? 'Updating…' : 'Unpublish'}
              </button>
            ) : (
              <button
                type="button"
                className={`btn accent ${canPublish ? 'pulse-once' : ''}`}
                onClick={() => void publishTest()}
                disabled={publishing || !canPublish}
                title={
                  settingsDirty
                    ? 'Save changes first'
                    : questionCount === 0
                      ? 'Add at least one question'
                      : 'Make test available to candidates'
                }
              >
                {publishing ? 'Publishing…' : 'Publish test'}
              </button>
            )}
          </div>
        </div>
      )}

      {!isNew && (
        <div className="tab-bar modern-tabs">
          <button type="button" className={tab === 'settings' ? 'active' : ''} onClick={() => setTab('settings')}>
            Settings
          </button>
          <button type="button" className={tab === 'questions' ? 'active' : ''} onClick={() => setTab('questions')}>
            Questions ({questionCount})
          </button>
          <button type="button" className={tab === 'live' ? 'active' : ''} onClick={() => setTab('live')}>
            Live monitor
          </button>
        </div>
      )}

      {(isNew || tab === 'settings') && (
        <form onSubmit={isNew ? onCreate : (e) => void saveSettings(e)} className="panel stack">
          <h2>Test configuration</h2>
          <p className="muted small">
            {isNew
              ? 'Create the test shell, then add questions and publish.'
              : 'Publish status is controlled separately — save changes, then use Publish test.'}
          </p>
          <label>
            Title
            <input value={title} onChange={(e) => setTitle(e.target.value)} required />
          </label>
          <label>
            Description (shown on invite page)
            <textarea value={description} onChange={(e) => setDescription(e.target.value)} rows={3} />
          </label>
          <label>
            Candidate instructions (shown before & during test)
            <textarea
              value={candidateInstructions}
              onChange={(e) => setCandidateInstructions(e.target.value)}
              rows={4}
              placeholder="Rules, allowed languages, submission guidelines…"
            />
          </label>
          <div className="form-row">
            <label>
              URL slug
              <input value={slug} onChange={(e) => setSlug(e.target.value)} placeholder="auto-generated if empty" />
            </label>
            <label>
              Duration (minutes)
              <input
                type="number"
                min={5}
                max={480}
                value={durationMinutes}
                onChange={(e) => setDurationMinutes(Number(e.target.value))}
              />
            </label>
          </div>
          {!isNew && (
            <div className="settings-toggles panel inset">
              <label className="checkbox-row">
                <input
                  type="checkbox"
                  checked={shuffleQuestions}
                  onChange={(e) => setShuffleQuestions(e.target.checked)}
                />
                Shuffle question order per candidate
              </label>
              <label className="checkbox-row">
                <input type="checkbox" checked={allowCodeRun} onChange={(e) => setAllowCodeRun(e.target.checked)} />
                Allow sample test runs during the attempt
              </label>
            </div>
          )}
          {inviteUrl && isPublished && (
            <div className="invite-box">
              <strong>Candidate invite link</strong>
              <code>{inviteUrl}</code>
            </div>
          )}
          {inviteUrl && !isPublished && (
            <p className="muted small">Invite link appears here after you publish the test.</p>
          )}
          {error && <p className="error">{error}</p>}
          {isNew && (
            <button type="submit" className="btn primary" disabled={saving}>
              {saving ? 'Creating…' : 'Create & add questions'}
            </button>
          )}
        </form>
      )}

      {!isNew && tab === 'questions' && detail && id && (
        <section className="panel">
          <div className="section-header">
            <h2>Questions ({detail.questions.length})</h2>
            <div className="header-actions">
              <button type="button" className="btn" onClick={() => setShowBank(true)}>
                Add from bank
              </button>
              <Link to={`/admin/assessments/${id}/questions/new`} className="btn primary">
                Custom question
              </Link>
            </div>
          </div>
          {detail.questions.length === 0 ? (
            <p className="muted">Add questions from the bank or create a custom question.</p>
          ) : (
            <ul className="question-list">
              {detail.questions.map((q) => (
                <li key={q.id}>
                  <Link to={`/admin/assessments/${id}/questions/${q.id}`}>
                    {q.orderIndex + 1}. {q.title}
                  </Link>
                  <span className="muted">
                    {q.points} pts · {q.testCases.length} tests
                    {q.sourceQuestionBankItemId && ' · from bank'}
                  </span>
                </li>
              ))}
            </ul>
          )}
          {showBank && (
            <QuestionBankPickerModal
              assessmentId={id}
              existingBankIds={bankIds}
              onClose={() => setShowBank(false)}
              onAdded={() => void loadDetail()}
            />
          )}
        </section>
      )}

      {!isNew && tab === 'live' && (
        <section className="panel">
          <h2>Live monitor</h2>
          <p className="muted">Refreshes every 5 seconds while this tab is open.</p>
          {live && (
            <>
              <div className="stats-grid compact">
                <div className="stat-card">
                  <span className="stat-label">In progress</span>
                  <strong>{live.inProgressCount}</strong>
                </div>
                <div className="stat-card">
                  <span className="stat-label">Registered</span>
                  <strong>{live.registeredCount}</strong>
                </div>
                <div className="stat-card">
                  <span className="stat-label">Finished</span>
                  <strong>{live.submittedCount}</strong>
                </div>
              </div>
              <div className="table-wrap">
                <table>
                  <thead>
                    <tr>
                      <th>Candidate</th>
                      <th>Status</th>
                      <th>Progress</th>
                      <th>Score</th>
                      <th>Expires</th>
                    </tr>
                  </thead>
                  <tbody>
                    {live.attempts.map((a) => (
                      <tr key={a.attemptId}>
                        <td>
                          {a.fullName}
                          <div className="muted small">{a.email}</div>
                        </td>
                        <td>{statusLabel(a.status)}</td>
                        <td>
                          {a.answeredQuestions}/{a.totalQuestions} final
                        </td>
                        <td>
                          {a.currentScore}/{a.maxScore}
                        </td>
                        <td>{a.expiresAtUtc ? new Date(a.expiresAtUtc).toLocaleTimeString() : '—'}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </>
          )}
        </section>
      )}
    </div>
  );
}
