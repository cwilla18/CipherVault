# Contributing to CipherVault

Thank you for your interest in contributing to CipherVault. This document describes the repository standards, workflow, and expectations for contributions.

## Repository workflow

- The main development branch is `develop`. Create feature branches from `develop` using the pattern `feature/<short-description>`.
- Open a Pull Request (PR) to `develop` when your feature or fix is ready for review.
- Small fixes and documentation updates may be accepted directly into `develop` by maintainers.

## Pull Requests

- PRs should have a clear description of what changes and why.
- Link any relevant issues in the PR description.
- Each PR must include at least one approving review from a maintainer before merge.
- Resolve merge conflicts locally and push the resolved branch; do not merge via the web UI if conflicts exist.

## Commit messages

Follow Conventional Commits: `type(scope?): subject`

Examples:
- `fix(crypto): correct IV generation for AES` 
- `feat(cli): add --dry-run flag`

Keep messages concise and reference issue numbers when applicable.

## Code style

- This repository enforces styles via `.editorconfig` at the repository root. Please ensure your code is formatted accordingly before submitting a PR.
- Prefer file-scoped namespaces, 4-space indentation, and `var` for built-in types where the type is apparent.

## Tests

- Add unit tests for new behavior. Tests should be fast and deterministic.
- Run the test suite locally before opening a PR.
- CI will run the build and test stage on each PR.

## Security and secrets

- Do not commit secrets (API keys, passwords, private keys). Use the .gitignore and secret management tools.
- For local development, use the .NET Secret Manager or environment variables. For production, use a secret vault (e.g., Azure Key Vault).
- If you discover a security issue, do not open a public issue. Contact the maintainers privately (see repository owner contact) or open a confidential security report.

## CI

- The repository uses GitHub Actions (or another CI) to build and run tests for PRs. Ensure your changes pass CI.

## Reviewing and testing changes locally

- Build: `dotnet build`
- Test: `dotnet test`
- Formatting: IDEs typically honor `.editorconfig`; run formatting checks in your IDE or via `dotnet format`.

## Adding or updating .editorconfig / coding rules

- Changes to coding rules should be discussed in the PR and ideally agreed on by maintainers.
- Maintain a clear rationale in the PR description when modifying style rules.

## Reporting bugs and feature requests

- Open issues in the repository with steps to reproduce, expected behavior, and environment details if applicable.

## License and CLA

- By contributing you agree that your contributions will be licensed under the repository license.

## Thank you

Thanks for taking the time to contribute. Your efforts help keep CipherVault useful and secure.