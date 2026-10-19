import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { api, ApiError } from '../../api/client';
import { AttemptStatus, type AssessmentAnalytics, type AttemptResult } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';

const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5200';

function statusLabel(status: AttemptStatus): string {
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

export function ResultsPage() {
  const { id } = useParams();
  const { auth } = useAuth();
  const [results, setResults] = useState<AttemptResult[]>([]);
  const [analytics, setAnalytics] = useState<AssessmentAnalytics | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!auth || !id) return;
    void (async () => {
      try {
        const [r, a] = await Promise.all([
          api.getResults(id, auth.token),
          api.getAnalytics(id, auth.token),
        ]);
        setResults(r);
        setAnalytics(a);
      } catch (err) {
        setError(err instanceof ApiError ? err.message : 'Failed to load results');
      } finally {
        setLoading(false);
      }
    })();
  }, [auth, id]);

  async function exportCsv() {
    if (!auth || !id) return;
    const response = await fetch(`${API_BASE}/api/admin/assessments/${id}/results/export`, {
      headers: { Authorization: `Bearer ${auth.token}` },
    });
    if (!response.ok) return;
    const blob = await response.blob();
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'results.csv';
    a.click();
    URL.revokeObjectURL(url);
  }

  return (
    <div className="page admin-page">
      <Link to={`/admin/assessments/${id}?tab=settings`} className="back-link">
        ← Assessment
      </Link>
      <div className="page-header">
        <h1>Results & analytics</h1>
        <button type="button" className="btn" onClick={() => void exportCsv()}>
          Export CSV
        </button>
      </div>

      {loading && <p>Loading…</p>}
      {error && <p className="error">{error}</p>}

      {analytics && (
        <div className="stats-grid compact">
          <div className="stat-card">
            <span className="stat-label">Attempts</span>
            <strong>{analytics.totalAttempts}</strong>
          </div>
          <div className="stat-card">
            <span className="stat-label">Submitted</span>
            <strong>{analytics.submittedAttempts}</strong>
          </div>
          <div className="stat-card">
            <span className="stat-label">Average score</span>
            <strong>
              {analytics.averageScore.toFixed(1)} / {analytics.maxPossibleScore}
            </strong>
          </div>
        </div>
      )}

      {analytics && analytics.perQuestion.length > 0 && (
        <section className="panel">
          <h2>Per-question analytics</h2>
          <ul>
            {analytics.perQuestion.map((q) => (
              <li key={q.questionId}>
                {q.title}: avg {q.averageScore.toFixed(1)}/{q.maxScore} ({q.attemptsWithFinalSubmission}{' '}
                submissions)
              </li>
            ))}
          </ul>
        </section>
      )}

      <section className="panel">
        <h2>Candidate scores</h2>
        {results.length === 0 ? (
          <p className="muted">No attempts yet.</p>
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Candidate</th>
                  <th>Email</th>
                  <th>Status</th>
                  <th>Score</th>
                  <th>Submitted</th>
                </tr>
              </thead>
              <tbody>
                {results.map((row) => (
                  <tr key={row.attemptId}>
                    <td>{row.fullName}</td>
                    <td>{row.email}</td>
                    <td>{statusLabel(row.status)}</td>
                    <td>
                      {row.totalScore} / {row.maxScore}
                    </td>
                    <td>{row.submittedAtUtc ? new Date(row.submittedAtUtc).toLocaleString() : '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  );
}
