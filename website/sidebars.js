// @ts-check

/**
 * A sidebar category whose label links to a generated overview page of cards (one per page, using each
 * page's front-matter description).
 * @param {string} label
 * @param {string} slug
 * @param {string} description
 * @param {string[]} items
 * @param {boolean} [collapsed]
 * @returns {import('@docusaurus/plugin-content-docs').SidebarItemCategory}
 */
function section(label, slug, description, items, collapsed = false) {
  return {
    type: 'category',
    label,
    collapsed,
    link: {type: 'generated-index', slug: `/category/${slug}`, title: label, description},
    items,
  };
}

/** @type {import('@docusaurus/plugin-content-docs').SidebarsConfig} */
const sidebars = {
  docs: [
    section('Getting Started', 'getting-started', 'Install the package and write your first non-empty list.', [
      'overview',
      'installation',
      'quick-start',
    ]),
    section(
      'Working with Lists',
      'working-with-lists',
      'Create, change, read, query and aggregate a NonEmptyList<T> without an empty-sequence exception path.',
      [
        'creating-lists',
        'safe-mutation',
        'reading-elements',
        'linq',
        'functional-operations',
        'generic-math',
        'async-operations',
      ],
    ),
    section(
      'Collection Types',
      'collection-types',
      'Immutable lists, sets, dictionaries, strings, spans, lazy sequences, modeling types and stream batching.',
      ['immutable-lists', 'sets-dictionaries-strings', 'spans-and-lazy-sequences', 'modeling-types', 'batching-streams'],
    ),
    section(
      'Constraints and Validation',
      'constraints-and-validation',
      'Cardinality and ordering in the type, error-accumulating validation, and generated domain collections.',
      ['constrained-lists', 'validation', 'domain-collections'],
    ),
    section('Tooling', 'tooling', 'The bundled Roslyn analyzer and code fixes.', ['analyzers']),
    section(
      'Integrations',
      'integrations',
      'ASP.NET Core, configuration, EF Core, serializers and test tools.',
      ['aspnetcore', 'configuration', 'entity-framework-core', 'serialization', 'testing'],
    ),
    'examples',
    section('Reference', 'reference', 'The public API of every package, and the 2.x to 3.0 upgrade guide.', [
      'api-reference',
      'upgrading-from-2x',
    ]),
  ],
};

export default sidebars;
