# TIKR functions inventory (user-facing controls)

**Generated:** 2026-11-15T22:47:59Z  
**Source:** `scripts/generate-functions-inventory.py`  
**Machine-readable:** [tests/e2e/functions-inventory.json](../tests/e2e/functions-inventory.json)  
**UX audit baseline:** [tikr-ux-evaluation.md](./tikr-ux-evaluation.md)

## Summary

| Metric | Count |
|--------|------:|
| Routes / pages | 14 |
| Buttons | 115 |
| Links / nav | 24 |
| Inputs / fields | 66 |
| **Total items** | **206** |
| Known failures (release blockers) | 17 |
| Smoke-safe (read-only live) | 103 |

## Inventory

| testId | page | label | type | file:line | deps | expected | blocker |
|--------|------|-------|------|-----------|------|----------|---------|
| `inv-control-add-requirement` | / | Add requirement | button | src/TIKR.Web/Components/Pages/Home.razor:87 | — | URL changes or target page loads. | — |
| `inv-control-ai-scan` | / | AI Scan | button | src/TIKR.Web/Components/Pages/Home.razor:88 | — | URL changes or target page loads. | RB-001 |
| `inv-control-assistant` | / | Assistant | button | src/TIKR.Web/Components/Pages/Home.razor:91 | — | URL changes or target page loads. | — |
| `inv-control-calendar` | / | Calendar | button | src/TIKR.Web/Components/Pages/Home.razor:90 | — | URL changes or target page loads. | — |
| `inv-control-edit` | / | Edit | button | src/TIKR.Web/Components/Pages/Home.razor:141 | — | Control responds without unhandled error. | — |
| `inv-control-reset-layout` | / | Reset layout | button | src/TIKR.Web/Components/Pages/Home.razor:23 | — | Control responds without unhandled error. | — |
| `inv-control-route-page-route` | / | Route / | route | src/TIKR.Web/Components/Pages/Home.razor:1 | — | Control responds without unhandled error. | — |
| `inv-control-upload-document` | / | Upload document | button | src/TIKR.Web/Components/Pages/Home.razor:89 | — | URL changes or target page loads. | — |
| `inv-control-user-guide` | / | User guide | button | src/TIKR.Web/Components/Pages/Home.razor:25 | — | Control responds without unhandled error. | — |
| `inv-error-route-error-page-route` | /Error | Route /Error | route | src/TIKR.Web/Components/Pages/Error.razor:1 | — | Control responds without unhandled error. | — |
| `inv-account-route-account-page-route` | /account | Route /account | route | src/TIKR.Web/Components/Pages/Account.razor:1 | — | Control responds without unhandled error. | — |
| `inv-account-text-field` | /account | Text field | input | src/TIKR.Web/Components/Pages/Account.razor:36 | — | Value binds and persists on save. | — |
| `inv-account-text-field-2` | /account | Text field | input | src/TIKR.Web/Components/Pages/Account.razor:41 | — | Value binds and persists on save. | — |
| `inv-account-text-field-3` | /account | Text field | input | src/TIKR.Web/Components/Pages/Account.razor:46 | — | Value binds and persists on save. | — |
| `inv-assistant-clear-conversation` | /assistant | Clear conversation | button | src/TIKR.Web/Components/Pages/Assistant.razor:101 | — | Control responds without unhandled error. | — |
| `inv-assistant-dropdown` | /assistant | Dropdown | dropdown | src/TIKR.Web/Components/Pages/Assistant.razor:100 | — | Value binds and persists on save. | — |
| `inv-assistant-forget` | /assistant | Forget | button | src/TIKR.Web/Components/Pages/Assistant.razor:68 | — | Control responds without unhandled error. | — |
| `inv-assistant-route-assistant-page-route` | /assistant | Route /assistant | route | src/TIKR.Web/Components/Pages/Assistant.razor:1 | API, Ollama | Progress visible; assistant message returned. | — |
| `inv-calendar-dropdown` | /calendar | Dropdown | dropdown | src/TIKR.Web/Components/Pages/Calendar.razor:103 | — | Value binds and persists on save. | — |
| `inv-calendar-dropdown-2` | /calendar | Dropdown | dropdown | src/TIKR.Web/Components/Pages/Calendar.razor:111 | — | Value binds and persists on save. | — |
| `inv-calendar-route-calendar-page-route` | /calendar | Route /calendar | route | src/TIKR.Web/Components/Pages/Calendar.razor:1 | — | Control responds without unhandled error. | — |
| `inv-calendar-text-field` | /calendar | Text field | input | src/TIKR.Web/Components/Pages/Calendar.razor:48 | — | Value binds and persists on save. | — |
| `inv-demo-clerk-tools-end-demo` | /demo/clerk-tools | End demo | button | src/TIKR.Web/Components/Pages/ClerkToolsShowcase.razor:35 | — | Control responds without unhandled error. | — |
| `inv-demo-clerk-tools-jump-to-current-act-card` | /demo/clerk-tools | Jump to current act card | button | src/TIKR.Web/Components/Pages/ClerkToolsShowcase.razor:72 | — | Control responds without unhandled error. | — |
| `inv-demo-clerk-tools-route-demo-clerk-tools-page-route` | /demo/clerk-tools | Route /demo/clerk-tools | route | src/TIKR.Web/Components/Pages/ClerkToolsShowcase.razor:1 | — | Control responds without unhandled error. | — |
| `inv-demo-clerk-tools-start-clerk-tools-demo` | /demo/clerk-tools | Start clerk tools demo | button | src/TIKR.Web/Components/Pages/ClerkToolsShowcase.razor:68 | — | Control responds without unhandled error. | — |
| `inv-documents-browse-file-manager` | /documents | Browse (File Manager) | button | src/TIKR.Web/Components/Pages/Documents.razor:39 | — | Control responds without unhandled error. | — |
| `inv-documents-checkbox` | /documents | Checkbox | checkbox | src/TIKR.Web/Components/Pages/Documents.razor:61 | — | Value binds and persists on save. | — |
| `inv-documents-clear-selection` | /documents | Clear selection | button | src/TIKR.Web/Components/Pages/Documents.razor:170 | — | Control responds without unhandled error. | — |
| `inv-documents-create-folder` | /documents | Create folder | button | src/TIKR.Web/Components/Pages/Documents.razor:81 | API | Data saved and UI reflects change. | — |
| `inv-documents-create-requirement-from-this-email` | /documents | Create Requirement from this email | button | src/TIKR.Web/Components/Pages/Documents.razor:134 | API | Data saved and UI reflects change. | — |
| `inv-documents-delete` | /documents | Delete | button | src/TIKR.Web/Components/Pages/Documents.razor:433 | — | Record removed or confirm dialog shown. | — |
| `inv-documents-delete-selected` | /documents | Delete selected | button | src/TIKR.Web/Components/Pages/Documents.razor:168 | — | Record removed or confirm dialog shown. | — |
| `inv-documents-dismiss` | /documents | Dismiss | button | src/TIKR.Web/Components/Pages/Documents.razor:109 | — | Control responds without unhandled error. | — |
| `inv-documents-dismiss-2` | /documents | Dismiss | button | src/TIKR.Web/Components/Pages/Documents.razor:139 | — | Control responds without unhandled error. | — |
| `inv-documents-file-upload` | /documents | File upload | file-upload | src/TIKR.Web/Components/Pages/Documents.razor:56 | API, NAS storage path | File appears in grid after upload. | — |
| `inv-documents-full-text-search` | /documents | Full-text search | button | src/TIKR.Web/Components/Pages/Documents.razor:148 | Ollama embeddings optional | Grid or results update. | — |
| `inv-documents-keep-in-general` | /documents | Keep in General | button | src/TIKR.Web/Components/Pages/Documents.razor:82 | — | Control responds without unhandled error. | — |
| `inv-documents-library` | /documents | Library | button | src/TIKR.Web/Components/Pages/Documents.razor:36 | — | Control responds without unhandled error. | — |
| `inv-documents-looks-good-accept` | /documents | Looks good / Accept | button | src/TIKR.Web/Components/Pages/Documents.razor:108 | — | Control responds without unhandled error. | — |
| `inv-documents-purge-permanently` | /documents | Purge permanently | button | src/TIKR.Web/Components/Pages/Documents.razor:416 | — | Control responds without unhandled error. | — |
| `inv-documents-re-tag` | /documents | Re-tag | button | src/TIKR.Web/Components/Pages/Documents.razor:110 | — | Control responds without unhandled error. | — |
| `inv-documents-re-tag-2` | /documents | Re-tag | button | src/TIKR.Web/Components/Pages/Documents.razor:432 | — | Control responds without unhandled error. | — |
| `inv-documents-re-tag-with-ai` | /documents | Re-tag with AI | button | src/TIKR.Web/Components/Pages/Documents.razor:169 | — | Control responds without unhandled error. | — |
| `inv-documents-recycle-bin` | /documents | Recycle bin | button | src/TIKR.Web/Components/Pages/Documents.razor:42 | — | Control responds without unhandled error. | — |
| `inv-documents-restore` | /documents | Restore | button | src/TIKR.Web/Components/Pages/Documents.razor:351 | — | Control responds without unhandled error. | — |
| `inv-documents-restore-2` | /documents | Restore | button | src/TIKR.Web/Components/Pages/Documents.razor:414 | — | Control responds without unhandled error. | — |
| `inv-documents-route-documents-page-route` | /documents | Route /documents | route | src/TIKR.Web/Components/Pages/Documents.razor:1 | — | Control responds without unhandled error. | — |
| `inv-documents-search-by-meaning` | /documents | Search by meaning | button | src/TIKR.Web/Components/Pages/Documents.razor:152 | Ollama embeddings optional | Grid or results update. | — |
| `inv-documents-text-field` | /documents | Text field | input | src/TIKR.Web/Components/Pages/Documents.razor:157 | — | Value binds and persists on save. | — |
| `inv-knowledge-route-knowledge-page-route` | /knowledge | Route /knowledge | route | src/TIKR.Web/Components/Pages/Knowledge.razor:1 | — | Control responds without unhandled error. | — |
| `inv-login-route-login-page-route` | /login | Route /login | route | src/TIKR.Web/Components/Pages/Login.razor:1 | — | Control responds without unhandled error. | — |
| `inv-login-text-field` | /login | Text field | input | src/TIKR.Web/Components/Pages/Login.razor:34 | — | Value binds and persists on save. | — |
| `inv-not-found-route-not-found-page-route` | /not-found | Route /not-found | route | src/TIKR.Web/Components/Pages/NotFound.razor:1 | — | Control responds without unhandled error. | — |
| `inv-requirements-add-requirement` | /requirements | Add requirement | button | src/TIKR.Web/Components/Pages/Requirements.razor:35 | API | Data saved and UI reflects change. | — |
| `inv-requirements-add-step` | /requirements | Add step | button | src/TIKR.Web/Components/Pages/Requirements.razor:425 | API | Data saved and UI reflects change. | — |
| `inv-requirements-apply-as-primary` | /requirements | Apply as primary | button | src/TIKR.Web/Components/Pages/Requirements.razor:268 | — | Control responds without unhandled error. | — |
| `inv-requirements-attach` | /requirements | Attach | button | src/TIKR.Web/Components/Pages/Requirements.razor:332 | — | Control responds without unhandled error. | — |
| `inv-requirements-cancel` | /requirements | Cancel | button | src/TIKR.Web/Components/Pages/Requirements.razor:370 | — | Control responds without unhandled error. | — |
| `inv-requirements-cancel-2` | /requirements | Cancel | button | src/TIKR.Web/Components/Pages/Requirements.razor:436 | — | Control responds without unhandled error. | — |
| `inv-requirements-cancel-3` | /requirements | Cancel | button | src/TIKR.Web/Components/Pages/Requirements.razor:519 | — | Control responds without unhandled error. | — |
| `inv-requirements-cancel-4` | /requirements | Cancel | button | src/TIKR.Web/Components/Pages/Requirements.razor:583 | — | Control responds without unhandled error. | — |
| `inv-requirements-checkbox` | /requirements | Checkbox | checkbox | src/TIKR.Web/Components/Pages/Requirements.razor:297 | — | Value binds and persists on save. | — |
| `inv-requirements-delete` | /requirements | Delete | button | src/TIKR.Web/Components/Pages/Requirements.razor:200 | — | Record removed or confirm dialog shown. | — |
| `inv-requirements-dropdown` | /requirements | Dropdown | dropdown | src/TIKR.Web/Components/Pages/Requirements.razor:63 | — | Value binds and persists on save. | — |
| `inv-requirements-dropdown-2` | /requirements | Dropdown | dropdown | src/TIKR.Web/Components/Pages/Requirements.razor:69 | — | Value binds and persists on save. | — |
| `inv-requirements-dropdown-3` | /requirements | Dropdown | dropdown | src/TIKR.Web/Components/Pages/Requirements.razor:244 | — | Value binds and persists on save. | — |
| `inv-requirements-dropdown-4` | /requirements | Dropdown | dropdown | src/TIKR.Web/Components/Pages/Requirements.razor:251 | — | Value binds and persists on save. | — |
| `inv-requirements-dropdown-5` | /requirements | Dropdown | dropdown | src/TIKR.Web/Components/Pages/Requirements.razor:260 | — | Value binds and persists on save. | — |
| `inv-requirements-dropdown-6` | /requirements | Dropdown | dropdown | src/TIKR.Web/Components/Pages/Requirements.razor:325 | — | Value binds and persists on save. | — |
| `inv-requirements-dropdown-7` | /requirements | Dropdown | dropdown | src/TIKR.Web/Components/Pages/Requirements.razor:457 | — | Value binds and persists on save. | — |
| `inv-requirements-dropdown-8` | /requirements | Dropdown | dropdown | src/TIKR.Web/Components/Pages/Requirements.razor:538 | — | Value binds and persists on save. | — |
| `inv-requirements-edit` | /requirements | Edit | button | src/TIKR.Web/Components/Pages/Requirements.razor:197 | — | Control responds without unhandled error. | — |
| `inv-requirements-edit-step` | /requirements | Edit step | button | src/TIKR.Web/Components/Pages/Requirements.razor:410 | — | Control responds without unhandled error. | — |
| `inv-requirements-export-csv` | /requirements | Export CSV | button | src/TIKR.Web/Components/Pages/Requirements.razor:38 | API, SYNCFUSION_LICENSE_KEY | Download/print dialog or PDF bytes returned. | — |
| `inv-requirements-file-upload` | /requirements | File upload | file-upload | src/TIKR.Web/Components/Pages/Requirements.razor:48 | API, NAS storage path | File appears in grid after upload. | — |
| `inv-requirements-load-from-actioned-agenda` | /requirements | Load from actioned agenda | button | src/TIKR.Web/Components/Pages/Requirements.razor:484 | — | Control responds without unhandled error. | — |
| `inv-requirements-meeting-minutes` | /requirements | Meeting minutes | button | src/TIKR.Web/Components/Pages/Requirements.razor:45 | — | Control responds without unhandled error. | — |
| `inv-requirements-print-council-packet` | /requirements | Print council packet | button | src/TIKR.Web/Components/Pages/Requirements.razor:46 | API, SYNCFUSION_LICENSE_KEY | Download/print dialog or PDF bytes returned. | — |
| `inv-requirements-refresh-preview` | /requirements | Refresh preview | button | src/TIKR.Web/Components/Pages/Requirements.razor:546 | — | Control responds without unhandled error. | — |
| `inv-requirements-remove` | /requirements | Remove | button | src/TIKR.Web/Components/Pages/Requirements.razor:413 | — | Record removed or confirm dialog shown. | — |
| `inv-requirements-reset-wiley-view` | /requirements | Reset Wiley view | button | src/TIKR.Web/Components/Pages/Requirements.razor:79 | — | Control responds without unhandled error. | — |
| `inv-requirements-route-requirements-page-route` | /requirements | Route /requirements | route | src/TIKR.Web/Components/Pages/Requirements.razor:1 | — | Control responds without unhandled error. | — |
| `inv-requirements-save` | /requirements | Save | button | src/TIKR.Web/Components/Pages/Requirements.razor:437 | API | Data saved and UI reflects change. | — |
| `inv-requirements-save-step` | /requirements | Save step | button | src/TIKR.Web/Components/Pages/Requirements.razor:367 | API | Data saved and UI reflects change. | — |
| `inv-requirements-suggest-unfinished-business` | /requirements | Suggest unfinished business | button | src/TIKR.Web/Components/Pages/Requirements.razor:547 | — | Control responds without unhandled error. | — |
| `inv-requirements-text-field` | /requirements | Text field | input | src/TIKR.Web/Components/Pages/Requirements.razor:59 | — | Value binds and persists on save. | — |
| `inv-requirements-text-field-2` | /requirements | Text field | input | src/TIKR.Web/Components/Pages/Requirements.razor:360 | — | Value binds and persists on save. | — |
| `inv-requirements-text-field-3` | /requirements | Text field | input | src/TIKR.Web/Components/Pages/Requirements.razor:361 | — | Value binds and persists on save. | — |
| `inv-requirements-text-field-4` | /requirements | Text field | input | src/TIKR.Web/Components/Pages/Requirements.razor:364 | — | Value binds and persists on save. | — |
| `inv-requirements-text-field-5` | /requirements | Text field | input | src/TIKR.Web/Components/Pages/Requirements.razor:423 | — | Value binds and persists on save. | — |
| `inv-requirements-text-field-6` | /requirements | Text field | input | src/TIKR.Web/Components/Pages/Requirements.razor:488 | — | Value binds and persists on save. | — |
| `inv-requirements-text-field-7` | /requirements | Text field | input | src/TIKR.Web/Components/Pages/Requirements.razor:492 | — | Value binds and persists on save. | — |
| `inv-requirements-text-field-8` | /requirements | Text field | input | src/TIKR.Web/Components/Pages/Requirements.razor:496 | — | Value binds and persists on save. | — |
| `inv-requirements-text-field-9` | /requirements | Text field | input | src/TIKR.Web/Components/Pages/Requirements.razor:500 | — | Value binds and persists on save. | — |
| `inv-requirements-unlink` | /requirements | Unlink | button | src/TIKR.Web/Components/Pages/Requirements.razor:282 | — | Control responds without unhandled error. | — |
| `inv-requirements-unlink-2` | /requirements | Unlink | button | src/TIKR.Web/Components/Pages/Requirements.razor:318 | — | Control responds without unhandled error. | — |
| `inv-settings-call-steve-for-help` | /settings | Call Steve for help | button | src/TIKR.Web/Components/Pages/Settings.razor:96 | — | Contact action beyond toast-only acknowledgment. | RB-002 |
| `inv-settings-call-steve-for-help-2` | /settings | Call Steve for help | button | src/TIKR.Web/Components/Pages/Settings.razor:177 | — | Contact action beyond toast-only acknowledgment. | RB-002 |
| `inv-settings-call-steve-for-help-3` | /settings | Call Steve for help | button | src/TIKR.Web/Components/Pages/Settings.razor:205 | — | Contact action beyond toast-only acknowledgment. | RB-002 |
| `inv-settings-call-steve-for-help-4` | /settings | Call Steve for help | button | src/TIKR.Web/Components/Pages/Settings.razor:228 | — | Contact action beyond toast-only acknowledgment. | RB-002 |
| `inv-settings-call-steve-for-help-5` | /settings | Call Steve for help | button | src/TIKR.Web/Components/Pages/Settings.razor:267 | — | Contact action beyond toast-only acknowledgment. | RB-002 |
| `inv-settings-call-steve-for-help-6` | /settings | Call Steve for help | button | src/TIKR.Web/Components/Pages/Settings.razor:319 | — | Contact action beyond toast-only acknowledgment. | RB-002 |
| `inv-settings-call-steve-for-help-7` | /settings | Call Steve for help | button | src/TIKR.Web/Components/Pages/Settings.razor:330 | — | Contact action beyond toast-only acknowledgment. | RB-002 |
| `inv-settings-call-steve-for-help-8` | /settings | Call Steve for help | button | src/TIKR.Web/Components/Pages/Settings.razor:349 | — | Contact action beyond toast-only acknowledgment. | RB-002 |
| `inv-settings-check-status` | /settings | Check status | button | src/TIKR.Web/Components/Pages/Settings.razor:297 | — | Control responds without unhandled error. | — |
| `inv-settings-checkbox` | /settings | Checkbox | checkbox | src/TIKR.Web/Components/Pages/Settings.razor:85 | — | Value binds and persists on save. | — |
| `inv-settings-checkbox-2` | /settings | Checkbox | checkbox | src/TIKR.Web/Components/Pages/Settings.razor:121 | — | Value binds and persists on save. | — |
| `inv-settings-checkbox-3` | /settings | Checkbox | checkbox | src/TIKR.Web/Components/Pages/Settings.razor:203 | — | Value binds and persists on save. | — |
| `inv-settings-checkbox-4` | /settings | Checkbox | checkbox | src/TIKR.Web/Components/Pages/Settings.razor:276 | — | Value binds and persists on save. | — |
| `inv-settings-checkbox-5` | /settings | Checkbox | checkbox | src/TIKR.Web/Components/Pages/Settings.razor:280 | — | Value binds and persists on save. | — |
| `inv-settings-checkbox-6` | /settings | Checkbox | checkbox | src/TIKR.Web/Components/Pages/Settings.razor:284 | — | Value binds and persists on save. | — |
| `inv-settings-dropdown` | /settings | Dropdown | dropdown | src/TIKR.Web/Components/Pages/Settings.razor:49 | — | Value binds and persists on save. | — |
| `inv-settings-helper-not-ready-call-steve-for-help` | /settings | Helper not ready? Call Steve for help | button | src/TIKR.Web/Components/Pages/Settings.razor:156 | Ollama | Support contact surfaced (not toast-only). | RB-002 |
| `inv-settings-need-a-new-advanced-helper-key-call-steve-for-help` | /settings | Need a new advanced helper key? Call Ste | button | src/TIKR.Web/Components/Pages/Settings.razor:124 | Ollama | Support contact surfaced (not toast-only). | RB-002 |
| `inv-settings-need-a-new-license-call-steve-for-help` | /settings | Need a new license? Call Steve for help | button | src/TIKR.Web/Components/Pages/Settings.razor:139 | SYNCFUSION_LICENSE_KEY | Support contact surfaced (not toast-only). | RB-002 |
| `inv-settings-need-to-move-files-call-steve-for-help` | /settings | Need to move files? Call Steve for help | button | src/TIKR.Web/Components/Pages/Settings.razor:192 | — | Support contact surfaced (not toast-only). | RB-002 |
| `inv-settings-not-sure-which-folder-call-steve-for-help` | /settings | Not sure which folder? Call Steve for he | button | src/TIKR.Web/Components/Pages/Settings.razor:252 | — | Support contact surfaced (not toast-only). | RB-002 |
| `inv-settings-open-tools-tour` | /settings | Open tools tour | button | src/TIKR.Web/Components/Pages/Settings.razor:310 | — | URL changes or target page loads. | — |
| `inv-settings-open-user-guide` | /settings | Open user guide | button | src/TIKR.Web/Components/Pages/Settings.razor:81 | — | Control responds without unhandled error. | — |
| `inv-settings-replay-sidebar-menu-tour` | /settings | Replay sidebar menu tour | button | src/TIKR.Web/Components/Pages/Settings.razor:311 | — | Control responds without unhandled error. | — |
| `inv-settings-route-settings-page-route` | /settings | Route /settings | route | src/TIKR.Web/Components/Pages/Settings.razor:1 | — | Control responds without unhandled error. | — |
| `inv-settings-save-settings` | /settings | Save settings | button | src/TIKR.Web/Components/Pages/Settings.razor:296 | API | Data saved and UI reflects change. | — |
| `inv-settings-save-this-clerk` | /settings | Save this clerk | button | src/TIKR.Web/Components/Pages/Settings.razor:58 | API | Data saved and UI reflects change. | — |
| `inv-settings-show-me-around-tikr` | /settings | Show me around TIKR | button | src/TIKR.Web/Components/Pages/Settings.razor:78 | — | Control responds without unhandled error. | — |
| `inv-settings-text-field` | /settings | Text field | input | src/TIKR.Web/Components/Pages/Settings.razor:117 | — | Value binds and persists on save. | — |
| `inv-settings-text-field-10` | /settings | Text field | input | src/TIKR.Web/Components/Pages/Settings.razor:242 | — | Value binds and persists on save. | — |
| `inv-settings-text-field-11` | /settings | Text field | input | src/TIKR.Web/Components/Pages/Settings.razor:249 | — | Value binds and persists on save. | — |
| `inv-settings-text-field-2` | /settings | Text field | input | src/TIKR.Web/Components/Pages/Settings.razor:135 | — | Value binds and persists on save. | — |
| `inv-settings-text-field-3` | /settings | Text field | input | src/TIKR.Web/Components/Pages/Settings.razor:167 | — | Value binds and persists on save. | — |
| `inv-settings-text-field-4` | /settings | Text field | input | src/TIKR.Web/Components/Pages/Settings.razor:171 | — | Value binds and persists on save. | — |
| `inv-settings-text-field-5` | /settings | Text field | input | src/TIKR.Web/Components/Pages/Settings.razor:175 | — | Value binds and persists on save. | — |
| `inv-settings-text-field-6` | /settings | Text field | input | src/TIKR.Web/Components/Pages/Settings.razor:201 | — | Value binds and persists on save. | — |
| `inv-settings-text-field-7` | /settings | Text field | input | src/TIKR.Web/Components/Pages/Settings.razor:216 | — | Value binds and persists on save. | — |
| `inv-settings-text-field-8` | /settings | Text field | input | src/TIKR.Web/Components/Pages/Settings.razor:222 | — | Value binds and persists on save. | — |
| `inv-settings-text-field-9` | /settings | Text field | input | src/TIKR.Web/Components/Pages/Settings.razor:227 | — | Value binds and persists on save. | — |
| `inv-settings-use-the-usual-clerk` | /settings | Use the usual clerk | button | src/TIKR.Web/Components/Pages/Settings.razor:61 | — | Control responds without unhandled error. | — |
| `inv-settings-users-add-user` | /settings/users | Add user | button | src/TIKR.Web/Components/Pages/Users.razor:19 | API | Data saved and UI reflects change. | — |
| `inv-settings-users-cancel` | /settings/users | Cancel | button | src/TIKR.Web/Components/Pages/Users.razor:76 | — | Control responds without unhandled error. | — |
| `inv-settings-users-create` | /settings/users | Create | button | src/TIKR.Web/Components/Pages/Users.razor:77 | API | Data saved and UI reflects change. | — |
| `inv-settings-users-dropdown` | /settings/users | Dropdown | dropdown | src/TIKR.Web/Components/Pages/Users.razor:70 | — | Value binds and persists on save. | — |
| `inv-settings-users-route-settings-users-page-route` | /settings/users | Route /settings/users | route | src/TIKR.Web/Components/Pages/Users.razor:1 | — | Control responds without unhandled error. | — |
| `inv-settings-users-text-field` | /settings/users | Text field | input | src/TIKR.Web/Components/Pages/Users.razor:65 | — | Value binds and persists on save. | — |
| `inv-vault-add-contact` | /vault | Add contact | button | src/TIKR.Web/Components/Pages/Vault.razor:139 | API | Data saved and UI reflects change. | — |
| `inv-vault-cancel` | /vault | Cancel | button | src/TIKR.Web/Components/Pages/Vault.razor:328 | — | Control responds without unhandled error. | — |
| `inv-vault-cancel-2` | /vault | Cancel | button | src/TIKR.Web/Components/Pages/Vault.razor:389 | — | Control responds without unhandled error. | — |
| `inv-vault-checkbox` | /vault | Checkbox | checkbox | src/TIKR.Web/Components/Pages/Vault.razor:380 | — | Value binds and persists on save. | — |
| `inv-vault-checkbox-2` | /vault | Checkbox | checkbox | src/TIKR.Web/Components/Pages/Vault.razor:381 | — | Value binds and persists on save. | — |
| `inv-vault-checkbox-3` | /vault | Checkbox | checkbox | src/TIKR.Web/Components/Pages/Vault.razor:382 | — | Value binds and persists on save. | — |
| `inv-vault-checkbox-4` | /vault | Checkbox | checkbox | src/TIKR.Web/Components/Pages/Vault.razor:383 | — | Value binds and persists on save. | — |
| `inv-vault-checkbox-5` | /vault | Checkbox | checkbox | src/TIKR.Web/Components/Pages/Vault.razor:384 | — | Value binds and persists on save. | — |
| `inv-vault-checkbox-6` | /vault | Checkbox | checkbox | src/TIKR.Web/Components/Pages/Vault.razor:385 | — | Value binds and persists on save. | — |
| `inv-vault-clear-selection` | /vault | Clear selection | button | src/TIKR.Web/Components/Pages/Vault.razor:55 | — | Control responds without unhandled error. | — |
| `inv-vault-copy-everything-for-new-clerk` | /vault | Copy Everything for New Clerk | button | src/TIKR.Web/Components/Pages/Vault.razor:37 | — | Control responds without unhandled error. | — |
| `inv-vault-del` | /vault | Del | button | src/TIKR.Web/Components/Pages/Vault.razor:295 | — | Record removed or confirm dialog shown. | — |
| `inv-vault-delete` | /vault | Delete | button | src/TIKR.Web/Components/Pages/Vault.razor:103 | — | Record removed or confirm dialog shown. | — |
| `inv-vault-delete-2` | /vault | Delete | button | src/TIKR.Web/Components/Pages/Vault.razor:169 | — | Record removed or confirm dialog shown. | — |
| `inv-vault-delete-3` | /vault | Delete | button | src/TIKR.Web/Components/Pages/Vault.razor:227 | — | Record removed or confirm dialog shown. | — |
| `inv-vault-delete-selected` | /vault | Delete selected | button | src/TIKR.Web/Components/Pages/Vault.razor:54 | — | Record removed or confirm dialog shown. | — |
| `inv-vault-edit` | /vault | Edit | button | src/TIKR.Web/Components/Pages/Vault.razor:102 | — | Control responds without unhandled error. | — |
| `inv-vault-edit-2` | /vault | Edit | button | src/TIKR.Web/Components/Pages/Vault.razor:168 | — | Control responds without unhandled error. | — |
| `inv-vault-edit-3` | /vault | Edit | button | src/TIKR.Web/Components/Pages/Vault.razor:226 | — | Control responds without unhandled error. | — |
| `inv-vault-generate-complete-handover-package` | /vault | Generate Complete Handover Package | button | src/TIKR.Web/Components/Pages/Vault.razor:43 | — | Control responds without unhandled error. | — |
| `inv-vault-insert-ai-draft-into-editor` | /vault | Insert AI draft into editor | button | src/TIKR.Web/Components/Pages/Vault.razor:323 | — | Control responds without unhandled error. | — |
| `inv-vault-restore` | /vault | Restore | button | src/TIKR.Web/Components/Pages/Vault.razor:164 | — | Control responds without unhandled error. | — |
| `inv-vault-route-vault-page-route` | /vault | Route /vault | route | src/TIKR.Web/Components/Pages/Vault.razor:1 | — | Control responds without unhandled error. | — |
| `inv-vault-save` | /vault | Save | button | src/TIKR.Web/Components/Pages/Vault.razor:390 | API | Data saved and UI reflects change. | — |
| `inv-vault-save-as-voice-note` | /vault | Save as Voice Note | button | src/TIKR.Web/Components/Pages/Vault.razor:273 | API | Data saved and UI reflects change. | — |
| `inv-vault-save-changes` | /vault | Save Changes | button | src/TIKR.Web/Components/Pages/Vault.razor:327 | API | Data saved and UI reflects change. | — |
| `inv-vault-text-field` | /vault | Text field | input | src/TIKR.Web/Components/Pages/Vault.razor:272 | — | Value binds and persists on save. | — |
| `inv-vault-text-field-2` | /vault | Text field | input | src/TIKR.Web/Components/Pages/Vault.razor:345 | — | Value binds and persists on save. | — |
| `inv-vault-text-field-3` | /vault | Text field | input | src/TIKR.Web/Components/Pages/Vault.razor:349 | — | Value binds and persists on save. | — |
| `inv-vault-text-field-4` | /vault | Text field | input | src/TIKR.Web/Components/Pages/Vault.razor:353 | — | Value binds and persists on save. | — |
| `inv-vault-text-field-5` | /vault | Text field | input | src/TIKR.Web/Components/Pages/Vault.razor:357 | — | Value binds and persists on save. | — |
| `inv-vault-text-field-6` | /vault | Text field | input | src/TIKR.Web/Components/Pages/Vault.razor:361 | — | Value binds and persists on save. | — |
| `inv-vault-text-field-7` | /vault | Text field | input | src/TIKR.Web/Components/Pages/Vault.razor:366 | — | Value binds and persists on save. | — |
| `inv-vault-text-field-8` | /vault | Text field | input | src/TIKR.Web/Components/Pages/Vault.razor:370 | — | Value binds and persists on save. | — |
| `inv-vault-text-field-9` | /vault | Text field | input | src/TIKR.Web/Components/Pages/Vault.razor:375 | — | Value binds and persists on save. | — |
| `inv-assistant-chat-stream` | assistant | Assistant chat (SfAIAssistView) | assistant-chat | src/TIKR.Web/Components/Pages/Assistant.razor:84 | Ollama, API, SYNCFUSION_LICENSE_KEY | User sees thinking/progress immediately; full answer streams | RB-003 |
| `inv-layout-account-account` | layout | Account | link-nav | src/TIKR.Web/Components/Layout/MainLayout.razor:57 | — | Target route loads with expected heading. | — |
| `inv-layout-ai-assistant-assistant` | layout | AI Assistant | link-nav | src/TIKR.Web/Components/Layout/MainLayout.razor:38 | — | Target route loads with expected heading. | — |
| `inv-layout-calendar-calendar` | layout | Calendar | link-nav | src/TIKR.Web/Components/Layout/MainLayout.razor:35 | — | Target route loads with expected heading. | — |
| `inv-layout-cancel` | layout | Cancel | button | src/TIKR.Web/Components/Shared/ConfirmDeleteDialog.razor:10 | — | Control responds without unhandled error. | — |
| `inv-layout-close-workspace` | layout | Close workspace | button | src/TIKR.Web/Components/Shared/DocumentWorkspaceDialog.razor:52 | — | Control responds without unhandled error. | — |
| `inv-layout-dashboard-control` | layout | Dashboard | link-nav | src/TIKR.Web/Components/Layout/MainLayout.razor:34 | — | Target route loads with expected heading. | — |
| `inv-layout-delete` | layout | Delete | button | src/TIKR.Web/Components/Shared/ConfirmDeleteDialog.razor:11 | — | Record removed or confirm dialog shown. | — |
| `inv-layout-delete` | layout | Delete | button | src/TIKR.Web/Components/Shared/DocumentWorkspaceDialog.razor:70 | — | Record removed or confirm dialog shown. | — |
| `inv-layout-documents-documents` | layout | Documents | link-nav | src/TIKR.Web/Components/Layout/MainLayout.razor:37 | — | Target route loads with expected heading. | — |
| `inv-layout-help-for-this-page` | layout | Help for this page | button | src/TIKR.Web/Components/Shared/PageHelp.razor:5 | — | Control responds without unhandled error. | — |
| `inv-layout-help-for-this-page-2` | layout | Help for this page | button | src/TIKR.Web/Components/Shared/PageHelp.razor:14 | — | Control responds without unhandled error. | — |
| `inv-layout-hide-menu` | layout | « Hide menu | button | src/TIKR.Web/Components/Layout/MainLayout.razor:23 | — | Control responds without unhandled error. | — |
| `inv-layout-knowledge-vault-vault` | layout | Knowledge Vault | link-nav | src/TIKR.Web/Components/Layout/MainLayout.razor:39 | — | Target route loads with expected heading. | — |
| `inv-layout-menu` | layout | ☰ Menu | button | src/TIKR.Web/Components/Layout/MainLayout.razor:66 | — | Control responds without unhandled error. | — |
| `inv-layout-open-in-document-library` | layout | Open in Document Library | button | src/TIKR.Web/Components/Shared/DocumentWorkspaceDialog.razor:185 | — | URL changes or target page loads. | — |
| `inv-layout-requirements-requirements` | layout | Requirements | link-nav | src/TIKR.Web/Components/Layout/MainLayout.razor:36 | — | Target route loads with expected heading. | — |
| `inv-layout-resume` | layout | Resume | button | src/TIKR.Web/Components/Layout/ReconnectModal.razor:27 | — | Control responds without unhandled error. | — |
| `inv-layout-retry` | layout | Retry | button | src/TIKR.Web/Components/Layout/ReconnectModal.razor:18 | — | Control responds without unhandled error. | — |
| `inv-layout-settings-settings` | layout | Settings | link-nav | src/TIKR.Web/Components/Layout/MainLayout.razor:40 | — | Target route loads with expected heading. | — |
| `inv-layout-sign-out` | layout | Sign out | button | src/TIKR.Web/Components/Layout/MainLayout.razor:56 | — | Control responds without unhandled error. | — |
| `inv-layout-statehaschanged-try-again` | layout | StateHasChanged()">Try again | button | src/TIKR.Web/Components/Layout/MainLayout.razor:92 | — | Control responds without unhandled error. | RB-004 |
| `inv-layout-text-field` | layout | Text field | input | src/TIKR.Web/Components/Shared/ClerkUserGuideDialog.razor:8 | — | Value binds and persists on save. | — |
| `inv-layout-tools-tour-demo-clerk-tools` | layout | Tools tour | link-nav | src/TIKR.Web/Components/Layout/MainLayout.razor:41 | — | Target route loads with expected heading. | — |
| `inv-layout-tour-this-page` | layout | Tour this page | button | src/TIKR.Web/Components/Shared/PageTourButton.razor:4 | — | Control responds without unhandled error. | — |
| `inv-layout-undo` | layout | Undo | button | src/TIKR.Web/Components/Shared/ClerkToastHost.razor:10 | — | Control responds without unhandled error. | — |
| `inv-layout-users-settings-users` | layout | Users | link-nav | src/TIKR.Web/Components/Layout/MainLayout.razor:45 | — | Target route loads with expected heading. | — |
| `inv-vault-add-howto` | vault | Add how-to / tribal entry (missing contr | button | src/TIKR.Web/Components/Pages/Vault.razor:— | API | New entry dialog or inline editor opens. | RB-005 |

## Regenerate

```bash
python3 scripts/generate-functions-inventory.py
```
