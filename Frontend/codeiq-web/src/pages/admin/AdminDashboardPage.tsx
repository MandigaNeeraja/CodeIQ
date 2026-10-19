import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api, ApiError } from '../../api/client';
import type { AdminDashboardOverview, AssessmentSummary } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';

export function AdminDashboardPage() {
  const { auth } = useAuth();
  const [overview, setOverview] = useState<AdminDashboardOverview | null>(null);
  const [assessments, setAssessments] = useState<AssessmentSummary[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!auth) return;
    void (async () => {
      try {
        const [ov, list] = await Promise.all([
          api.getDashboardOverview(auth.token),
          api.listAssessments(auth.token),
        ]);
        setOverview(ov);
        setAssessments(list);
      } catch (err) {
        setError(err instanceof ApiError ? err.message : 'Failed to load dashboard');
      }
    })();
  }, [auth]);

  return (
    <div className="page admin-page">
      <div className="page-header">
        <div>
          <h1>Dashboard</h1>
          <p className="muted">Create tests, manage the question bank, and monitor candidates.</p>
        </div>
        <Link to="/admin/assessments/new" className="btn primary">
          New assessment
        </Link>
      </div>

      {error && <p className="error">{error}</p>}

      {overview && (
        <div className="stats-grid">
          <div className="stat-card">
            <span className="stat-label">Assessments</span>
            <strong className="stat-value">{overview.totalAssessments}</strong>
            <span className="muted small">{overview.publishedAssessments} published</span>
          </div>
          <div className="stat-card">
            <span className="stat-label">Candidates</span>
            <strong className="stat-value">{overview.totalCandidates}</strong>
            <span className="muted small">{overview.activeAttempts} in progress now</span>
          </div>
          <div className="stat-card">
            <span className="stat-label">Question bank</span>
            <strong className="stat-value">{overview.questionBankCount}</strong>
            <Link to="/admin/question-bank" className="small">
              Browse library
            </Link>
          </div>
        </div>
      )}

      <section className="panel">
        <h2>All assessments</h2>
        {assessments.length === 0 ? (
          <p className="muted">No assessments yet. Create one or add questions from the bank.</p>
        ) : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Title</th>
                  <th>Status</th>
                  <th>Questions</th>
                  <th>Attempts</th>
                  <th>Duration</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {assessments.map((item) => (
                  <tr key={item.id}>
                    <td>
                      <Link to={`/admin/assessments/${item.id}`}>{item.title}</Link>
                      <div className="muted small">/{item.slug}</div>
                    </td>
                    <td>
                      <span className={`badge ${item.isPublished ? 'success' : 'muted'}`}>
                        {item.isPublished ? 'Published' : 'Draft'}
                      </span>
                    </td>
                    <td>{item.questionCount}</td>
                    <td>{item.attemptCount}</td>
                    <td>{item.durationMinutes} min</td>
                    <td className="row-actions">
                      <Link to={`/admin/assessments/${item.id}?tab=live`} className="btn small-btn">
                        Monitor
                      </Link>
                      <Link to={`/admin/assessments/${item.id}`} className="btn small-btn">
                        Edit
                      </Link>
                    </td>
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
