# VcsManager Documentation

This is the documentation site for VcsManager, built with
[Docusaurus 3](https://docusaurus.io/).

## Local development

```bash
cd docs
npm install
npm start
```

The development server runs at `http://localhost:3000/VcsManager/`.

## Build

```bash
cd docs
npm run build
```

The static site is generated in `docs/build/`.

## Serve the production build

```bash
cd docs
npm run serve
```

## Docker

```bash
cd docs
docker compose up --build
```

The site is served at `http://localhost:8080`.

## Content layout

The site reads directly from the repository documentation:

| Path | Content |
| --- | --- |
| `docs/intro.md` | Introduction |
| `docs/getting-started/` | Installation, quick start, and configuration |
| `docs/providers/` | Provider guides and capability matrix |
| `docs/advanced/` | Layers, custom providers, pagination, merge, and close |
| `docs/adr/` | Architecture decision records |
| `docs/api-reference.md` | Public API summary |

Add a page by creating a Markdown file in the relevant folder and adding its ID
to `sidebars.ts`. Use the file path without the `.md` extension as the ID, for
example `providers/github`.

## Deployment

The `.github/workflows/docs-build.yml` workflow builds the site on every push
to `main` and deploys it to the `gh-pages` branch. The published URL is:

[https://mrhamburg.github.io/VcsManager/](https://mrhamburg.github.io/VcsManager/)
