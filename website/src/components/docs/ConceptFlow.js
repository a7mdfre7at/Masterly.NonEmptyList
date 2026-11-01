import React from 'react';
import Link from '@docusaurus/Link';
import styles from './ConceptFlow.module.css';

const stages = [
  {
    step: '1',
    title: 'Create',
    text: 'Build a list that has at least one element, or find out up front that you cannot.',
    apis: ['[1, 2, 3]', 'NonEmptyList.Of(...)', 'NonEmptyList<T>.TryFrom'],
    to: '/docs/creating-lists',
  },
  {
    step: '2',
    title: 'Read',
    text: 'Head and Last never throw. List patterns, ranges and deconstruction work as usual.',
    apis: ['Head  Last', '[var head, .. var rest]', 'xs[1..]'],
    to: '/docs/reading-elements',
  },
  {
    step: '3',
    title: 'Transform',
    text: 'LINQ keeps the type whenever the result cannot be empty, and says so when it can.',
    apis: ['Select  OrderBy  GroupBy', 'Map  FlatMap  Scan', 'Where → IEnumerable<T>'],
    to: '/docs/linq',
  },
  {
    step: '4',
    title: 'Aggregate',
    text: 'No seed, no nullable result and no empty-sequence exception.',
    apis: ['Max()  Min()  Sum()', 'Average()  Median()', 'Reduce(f)'],
    to: '/docs/generic-math',
  },
];

const extras = [
  {api: 'ConstrainedList<T, C>', text: 'cardinality in the type', to: '/docs/constrained-lists'},
  {api: 'Validation<T, E>', text: 'collect every error', to: '/docs/validation'},
  {api: '[NonEmptyCollection]', text: 'generate domain types', to: '/docs/domain-collections'},
  {api: 'BatchNonEmpty(...)', text: 'batch streams', to: '/docs/batching-streams'},
  {api: '.UseNonEmptyList()', text: 'EF Core queries', to: '/docs/entity-framework-core'},
];

// Long member names may wrap only after a dot, never mid-word.
function withBreaksAfterDots(api) {
  return api.split(/(?<=\.)/).map((part, index) => (
    <React.Fragment key={index}>
      {index > 0 && <wbr />}
      {part}
    </React.Fragment>
  ));
}

/**
 * The four stages of working with a non-empty list, plus the building blocks that go further.
 */
export default function ConceptFlow() {
  return (
    <div className={styles.flow}>
      <ol className={styles.stages}>
        {stages.map((stage) => (
          <li key={stage.title} className={styles.stage}>
            <Link to={stage.to} className={styles.stageLink}>
              <span className={styles.step} aria-hidden="true">
                {stage.step}
              </span>
              <span className={styles.title}>{stage.title}</span>
              <span className={styles.text}>{stage.text}</span>
              <span className={styles.apis}>
                {stage.apis.map((api) => (
                  <code key={api}>{withBreaksAfterDots(api)}</code>
                ))}
              </span>
            </Link>
          </li>
        ))}
      </ol>
      <div className={styles.wrappers}>
        <span className={styles.wrappersLabel}>Go further</span>
        <ul className={styles.wrapperList}>
          {extras.map((extra) => (
            <li key={extra.api}>
              <Link to={extra.to} className={styles.wrapper}>
                <code>{extra.api}</code>
                <span>{extra.text}</span>
              </Link>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}
