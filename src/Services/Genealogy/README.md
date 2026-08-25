# DogPlatform Genealogy

Genealogy stores only parent-to-child links. Children are derived from active links and
pending invitations never appear in confirmed trees.

## Cross-service configuration

Pets remains the source of truth. Genealogy calls the existing authenticated `pets/mine`
endpoint for ownership and sex validation, and the existing internal vaccination-context
endpoint for batched minimal context. Configure the same internal credential used by Pets
as an environment or IIS application-pool variable; no credential is stored here:

```text
InternalServices__ApiKey=<shared internal service key>
GenealogyInvitations__ExpirationHours=72
```

## Invitation delivery gaps

`IGenealogyInvitationEmailSender` and `IGenealogyNotificationPublisher` are deliberately
decoupled. Their development implementations log only technical invitation identifiers;
they never log invitation tokens. No compatible internal Notifications command/event and
no configured email provider currently exist, so actual push/email delivery remains an
integration gap. The create response returns the raw high-entropy token once for in-app
sharing; only its SHA-256 hash is stored.

Run `scripts/database/Genealogy/CreateGenealogyDatabase.sql` and then
`GrantGenealogyIisPermissions.sql` manually as an administrator before deploying. These
scripts are never executed automatically.
