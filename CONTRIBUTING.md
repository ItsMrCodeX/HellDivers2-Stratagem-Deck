# Contributing to HD2 Companion

Thanks for your interest in contributing. Please follow the guidelines below.

## Branching Strategy

| Branch | Purpose |
|---|---|
| `main` | Stable, game-ready code. Tagged releases come from here. |
| `develop` | Integration branch. Features and fixes are merged here before reaching `main`. |
| `feature/*` | New features branched from `develop`. Example: `feature/loadout-presets` |
| `fix/*` | Bug fixes branched from `develop`. Example: `fix/udp-timeout` |
| `release/*` | Release candidates branched from `develop`. Example: `release/1.0.0` |
| `hotfix/*` | Urgent fixes branched from `main`. Example: `hotfix/crash-on-startup` |

### Workflow

1. Fork the repo and create a branch from `develop`.
2. Make your changes, following the code style of the project.
3. Test your changes thoroughly.
4. Submit a pull request to the `develop` branch.

## Development Setup

```bash
git clone https://github.com/ItsMrCodeX/HD2-Companion
cd HD2-Companion
dotnet restore HD2Companion.slnx
```

## Code Style

- Follow existing patterns in the project.
- Use PascalCase for public members, camelCase for private.
- Keep methods focused and small.
- No commented-out code in PRs.

## Commit Messages

- Use present tense, imperative mood: "Add stratagem search" not "Added" or "Adds".
- Keep the first line under 72 characters.
- Reference issues: `Fixes #42` or `Relates to #42`.

## Pull Requests

- PRs go to `develop`, not `main`.
- Describe what your PR does and why.
- Keep PRs focused on a single change.
- Make sure the project builds before submitting.

## Reporting Issues

Use the issue templates for [bugs](https://github.com/ItsMrCodeX/HD2-Companion/issues/new?template=bug_report.yml) or [feature requests](https://github.com/ItsMrCodeX/HD2-Companion/issues/new?template=feature_request.yml). For general questions, use [Discussions](https://github.com/ItsMrCodeX/HD2-Companion/discussions).
