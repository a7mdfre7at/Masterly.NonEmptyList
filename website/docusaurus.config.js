// @ts-check
// Docusaurus configuration for the Masterly.NonEmptyList documentation site.
// See: https://docusaurus.io/docs/api/docusaurus-config

import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {themes as prismThemes} from 'prism-react-renderer';

const repositoryUrl = 'https://github.com/a7mdfre7at/Masterly.NonEmptyList';
const socialImageUrl = 'https://raw.githubusercontent.com/a7mdfre7at/Masterly.NonEmptyList/master/repo_image.png';

// The package version has one source of truth: <Version> in the core project file.
const siteDirectory = path.dirname(fileURLToPath(import.meta.url));
const coreProject = fs.readFileSync(
  path.join(siteDirectory, '..', 'src', 'Masterly.NonEmptyList', 'Masterly.NonEmptyList.csproj'),
  'utf8',
);
const versionMatch = /<Version>([^<]+)<\/Version>/.exec(coreProject);
if (!versionMatch) {
  throw new Error('src/Masterly.NonEmptyList/Masterly.NonEmptyList.csproj has no <Version> element.');
}
const packageVersion = versionMatch[1].trim();
const majorMinor = packageVersion.split('.').slice(0, 2).join('.');

/** @type {import('@docusaurus/types').Config} */
const config = {
  title: 'Masterly.NonEmptyList',
  tagline: 'Collections the compiler knows are never empty.',
  favicon: 'img/favicon.svg',

  future: {
    v4: true,
  },

  url: 'https://a7mdfre7at.github.io',
  baseUrl: '/Masterly.NonEmptyList/',
  organizationName: 'a7mdfre7at',
  projectName: 'Masterly.NonEmptyList',
  trailingSlash: false,

  onBrokenLinks: 'throw',
  onBrokenAnchors: 'throw',

  customFields: {
    packageVersion,
    repositoryUrl,
  },

  markdown: {
    // .md files are plain CommonMark, so C# generics such as NonEmptyList<T> need no escaping.
    format: 'detect',
    hooks: {
      onBrokenMarkdownLinks: 'throw',
    },
  },

  headTags: [
    {tagName: 'link', attributes: {rel: 'preconnect', href: 'https://fonts.googleapis.com'}},
    {tagName: 'link', attributes: {rel: 'preconnect', href: 'https://fonts.gstatic.com', crossorigin: 'anonymous'}},
  ],

  stylesheets: [
    'https://fonts.googleapis.com/css2?family=IBM+Plex+Sans:ital,wght@0,400;0,500;0,600;0,700;1,400&family=JetBrains+Mono:wght@400;500;600&display=swap',
  ],

  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  presets: [
    [
      'classic',
      /** @type {import('@docusaurus/preset-classic').Options} */
      ({
        docs: {
          sidebarPath: './sidebars.js',
          editUrl: `${repositoryUrl}/tree/master/website/`,
          showLastUpdateTime: true,
        },
        blog: false,
        theme: {
          customCss: './src/css/custom.css',
        },
      }),
    ],
  ],

  themes: [
    [
      '@easyops-cn/docusaurus-search-local',
      /** @type {import('@easyops-cn/docusaurus-search-local').PluginOptions} */
      ({
        hashed: true,
        indexBlog: false,
        docsRouteBasePath: '/docs',
        highlightSearchTermsOnTargetPage: true,
        explicitSearchResultPath: true,
        searchResultLimits: 10,
        searchBarShortcutHint: true,
      }),
    ],
  ],

  themeConfig:
    /** @type {import('@docusaurus/preset-classic').ThemeConfig} */
    ({
      image: socialImageUrl,
      metadata: [
        {
          name: 'keywords',
          content: '.NET, C#, non-empty list, NonEmptyList, collections, LINQ, generic math, Roslyn analyzer, EF Core, ASP.NET Core',
        },
      ],
      colorMode: {
        defaultMode: 'dark',
        respectPrefersColorScheme: true,
      },
      announcementBar: {
        id: `release-${majorMinor}`,
        content: `<strong>${packageVersion} is out.</strong> Collection expressions, type-preserving LINQ, constraint types and a bundled analyzer. <a href="/Masterly.NonEmptyList/docs/upgrading-from-2x">Upgrading from 2.x →</a>`,
        isCloseable: true,
      },
      docs: {
        sidebar: {
          hideable: true,
          autoCollapseCategories: false,
        },
      },
      tableOfContents: {
        minHeadingLevel: 2,
        maxHeadingLevel: 3,
      },
      navbar: {
        title: 'Masterly.NonEmptyList',
        hideOnScroll: false,
        logo: {
          alt: '',
          src: 'img/logo.svg',
          width: 28,
          height: 28,
        },
        items: [
          {type: 'docSidebar', sidebarId: 'docs', position: 'left', label: 'Docs'},
          {to: '/docs/examples', label: 'Examples', position: 'left'},
          {to: '/docs/api-reference', label: 'API', position: 'left'},
          {
            type: 'html',
            position: 'right',
            value: `<a class="navbar-version" href="${repositoryUrl}/blob/master/CHANGELOG.md" title="Changelog">v${packageVersion}</a>`,
          },
          {
            href: 'https://www.nuget.org/packages/Masterly.NonEmptyList',
            position: 'right',
            className: 'navbar-icon-link navbar-icon-link--nuget',
            'aria-label': 'NuGet package',
          },
          {
            href: repositoryUrl,
            position: 'right',
            className: 'navbar-icon-link navbar-icon-link--github',
            'aria-label': 'GitHub repository',
          },
        ],
      },
      footer: {
        style: 'dark',
        links: [
          {
            title: 'Learn',
            items: [
              {label: 'Quick Start', to: '/docs/quick-start'},
              {label: 'Type-preserving LINQ', to: '/docs/linq'},
              {label: 'Real-World Examples', to: '/docs/examples'},
            ],
          },
          {
            title: 'Reference',
            items: [
              {label: 'API Reference', to: '/docs/api-reference'},
              {label: 'Analyzer Rules', to: '/docs/analyzers'},
              {label: 'Upgrading from 2.x', to: '/docs/upgrading-from-2x'},
              {label: 'Changelog', href: `${repositoryUrl}/blob/master/CHANGELOG.md`},
            ],
          },
          {
            title: 'Project',
            items: [
              {label: 'GitHub', href: repositoryUrl},
              {label: 'Issues', href: `${repositoryUrl}/issues`},
              {label: 'NuGet', href: 'https://www.nuget.org/packages?q=Masterly.NonEmptyList'},
            ],
          },
        ],
        copyright: `MIT licensed · © ${new Date().getFullYear()} Ahmad Omar Al-Freihat`,
      },
      prism: {
        theme: prismThemes.oneLight,
        darkTheme: prismThemes.oneDark,
        additionalLanguages: ['csharp', 'bash', 'json', 'powershell', 'ini'],
      },
    }),
};

export default config;
