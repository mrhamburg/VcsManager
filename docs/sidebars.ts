import type {SidebarsConfig} from '@docusaurus/plugin-content-docs';

const sidebars: SidebarsConfig = {
  docsSidebar: [
    'intro',
    {
      type: 'category',
      label: 'Getting Started',
      items: [
        'getting-started/installation',
        'getting-started/quick-start',
        'getting-started/configuration',
      ],
    },
    {
      type: 'category',
      label: 'Providers',
      items: [
        'providers/overview',
        'providers/github',
        'providers/gitlab',
        'providers/azure-devops',
        'providers/gitea',
        'providers/capability-matrix',
      ],
    },
    {
      type: 'category',
      label: 'Advanced Usage',
      items: [
        'advanced/layers',
        'advanced/custom-providers',
        'advanced/pagination-and-errors',
        'advanced/merge-and-close',
      ],
    },
    {
      type: 'category',
      label: 'Architecture Decisions',
      items: [
        'adr/adr-index',
        'adr/0001-repository-addressing',
        'adr/0002-authentication',
        'adr/0003-github-client',
        'adr/0004-gitlab-client',
        'adr/0005-azure-devops-client',
        'adr/0006-repository-list-scope',
        'adr/0007-merge-method-mapping',
        'adr/0008-close-semantics',
        'adr/0009-pagination-and-errors',
        'adr/0010-exception-contract',
        'adr/0011-gitea-client',
        'adr/0012-provider-listing-and-merge-scope',
      ],
    },
    'api-reference',
    {
      type: 'link',
      label: 'GitHub',
      href: 'https://github.com/mrhamburg/VcsManager',
    },
  ],
};

export default sidebars;
