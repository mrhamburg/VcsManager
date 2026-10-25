import React from 'react';
import GitHubLogoSvg from '@site/static/img/github-logo.svg';
import GitLabLogoSvg from '@site/static/img/gitlab-logo.svg';
import AzureDevOpsLogoSvg from '@site/static/img/azure-devops-logo.svg';
import GiteaLogoSvg from '@site/static/img/gitea-logo.svg';

interface LogoProps {
  style?: React.CSSProperties;
}

export function GitHubLogo({style}: LogoProps): React.JSX.Element {
  return <GitHubLogoSvg style={style} />;
}

export function GitLabLogo({style}: LogoProps): React.JSX.Element {
  return <GitLabLogoSvg style={style} />;
}

export function AzureDevOpsLogo({style}: LogoProps): React.JSX.Element {
  return <AzureDevOpsLogoSvg style={style} />;
}

export function GiteaLogo({style}: LogoProps): React.JSX.Element {
  return <GiteaLogoSvg style={style} />;
}
