export const SupportedLanguage = {
  Python3: 0,
  Cpp17: 1,
  C: 2,
  JavaScript: 3,
} as const;

export type SupportedLanguage = (typeof SupportedLanguage)[keyof typeof SupportedLanguage];

export const AttemptStatus = {
  Registered: 0,
  InProgress: 1,
  Submitted: 2,
  Expired: 3,
} as const;

export type AttemptStatus = (typeof AttemptStatus)[keyof typeof AttemptStatus];

export const QuestionDifficulty = {
  Easy: 0,
  Medium: 1,
  Hard: 2,
} as const;

export type QuestionDifficulty = (typeof QuestionDifficulty)[keyof typeof QuestionDifficulty];

export interface LoginResponse {
  token: string;
  email: string;
  displayName: string;
  expiresAtUtc: string;
}

export interface AssessmentSummary {
  id: string;
  title: string;
  slug: string;
  isPublished: boolean;
  durationMinutes: number;
  questionCount: number;
  attemptCount: number;
  updatedAtUtc: string;
}

export interface TestCaseAdmin {
  id: string;
  input: string;
  expectedOutput: string;
  isSample: boolean;
  orderIndex: number;
}

export interface QuestionAdmin {
  id: string;
  orderIndex: number;
  title: string;
  problemStatement: string;
  defaultLanguage: SupportedLanguage;
  starterCode: string;
  points: number;
  timeLimitSeconds: number;
  sourceQuestionBankItemId: string | null;
  testCases: TestCaseAdmin[];
}

export interface AssessmentDetail {
  id: string;
  title: string;
  description: string;
  slug: string;
  durationMinutes: number;
  opensAtUtc: string | null;
  closesAtUtc: string | null;
  isPublished: boolean;
  candidateInstructions: string;
  shuffleQuestions: boolean;
  allowCodeRun: boolean;
  invitePath: string;
  questions: QuestionAdmin[];
}

export interface AdminDashboardOverview {
  totalAssessments: number;
  publishedAssessments: number;
  totalCandidates: number;
  activeAttempts: number;
  questionBankCount: number;
  recentAssessments: AssessmentSummary[];
}

export interface QuestionBankSummary {
  id: string;
  slug: string;
  title: string;
  category: string;
  difficulty: QuestionDifficulty;
  tags: string;
  defaultPoints: number;
  testCaseCount: number;
  defaultLanguage: SupportedLanguage;
}

export interface QuestionBankDetail extends Omit<QuestionBankSummary, 'testCaseCount'> {
  problemStatement: string;
  starterCode: string;
  timeLimitSeconds: number;
  testCases: TestCaseAdmin[];
}

export interface LiveAttempt {
  attemptId: string;
  fullName: string;
  email: string;
  status: AttemptStatus;
  startedAtUtc: string | null;
  expiresAtUtc: string | null;
  answeredQuestions: number;
  totalQuestions: number;
  currentScore: number;
  maxScore: number;
}

export interface AssessmentLiveMonitor {
  assessmentId: string;
  title: string;
  inProgressCount: number;
  registeredCount: number;
  submittedCount: number;
  attempts: LiveAttempt[];
}

export interface AssessmentAnalytics {
  assessmentId: string;
  title: string;
  totalAttempts: number;
  submittedAttempts: number;
  averageScore: number;
  maxPossibleScore: number;
  questionCount: number;
  perQuestion: QuestionAnalytics[];
}

export interface QuestionAnalytics {
  questionId: string;
  title: string;
  attemptsWithFinalSubmission: number;
  averageScore: number;
  maxScore: number;
}

export interface AttemptResult {
  attemptId: string;
  fullName: string;
  email: string;
  status: AttemptStatus;
  totalScore: number;
  maxScore: number;
  startedAtUtc: string | null;
  submittedAtUtc: string | null;
  questions: QuestionResult[];
}

export interface QuestionResult {
  questionId: string;
  title: string;
  score: number;
  maxScore: number;
  passedTests: number;
  totalTests: number;
  language: SupportedLanguage | null;
  hasFinalSubmission: boolean;
}

export interface PublicAssessment {
  id: string;
  title: string;
  description: string;
  durationMinutes: number;
  questionCount: number;
  registrationOpen: boolean;
}

export interface RegisterCandidateResponse {
  attemptId: string;
  accessToken: string;
  sessionPath: string;
}

export interface SampleTestCase {
  input: string;
  expectedOutput: string;
}

export interface CandidateQuestionProgress {
  language: SupportedLanguage;
  sourceCode: string;
  isFinal: boolean;
  score: number | null;
}

export interface CandidateQuestion {
  id: string;
  orderIndex: number;
  title: string;
  problemStatement: string;
  defaultLanguage: SupportedLanguage;
  starterCode: string;
  points: number;
  timeLimitSeconds: number;
  sampleTestCases: SampleTestCase[];
  progress: CandidateQuestionProgress | null;
}

export interface CandidateSession {
  attemptId: string;
  assessmentId: string;
  assessmentTitle: string;
  status: AttemptStatus;
  startedAtUtc: string | null;
  expiresAtUtc: string | null;
  durationMinutes: number;
  candidateInstructions: string;
  allowCodeRun: boolean;
  questions: CandidateQuestion[];
}

export interface TestRunResult {
  testCaseId: string | null;
  isSample: boolean;
  passed: boolean;
  input: string;
  expectedOutput: string;
  actualOutput: string;
  stderr: string;
  timeMs: number;
  compileError: string | null;
}

export interface RunCodeResponse {
  results: TestRunResult[];
  passed: number;
  total: number;
}

export interface CandidateSubmitQuestionResponse {
  passedTests: number;
  totalTests: number;
  isFinal: boolean;
  results: TestRunResult[];
}
