const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5200';

export class ApiError extends Error {
  status: number;

  constructor(message: string, status: number) {
    super(message);
    this.status = status;
  }
}

async function request<T>(
  path: string,
  options: RequestInit = {},
  token?: string | null,
): Promise<T> {
  const headers = new Headers(options.headers);
  if (!headers.has('Content-Type') && options.body) {
    headers.set('Content-Type', 'application/json');
  }
  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  let response: Response;
  try {
    response = await fetch(`${API_BASE}${path}`, { ...options, headers });
  } catch {
    throw new ApiError(
      `Cannot reach API at ${API_BASE}. Is CodeIQ.Api running? (CORS/network)`,
      0,
    );
  }

  if (!response.ok) {
    let message = response.statusText;
    try {
      const body = (await response.json()) as { message?: string };
      if (body.message) message = body.message;
    } catch {
      /* ignore */
    }
    if (response.status === 403) {
      message = 'Access denied. Sign out and sign in again (admin session may be invalid).';
    }
    throw new ApiError(message, response.status);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export const api = {
  login: (email: string, password: string) =>
    request<import('./types').LoginResponse>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    }),

  listAssessments: (token: string) =>
    request<import('./types').AssessmentSummary[]>('/api/admin/assessments', {}, token),

  getAssessment: (id: string, token: string) =>
    request<import('./types').AssessmentDetail>(`/api/admin/assessments/${id}`, {}, token),

  getDashboardOverview: (token: string) =>
    request<import('./types').AdminDashboardOverview>('/api/admin/dashboard/overview', {}, token),

  listQuestionBank: (
    token: string,
    params?: { category?: string; difficulty?: string; search?: string },
  ) => {
    const q = new URLSearchParams();
    if (params?.category) q.set('category', params.category);
    if (params?.difficulty) q.set('difficulty', params.difficulty);
    if (params?.search) q.set('search', params.search);
    const qs = q.toString();
    return request<import('./types').QuestionBankSummary[]>(
      `/api/admin/question-bank${qs ? `?${qs}` : ''}`,
      {},
      token,
    );
  },

  getQuestionBankItem: (id: string, token: string) =>
    request<import('./types').QuestionBankDetail>(`/api/admin/question-bank/${id}`, {}, token),

  addQuestionsFromBank: (assessmentId: string, questionBankItemIds: string[], token: string) =>
    request<{ addedQuestions: import('./types').QuestionAdmin[] }>(
      `/api/admin/assessments/${assessmentId}/questions/from-bank`,
      { method: 'POST', body: JSON.stringify({ questionBankItemIds }) },
      token,
    ),

  getLiveMonitor: (assessmentId: string, token: string) =>
    request<import('./types').AssessmentLiveMonitor>(
      `/api/admin/assessments/${assessmentId}/live`,
      {},
      token,
    ),

  getAnalytics: (assessmentId: string, token: string) =>
    request<import('./types').AssessmentAnalytics>(
      `/api/admin/assessments/${assessmentId}/analytics`,
      {},
      token,
    ),

  exportResultsCsvUrl: (assessmentId: string) =>
    `${API_BASE}/api/admin/assessments/${assessmentId}/results/export`,

  createAssessment: (
    body: {
      title: string;
      description: string;
      slug?: string;
      durationMinutes: number;
      opensAtUtc?: string | null;
      closesAtUtc?: string | null;
      candidateInstructions?: string;
    },
    token: string,
  ) =>
    request<import('./types').AssessmentDetail>('/api/admin/assessments', {
      method: 'POST',
      body: JSON.stringify(body),
    }, token),

  updateAssessment: (
    id: string,
    body: {
      title: string;
      description: string;
      slug: string;
      durationMinutes: number;
      opensAtUtc?: string | null;
      closesAtUtc?: string | null;
      isPublished: boolean;
      candidateInstructions: string;
      shuffleQuestions: boolean;
      allowCodeRun: boolean;
    },
    token: string,
  ) =>
    request<import('./types').AssessmentDetail>(`/api/admin/assessments/${id}`, {
      method: 'PUT',
      body: JSON.stringify(body),
    }, token),

  deleteAssessment: (id: string, token: string) =>
    request<void>(`/api/admin/assessments/${id}`, { method: 'DELETE' }, token),

  upsertQuestion: (
    assessmentId: string,
    questionId: string | null,
    body: unknown,
    token: string,
  ) =>
    request<import('./types').QuestionAdmin>(
      questionId
        ? `/api/admin/assessments/${assessmentId}/questions/${questionId}`
        : `/api/admin/assessments/${assessmentId}/questions`,
      { method: questionId ? 'PUT' : 'POST', body: JSON.stringify(body) },
      token,
    ),

  deleteQuestion: (assessmentId: string, questionId: string, token: string) =>
    request<void>(
      `/api/admin/assessments/${assessmentId}/questions/${questionId}`,
      { method: 'DELETE' },
      token,
    ),

  getResults: (assessmentId: string, token: string) =>
    request<import('./types').AttemptResult[]>(
      `/api/admin/assessments/${assessmentId}/results`,
      {},
      token,
    ),

  getPublicAssessment: (slug: string) =>
    request<import('./types').PublicAssessment>(`/api/public/assessments/${slug}`),

  registerCandidate: (slug: string, fullName: string, email: string) =>
    request<import('./types').RegisterCandidateResponse>(
      `/api/public/assessments/${slug}/register`,
      { method: 'POST', body: JSON.stringify({ fullName, email }) },
    ),

  getSession: (accessToken: string) =>
    request<import('./types').CandidateSession>(`/api/public/sessions/${accessToken}`),

  startSession: (accessToken: string) =>
    request<import('./types').CandidateSession>(
      `/api/public/sessions/${accessToken}/start`,
      { method: 'POST' },
    ),

  runSamples: (accessToken: string, questionId: string, language: number, sourceCode: string) =>
    request<import('./types').RunCodeResponse>(
      `/api/public/sessions/${accessToken}/questions/${questionId}/run`,
      { method: 'POST', body: JSON.stringify({ language, sourceCode }) },
    ),

  submitQuestion: (
    accessToken: string,
    questionId: string,
    language: number,
    sourceCode: string,
    finalize: boolean,
  ) =>
    request<import('./types').CandidateSubmitQuestionResponse>(
      `/api/public/sessions/${accessToken}/questions/${questionId}/submit`,
      { method: 'POST', body: JSON.stringify({ language, sourceCode, finalize }) },
    ),

  finalizeAttempt: (accessToken: string) =>
    request<import('./types').CandidateSession>(
      `/api/public/sessions/${accessToken}/finalize`,
      { method: 'POST' },
    ),
};

export function languageLabel(lang: import('./types').SupportedLanguage): string {
  switch (lang) {
    case 0:
      return 'Python 3';
    case 1:
      return 'C++17';
    case 2:
      return 'C';
    case 3:
      return 'JavaScript';
    default:
      return 'Unknown';
  }
}

export function difficultyLabel(d: import('./types').QuestionDifficulty): string {
  switch (d) {
    case 0:
      return 'Easy';
    case 1:
      return 'Medium';
    case 2:
      return 'Hard';
    default:
      return 'Unknown';
  }
}

export function monacoLanguage(lang: import('./types').SupportedLanguage): string {
  switch (lang) {
    case 0:
      return 'python';
    case 1:
      return 'cpp';
    case 2:
      return 'c';
    case 3:
      return 'javascript';
    default:
      return 'plaintext';
  }
}
