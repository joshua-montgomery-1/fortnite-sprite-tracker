# Repository workflow

## GitHub Flow

- Use GitHub Flow: branch from `main`, keep the branch focused, open a pull request back to `main`, and merge only after review and required checks pass.
- Use one of these branch prefixes:
  - `feature/` for new functionality; maps to the `enhancement` label.
  - `bug/` for a normal bug fix; maps to the `bug` label.
  - `hotfix/` for an urgent production fix; maps to the `bug` label.
  - `tech-debt/` for maintainability or refactoring work; maps to the `tech_debt` label.
  - `docs/` for documentation-only changes; maps to the `documentation` label.
  - `chore/` for routine maintenance that does not fit another category.
- Use lowercase kebab-case after the prefix, for example `feature/player-search`.
- Pull requests should target `main` and use `.github/PULL_REQUEST_TEMPLATE.md`.

## Pull request labels

The repository currently uses these labels:

| Label | Use for |
| --- | --- |
| `bug` | Something is not working, including urgent hotfixes |
| `documentation` | Improvements or additions to documentation |
| `enhancement` | New features or requests |
| `dependencies` | Pull requests that update a dependency file |
| `github_actions` | Changes to GitHub Actions code |
| `dependabot` | Pull requests created by Dependabot |
| `tech_debt` | Refactoring, cleanup, or maintainability work without a user-facing feature |

The pull-request labeling workflow applies the category label from the branch prefix. Add any additional applicable labels manually; for example, a `feature/` branch that changes an Action should also receive `github_actions`.
