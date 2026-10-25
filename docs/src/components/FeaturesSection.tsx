import React from 'react';
import {Puzzle, CloudCog, Feather, ListTree, Layers, Sliders} from 'lucide-react';

const features = [
  {
    icon: Puzzle,
    title: 'Unified API',
    description: 'Create, list, get, and delete repositories. Create, list, get, merge, and close pull requests through one contract.',
    color: '#00D4FF',
  },
  {
    icon: CloudCog,
    title: 'Four Providers',
    description: 'Use GitHub, GitLab, Azure DevOps, or Gitea without changing your application logic. Swap the package and configuration.',
    color: '#a78bfa',
  },
  {
    icon: Feather,
    title: 'Modular Packages',
    description: 'The core package has no provider SDK. Install only the provider packages your application uses.',
    color: '#34d399',
  },
  {
    icon: ListTree,
    title: 'Pagination and Errors',
    description: 'One page shape with nullable totals and provider-driven next-page metadata. One exception family for every provider.',
    color: '#60a5fa',
  },
  {
    icon: Layers,
    title: 'Operational Layers',
    description: 'Add retries, timeouts, concurrency limits, logging, and OpenTelemetry without modifying provider code.',
    color: '#fb923c',
  },
  {
    icon: Sliders,
    title: 'Flexible Configuration',
    description: 'Configure with the fluent API, environment variables, or YAML. The last configuration source wins.',
    color: '#f472b6',
  },
];

export default function FeaturesSection(): React.JSX.Element {
  return (
    <section className="section-dark" style={{padding: '6rem 0'}}>
      <div style={{maxWidth: '1200px', margin: '0 auto', padding: '0 1.5rem'}}>
        <div style={{textAlign: 'center', marginBottom: '4rem'}}>
          <h2 style={{fontSize: '2.5rem', fontWeight: 700, marginBottom: '1rem'}}>
            Why VcsManager?
          </h2>
          <p style={{fontSize: '1.25rem', color: 'var(--ifm-color-emphasis-600)'}}>
            One portable contract for repository and pull request automation
          </p>
        </div>

        <div style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fit, minmax(min(100%, 320px), 1fr))',
          gap: '1.5rem',
        }}>
          {features.map((feature, i) => {
            const Icon = feature.icon;
            return (
              <div key={i} className="feature-card">
                <Icon style={{width: '2.5rem', height: '2.5rem', marginBottom: '1rem', color: feature.color}} />
                <h3 style={{fontSize: '1.25rem', fontWeight: 600, marginBottom: '0.5rem'}}>
                  {feature.title}
                </h3>
                <p style={{color: 'var(--ifm-color-emphasis-600)', margin: 0}}>
                  {feature.description}
                </p>
              </div>
            );
          })}
        </div>
      </div>
    </section>
  );
}
