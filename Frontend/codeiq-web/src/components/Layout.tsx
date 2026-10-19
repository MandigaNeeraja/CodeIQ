import { Link, NavLink, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

export function AdminLayout() {
  const { auth, logout } = useAuth();

  return (
    <div className="app-shell admin-shell">
      <aside className="admin-sidebar">
        <Link to="/admin" className="brand">
          <span className="brand-mark">{'</>'}</span> CodeIQ
        </Link>
        <nav className="admin-nav">
          <NavLink to="/admin" end>
            Dashboard
          </NavLink>
          <NavLink to="/admin/assessments/new">Create test</NavLink>
          <NavLink to="/admin/question-bank">Question bank</NavLink>
        </nav>
        <div className="sidebar-footer">
          <div className="admin-user-chip">
            <span className="admin-user-avatar">{auth?.displayName?.[0] ?? 'A'}</span>
            <span className="small">{auth?.displayName}</span>
          </div>
          <button type="button" className="btn ghost full-width" onClick={logout}>
            Sign out
          </button>
        </div>
      </aside>
      <div className="admin-main">
        <Outlet />
      </div>
    </div>
  );
}

export function PublicLayout() {
  const location = useLocation();
  const isFullScreenTest = location.pathname.startsWith('/session/');

  return (
    <div className={`app-shell public ${isFullScreenTest ? 'public-test-mode' : ''}`}>
      {!isFullScreenTest && (
        <header className="topbar">
          <span className="brand">
            <span className="brand-mark">{'</>'}</span> CodeIQ Assessments
          </span>
        </header>
      )}
      <main className={isFullScreenTest ? 'main-fullscreen' : 'main-content'}>
        <Outlet />
      </main>
    </div>
  );
}
