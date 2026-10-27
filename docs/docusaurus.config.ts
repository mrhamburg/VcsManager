import {themes as prismThemes} from 'prism-react-renderer';
import type {PrismTheme} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

/**
 * One code surface in both colour modes, on the site's graphite (#11141a).
 * The light Prism themes do not keep comments and strings readable on a light
 * ground, so documentation samples use the same dark surface throughout.
 */
const codeTheme: PrismTheme = {
  plain: {color: '#d5d9e1', backgroundColor: '#11141a'},
  styles: [
    ...prismThemes.oneDark.styles,
    {types: ['comment', 'prolog', 'cdata'], style: {color: '#8a93a4', fontStyle: 'italic'}},
  ],
};

type ThemeConfig = Preset.ThemeConfig & {
  zoom?: {
    selector?: string;
    background?: {light?: string; dark?: string};
  };
};

const config: Config = {
  title: 'VcsManager',
  tagline: 'Unified version control management for .NET',
  favicon: 'img/favicon.svg',

  url: 'https://mrhamburg.github.io',
  baseUrl: '/VcsManager/',

  organizationName: 'mrhamburg',
  projectName: 'VcsManager',

  onBrokenLinks: 'throw',
  onBrokenAnchors: 'throw',
  markdown: {
    format: 'detect',
    hooks: {
      onBrokenMarkdownLinks: 'throw',
    },
  },

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
          editUrl: 'https://github.com/mrhamburg/VcsManager/tree/main/docs/',
        },
        blog: false,
        theme: {
          customCss: [
            './src/css/custom.css',
            './src/css/provider-logos.css',
          ],
        },
      } satisfies Preset.Options,
    ],
  ],

  clientModules: ['./src/fonts.js'],

  plugins: ['docusaurus-plugin-image-zoom'],

  themes: [
    [
      '@easyops-cn/docusaurus-search-local',
      {
        hashed: true,
        docsDir: '.',
        docsRouteBasePath: '/docs',
        indexBlog: false,
      },
    ],
  ],

  themeConfig: {
    zoom: {
      selector: '.markdown img:not(a img):not(.no-frame)',
      background: {
        light: 'rgba(248, 250, 252, 0.95)',
        dark: 'rgba(11, 18, 32, 0.95)',
      },
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
    colorMode: {
      defaultMode: 'dark',
      disableSwitch: false,
      respectPrefersColorScheme: true,
    },
    footer: {
      style: 'light',
      links: [
        {
          title: 'Links',
          items: [
            {label: 'Documentation', to: '/docs/intro'},
            {label: 'GitHub', href: 'https://github.com/mrhamburg/VcsManager'},
            {label: 'NuGet Packages', href: 'https://www.nuget.org/'},
            {label: 'Issues', href: 'https://github.com/mrhamburg/VcsManager/issues'},
          ],
        },
      ],
      copyright: `Copyright \u00A9 ${new Date().getFullYear()} VcsManager. Built with Docusaurus.`,
    },
    prism: {
      theme: codeTheme,
      darkTheme: codeTheme,
      additionalLanguages: ['csharp', 'bash', 'yaml', 'json'],
    },
  } satisfies ThemeConfig,
};

export default config;
