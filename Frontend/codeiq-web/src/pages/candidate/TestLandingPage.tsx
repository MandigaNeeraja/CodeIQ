import { useEffect, useState, type FormEvent } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { api, ApiError } from '../../api/client';
import type { PublicAssessment } from '../../api/types';

export function TestLandingPage() {
  const { slug } = useParams();
  const navigate = useNavigate();
  const [assessment, setAssessment] = useState<PublicAssessment | null>(null);
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (!slug) return;
    void (async () => {
      try {
        setAssessment(await api.getPublicAssessment(slug));
      } catch (err) {
        setError(err instanceof ApiError ? err.message : 'Assessment not found');
      } finally {
        setLoading(false);
      }
    })();
  }, [slug]);

  async function onRegister(e: FormEvent) {
    e.preventDefault();
    if (!slug) return;
    setSubmitting(true);
    setError(null);
    try {
      const response = await api.registerCandidate(slug, fullName, email);
      navigate(response.sessionPath);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Registration failed');
    } finally {
      setSubmitting(false);
    }
  }

  if (loading) return <p>Loading assessment…</p>;
  if (!assessment) {
    return (
      <div className="center-card">
        <h1>Assessment unavailable</h1>
        <p className="error">{error ?? 'Not found or not published.'}</p>
      </div>
    );
  }

  return (
    <div className="center-card wide">
      <h1>{assessment.title}</h1>
      <p>{assessment.description || 'Online coding assessment'}</p>
      <div className="meta-row">
        <span>{assessment.questionCount} questions</span>
        <span>{assessment.durationMinutes} minutes</span>
      </div>

      {!assessment.registrationOpen && (
        <p className="error">Registration is currently closed for this assessment.</p>
      )}

      <form onSubmit={onRegister} className="stack">
        <label>
          Full name
          <input value={fullName} onChange={(e) => setFullName(e.target.value)} required />
        </label>
        <label>
          Email
          <input value={email} onChange={(e) => setEmail(e.target.value)} type="email" required />
        </label>
        {error && <p className="error">{error}</p>}
        <button
          type="submit"
          className="btn primary"
          disabled={!assessment.registrationOpen || submitting}
        >
          {submitting ? 'Registering…' : 'Register & continue'}
        </button>
      </form>

      <p className="muted small">
        Already registered with the same email? You will resume your existing session.
      </p>
      <p className="muted small">
        Admin? <Link to="/admin/login">Sign in here</Link>
      </p>
    </div>
  );
}
