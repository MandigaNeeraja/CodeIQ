import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { AuthProvider } from './auth/AuthContext';
import { AdminLayout, PublicLayout } from './components/Layout';
import { ProtectedRoute } from './components/ProtectedRoute';
import { AdminDashboardPage } from './pages/admin/AdminDashboardPage';
import { AdminLoginPage } from './pages/admin/AdminLoginPage';
import { AssessmentEditorPage } from './pages/admin/AssessmentEditorPage';
import { QuestionEditorPage } from './pages/admin/QuestionEditorPage';
import { QuestionBankPage } from './pages/admin/QuestionBankPage';
import { ResultsPage } from './pages/admin/ResultsPage';
import { CandidateSessionPage } from './pages/candidate/CandidateSessionPage';
import { TestLandingPage } from './pages/candidate/TestLandingPage';

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/" element={<Navigate to="/admin/login" replace />} />

          <Route element={<PublicLayout />}>
            <Route path="/test/:slug" element={<TestLandingPage />} />
            <Route path="/session/:accessToken" element={<CandidateSessionPage />} />
          </Route>

          <Route path="/admin/login" element={<AdminLoginPage />} />

          <Route element={<ProtectedRoute />}>
            <Route element={<AdminLayout />}>
              <Route path="/admin" element={<AdminDashboardPage />} />
              <Route path="/admin/question-bank" element={<QuestionBankPage />} />
              <Route path="/admin/assessments/:id/results" element={<ResultsPage />} />
              <Route
                path="/admin/assessments/:assessmentId/questions/:questionId"
                element={<QuestionEditorPage />}
              />
              <Route path="/admin/assessments/:id" element={<AssessmentEditorPage />} />
            </Route>
          </Route>

          <Route path="*" element={<Navigate to="/admin/login" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}
