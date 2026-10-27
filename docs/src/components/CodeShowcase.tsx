import React from 'react';
import Tabs from '@theme/Tabs';
import TabItem from '@theme/TabItem';
import CodeBlock from '@theme/CodeBlock';
import styles from './landing.module.css';

const fluentCode = `using VcsManager;
using VcsManager.Builder;
using VcsManager.GitHub;

await using var provider = new VcsProviderBuilder()
    .WithGitHub(options =>
    {
        options.Token = Environment.GetEnvironmentVariable("VCS_GITHUB_TOKEN");
        options.DefaultOwner = "my-org";
    })
    .Build();

var repository = await provider.GetRepositoryAsync(new RepositoryRef
{
    Owner = "my-org",
    Name = "payments-api",
});

Console.WriteLine(repository.FullName);`;

const envCode = `export VCS_PROVIDER=github
export VCS_GITHUB_TOKEN=<token>
export VCS_GITHUB_DEFAULT_OWNER=my-org

# In your C# code:
var provider = new VcsProviderBuilder()
    .WithGitHub()
    .FromEnvironment()
    .Build();`;

const yamlCode = `# vcs.yaml
provider:
  github:
    token: <token>
    default:
      owner: my-org`;

const yamlCsharpCode = `var provider = new VcsProviderBuilder()
    .WithGitHub()
    .FromYamlFile("vcs.yaml")
    .Build();`;

export default function CodeShowcase(): React.JSX.Element {
  return (
    <section className={`${styles.section} ${styles.sectionAlt}`}>
      <div className={styles.sectionNarrow}>
        <div className={styles.sectionHead}>
          <h2 className={styles.sectionTitle}>Configure your way</h2>
          <p className={styles.sectionLede}>
            Register a provider, choose a configuration source, and build. Every
            source produces the same settings dictionary.
          </p>
        </div>

        <Tabs>
          <TabItem value="fluent" label="Fluent API" default>
            <CodeBlock language="csharp" title="Program.cs">
              {fluentCode}
            </CodeBlock>
          </TabItem>
          <TabItem value="env" label="Environment">
            <CodeBlock language="bash" title="Terminal">
              {envCode}
            </CodeBlock>
          </TabItem>
          <TabItem value="yaml" label="YAML">
            <CodeBlock language="yaml" title="vcs.yaml">
              {yamlCode}
            </CodeBlock>
            <CodeBlock language="csharp" title="Program.cs">
              {yamlCsharpCode}
            </CodeBlock>
          </TabItem>
        </Tabs>
      </div>
    </section>
  );
}
