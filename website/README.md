# Documentation site

The [Masterly.NonEmptyList documentation](https://a7mdfre7at.github.io/Masterly.NonEmptyList/), built with [Docusaurus](https://docusaurus.io/). The Docs workflow builds it on every pull request and deploys it to GitHub Pages from `master`.

## Run it locally

```bash
npm ci
npm start          # live-reloading dev server
npm run build      # production build; fails on any broken link or anchor
npm run serve      # serve the production build
```

## Versions

| Folder | Version | URL |
|--------|---------|-----|
| `versioned_docs/version-3.0/` | 3.0, the latest release | `/docs/...` |
| `docs/` | Next, unreleased | `/docs/next/...` |

- A fix that applies to the released version goes in `versioned_docs/version-<x.y>/`, and usually in `docs/` too.
- A new feature is documented in `docs/` only, until it is released.
- When a new minor or major version is released, snapshot the docs. The new version becomes the default at `/docs/...`:

  ```bash
  npm run docusaurus -- docs:version 3.1
  ```

`versions.json` lists the released versions, newest first. Patch releases (3.0.1, 3.0.2, ...) share their minor version's docs.
