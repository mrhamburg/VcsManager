import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

const config: Config = {
  title: 'VcsManager',
  tagline: 'Unified version control management for .NET',
  favicon: 'img/favicon.svg',

  url: 'https://mrhamburg.github.io',
  baseUrl: '/VcsManager/',

  organizationName: 'mrhamburg',
  projectName: 'VcsManager',

  onBrokenLinks: 'throw',
  onBrokenMarkdownLinks: 'warn',

  i18n: {
    defaultLocale: 'en',
    locales: ['en'],
  },

  presets: [
    [
      'classic',
      {
        docs: {
          path: '.',
          include: [
            'intro.md',
            'api-reference.md',
            'getting-started/**/*.{md,mdx}',
            'providers/**/*.{md,mdx}',
            'advanced/**/*.{md,mdx}',
            'adr/**/*.{md,mdx}',
          ],
          sidebarPath: './sidebars.ts',
          numberPrefixParser: false,
        },
        blog: false,
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],

  themeConfig: {
    colorMode: {
      defaultMode: 'dark',
      disableSwitch: false,
      respectPrefersColorScheme: true,
    },
    navbar: {
      title: 'VcsManager',
      logo: {
        alt: 'VcsManager Logo',
        src: 'img/logo.svg',
      },
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'docsSidebar',
          position: 'left',
          label: 'Docs',
        },
        {
          href: 'https://github.com/mrhamburg/VcsManager',
          label: 'GitHub',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Documentation',
          items: [
            {label: 'Introduction', to: '/docs/intro'},
            {label: 'Quick Start', to: '/docs/getting-started/quick-start'},
            {label: 'Providers', to: '/docs/providers/overview'},
            {label: 'API Reference', to: '/docs/api-reference'},
          ],
        },
        {
          title: 'Resources',
          items: [
            {label: 'GitHub', href: 'https://github.com/mrhamburg/VcsManager'},
            {label: 'NuGet Packages', href: 'https://www.nuget.org/'},
            {label: 'Provider Capability Matrix', to: '/docs/providers/capability-matrix'},
          ],
        },
        {
          title: 'Community',
          items: [
            {label: 'GitHub Issues', href: 'https://github.com/mrhamburg/VcsManager/issues'},
            {label: 'GitHub Discussions', href: 'https://github.com/mrhamburg/VcsManager/discussions'},
          ],
        },
        {
          title: 'Legal',
          items: [
            {label: 'MIT License', href: 'https://github.com/mrhamburg/VcsManager/blob/main/LICENSE-MIT'},
            {label: 'Apache 2.0 License', href: 'https://github.com/mrhamburg/VcsManager/blob/main/LICENSE-APACHE'},
          ],
        },
      ],
      copyright: `Copyright \u00A9 ${new Date().getFullYear()} VcsManager. Built with Docusaurus.`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
      additionalLanguages: ['csharp', 'bash', 'yaml', 'json'],
    },
  } satisfies Preset.ThemeConfig,
};

export default config;
