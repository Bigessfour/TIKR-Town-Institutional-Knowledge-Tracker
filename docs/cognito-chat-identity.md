# TIKR chat identity (ClerkSuite Cognito)

TIKR sign-in stays off (`TIKR_AUTH_ENABLED=false`). Clerks open `http://mr-storage:8080` from the Town Hub card. That card URL has no access token and no email.

If the browser already has a ClerkSuite Cognito session, TIKR asks the hosted UI once with `prompt=none` and then keys chat history to `cognito:{sub}`. Email is display-only. If there is no session, TIKR keeps the auth-off path (install profile, then computer name, then a browser id). The computer name is not used once a Cognito subject is stored.

## Pool and app client

Do not create a new pool.

| Item | Value |
| ---- | ----- |
| AWS account | `818904800844` (profile `tow`) |
| Region | `us-east-2` |
| Pool | `Townofwiley-staff` (`us-east-2_bHk9UcenK`) |
| App client name | `townofwiley-staff-web` |
| App client id | `258binbcvsms0rfqj5g20qakki` (`TIKR_COGNITO_CLIENT_ID`) |
| Hosted UI domain | `tow-gov-staff.auth.us-east-2.amazoncognito.com` (`TIKR_COGNITO_DOMAIN`). Leave the env var empty to skip silent login. |

## Redirect URIs on `townofwiley-staff-web`

Registered:

- `http://localhost:8080/auth/cognito-callback`

Cognito rejects non-localhost HTTP callbacks, so `http://mr-storage:8080/auth/cognito-callback` cannot be added. When the NAS web app has an HTTPS name, add that `https://…/auth/cognito-callback` URL on this same client. Until then, a direct NAS visit still loads with no password and uses the auth-off chat id.

Allowed OAuth flows: authorization code. Scopes: `openid`, `email`. PKCE is required. The callback exchanges the code on the server and redirects to a clean path. The access token is not placed in the query string.

```bash
aws cognito-idp describe-user-pool-client \
  --profile tow --region us-east-2 \
  --user-pool-id us-east-2_bHk9UcenK \
  --client-id 258binbcvsms0rfqj5g20qakki
```

Append the two callback URLs to `CallbackURLs` and set `TIKR_COGNITO_DOMAIN` to the pool's hosted UI domain (for example `your-prefix.auth.us-east-2.amazoncognito.com`). Optional client secret, if the client is confidential, belongs in the NAS env as `TIKR_COGNITO_CLIENT_SECRET` and must not be committed.

## Cookies

| Cookie | Contents |
| ------ | -------- |
| `tikr-chat-user` | `cognito:{sub}` when a session exists; otherwise the existing auth-off id |
| `tikr-chat-display` | Email for the banner only |
| `tikr-cognito-checked` | Present after one silent attempt so the browser does not redirect again |
