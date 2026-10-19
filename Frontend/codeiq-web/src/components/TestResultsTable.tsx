import type { TestRunResult } from '../api/types';

type Props = {
  results: TestRunResult[];
  mode: 'sample' | 'submit';
};

export function TestResultsTable({ results, mode }: Props) {
  let sampleCount = 0;
  let hiddenCount = 0;

  return (
    <table className="hr-test-table">
      <thead>
        <tr>
          <th>Test case</th>
          <th>Status</th>
          {mode === 'sample' && <th>Input</th>}
          {mode === 'sample' && <th>Expected</th>}
          <th>{mode === 'sample' ? 'Your output' : 'Result'}</th>
          <th>Time</th>
        </tr>
      </thead>
      <tbody>
        {results.map((r, idx) => {
          const label = r.isSample
            ? `Sample Test ${++sampleCount}`
            : `Hidden Test ${++hiddenCount}`;
          const status = r.compileError
            ? 'Compile Error'
            : r.passed
              ? 'Passed'
              : 'Failed';

          const outputCell =
            r.compileError ??
            (r.isSample
              ? r.actualOutput || r.stderr || '(no output)'
              : r.passed
                ? 'Accepted'
                : r.actualOutput && r.actualOutput !== 'Wrong Answer'
                  ? 'Wrong Answer'
                  : 'Wrong Answer');

          return (
            <tr key={idx} className={r.passed ? 'pass' : 'fail'}>
              <td className="hr-case-name">
                {label}
                {!r.isSample && <span className="hr-hidden-tag">Hidden</span>}
              </td>
              <td>
                <span className={`hr-status-pill ${r.passed ? 'ok' : 'bad'}`}>{status}</span>
              </td>
              {mode === 'sample' && (
                <td>
                  <pre>{r.input || '(empty)'}</pre>
                </td>
              )}
              {mode === 'sample' && (
                <td>
                  <pre>{r.expectedOutput}</pre>
                </td>
              )}
              <td>
                <pre>{outputCell}</pre>
              </td>
              <td>{r.timeMs} ms</td>
            </tr>
          );
        })}
      </tbody>
    </table>
  );
}
