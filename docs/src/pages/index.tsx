import React from 'react';
import Layout from '@theme/Layout';
import HeroSection from '../components/HeroSection';
import FeaturesSection from '../components/FeaturesSection';
import CodeShowcase from '../components/CodeShowcase';

export default function Home(): React.JSX.Element {
  return (
    <Layout
      title="Home"
      description="Unified version control management for .NET - One API for GitHub, GitLab, Azure DevOps, and Gitea"
    >
      <HeroSection />
      <FeaturesSection />
      <CodeShowcase />
    </Layout>
  );
}
