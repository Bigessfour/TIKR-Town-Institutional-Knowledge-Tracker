# TIKR sign-in (ClerkSuite Cognito)

Local passwords stay off (`TIKR_AUTH_ENABLED=false`). When `TIKR_COGNITO_DOMAIN` is set, a clerk must finish the town Hosted UI before TIKR opens. The first try uses `prompt=none` so an existing ClerkSuite session signs them in without another password. If Cognito returns `login_required` or `interaction_required`, TIKR sends them to the town sign-in page. There is no anonymous fallback.

Chat history is still keyed by `cognito:{sub}`. Email is display-only. Leave `TIKR_COGNITO_DOMAIN` empty only for a machine that is not the town server (local `dotnet run` without that variable).

Staff, admin, and Clerk may enter. Driver may not. A token with no groups is allowed so an unstamped clerk is not locked out. Sign out calls Hosted UI `/logout`, which ends the town session for the other clerk apps too.

`/account` shows the signed-in email and Sign out. It does not change a local password. Grok and Syncfusion keys are not on the Settings page. They stay in the NAS env file.

## Pool and app client

Do not create a new pool. This is the same pool Town Hub and Trash Truck use.

| Item             | Value                                                                                                                   |
| ---------------- | ----------------------------------------------------------------------------------------------------------------------- |
| AWS account      | `818904800844` (profile `tow`)                                                                                          |
| Region           | `us-east-2` (`TIKR_COGNITO_REGION`)                                                                                     |
| Pool             | `Townofwiley-staff` (`us-east-2_bHk9UcenK`, `TIKR_COGNITO_USER_POOL_ID`)                                                |
| App client name  | `townofwiley-staff-web`                                                                                                 |
| App client id    | `258binbcvsms0rfqj5g20qakki` (`TIKR_COGNITO_CLIENT_ID`)                                                                 |
| Hosted UI domain | `tow-gov-staff.auth.us-east-2.amazoncognito.com` (`TIKR_COGNITO_DOMAIN`). Leave the env var empty to skip the gate.     |

The ID token is checked against that pool's JWKS (issuer, audience, expiry, signature) before the session cookie is stored.

## HTTPS callback (not registered yet)

Cognito rejects `http://mr-storage:8080/auth/cognito-callback`. A direct visit to that HTTP address cannot finish sign-in. Apartments already uses `https://mr-storage.tail57aa13.ts.net`. Put Tailscale Serve or the Synology proxy in front of `tikr-web`, then register that exact origin on `townofwiley-staff-web`:

- Callback: `https://<tikr-host>/auth/cognito-callback`
- Logout: `https://<tikr-host>/`

Keep `http://localhost:8080/auth/cognito-callback` for dev. Do not run `update-user-pool-client` until the HTTPS host is confirmed. Read the client first:

```bash
aws cognito-idp describe-user-pool-client \
  --profile tow --region us-east-2 \
  --user-pool-id us-east-2_bHk9UcenK \
  --client-id 258binbcvsms0rfqj5g20qakki
```

Allowed OAuth flow: authorization code. Scopes: `openid`, `email`. PKCE is required. The callback exchanges the code on the server and redirects to a clean path. Tokens are not placed in the query string or the Town Hub card URL.

After the HTTPS callback works, point the Town Hub TIKR card at that URL (`TOWN_HUB_TIKR_URL`). Until then the card cannot complete SSO.

Optional client secret, if the client is confidential, belongs in the NAS env as `TIKR_COGNITO_CLIENT_SECRET` and must not be committed. `tikr-api` stays on `127.0.0.1` only.

## Cookies

| Cookie                 | Contents                                                                 |
| ---------------------- | ------------------------------------------------------------------------ |
| `tikr-cognito-session` | Validated Cognito ID token. HttpOnly. This is the sign-in.              |
| `tikr-chat-user`       | `cognito:{sub}` when a session exists; otherwise the auth-off chat id   |
| `tikr-chat-display`    | Email for the banner only                                                |
| `tikr-cognito-silent`  | Short-lived. Marks a `prompt=none` attempt so login can be shown next.  |
