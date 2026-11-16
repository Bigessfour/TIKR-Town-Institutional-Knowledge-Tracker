# TIKR sign-in (ClerkSuite Cognito)

Local passwords stay off (`TIKR_AUTH_ENABLED=false`). When `TIKR_COGNITO_DOMAIN` is set, a clerk must finish the town Hosted UI before TIKR opens. The first try uses `prompt=none` so an existing ClerkSuite session signs them in without another password. If Cognito returns `login_required` or `interaction_required`, TIKR sends them to the town sign-in page. There is no anonymous fallback.

Chat history is still keyed by `cognito:{sub}`. Email is display-only. Leave `TIKR_COGNITO_DOMAIN` empty only for a machine that is not the town server (local `dotnet run` without that variable).

Staff, admin, and Clerk may enter. Driver may not. A token with no groups is allowed so an unstamped clerk is not locked out. Sign out calls Hosted UI `/logout`, which ends the town session for the other clerk apps too.

`/account` shows the signed-in email and Sign out. It does not change a local password. Grok and Syncfusion keys are not on the Settings page. They stay in the NAS env file.

## Pool and app client

Do not create a new pool. This is the same pool Town Hub and Trash Truck use.

| Item             | Value                                                                                                               |
| ---------------- | ------------------------------------------------------------------------------------------------------------------- |
| AWS account      | `818904800844` (profile `tow`)                                                                                      |
| Region           | `us-east-2` (`TIKR_COGNITO_REGION`)                                                                                 |
| Pool             | `Townofwiley-staff` (`us-east-2_bHk9UcenK`, `TIKR_COGNITO_USER_POOL_ID`)                                            |
| App client name  | `townofwiley-staff-web`                                                                                             |
| App client id    | `258binbcvsms0rfqj5g20qakki` (`TIKR_COGNITO_CLIENT_ID`)                                                             |
| Hosted UI domain | `tow-gov-staff.auth.us-east-2.amazoncognito.com` (`TIKR_COGNITO_DOMAIN`). Leave the env var empty to skip the gate. |

The ID token is checked against that pool's JWKS (issuer, audience, expiry, signature) before the session cookie is stored.

## HTTPS callback

Cognito rejects `http://mr-storage:8080/auth/cognito-callback`. Clerks open TIKR at `https://mr-storage.tail57aa13.ts.net:8444`. Tailscale Serve on the NAS proxies that origin to `http://127.0.0.1:8080`. Apartments stays on port `8443`. Postgres stays on `5433`.

Registered on `townofwiley-staff-web` (added; other apps' URLs were left in place):

- Callback: `https://mr-storage.tail57aa13.ts.net:8444/auth/cognito-callback`
- Logout: `https://mr-storage.tail57aa13.ts.net:8444/`

`http://localhost:8080/auth/cognito-callback` stays for dev. Prod compose sets `TIKR_PUBLIC_ORIGIN` to the Tailscale origin so the callback matches even though the container itself speaks HTTP. The next NAS image deploy picks that up. Until that image is running, the HTTPS address still opens the current app, and the town sign-in finishes after the deploy.

Allowed OAuth flow: authorization code. Scopes requested by TIKR: `openid`, `email`. PKCE is required. The callback exchanges the code on the server and redirects to a clean path. Tokens are not placed in the query string or the Town Hub card URL.

Town Hub `TOWN_HUB_TIKR_URL` is that same origin. Republish the Town Hub card so clerks leave `http://mr-storage:8080`.

Optional client secret, if the client is confidential, belongs in the NAS env as `TIKR_COGNITO_CLIENT_SECRET` and must not be committed. `tikr-api` stays on `127.0.0.1` only.

## Cookies

| Cookie                 | Contents                                                               |
| ---------------------- | ---------------------------------------------------------------------- |
| `tikr-cognito-session` | Validated Cognito ID token. HttpOnly. This is the sign-in.             |
| `tikr-chat-user`       | `cognito:{sub}` when a session exists; otherwise the auth-off chat id  |
| `tikr-chat-display`    | Email for the banner only                                              |
| `tikr-cognito-silent`  | Short-lived. Marks a `prompt=none` attempt so login can be shown next. |
