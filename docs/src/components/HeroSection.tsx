import React, {useRef, useState} from 'react';
import Link from '@docusaurus/Link';
import styles from './landing.module.css';

const providers = [
  {
    id: 'github',
    name: 'GitHub',
    package: 'VcsManager.GitHub',
    to: '/docs/providers/github',
    snippet: `var provider = new VcsProviderBuilder()
    .WithGitHub(options =>
    {
        options.Token = Environment
            .GetEnvironmentVariable("VCS_GITHUB_TOKEN");
        options.DefaultOwner = "my-org";
    })
    .Build();`,
  },
  {
    id: 'gitlab',
    name: 'GitLab',
    package: 'VcsManager.GitLab',
    to: '/docs/providers/gitlab',
    snippet: `var provider = new VcsProviderBuilder()
    .WithGitLab(options =>
    {
        options.Token = Environment
            .GetEnvironmentVariable("VCS_GITLAB_TOKEN");
        options.DefaultNamespace = "my-group";
    })
    .Build();`,
  },
  {
    id: 'azuredevops',
    name: 'Azure DevOps',
    package: 'VcsManager.AzureDevOps',
    to: '/docs/providers/azure-devops',
    snippet: `var provider = new VcsProviderBuilder()
    .WithAzureDevOps(options =>
    {
        options.OrganizationUrl = organizationUrl;
        options.PersonalAccessToken = token;
        options.DefaultProject = "my-project";
    })
    .Build();`,
  },
  {
    id: 'gitea',
    name: 'Gitea',
    package: 'VcsManager.Gitea',
    to: '/docs/providers/gitea',
    snippet: `var provider = new VcsProviderBuilder()
    .WithGitea(options =>
    {
        options.BaseUrl = "https://gitea.example.com";
        options.Token = token;
        options.DefaultOwner = "my-owner";
    })
    .Build();`,
  },
];

function ProviderPanel(): React.JSX.Element {
  const [active, setActive] = useState(0);
  const tabs = useRef<(HTMLButtonElement | null)[]>([]);
  const provider = providers[active];

  const onKeyDown = (event: React.KeyboardEvent) => {
    const step = {ArrowRight: 1, ArrowLeft: -1}[event.key];
    if (!step) {
      return;
    }

    event.preventDefault();
    const next = (active + step + providers.length) % providers.length;
    setActive(next);
    tabs.current[next]?.focus();
  };

  return (
    <figure className={styles.panel} aria-label="Provider configuration example">
      <div className={styles.panelHead}>
        <span className={styles.panelTitle}>Program.cs</span>
        <span className={styles.panelBadge}>{provider.package}</span>
      </div>

      <div className={styles.tabs} role="tablist" aria-label="Provider" onKeyDown={onKeyDown}>
        {providers.map((item, index) => (
          <button
            key={item.id}
            ref={(element) => {
              tabs.current[index] = element;
            }}
            type="button"
            role="tab"
            id={`vcs-tab-${item.id}`}
            aria-selected={index === active}
            aria-controls="vcs-provider-panel"
            tabIndex={index === active ? 0 : -1}
            className={styles.tab}
            onClick={() => setActive(index)}>
            <span className={`vcs-logo vcs-logo--${item.id} ${styles.logo}`} aria-hidden="true" />
            {item.name}
          </button>
        ))}
      </div>

      <div
        className={styles.panelBody}
        role="tabpanel"
        id="vcs-provider-panel"
        aria-labelledby={`vcs-tab-${provider.id}`}>
        <div className={styles.panelMeta}>
          <span>Configure</span>
          <code>{provider.name}</code>
          <span>with one builder</span>
        </div>
        <pre className={styles.code}>
          <span className={styles.codeComment}>{`// ${provider.name}`}</span>
          {'\n'}
          {provider.snippet}
        </pre>
      </div>

      <Link className={styles.panelFoot} to={provider.to}>
        Read the {provider.name} guide →
      </Link>
    </figure>
  );
}

export default function HeroSection(): React.JSX.Element {
  return (
    <section className={styles.hero}>
      <div className={styles.heroInner}>
        <div className={styles.intro}>
          <span className={styles.eyebrow}>VcsManager · .NET 10</span>
          <h1 className={styles.title}>
            One API.
            <br />
            <span className={styles.titleAccent}>Any Git provider.</span>
          </h1>
          <p className={styles.lede}>
            A unified .NET abstraction for GitHub, GitLab, Azure DevOps, and Gitea.
            Create repositories and pull requests, merge or close them, and add
            retries, timeouts, logging, and telemetry without changing your
            application code.
          </p>
          <div className={styles.actions}>
            <Link className={styles.primary} to="/docs/getting-started/installation">
              Get started
            </Link>
            <a className={styles.secondary} href="https://github.com/mrhamburg/VcsManager">
              View on GitHub
            </a>
          </div>
          <div className={styles.providers}>
            <span className={styles.providersLabel}>Works with</span>
            <ul className={styles.providerList}>
              {providers.map((provider) => (
                <li key={provider.id}>
                  <span
                    className={`vcs-logo vcs-logo--${provider.id} ${styles.logo}`}
                    aria-hidden="true"
                  />
                  {provider.name}
                </li>
              ))}
            </ul>
          </div>
        </div>

        <ProviderPanel />
      </div>
    </section>
  );
}
