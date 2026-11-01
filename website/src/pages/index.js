import React from 'react';
import clsx from 'clsx';
import Link from '@docusaurus/Link';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import Layout from '@theme/Layout';
import Heading from '@theme/Heading';
import CodeBlock from '@theme/CodeBlock';
import Tabs from '@theme/Tabs';
import TabItem from '@theme/TabItem';
import Icon from '@site/src/components/home/Icon';
import CopyCommand from '@site/src/components/home/CopyCommand';
import TypeDemo from '@site/src/components/home/TypeDemo';
import styles from './index.module.css';

// Every sample below uses the public API exactly as declared in src/.
const showcases = [
  {
    value: 'syntax',
    label: 'Collection expressions',
    caption: 'Create lists with [..], match them with list patterns and slice them with ranges. Head never throws.',
    to: '/docs/creating-lists',
    code: `NonEmptyList<int> numbers = [3, 1, 2];
var names = NonEmptyList.Of("Ada", "Grace", "Linus");

int first = numbers.Head;
int last  = numbers.Last;

if (numbers is [var head, .. var rest])
{
    Console.WriteLine($"{head} then {rest.Count} more");
}

List<int> tail = numbers[1..];`,
  },
  {
    value: 'linq',
    label: 'LINQ',
    caption: 'Operators that cannot produce an empty result return a NonEmptyList. The rest fall back to IEnumerable.',
    to: '/docs/linq',
    code: `NonEmptyList<string> sorted =
    from n in names
    orderby n.Length, n
    select n.ToUpperInvariant();

NonEmptyList<int> doubled = numbers.Select(n => n * 2);  // not IEnumerable<int>
IEnumerable<int> big = numbers.Where(n => n > 10);       // may be empty, so not promoted`,
  },
  {
    value: 'math',
    label: 'Generic math',
    caption: 'Aggregations with no empty-sequence path, for every type that implements the System.Numerics interfaces.',
    to: '/docs/generic-math',
    code: `int    max     = numbers.Max();      // int, not int?
double average = numbers.Average();
int    sum     = numbers.Sum();      // no seed needed
double median  = numbers.Median();

Money total = invoice.Lines.Select(l => l.Amount).Sum();  // any type with a + operator`,
  },
  {
    value: 'constraints',
    label: 'Constraints',
    caption: 'Put cardinality, uniqueness and ordering in the type. Members such as Second() exist only when proven.',
    to: '/docs/constrained-lists',
    code: `ConstrainedList<Email, And<AtLeast<Two>, Distinct>> recipients = [a, b, c];
ConstrainedList<Candidate, AtLeast<Two>> finalists = ranked.Constrain<Candidate, AtLeast<Two>>();

Candidate winner   = finalists.Head;
Candidate runnerUp = finalists.Second();   // compiles only for AtLeast<Two> and up`,
  },
  {
    value: 'validation',
    label: 'Validation',
    caption: 'Collect every error instead of stopping at the first. An invalid result always has at least one error.',
    to: '/docs/validation',
    code: `Validation<User, ValidationError> user = Validation.Combine(
    Validation.NotBlank(input.Name,  new ValidationError("required", "Name is required", "name")),
    Validation.NotBlank(input.Email, new ValidationError("required", "Email is required", "email")),
    Validation.Ensure(input.Age, a => a >= 18, new ValidationError("range", "Must be an adult", "age")),
    (name, email, age) => new User(name, email, age));

return user.ToResult();   // 200 with the user, or 400 validation problem details`,
  },
];

const features = [
  {
    icon: 'shuffle',
    title: 'Type-preserving LINQ',
    text: 'Select, OrderBy, GroupBy, Distinct, Concat and Chunk keep the guarantee. Where, Skip and Take return IEnumerable, so the type never lies.',
    to: '/docs/linq',
    size: 'wide',
    visual: 'linq',
  },
  {
    icon: 'lock',
    title: 'A guarantee you cannot break',
    text: 'The list wraps its storage. Emptying it through IList<T>, ICollection<T> or IList throws instead of succeeding.',
    to: '/docs/safe-mutation',
  },
  {
    icon: 'sigma',
    title: 'Generic math',
    text: 'Sum, Product, Average, Median and Mode for any System.Numerics type, including your own.',
    to: '/docs/generic-math',
  },
  {
    icon: 'checks',
    title: 'Errors that accumulate',
    text: 'Validation<T, TError> combines up to eight checks and reports every failure at once.',
    to: '/docs/validation',
  },
  {
    icon: 'layers',
    title: 'Sets, dictionaries, strings',
    text: 'NonEmptySet, NonEmptyDictionary, NonEmptyString, immutable lists, spans and lazy sequences.',
    to: '/docs/sets-dictionaries-strings',
  },
  {
    icon: 'ruler',
    title: 'Constraints in the type',
    text: 'AtLeast, AtMost, Exactly, Distinct and Sorted, composed with And and Or, checked once at construction.',
    to: '/docs/constrained-lists',
    size: 'wide',
    visual: 'constraints',
  },
  {
    icon: 'tree',
    title: 'Modeling types',
    text: 'A zipper that always has a focus, undo history, event streams with an origin, and rose trees.',
    to: '/docs/modeling-types',
  },
  {
    icon: 'activity',
    title: 'Non-empty batching',
    text: 'Batch streams and channels into NonEmptyList batches, by size or by time. No more empty-batch checks.',
    to: '/docs/batching-streams',
  },
  {
    icon: 'scan',
    title: 'Analyzer and generator',
    text: 'Catch [] and provably empty input at build time, and generate domain collections from one attribute.',
    to: '/docs/analyzers',
  },
  {
    icon: 'plug',
    title: 'Works with your stack',
    text: 'Query-string binding, OpenAPI schemas, options validation, EF Core, Json.NET, MessagePack and Dapper.',
    to: '/docs/aspnetcore',
  },
];

const packages = [
  {name: 'Masterly.NonEmptyList', role: 'Core', text: 'The collection types, LINQ, generic math, Validation and constraints, plus the bundled analyzer and source generator.', target: 'net8.0 · net9.0 · net10.0', deps: 'No dependencies', to: '/docs/overview'},
  {name: 'Masterly.NonEmptyList.AspNetCore', role: 'ASP.NET Core', text: 'OpenAPI minItems and minLength for every non-empty type (.NET 9+), and Validation to problem details.', target: 'net8.0 · net9.0 · net10.0', deps: 'AspNetCore.OpenApi', to: '/docs/aspnetcore'},
  {name: 'Masterly.NonEmptyList.Configuration', role: 'Configuration', text: 'Options binding that refuses to start the host when a required collection is empty.', target: 'net8.0 · net9.0 · net10.0', deps: 'Options.ConfigurationExtensions', to: '/docs/configuration'},
  {name: 'Masterly.NonEmptyList.EntityFrameworkCore', role: 'EF Core', text: 'JSON column converters, non-empty relationships, SaveChanges validation and query translation.', target: 'net8.0 · net9.0 · net10.0', deps: 'Entity Framework Core', to: '/docs/entity-framework-core'},
  {name: 'Masterly.NonEmptyList.NewtonsoftJson', role: 'Json.NET', text: 'One converter for every non-empty type. Empty arrays are rejected on read.', target: 'net8.0 · net9.0 · net10.0', deps: 'Newtonsoft.Json', to: '/docs/serialization'},
  {name: 'Masterly.NonEmptyList.MessagePack', role: 'MessagePack', text: 'Formatters and a resolver for every non-empty type. Empty payloads are rejected.', target: 'net8.0 · net9.0 · net10.0', deps: 'MessagePack', to: '/docs/serialization'},
  {name: 'Masterly.NonEmptyList.Dapper', role: 'Dapper', text: 'Type handlers that store non-empty collections as JSON text columns.', target: 'net8.0 · net9.0 · net10.0', deps: 'Dapper', to: '/docs/serialization'},
  {name: 'Masterly.NonEmptyList.Testing', role: 'Testing', text: 'CsCheck generators, an AutoFixture customization and the published law suite.', target: 'net8.0 · net9.0 · net10.0', deps: 'CsCheck, AutoFixture', to: '/docs/testing'},
];

// Return types as declared in NonEmptyListLinqExtensions.cs; Where comes from System.Linq.
const linqRows = [
  {call: 'Select(f)', type: 'NonEmptyList<R>', kept: true},
  {call: 'OrderBy(k)', type: 'OrderedNonEmptyList<T>', kept: true},
  {call: 'GroupBy(k)', type: 'NonEmptyList<NonEmptyGrouping<K, T>>', kept: true},
  {call: 'Max()', type: 'T', kept: true},
  {call: 'Where(p)', type: 'IEnumerable<T>', kept: false},
];

// Second() and Third() are extension methods constrained on the cardinality (ConstrainedListExtensions).
const constraintRows = [
  {constraint: 'NonEmpty', unlocks: 'Head, Last, Max()'},
  {constraint: 'AtLeast<Two>', unlocks: '+ Second()'},
  {constraint: 'AtLeast<Three>', unlocks: '+ Third()'},
  {constraint: 'And<AtLeast<Two>, Distinct>', unlocks: 'Second(), no duplicates', highlight: true},
];

function Hero() {
  const {siteConfig} = useDocusaurusContext();
  const version = siteConfig.customFields.packageVersion;
  return (
    <header className={styles.hero}>
      <div className={styles.heroBackdrop} aria-hidden="true" />
      <div className={clsx('container', styles.heroGrid)}>
        <div className={styles.heroCopy}>
          <Link className={styles.eyebrow} to="/docs/upgrading-from-2x">
            <span className={styles.eyebrowVersion}>v{version}</span>
            <span>Open source · MIT · .NET 8, 9 and 10</span>
            <Icon name="arrowRight" size={14} />
          </Link>
          <Heading as="h1" className={styles.heroTitle}>
            Collections that are <span className={styles.heroAccent}>never empty.</span>
          </Heading>
          <p className={styles.heroLead}>
            NonEmptyList&lt;T&gt; is a list the type system guarantees has at least one element. Head, Max, Average and
            Reduce cannot fail for lack of data, LINQ keeps the guarantee whenever it can, and a bundled analyzer catches
            empty input at build time.
          </p>
          <div className={styles.heroActions}>
            <Link className={clsx('button button--primary button--lg', styles.primaryButton)} to="/docs/quick-start">
              Get started
              <Icon name="arrowRight" size={18} />
            </Link>
            <Link className={clsx('button button--lg', styles.secondaryButton)} to="/docs/overview">
              Read the docs
            </Link>
          </div>
          <CopyCommand command="dotnet add package Masterly.NonEmptyList" className={styles.heroInstall} />
        </div>
        <div className={styles.heroDemo}>
          <TypeDemo />
        </div>
      </div>
    </header>
  );
}

function Stats() {
  const stats = [
    {value: '0', label: 'dependencies in the core package'},
    {value: '8', label: 'packages, one shared version'},
    {value: 'Built in', label: 'Roslyn analyzer, code fixes and source generator'},
    {value: '.NET 8+', label: 'targets .NET 8, .NET 9 and .NET 10'},
  ];
  return (
    <section className={styles.stats} aria-label="At a glance">
      <div className={clsx('container', styles.statsGrid)}>
        {stats.map((stat) => (
          <div key={stat.label} className={styles.stat}>
            <div className={styles.statValue}>{stat.value}</div>
            <div className={styles.statLabel}>{stat.label}</div>
          </div>
        ))}
      </div>
    </section>
  );
}

function SectionHeader({eyebrow, title, children}) {
  return (
    <div className={styles.sectionHeader}>
      <div className={styles.sectionEyebrow}>{eyebrow}</div>
      <Heading as="h2" className={styles.sectionTitle}>
        {title}
      </Heading>
      {children && <p className={styles.sectionLead}>{children}</p>}
    </div>
  );
}

function Showcase() {
  return (
    <section className={styles.section}>
      <div className="container">
        <SectionHeader eyebrow="Write" title="One guarantee, in every kind of C# you write">
          Collection expressions, list patterns, query syntax and generic math all know the list is non-empty, so the
          code you already write stops needing defensive checks.
        </SectionHeader>
        <div className={styles.tabsCard}>
          <Tabs groupId="showcase" className={styles.tabs}>
            {showcases.map((item) => (
              <TabItem key={item.value} value={item.value} label={item.label}>
                <p className={styles.tabCaption}>
                  {item.caption}{' '}
                  <Link to={item.to} className={styles.inlineLink}>
                    Learn more
                    <Icon name="arrowRight" size={14} />
                  </Link>
                </p>
                <CodeBlock language="csharp">{item.code}</CodeBlock>
              </TabItem>
            ))}
          </Tabs>
        </div>
      </div>
    </section>
  );
}

function MiniTable({caption, headers, rows}) {
  return (
    <table className={styles.miniTable}>
      <caption className={styles.srOnly}>{caption}</caption>
      <thead>
        <tr>
          {headers.map((header) => (
            <th key={header} scope="col">
              {header}
            </th>
          ))}
        </tr>
      </thead>
      <tbody>
        {rows.map((row) => (
          <tr key={row.key} className={row.highlight ? styles.miniTableHighlight : undefined}>
            <td>{row.left}</td>
            <td>
              {row.right}
              {row.badge && <span className={clsx(styles.miniTableBadge, row.muted && styles.miniTableBadgeMuted)}>{row.badge}</span>}
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

function LinqVisual() {
  return (
    <MiniTable
      caption="Return type of each LINQ operator on a NonEmptyList"
      headers={['On a NonEmptyList<T>', 'Returns']}
      rows={linqRows.map((row) => ({
        key: row.call,
        left: row.call,
        right: row.type,
        badge: row.kept ? undefined : 'may be empty',
        muted: !row.kept,
      }))}
    />
  );
}

function ConstraintVisual() {
  return (
    <MiniTable
      caption="Members each constraint makes available"
      headers={['ConstrainedList<T, …>', 'You get']}
      rows={constraintRows.map((row) => ({
        key: row.constraint,
        left: row.constraint,
        right: row.unlocks,
        highlight: row.highlight,
      }))}
    />
  );
}

function Features() {
  return (
    <section className={clsx(styles.section, styles.sectionAlt)}>
      <div className="container">
        <SectionHeader eyebrow="Capabilities" title="Everything a non-empty collection needs in production">
          Start by replacing one List&lt;T&gt;. Add constraints, validation and framework integration when you need them.
        </SectionHeader>
        <div className={styles.bento}>
          {features.map((feature) => (
            <Link
              key={feature.title}
              to={feature.to}
              className={clsx(styles.featureCard, feature.size === 'wide' && styles.featureWide)}>
              <div className={styles.featureBody}>
                <span className={styles.featureIcon}>
                  <Icon name={feature.icon} size={20} />
                </span>
                <Heading as="h3" className={styles.featureTitle}>
                  {feature.title}
                </Heading>
                <p className={styles.featureText}>{feature.text}</p>
                <span className={styles.featureMore}>
                  Read the guide
                  <Icon name="arrowRight" size={14} />
                </span>
              </div>
              {feature.visual === 'linq' && <LinqVisual />}
              {feature.visual === 'constraints' && <ConstraintVisual />}
            </Link>
          ))}
        </div>
      </div>
    </section>
  );
}

function Packages() {
  return (
    <section className={styles.section}>
      <div className="container">
        <SectionHeader eyebrow="Packages" title="Install the core. Add only what you use.">
          All eight packages ship together with the same version number. Every companion package depends only on the
          core and the library it integrates with.
        </SectionHeader>
        <div className={styles.packages}>
          {packages.map((pkg) => (
            <Link key={pkg.name} to={pkg.to} className={styles.packageCard}>
              <div className={styles.packageTop}>
                <span className={styles.packageIcon}>
                  <Icon name="box" size={18} />
                </span>
                <span className={styles.packageRole}>{pkg.role}</span>
              </div>
              <code className={styles.packageName}>{pkg.name}</code>
              <p className={styles.packageText}>{pkg.text}</p>
              <div className={styles.packageMeta}>
                <span>{pkg.target}</span>
                <span>{pkg.deps}</span>
              </div>
            </Link>
          ))}
        </div>
      </div>
    </section>
  );
}

function CallToAction() {
  return (
    <section className={styles.cta}>
      <div className={clsx('container', styles.ctaInner)}>
        <Heading as="h2" className={styles.ctaTitle}>
          Replace your first List&lt;T&gt; in five minutes.
        </Heading>
        <p className={styles.ctaText}>The Quick Start creates a list, reads it safely, queries it and handles empty input.</p>
        <div className={styles.heroActions}>
          <Link className={clsx('button button--primary button--lg', styles.primaryButton)} to="/docs/quick-start">
            Open the Quick Start
            <Icon name="arrowRight" size={18} />
          </Link>
          <Link className={clsx('button button--lg', styles.secondaryButton)} to="/docs/examples">
            See real-world examples
          </Link>
        </div>
      </div>
    </section>
  );
}

export default function Home() {
  return (
    <Layout description="Proof-carrying collections for .NET: NonEmptyList<T> with collection expressions, type-preserving LINQ, generic math, constraint types, validation, a Roslyn analyzer and integrations for ASP.NET Core, EF Core and more.">
      <Hero />
      <main>
        <Stats />
        <Showcase />
        <Features />
        <Packages />
        <CallToAction />
      </main>
    </Layout>
  );
}
