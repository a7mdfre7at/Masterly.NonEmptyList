import React, {useCallback, useEffect, useRef, useState} from 'react';
import clsx from 'clsx';
import CodeBlock from '@theme/CodeBlock';
import Icon from './Icon';
import styles from './TypeDemo.module.css';

// Return types are the declared ones in NonEmptyListLinqExtensions.cs; the NEL001 text is the analyzer's message.
const code = `NonEmptyList<int> numbers = [3, 1, 2];

var doubled = numbers.Select(n => n * 2);
var sorted  = numbers.OrderBy(n => n);
var large   = numbers.Where(n => n > 10);
var max     = numbers.Max();

NonEmptyList<int> none = [];`;

const finalStep = 5;
const stepDelayMs = 550;

// outcome: the badge shown once the row resolves at step resolveAt.
const rows = [
  {id: 'doubled', name: 'doubled', outcome: 'passed', type: 'NonEmptyList<int>', note: 'still non-empty', resolveAt: 1},
  {id: 'sorted', name: 'sorted', outcome: 'passed', type: 'OrderedNonEmptyList<int>', note: 'ThenBy keeps it', resolveAt: 2},
  {id: 'large', name: 'large', outcome: 'skipped', type: 'IEnumerable<int>', note: 'may be empty, so not promoted', resolveAt: 3},
  {id: 'max', name: 'max', outcome: 'passed', type: 'int', note: 'cannot throw', resolveAt: 4},
  {
    id: 'none',
    name: 'NEL001',
    outcome: 'broken',
    type: 'error',
    note: "An empty collection expression can never produce a 'NonEmptyList<int>'",
    resolveAt: 5,
  },
];

const outcomeLabels = {
  pending: 'PENDING',
  running: 'CHECKING',
  passed: 'NON-EMPTY',
  skipped: 'MAYBE EMPTY',
  broken: 'ERROR',
};

function stateOf(row, step) {
  if (step >= row.resolveAt) {
    return row.outcome;
  }
  if (step === row.resolveAt - 1) {
    return 'running';
  }
  return 'pending';
}

function prefersReducedMotion() {
  return typeof window !== 'undefined' && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
}

/**
 * A code window that "builds" a few lines and reveals the type the compiler infers for each one,
 * ending with the analyzer error for an empty collection expression.
 * Every row is rendered from the start, so the animation never shifts the layout.
 */
export default function TypeDemo() {
  const [step, setStep] = useState(0);
  const [running, setRunning] = useState(false);
  const container = useRef(null);
  const started = useRef(false);

  const run = useCallback(() => {
    if (prefersReducedMotion()) {
      setStep(finalStep);
      return;
    }
    setStep(0);
    setRunning(true);
  }, []);

  useEffect(() => {
    if (!running) {
      return undefined;
    }
    if (step >= finalStep) {
      setRunning(false);
      return undefined;
    }
    const timer = setTimeout(() => setStep((current) => current + 1), stepDelayMs);
    return () => clearTimeout(timer);
  }, [running, step]);

  useEffect(() => {
    const element = container.current;
    if (!element || typeof IntersectionObserver === 'undefined') {
      run();
      return undefined;
    }
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries.some((entry) => entry.isIntersecting) && !started.current) {
          started.current = true;
          run();
          observer.disconnect();
        }
      },
      {threshold: 0.4},
    );
    observer.observe(element);
    return () => observer.disconnect();
  }, [run]);

  const done = step >= finalStep;

  return (
    <div className={styles.window} ref={container}>
      <div className={styles.titleBar}>
        <span className={styles.dots} aria-hidden="true">
          <span />
          <span />
          <span />
        </span>
        <span className={styles.fileName}>Numbers.cs</span>
        <button type="button" className={styles.runButton} onClick={run} disabled={running}>
          <Icon name={done ? 'replay' : 'play'} size={14} />
          <span>{done ? 'Build again' : running ? 'Building' : 'Build'}</span>
        </button>
      </div>

      <CodeBlock language="csharp" className={styles.code}>
        {code}
      </CodeBlock>

      <div className={styles.console}>
        <div className={styles.consoleHeader}>
          <span>Inferred types</span>
          <span className={styles.status} role="status" aria-live="polite">
            {done ? (
              <>
                <span className={styles.statusBroken}>1 error</span> · 3 non-empty · 1 maybe empty
              </>
            ) : (
              'Building…'
            )}
          </span>
        </div>
        <ol className={styles.tree} aria-label="Inferred type of each line">
          {rows.map((row) => {
            const state = stateOf(row, step);
            const resolved = step >= row.resolveAt;
            return (
              <li key={row.id} className={styles.row}>
                <span className={clsx(styles.badge, styles[state])}>{outcomeLabels[state]}</span>
                <span className={styles.ruleCode}>{row.name}</span>
                {resolved && <span className={styles.operator}>{row.type}</span>}
                {resolved && <span className={styles.message}>{row.note}</span>}
              </li>
            );
          })}
        </ol>
      </div>
    </div>
  );
}
