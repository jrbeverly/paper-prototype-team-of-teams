# Vision: Team of Teams

## Purpose

The purpose of this project is to explore a modern, self-assembling organizational model where teams are treated as dynamic, temporary units instead of fixed organizational structures.

Traditional organizational tooling assumes that teams are relatively static, distribution lists rarely change, and movement between teams is an exceptional event. This project starts from the opposite assumption: teams are expected to evolve continuously as work changes.

The goal is not to build another project management application. The goal is to build an experimentation platform for operating a Team of Teams (Scrum of Scrums) model in which individuals can discover work, observe teams, transfer between teams, and participate in communities with minimal administrative overhead.

This project should act as both a functional tool and a reference implementation demonstrating how self-assembly can work in practice.

---

# Vision

The system models three primary concepts:

* Communities
* Teams
* People

A community represents the overall organization or collection of people participating in related work.

Within a community are multiple teams.

Teams are intentionally lightweight and ephemeral. They are expected to form, evolve, merge, split, and dissolve as work changes. Long-lived organizational structures should not be assumed.

The system should encourage movement of people toward the work rather than forcing work toward existing organizational boundaries.

---

# Core Principles

## Teams are temporary

A team exists because work exists.

When the work changes, the team should be able to change without administrative friction.

The system should never assume permanent ownership.

---

## Self Assembly

People should be able to discover opportunities and request participation themselves.

Rather than assignments flowing downward through management, the system should support work flowing toward interested and qualified contributors.

Joining a team should be a lightweight process while still allowing limited governance where appropriate.

---

## Transparency

Every team should clearly communicate:

* Purpose
* Mission
* Current objectives
* Required skills
* Responsibilities
* Expected commitments
* Current members
* Open positions
* Team roles

Someone unfamiliar with the team should quickly understand what it exists to accomplish.

---

## Observability

Participation should not require membership.

Users should be able to observe any number of teams without becoming members.

Observers represent people who want awareness rather than ownership.

Examples include:

* future contributors
* stakeholders
* managers
* architects
* adjacent teams
* interested engineers

Observers receive updates and visibility while remaining outside day-to-day execution.

---

# Membership Model

Each user may belong to:

* one active delivery team
* many observed teams

This constraint intentionally encourages focused execution while still allowing broad awareness across the organization.

The system should explicitly model transfers between teams rather than allowing simultaneous active membership across multiple delivery teams.

Transfer workflows are considered first-class concepts rather than exceptional administrative actions.

---

# Team Roles

Membership is intentionally lightweight.

A person may simply be a team member.

Additional responsibilities are represented through optional roles.

Examples include:

* Directly Responsible Individual (DRI)
* Quality Champion
* Security Champion
* Accessibility Champion
* Documentation Champion
* Operations Champion

The role system should remain flexible and extensible rather than being hardcoded around a fixed set of responsibilities.

---

# Discovery

Users should be able to browse available teams.

Each team should advertise:

* what it is building
* why it exists
* current priorities
* desired skills
* current challenges
* opportunities to contribute

The experience should resemble discovering open opportunities rather than browsing organizational charts.

---

# Governance

The project intentionally minimizes administrative control.

Administration exists only where necessary.

Examples include:

* approving transfers when required
* resolving conflicts
* managing community configuration
* recovering from exceptional situations

The administrative experience does not need to be feature rich.

It is expected to be used infrequently.

The platform should optimize for self-service rather than administrator intervention.

---

# Philosophy

This project intentionally explores the human side of organizational design.

People are not interchangeable resources.

Movement between teams is influenced by:

* interest
* experience
* confidence
* relationships
* learning opportunities
* organizational priorities

The platform should encourage healthy self-organization while recognizing that human factors are often more important than process.

---

# Technology Goals

This project should be implemented using the author's standard serverless architecture.

The implementation should target:

* AWS Lambda
* Amazon API Gateway
* Amazon DynamoDB
* Amazon CloudFront
* Amazon S3
* AWS Cognito for authentication

The frontend should use Vue.js.

The backend should use C#.

Infrastructure should remain fully serverless.

Persistent servers, container orchestration platforms, and always-running infrastructure should be avoided.

---

# Design Philosophy

The implementation should prioritize simplicity.

The objective is to validate the operating model rather than build a comprehensive enterprise platform.

Features should exist only when they help demonstrate:

* self assembly
* transparency
* discoverability
* lightweight governance
* dynamic team formation

Complex enterprise workflow should be intentionally deferred unless it directly contributes to validating the underlying organizational model.

---

# Success Criteria

The project is successful if it demonstrates that:

* teams can form around work rather than organizational hierarchy
* individuals can discover meaningful opportunities themselves
* observers can remain informed without becoming members
* team membership can evolve naturally over time
* transfers are simple and explicit
* governance remains lightweight
* the platform provides enough structure to support collaboration without becoming bureaucratic

The final outcome should serve as both a usable prototype and a reference architecture for future experimentation with self-organizing teams.
