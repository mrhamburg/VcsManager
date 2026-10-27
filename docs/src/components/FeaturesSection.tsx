import React from 'react';
import Link from '@docusaurus/Link';
import styles from './landing.module.css';

const features = [
  {
    title: 'Unified API',
    description:
      'Create, list, get, and delete repositories. Create, list, get, merge, and close pull requests through one contract.',
  },
  {
    title: 'Four providers',
    description:
      'Use GitHub, GitLab, Azure DevOps, or Gitea without changing your application logic. Swap the package and configuration.',
  },
  {
    title: 'Modular packages',
    description:
      'The core package has no provider SDK. Install only the provider packages your application uses.',
  },
  {
    title: 'Pagination and errors',
    description:
      'One page shape with nullable totals and provider-driven next-page metadata. One exception family for every provider.',
  },
  {
    title: 'Operational layers',
    description:
      'Add retries, timeouts, concurrency limits, logging, and OpenTelemetry without modifying provider code.',
  },
  {
    title: 'Flexible configuration',
    description:
      'Configure with the fluent API, environment variables, or YAML. The last configuration source wins.',
  },
];

export default function FeaturesSection(): React.JSX.Element {
  return (
    <section className={styles.section} id="features">
      <div className={styles.sectionInner}>
        <div className={styles.sectionHead}>
          <h2 className={styles.sectionTitle}>One contract, every provider</h2>
          <p className={styles.sectionLede}>
            Repository and pull request automation that stays portable when the
            provider changes.
          </p>
        </div>

        <div className={styles.grid}>
          {features.map((feature, index) => (
            <article key={feature.title} className={styles.card}>
              <span className={styles.cardIndex}>
                {String(index + 1).padStart(2, '0')}
              </span>
              <h3 className={styles.cardTitle}>{feature.title}</h3>
              <p className={styles.cardText}>{feature.description}</p>
            </article>
          ))}
        </div>

        <div className={styles.quickStart}>
          <span className={styles.quickLabel}>Start with the core package</span>
          <div className={styles.quickCommand}>
            <span className={styles.prompt}>$</span>
            <code>dotnet add package VcsManager.Core</code>
          </div>
          <div>
            <Link to="/docs/getting-started/installation">Installation guide →</Link>
          </div>
        </div>
      </div>
    </section>
  );
}
