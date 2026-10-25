import React, {useState} from 'react';
import Link from '@docusaurus/Link';
import {Copy, Check} from 'lucide-react';
import {GitHubLogo, GitLabLogo, AzureDevOpsLogo, GiteaLogo} from './ProviderLogos';

const providers = [
  {id: 'github', name: 'GitHub', short: 'GitHub', package: 'VcsManager.GitHub'},
  {id: 'gitlab', name: 'GitLab', short: 'GitLab', package: 'VcsManager.GitLab'},
  {id: 'azuredevops', name: 'Azure DevOps', short: 'Azure DevOps', package: 'VcsManager.AzureDevOps'},
  {id: 'gitea', name: 'Gitea', short: 'Gitea', package: 'VcsManager.Gitea'},
];

const providerLogos: Record<string, React.ReactNode> = {
  github: <GitHubLogo style={{width: '1.75rem', height: '1.75rem'}} />,
  gitlab: <GitLabLogo style={{width: '1.75rem', height: '1.75rem'}} />,
  azuredevops: <AzureDevOpsLogo style={{width: '1.75rem', height: '1.75rem'}} />,
  gitea: <GiteaLogo style={{width: '1.75rem', height: '1.75rem'}} />,
};

export default function HeroSection(): React.JSX.Element {
  const [selectedProvider, setSelectedProvider] = useState('github');
  const [copiedStep, setCopiedStep] = useState<number | null>(null);

  const handleCopy = (text: string, step: number) => {
    navigator.clipboard.writeText(text);
    setCopiedStep(step);
    setTimeout(() => setCopiedStep(null), 2000);
  };

  const selectedPackage = providers.find(p => p.id === selectedProvider)?.package;

  return (
    <section className="hero-gradient">
      <div style={{maxWidth: '1200px', margin: '0 auto', padding: '4rem 1.5rem'}}>
        <div style={{display: 'grid', gridTemplateColumns: '1fr', gap: '3rem', alignItems: 'center'}}>
          <div style={{display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(min(100%, 450px), 1fr))', gap: '3rem', alignItems: 'center'}}>
            <div>
              <div style={{display: 'flex', flexWrap: 'wrap', gap: '0.5rem', marginBottom: '1.5rem'}}>
                <span className="badge-pill badge-dotnet">.NET 10.0</span>
                <span className="badge-pill badge-license">MIT / Apache 2.0</span>
              </div>

              <h1 style={{fontSize: 'clamp(2.5rem, 5vw, 3.5rem)', fontWeight: 700, marginBottom: '1.5rem', lineHeight: 1.1}}>
                One API.{' '}
                <span style={{
                  background: 'linear-gradient(135deg, var(--ifm-color-primary), var(--vcs-color-secondary))',
                  WebkitBackgroundClip: 'text',
                  WebkitTextFillColor: 'transparent',
                  backgroundClip: 'text',
                }}>
                  Any Git Provider.
                </span>
              </h1>

              <p style={{fontSize: '1.25rem', color: 'var(--ifm-color-emphasis-700)', marginBottom: '2rem', lineHeight: 1.6}}>
                A unified .NET abstraction for GitHub, GitLab, Azure DevOps, and
                Gitea. Create repositories and pull requests, merge or close
                them, and add resilient, observable behavior without changing
                your application code.
              </p>

              <div style={{display: 'flex', flexWrap: 'wrap', gap: '1rem'}}>
                <Link className="btn-primary" to="/docs/intro">
                  Get Started
                </Link>
                <a className="btn-outline" href="https://github.com/mrhamburg/VcsManager">
                  View on GitHub
                </a>
              </div>
            </div>

            <div className="diagram-box">
              <div style={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: '0.75rem',
                padding: '0.5rem 1rem',
                borderRadius: '0.5rem',
                background: 'rgba(81, 43, 212, 0.2)',
                border: '1px solid rgba(81, 43, 212, 0.3)',
                marginBottom: '1rem',
              }}>
                <code style={{color: 'var(--vcs-color-secondary)', fontFamily: 'var(--ifm-font-family-monospace)', fontSize: '0.9rem'}}>
                  IVcsProvider
                </code>
              </div>

              <div style={{
                display: 'grid',
                gridTemplateColumns: 'repeat(2, 1fr)',
                gap: '0.75rem',
                padding: '1rem',
                borderRadius: '0.5rem',
                background: 'rgba(81, 43, 212, 0.05)',
                border: '1px solid rgba(81, 43, 212, 0.15)',
              }}>
                {providers.map(p => (
                  <div
                    key={p.id}
                    style={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: '0.5rem',
                      padding: '0.75rem',
                      borderRadius: '0.5rem',
                      background: 'rgba(255, 255, 255, 0.5)',
                      border: '1px solid rgba(81, 43, 212, 0.1)',
                    }}
                    title={p.name}
                  >
                    {providerLogos[p.id]}
                    <span style={{fontSize: '0.8rem', fontWeight: 600, color: 'var(--ifm-color-emphasis-700)', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis'}}>
                      {p.short}
                    </span>
                  </div>
                ))}
              </div>

              <div style={{
                marginTop: '1rem',
                padding: '0.75rem 1rem',
                borderRadius: '0.5rem',
                background: 'rgba(0, 212, 255, 0.08)',
                border: '1px solid rgba(0, 212, 255, 0.2)',
                fontSize: '0.8rem',
                color: 'var(--ifm-color-emphasis-700)',
              }}>
                Repositories, pull requests, pagination, retries, timeouts,
                logging, and OpenTelemetry behind one contract.
              </div>
            </div>
          </div>
        </div>

        <div style={{marginTop: '3rem', display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(min(100%, 350px), 1fr))', gap: '1rem'}}>
          <div className="install-box">
            <div style={{display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.75rem'}}>
              <span style={{
                display: 'flex', alignItems: 'center', justifyContent: 'center',
                width: '1.5rem', height: '1.5rem', borderRadius: '50%',
                background: 'var(--ifm-color-primary)', color: '#fff',
                fontSize: '0.75rem', fontWeight: 700,
              }}>1</span>
              <span style={{fontSize: '0.875rem', fontWeight: 600}}>Install Core Package</span>
            </div>
            <code style={{color: 'var(--vcs-color-secondary)', fontFamily: 'var(--ifm-font-family-monospace)', fontSize: '0.875rem'}}>
              dotnet add package VcsManager.Core
            </code>
            <button
              className="copy-btn"
              onClick={() => handleCopy('dotnet add package VcsManager.Core', 1)}
              aria-label="Copy command"
            >
              {copiedStep === 1
                ? <Check style={{width: '1rem', height: '1rem', color: '#34d399'}} />
                : <Copy style={{width: '1rem', height: '1rem'}} />}
            </button>
          </div>

          <div className="install-box">
            <div style={{display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.75rem'}}>
              <span style={{
                display: 'flex', alignItems: 'center', justifyContent: 'center',
                width: '1.5rem', height: '1.5rem', borderRadius: '50%',
                background: 'var(--ifm-color-primary)', color: '#fff',
                fontSize: '0.75rem', fontWeight: 700,
              }}>2</span>
              <span style={{fontSize: '0.875rem', fontWeight: 600}}>Select a Provider</span>
            </div>
            <div style={{display: 'flex', flexWrap: 'wrap', gap: '0.5rem', marginBottom: '0.75rem'}}>
              {providers.map(p => (
                <button
                  key={p.id}
                  className={`provider-selector-btn ${selectedProvider === p.id ? 'provider-selector-btn--active' : 'provider-selector-btn--inactive'}`}
                  onClick={() => setSelectedProvider(p.id)}
                >
                  {p.name}
                </button>
              ))}
            </div>
            <code style={{color: 'var(--vcs-color-secondary)', fontFamily: 'var(--ifm-font-family-monospace)', fontSize: '0.875rem'}}>
              dotnet add package {selectedPackage}
            </code>
            <button
              className="copy-btn"
              onClick={() => handleCopy(`dotnet add package ${selectedPackage}`, 2)}
              aria-label="Copy command"
            >
              {copiedStep === 2
                ? <Check style={{width: '1rem', height: '1rem', color: '#34d399'}} />
                : <Copy style={{width: '1rem', height: '1rem'}} />}
            </button>
          </div>
        </div>
      </div>
    </section>
  );
}
