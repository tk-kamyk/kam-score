# Security Policy

## Reporting a Vulnerability

Please do **not** open a public GitHub issue for security vulnerabilities.

Use GitHub's [Private Vulnerability Reporting](https://github.com/tk-kamyk/kam-score/security/advisories/new)
to file a report privately.

You can expect:

- An acknowledgement within **3 working days**.
- A triage assessment within **10 working days**.
- A fix or mitigation plan, with a target date, for any confirmed issue.

I work on this project alongside a full-time job, so response times are
best-effort. Critical issues will be prioritised.

## Supported Versions

Only the `main` branch is supported. There are no maintenance branches.

## Scope

In scope:

- The KamScore API (`api/`)
- The KamScore SPA (`spa/`)
- Infrastructure-as-code under `infra/`

Out of scope:

- Vulnerabilities in third-party dependencies — please report those upstream.
  I will pick them up via Dependabot.
- Issues that require a tournament organiser's existing JWT or `X-Tournament-Code`
  to exploit are considered low-severity unless they enable privilege escalation
  beyond that tier.

## Disclosure

Once a fix has shipped to `main`, I will publish a GitHub Security Advisory
crediting the reporter (unless you ask to remain anonymous).
