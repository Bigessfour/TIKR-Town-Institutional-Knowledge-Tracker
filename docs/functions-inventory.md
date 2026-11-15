# TIKR functions inventory (user-facing controls)

**Generated:** 2026-11-15T23:14:23Z  
**Source:** `scripts/generate-functions-inventory.py`  
**Machine-readable:** [tests/e2e/functions-inventory.json](../tests/e2e/functions-inventory.json)  
**UX audit baseline:** [tikr-ux-evaluation.md](./tikr-ux-evaluation.md)  
**Production walkthrough (main e1c5399):** manual items in `scripts/inventory-manual-items.json`

## Summary

| Metric | Count |
|--------|------:|
| Routes / pages | 14 |
| Buttons | 122 |
| Links / nav | 25 |
| Inputs / fields | 66 |
| **Total items** | **226** |
| Known failures (release blockers) | 36 |
| Smoke-safe (read-only live) | 111 |

## Inventory

| testId | page | label | type | file:line | deps | expected | blocker |
|--------|------|-------|------|-----------|------|----------|---------|
| `inv-control-home-add-requirement-L87` | / | Add requirement | button | src/TIKR.Web/Components/Pages/Home.razor:87 | — | URL changes or target page loads. | — |
| `inv-control-home-ai-scan-L88` | / | AI Scan | button | src/TIKR.Web/Components/Pages/Home.razor:88 | — | URL changes or target page loads. | RB-001 |
| `inv-control-home-assistant-L91` | / | Assistant | button | src/TIKR.Web/Components/Pages/Home.razor:91 | — | URL changes or target page loads. | — |
| `inv-control-home-calendar-L90` | / | Calendar | button | src/TIKR.Web/Components/Pages/Home.razor:90 | — | URL changes or target page loads. | — |
| `inv-control-home-edit-L141` | / | Edit | button | src/TIKR.Web/Components/Pages/Home.razor:141 | — | Control responds without unhandled error. | — |
| `inv-control-home-reset-layout-L23` | / | Reset layout | button | src/TIKR.Web/Components/Pages/Home.razor:23 | — | Control responds without unhandled error. | — |
| `inv-control-home-route-page-route-L1` | / | Route / | route | src/TIKR.Web/Components/Pages/Home.razor:1 | — | Control responds without unhandled error. | — |
| `inv-control-home-upload-document-L89` | / | Upload document | button | src/TIKR.Web/Components/Pages/Home.razor:89 | — | URL changes or target page loads. | — |
| `inv-control-home-user-guide-L25` | / | User guide | button | src/TIKR.Web/Components/Pages/Home.razor:25 | — | Control responds without unhandled error. | — |
| `inv-error-error-route-error-page-route-L1` | /Error | Route /Error | route | src/TIKR.Web/Components/Pages/Error.razor:1 | — | Control responds without unhandled error. | — |
| `inv-account-account-route-account-page-route-L1` | /account | Route /account | route | src/TIKR.Web/Components/Pages/Account.razor:1 | — | Control responds without unhandled error. | — |
| `inv-account-account-text-field-L36` | /account | Text field | input | src/TIKR.Web/Components/Pages/Account.razor:36 | — | Value binds and persists on save. | — |
| `inv-account-account-text-field-L41` | /account | Text field | input | src/TIKR.Web/Components/Pages/Account.razor:41 | — | Value binds and persists on save. | — |
| `inv-account-account-text-field-L46` | /account | Text field | input | src/TIKR.Web/Components/Pages/Account.razor:46 | — | Value binds and persists on save. | — |
| `inv-assistant-assistant-clear-conversation-L101` | /assistant | Clear conversation | button | src/TIKR.Web/Components/Pages/Assistant.razor:101 | — | Control responds without unhandled error. | — |
| `inv-assistant-assistant-dropdown-L100` | /assistant | Dropdown | dropdown | src/TIKR.Web/Components/Pages/Assistant.razor:100 | — | Value binds and persists on save. | — |
| `inv-assistant-assistant-forget-L68` | /assistant | Forget | button | src/TIKR.Web/Components/Pages/Assistant.razor:68 | — | Control responds without unhandled error. | — |
| `inv-assistant-assistant-route-assistant-page-route-L1` | /assistant | Route /assistant | route | src/TIKR.Web/Components/Pages/Assistant.razor:1 | API, Ollama | Progress visible; assistant message returned. | — |
| `inv-calendar-calendar-dropdown-L103` | /calendar | Dropdown | dropdown | src/TIKR.Web/Components/Pages/Calendar.razor:103 | — | Value binds and persists on save. | — |
| `inv-calendar-calendar-dropdown-L111` | /calendar | Dropdown | dropdown | src/TIKR.Web/Components/Pages/Calendar.razor:111 | — | Value binds and persists on save. | — |
| `inv-calendar-calendar-route-calendar-page-route-L1` | /calendar | Route /calendar | route | src/TIKR.Web/Components/Pages/Calendar.razor:1 | — | Control responds without unhandled error. | — |
| `inv-calendar-calendar-text-field-L48` | /calendar | Text field | input | src/TIKR.Web/Components/Pages/Calendar.razor:48 | — | Value binds and persists on save. | — |
| `inv-demo-clerk-tools-clerktoolsshowcase-end-demo-L35` | /demo/clerk-tools | End demo | button | src/TIKR.Web/Components/Pages/ClerkToolsShowcase.razor:35 | — | Control responds without unhandled error. | — |
| `inv-demo-clerk-tools-clerktoolsshowcase-jump-to-current-act-card-L72` | /demo/clerk-tools | Jump to current act card | button | src/TIKR.Web/Components/Pages/ClerkToolsShowcase.razor:72 | — | Control responds without unhandled error. | — |
| `inv-demo-clerk-tools-clerktoolsshowcase-route-demo-clerk-tools-page-route-L1` | /demo/clerk-tools | Route /demo/clerk-tools | route | src/TIKR.Web/Components/Pages/ClerkToolsShowcase.razor:1 | — | Control responds without unhandled error. | — |
| `inv-demo-clerk-tools-clerktoolsshowcase-start-clerk-tools-demo-L68` | /demo/clerk-tools | Start clerk tools demo | button | src/TIKR.Web/Components/Pages/ClerkToolsShowcase.razor:68 | — | Control responds without unhandled error. | — |
| `inv-documents-documents-browse-file-manager-L39` | /documents | Browse (File Manager) | button | src/TIKR.Web/Components/Pages/Documents.razor:39 | — | Control responds without unhandled error. | — |
| `inv-documents-documents-checkbox-L61` | /documents | Checkbox | checkbox | src/TIKR.Web/Components/Pages/Documents.razor:61 | — | Value binds and persists on save. | — |
| `inv-documents-documents-clear-selection-L170` | /documents | Clear selection | button | src/TIKR.Web/Components/Pages/Documents.razor:170 | — | Control responds without unhandled error. | — |
| `inv-documents-documents-create-folder-L81` | /documents | Create folder | button | src/TIKR.Web/Components/Pages/Documents.razor:81 | API | Data saved and UI reflects change. | — |
| `inv-documents-documents-create-requirement-from-this-email-L134` | /documents | Create Requirement from this email | button | src/TIKR.Web/Components/Pages/Documents.razor:134 | API | Data saved and UI reflects change. | — |
| `inv-documents-documents-delete-L433` | /documents | Delete | button | src/TIKR.Web/Components/Pages/Documents.razor:433 | — | Record removed or confirm dialog shown. | — |
| `inv-documents-documents-delete-selected-L168` | /documents | Delete selected | button | src/TIKR.Web/Components/Pages/Documents.razor:168 | — | Record removed or confirm dialog shown. | — |
| `inv-documents-documents-dismiss-L109` | /documents | Dismiss | button | src/TIKR.Web/Components/Pages/Documents.razor:109 | — | Control responds without unhandled error. | — |
| `inv-documents-documents-dismiss-L139` | /documents | Dismiss | button | src/TIKR.Web/Components/Pages/Documents.razor:139 | — | Control responds without unhandled error. | — |
| `inv-documents-documents-file-upload-L56` | /documents | File upload | file-upload | src/TIKR.Web/Components/Pages/Documents.razor:56 | API, NAS storage path | File appears in grid after upload. | — |
| `inv-documents-documents-full-text-search-L148` | /documents | Full-text search | button | src/TIKR.Web/Components/Pages/Documents.razor:148 | Ollama embeddings optional | Grid or results update. | — |
| `inv-documents-documents-keep-in-general-L82` | /documents | Keep in General | button | src/TIKR.Web/Components/Pages/Documents.razor:82 | — | Control responds without unhandled error. | — |
| `inv-documents-documents-library-L36` | /documents | Library | button | src/TIKR.Web/Components/Pages/Documents.razor:36 | — | Control responds without unhandled error. | — |
| `inv-documents-documents-looks-good-accept-L108` | /documents | Looks good / Accept | button | src/TIKR.Web/Components/Pages/Documents.razor:108 | — | Control responds without unhandled error. | — |
| `inv-documents-documents-purge-permanently-L416` | /documents | Purge permanently | button | src/TIKR.Web/Components/Pages/Documents.razor:416 | — | Control responds without unhandled error. | — |
| `inv-documents-documents-re-tag-L110` | /documents | Re-tag | button | src/TIKR.Web/Components/Pages/Documents.razor:110 | — | Control responds without unhandled error. | — |
| `inv-documents-documents-re-tag-L432` | /documents | Re-tag | button | src/TIKR.Web/Components/Pages/Documents.razor:432 | — | Control responds without unhandled error. | — |
| `inv-documents-documents-re-tag-with-ai-L169` | /documents | Re-tag with AI | button | src/TIKR.Web/Components/Pages/Documents.razor:169 | — | Control responds without unhandled error. | — |
| `inv-documents-documents-recycle-bin-L42` | /documents | Recycle bin | button | src/TIKR.Web/Components/Pages/Documents.razor:42 | — | Control responds without unhandled error. | — |
| `inv-documents-documents-restore-L351` | /documents | Restore | button | src/TIKR.Web/Components/Pages/Documents.razor:351 | — | Control responds without unhandled error. | — |
| `inv-documents-documents-restore-L414` | /documents | Restore | button | src/TIKR.Web/Components/Pages/Documents.razor:414 | — | Control responds without unhandled error. | — |
| `inv-documents-documents-route-documents-page-route-L1` | /documents | Route /documents | route | src/TIKR.Web/Components/Pages/Documents.razor:1 | — | Control responds without unhandled error. | — |
| `inv-documents-documents-search-by-meaning-L152` | /documents | Search by meaning | button | src/TIKR.Web/Components/Pages/Documents.razor:152 | Ollama embeddings optional | Grid or results update. | — |
| `inv-documents-documents-text-field-L157` | /documents | Text field | input | src/TIKR.Web/Components/Pages/Documents.razor:157 | — | Value binds and persists on save. | — |
| `inv-knowledge-knowledge-route-knowledge-page-route-L1` | /knowledge | Route /knowledge | route | src/TIKR.Web/Components/Pages/Knowledge.razor:1 | — | Control responds without unhandled error. | — |
| `inv-login-login-route-login-page-route-L1` | /login | Route /login | route | src/TIKR.Web/Components/Pages/Login.razor:1 | — | Control responds without unhandled error. | — |
| `inv-login-login-text-field-L34` | /login | Text field | input | src/TIKR.Web/Components/Pages/Login.razor:34 | — | Value binds and persists on save. | — |
| `inv-not-found-notfound-route-not-found-page-route-L1` | /not-found | Route /not-found | route | src/TIKR.Web/Components/Pages/NotFound.razor:1 | — | Control responds without unhandled error. | — |
| `inv-requirements-requirements-add-requirement-L35` | /requirements | Add requirement | button | src/TIKR.Web/Components/Pages/Requirements.razor:35 | API | Data saved and UI reflects change. | — |
| `inv-requirements-requirements-add-step-L425` | /requirements | Add step | button | src/TIKR.Web/Components/Pages/Requirements.razor:425 | API | Data saved and UI reflects change. | — |
| `inv-requirements-requirements-apply-as-primary-L268` | /requirements | Apply as primary | button | src/TIKR.Web/Components/Pages/Requirements.razor:268 | — | Control responds without unhandled error. | — |
| `inv-requirements-requirements-attach-L332` | /requirements | Attach | button | src/TIKR.Web/Components/Pages/Requirements.razor:332 | — | Control responds without unhandled error. | — |
| `inv-requirements-requirements-cancel-L370` | /requirements | Cancel | button | src/TIKR.Web/Components/Pages/Requirements.razor:370 | — | Control responds without unhandled error. | — |
| `inv-requirements-requirements-cancel-L436` | /requirements | Cancel | button | src/TIKR.Web/Components/Pages/Requirements.razor:436 | — | Control responds without unhandled error. | — |
| `inv-requirements-requirements-cancel-L519` | /requirements | Cancel | button | src/TIKR.Web/Components/Pages/Requirements.razor:519 | — | Control responds without unhandled error. | — |
| `inv-requirements-requirements-cancel-L583` | /requirements | Cancel | button | src/TIKR.Web/Components/Pages/Requirements.razor:583 | — | Control responds without unhandled error. | — |
| `inv-requirements-requirements-checkbox-L297` | /requirements | Checkbox | checkbox | src/TIKR.Web/Components/Pages/Requirements.razor:297 | — | Value binds and persists on save. | — |
| `inv-requirements-requirements-delete-L200` | /requirements | Delete | button | src/TIKR.Web/Components/Pages/Requirements.razor:200 | — | Record removed or confirm dialog shown. | — |
| `inv-requirements-requirements-dropdown-L244` | /requirements | Dropdown | dropdown | src/TIKR.Web/Components/Pages/Requirements.razor:244 | — | Value binds and persists on save. | — |
| `inv-requirements-requirements-dropdown-L251` | /requirements | Dropdown | dropdown | src/TIKR.Web/Components/Pages/Requirements.razor:251 | — | Value binds and persists on save. | — |
| `inv-requirements-requirements-dropdown-L260` | /requirements | Dropdown | dropdown | src/TIKR.Web/Components/Pages/Requirements.razor:260 | — | Value binds and persists on save. | — |
| `inv-requirements-requirements-dropdown-L325` | /requirements | Dropdown | dropdown | src/TIKR.Web/Components/Pages/Requirements.razor:325 | — | Value binds and persists on save. | — |
| `inv-requirements-requirements-dropdown-L457` | /requirements | Dropdown | dropdown | src/TIKR.Web/Components/Pages/Requirements.razor:457 | — | Value binds and persists on save. | — |
| `inv-requirements-requirements-dropdown-L538` | /requirements | Dropdown | dropdown | src/TIKR.Web/Components/Pages/Requirements.razor:538 | — | Value binds and persists on save. | — |
| `inv-requirements-requirements-dropdown-L63` | /requirements | Dropdown | dropdown | src/TIKR.Web/Components/Pages/Requirements.razor:63 | — | Value binds and persists on save. | — |
| `inv-requirements-requirements-dropdown-L69` | /requirements | Dropdown | dropdown | src/TIKR.Web/Components/Pages/Requirements.razor:69 | — | Value binds and persists on save. | — |
| `inv-requirements-requirements-edit-L197` | /requirements | Edit | button | src/TIKR.Web/Components/Pages/Requirements.razor:197 | — | Control responds without unhandled error. | — |
| `inv-requirements-requirements-edit-step-L410` | /requirements | Edit step | button | src/TIKR.Web/Components/Pages/Requirements.razor:410 | — | Control responds without unhandled error. | — |
| `inv-requirements-requirements-export-csv-L38` | /requirements | Export CSV | button | src/TIKR.Web/Components/Pages/Requirements.razor:38 | API, SYNCFUSION_LICENSE_KEY | Download/print dialog or PDF bytes returned. | — |
| `inv-requirements-requirements-file-upload-L48` | /requirements | File upload | file-upload | src/TIKR.Web/Components/Pages/Requirements.razor:48 | API, NAS storage path | File appears in grid after upload. | — |
| `inv-requirements-requirements-load-from-actioned-agenda-L484` | /requirements | Load from actioned agenda | button | src/TIKR.Web/Components/Pages/Requirements.razor:484 | — | Control responds without unhandled error. | — |
| `inv-requirements-requirements-meeting-minutes-L45` | /requirements | Meeting minutes | button | src/TIKR.Web/Components/Pages/Requirements.razor:45 | — | Control responds without unhandled error. | — |
| `inv-requirements-requirements-print-council-packet-L46` | /requirements | Print council packet | button | src/TIKR.Web/Components/Pages/Requirements.razor:46 | API, SYNCFUSION_LICENSE_KEY | Download/print dialog or PDF bytes returned. | — |
| `inv-requirements-requirements-refresh-preview-L546` | /requirements | Refresh preview | button | src/TIKR.Web/Components/Pages/Requirements.razor:546 | — | Control responds without unhandled error. | — |
| `inv-requirements-requirements-remove-L413` | /requirements | Remove | button | src/TIKR.Web/Components/Pages/Requirements.razor:413 | — | Record removed or confirm dialog shown. | — |
| `inv-requirements-requirements-reset-wiley-view-L79` | /requirements | Reset Wiley view | button | src/TIKR.Web/Components/Pages/Requirements.razor:79 | — | Control responds without unhandled error. | — |
| `inv-requirements-requirements-route-requirements-page-route-L1` | /requirements | Route /requirements | route | src/TIKR.Web/Components/Pages/Requirements.razor:1 | — | Control responds without unhandled error. | — |
| `inv-requirements-requirements-save-L437` | /requirements | Save | button | src/TIKR.Web/Components/Pages/Requirements.razor:437 | API | Data saved and UI reflects change. | — |
| `inv-requirements-requirements-save-step-L367` | /requirements | Save step | button | src/TIKR.Web/Components/Pages/Requirements.razor:367 | API | Data saved and UI reflects change. | — |
| `inv-requirements-requirements-suggest-unfinished-business-L547` | /requirements | Suggest unfinished business | button | src/TIKR.Web/Components/Pages/Requirements.razor:547 | — | Control responds without unhandled error. | — |
| `inv-requirements-requirements-text-field-L360` | /requirements | Text field | input | src/TIKR.Web/Components/Pages/Requirements.razor:360 | — | Value binds and persists on save. | — |
| `inv-requirements-requirements-text-field-L361` | /requirements | Text field | input | src/TIKR.Web/Components/Pages/Requirements.razor:361 | — | Value binds and persists on save. | — |
| `inv-requirements-requirements-text-field-L364` | /requirements | Text field | input | src/TIKR.Web/Components/Pages/Requirements.razor:364 | — | Value binds and persists on save. | — |
| `inv-requirements-requirements-text-field-L423` | /requirements | Text field | input | src/TIKR.Web/Components/Pages/Requirements.razor:423 | — | Value binds and persists on save. | — |
| `inv-requirements-requirements-text-field-L488` | /requirements | Text field | input | src/TIKR.Web/Components/Pages/Requirements.razor:488 | — | Value binds and persists on save. | — |
| `inv-requirements-requirements-text-field-L492` | /requirements | Text field | input | src/TIKR.Web/Components/Pages/Requirements.razor:492 | — | Value binds and persists on save. | — |
| `inv-requirements-requirements-text-field-L496` | /requirements | Text field | input | src/TIKR.Web/Components/Pages/Requirements.razor:496 | — | Value binds and persists on save. | — |
| `inv-requirements-requirements-text-field-L500` | /requirements | Text field | input | src/TIKR.Web/Components/Pages/Requirements.razor:500 | — | Value binds and persists on save. | — |
| `inv-requirements-requirements-text-field-L59` | /requirements | Text field | input | src/TIKR.Web/Components/Pages/Requirements.razor:59 | — | Value binds and persists on save. | — |
| `inv-requirements-requirements-unlink-L282` | /requirements | Unlink | button | src/TIKR.Web/Components/Pages/Requirements.razor:282 | — | Control responds without unhandled error. | — |
| `inv-requirements-requirements-unlink-L318` | /requirements | Unlink | button | src/TIKR.Web/Components/Pages/Requirements.razor:318 | — | Control responds without unhandled error. | — |
| `inv-settings-settings-call-steve-for-help-L177` | /settings | Call Steve for help | button | src/TIKR.Web/Components/Pages/Settings.razor:177 | — | Support contact surfaced (not toast-only). | RB-002 |
| `inv-settings-settings-call-steve-for-help-L205` | /settings | Call Steve for help | button | src/TIKR.Web/Components/Pages/Settings.razor:205 | — | Support contact surfaced (not toast-only). | RB-002 |
| `inv-settings-settings-call-steve-for-help-L228` | /settings | Call Steve for help | button | src/TIKR.Web/Components/Pages/Settings.razor:228 | — | Support contact surfaced (not toast-only). | RB-002 |
| `inv-settings-settings-call-steve-for-help-L267` | /settings | Call Steve for help | button | src/TIKR.Web/Components/Pages/Settings.razor:267 | — | Support contact surfaced (not toast-only). | RB-002 |
| `inv-settings-settings-call-steve-for-help-L319` | /settings | Call Steve for help | button | src/TIKR.Web/Components/Pages/Settings.razor:319 | — | Support contact surfaced (not toast-only). | RB-002 |
| `inv-settings-settings-call-steve-for-help-L330` | /settings | Call Steve for help | button | src/TIKR.Web/Components/Pages/Settings.razor:330 | — | Support contact surfaced (not toast-only). | RB-002 |
| `inv-settings-settings-call-steve-for-help-L349` | /settings | Call Steve for help | button | src/TIKR.Web/Components/Pages/Settings.razor:349 | — | Support contact surfaced (not toast-only). | RB-002 |
| `inv-settings-settings-call-steve-for-help-L96` | /settings | Call Steve for help | button | src/TIKR.Web/Components/Pages/Settings.razor:96 | — | Support contact surfaced (not toast-only). | RB-002 |
| `inv-settings-settings-check-status-L297` | /settings | Check status | button | src/TIKR.Web/Components/Pages/Settings.razor:297 | — | Control responds without unhandled error. | — |
| `inv-settings-settings-checkbox-L121` | /settings | Checkbox | checkbox | src/TIKR.Web/Components/Pages/Settings.razor:121 | — | Value binds and persists on save. | — |
| `inv-settings-settings-checkbox-L203` | /settings | Checkbox | checkbox | src/TIKR.Web/Components/Pages/Settings.razor:203 | — | Value binds and persists on save. | — |
| `inv-settings-settings-checkbox-L276` | /settings | Checkbox | checkbox | src/TIKR.Web/Components/Pages/Settings.razor:276 | — | Value binds and persists on save. | — |
| `inv-settings-settings-checkbox-L280` | /settings | Checkbox | checkbox | src/TIKR.Web/Components/Pages/Settings.razor:280 | — | Value binds and persists on save. | — |
| `inv-settings-settings-checkbox-L284` | /settings | Checkbox | checkbox | src/TIKR.Web/Components/Pages/Settings.razor:284 | — | Value binds and persists on save. | — |
| `inv-settings-settings-checkbox-L85` | /settings | Checkbox | checkbox | src/TIKR.Web/Components/Pages/Settings.razor:85 | — | Value binds and persists on save. | — |
| `inv-settings-settings-dropdown-L49` | /settings | Dropdown | dropdown | src/TIKR.Web/Components/Pages/Settings.razor:49 | — | Value binds and persists on save. | — |
| `inv-settings-settings-helper-not-ready-call-steve-for-help-L156` | /settings | Helper not ready? Call Steve for help | button | src/TIKR.Web/Components/Pages/Settings.razor:156 | Ollama | Support contact surfaced (not toast-only). | RB-002 |
| `inv-settings-settings-need-a-new-advanced-helper-key-call-steve-for-help-L124` | /settings | Need a new advanced helper key? Call Ste | button | src/TIKR.Web/Components/Pages/Settings.razor:124 | Ollama | Support contact surfaced (not toast-only). | RB-002 |
| `inv-settings-settings-need-a-new-license-call-steve-for-help-L139` | /settings | Need a new license? Call Steve for help | button | src/TIKR.Web/Components/Pages/Settings.razor:139 | SYNCFUSION_LICENSE_KEY | Support contact surfaced (not toast-only). | RB-002 |
| `inv-settings-settings-need-to-move-files-call-steve-for-help-L192` | /settings | Need to move files? Call Steve for help | button | src/TIKR.Web/Components/Pages/Settings.razor:192 | — | Support contact surfaced (not toast-only). | RB-002 |
| `inv-settings-settings-not-sure-which-folder-call-steve-for-help-L252` | /settings | Not sure which folder? Call Steve for he | button | src/TIKR.Web/Components/Pages/Settings.razor:252 | — | Support contact surfaced (not toast-only). | RB-002 |
| `inv-settings-settings-open-tools-tour-L310` | /settings | Open tools tour | button | src/TIKR.Web/Components/Pages/Settings.razor:310 | — | URL changes or target page loads. | — |
| `inv-settings-settings-open-user-guide-L81` | /settings | Open user guide | button | src/TIKR.Web/Components/Pages/Settings.razor:81 | — | Control responds without unhandled error. | — |
| `inv-settings-settings-replay-sidebar-menu-tour-L311` | /settings | Replay sidebar menu tour | button | src/TIKR.Web/Components/Pages/Settings.razor:311 | — | Control responds without unhandled error. | — |
| `inv-settings-settings-route-settings-page-route-L1` | /settings | Route /settings | route | src/TIKR.Web/Components/Pages/Settings.razor:1 | — | Control responds without unhandled error. | — |
| `inv-settings-settings-save-settings-L296` | /settings | Save settings | button | src/TIKR.Web/Components/Pages/Settings.razor:296 | API | Data saved and UI reflects change. | — |
| `inv-settings-settings-save-this-clerk-L58` | /settings | Save this clerk | button | src/TIKR.Web/Components/Pages/Settings.razor:58 | API | Data saved and UI reflects change. | — |
| `inv-settings-settings-show-me-around-tikr-L78` | /settings | Show me around TIKR | button | src/TIKR.Web/Components/Pages/Settings.razor:78 | — | Control responds without unhandled error. | — |
| `inv-settings-settings-text-field-L117` | /settings | Text field | input | src/TIKR.Web/Components/Pages/Settings.razor:117 | — | Value binds and persists on save. | — |
| `inv-settings-settings-text-field-L135` | /settings | Text field | input | src/TIKR.Web/Components/Pages/Settings.razor:135 | — | Value binds and persists on save. | — |
| `inv-settings-settings-text-field-L167` | /settings | Text field | input | src/TIKR.Web/Components/Pages/Settings.razor:167 | — | Value binds and persists on save. | — |
| `inv-settings-settings-text-field-L171` | /settings | Text field | input | src/TIKR.Web/Components/Pages/Settings.razor:171 | — | Value binds and persists on save. | — |
| `inv-settings-settings-text-field-L175` | /settings | Text field | input | src/TIKR.Web/Components/Pages/Settings.razor:175 | — | Value binds and persists on save. | — |
| `inv-settings-settings-text-field-L201` | /settings | Text field | input | src/TIKR.Web/Components/Pages/Settings.razor:201 | — | Value binds and persists on save. | — |
| `inv-settings-settings-text-field-L216` | /settings | Text field | input | src/TIKR.Web/Components/Pages/Settings.razor:216 | — | Value binds and persists on save. | — |
| `inv-settings-settings-text-field-L222` | /settings | Text field | input | src/TIKR.Web/Components/Pages/Settings.razor:222 | — | Value binds and persists on save. | — |
| `inv-settings-settings-text-field-L227` | /settings | Text field | input | src/TIKR.Web/Components/Pages/Settings.razor:227 | — | Value binds and persists on save. | — |
| `inv-settings-settings-text-field-L242` | /settings | Text field | input | src/TIKR.Web/Components/Pages/Settings.razor:242 | — | Value binds and persists on save. | — |
| `inv-settings-settings-text-field-L249` | /settings | Text field | input | src/TIKR.Web/Components/Pages/Settings.razor:249 | — | Value binds and persists on save. | — |
| `inv-settings-settings-use-the-usual-clerk-L61` | /settings | Use the usual clerk | button | src/TIKR.Web/Components/Pages/Settings.razor:61 | — | Control responds without unhandled error. | — |
| `inv-settings-users-users-add-user-L19` | /settings/users | Add user | button | src/TIKR.Web/Components/Pages/Users.razor:19 | API | Data saved and UI reflects change. | — |
| `inv-settings-users-users-cancel-L76` | /settings/users | Cancel | button | src/TIKR.Web/Components/Pages/Users.razor:76 | — | Control responds without unhandled error. | — |
| `inv-settings-users-users-create-L77` | /settings/users | Create | button | src/TIKR.Web/Components/Pages/Users.razor:77 | API | Data saved and UI reflects change. | — |
| `inv-settings-users-users-dropdown-L70` | /settings/users | Dropdown | dropdown | src/TIKR.Web/Components/Pages/Users.razor:70 | — | Value binds and persists on save. | — |
| `inv-settings-users-users-route-settings-users-page-route-L1` | /settings/users | Route /settings/users | route | src/TIKR.Web/Components/Pages/Users.razor:1 | — | Control responds without unhandled error. | — |
| `inv-settings-users-users-text-field-L65` | /settings/users | Text field | input | src/TIKR.Web/Components/Pages/Users.razor:65 | — | Value binds and persists on save. | — |
| `inv-vault-vault-add-contact-L139` | /vault | Add contact | button | src/TIKR.Web/Components/Pages/Vault.razor:139 | API | Data saved and UI reflects change. | — |
| `inv-vault-vault-cancel-L328` | /vault | Cancel | button | src/TIKR.Web/Components/Pages/Vault.razor:328 | — | Control responds without unhandled error. | — |
| `inv-vault-vault-cancel-L389` | /vault | Cancel | button | src/TIKR.Web/Components/Pages/Vault.razor:389 | — | Control responds without unhandled error. | — |
| `inv-vault-vault-checkbox-L380` | /vault | Checkbox | checkbox | src/TIKR.Web/Components/Pages/Vault.razor:380 | — | Value binds and persists on save. | — |
| `inv-vault-vault-checkbox-L381` | /vault | Checkbox | checkbox | src/TIKR.Web/Components/Pages/Vault.razor:381 | — | Value binds and persists on save. | — |
| `inv-vault-vault-checkbox-L382` | /vault | Checkbox | checkbox | src/TIKR.Web/Components/Pages/Vault.razor:382 | — | Value binds and persists on save. | — |
| `inv-vault-vault-checkbox-L383` | /vault | Checkbox | checkbox | src/TIKR.Web/Components/Pages/Vault.razor:383 | — | Value binds and persists on save. | — |
| `inv-vault-vault-checkbox-L384` | /vault | Checkbox | checkbox | src/TIKR.Web/Components/Pages/Vault.razor:384 | — | Value binds and persists on save. | — |
| `inv-vault-vault-checkbox-L385` | /vault | Checkbox | checkbox | src/TIKR.Web/Components/Pages/Vault.razor:385 | — | Value binds and persists on save. | — |
| `inv-vault-vault-clear-selection-L55` | /vault | Clear selection | button | src/TIKR.Web/Components/Pages/Vault.razor:55 | — | Control responds without unhandled error. | — |
| `inv-vault-vault-copy-everything-for-new-clerk-L37` | /vault | Copy Everything for New Clerk | button | src/TIKR.Web/Components/Pages/Vault.razor:37 | — | Control responds without unhandled error. | — |
| `inv-vault-vault-del-L295` | /vault | Del | button | src/TIKR.Web/Components/Pages/Vault.razor:295 | — | Record removed or confirm dialog shown. | — |
| `inv-vault-vault-delete-L103` | /vault | Delete | button | src/TIKR.Web/Components/Pages/Vault.razor:103 | — | Record removed or confirm dialog shown. | — |
| `inv-vault-vault-delete-L169` | /vault | Delete | button | src/TIKR.Web/Components/Pages/Vault.razor:169 | — | Record removed or confirm dialog shown. | — |
| `inv-vault-vault-delete-L227` | /vault | Delete | button | src/TIKR.Web/Components/Pages/Vault.razor:227 | — | Record removed or confirm dialog shown. | — |
| `inv-vault-vault-delete-selected-L54` | /vault | Delete selected | button | src/TIKR.Web/Components/Pages/Vault.razor:54 | — | Record removed or confirm dialog shown. | — |
| `inv-vault-vault-edit-L102` | /vault | Edit | button | src/TIKR.Web/Components/Pages/Vault.razor:102 | — | Control responds without unhandled error. | — |
| `inv-vault-vault-edit-L168` | /vault | Edit | button | src/TIKR.Web/Components/Pages/Vault.razor:168 | — | Control responds without unhandled error. | — |
| `inv-vault-vault-edit-L226` | /vault | Edit | button | src/TIKR.Web/Components/Pages/Vault.razor:226 | — | Control responds without unhandled error. | — |
| `inv-vault-vault-generate-complete-handover-package-L43` | /vault | Generate Complete Handover Package | button | src/TIKR.Web/Components/Pages/Vault.razor:43 | — | Control responds without unhandled error. | — |
| `inv-vault-vault-insert-ai-draft-into-editor-L323` | /vault | Insert AI draft into editor | button | src/TIKR.Web/Components/Pages/Vault.razor:323 | — | Control responds without unhandled error. | — |
| `inv-vault-vault-restore-L164` | /vault | Restore | button | src/TIKR.Web/Components/Pages/Vault.razor:164 | — | Control responds without unhandled error. | — |
| `inv-vault-vault-route-vault-page-route-L1` | /vault | Route /vault | route | src/TIKR.Web/Components/Pages/Vault.razor:1 | — | Control responds without unhandled error. | — |
| `inv-vault-vault-save-L390` | /vault | Save | button | src/TIKR.Web/Components/Pages/Vault.razor:390 | API | Data saved and UI reflects change. | — |
| `inv-vault-vault-save-as-voice-note-L273` | /vault | Save as Voice Note | button | src/TIKR.Web/Components/Pages/Vault.razor:273 | API | Data saved and UI reflects change. | — |
| `inv-vault-vault-save-changes-L327` | /vault | Save Changes | button | src/TIKR.Web/Components/Pages/Vault.razor:327 | API | Data saved and UI reflects change. | — |
| `inv-vault-vault-text-field-L272` | /vault | Text field | input | src/TIKR.Web/Components/Pages/Vault.razor:272 | — | Value binds and persists on save. | — |
| `inv-vault-vault-text-field-L345` | /vault | Text field | input | src/TIKR.Web/Components/Pages/Vault.razor:345 | — | Value binds and persists on save. | — |
| `inv-vault-vault-text-field-L349` | /vault | Text field | input | src/TIKR.Web/Components/Pages/Vault.razor:349 | — | Value binds and persists on save. | — |
| `inv-vault-vault-text-field-L353` | /vault | Text field | input | src/TIKR.Web/Components/Pages/Vault.razor:353 | — | Value binds and persists on save. | — |
| `inv-vault-vault-text-field-L357` | /vault | Text field | input | src/TIKR.Web/Components/Pages/Vault.razor:357 | — | Value binds and persists on save. | — |
| `inv-vault-vault-text-field-L361` | /vault | Text field | input | src/TIKR.Web/Components/Pages/Vault.razor:361 | — | Value binds and persists on save. | — |
| `inv-vault-vault-text-field-L366` | /vault | Text field | input | src/TIKR.Web/Components/Pages/Vault.razor:366 | — | Value binds and persists on save. | — |
| `inv-vault-vault-text-field-L370` | /vault | Text field | input | src/TIKR.Web/Components/Pages/Vault.razor:370 | — | Value binds and persists on save. | — |
| `inv-vault-vault-text-field-L375` | /vault | Text field | input | src/TIKR.Web/Components/Pages/Vault.razor:375 | — | Value binds and persists on save. | — |
| `inv-prod-account-route-auth` | account | /account loads with auth; secrets not ex | route | src/TIKR.Web/Components/Pages/Account.razor:3 Authorize; Settings keys cards | Auth, JWT | No HTTP 500; unauthenticated users blocked from keys. | RB-020 |
| `inv-assistant-chat-stream` | assistant | Assistant chat (SfAIAssistView) | assistant-chat | src/TIKR.Web/Components/Pages/Assistant.razor:84 | Ollama, API, SYNCFUSION_LICENSE_KEY | User sees thinking/progress immediately; full answer streams | RB-003 |
| `inv-prod-assistant-grok-fallback-on-timeout` | assistant | Assistant: Grok fallback on timeout (not | assistant-fallback | src/TIKR.Web/Components/Pages/Assistant.razor:496-503 | GROK_API_KEY, Ollama | Grok path invoked after local timeout. | RB-006 |
| `inv-prod-assistant-prompt-reset` | assistant | Assistant prompt box resets to default a | assistant-input | src/TIKR.Web/Components/Pages/Assistant.razor:87 Prompt=... | SfAIAssistView | Input stays cleared or retains typed text — not default plac | RB-007 |
| `inv-prod-assistant-timeout-response-stopped` | assistant | Assistant: timeout ends as Response stop | assistant-timeout | src/TIKR.Web/Components/Pages/Assistant.razor:524-529; TikrApiClient HttpClient default 100s | Ollama, API, HttpClient.Timeout | Retry/help text — not 'Response stopped.' from OperationCanc | RB-006 |
| `inv-prod-calendar-all-day-midnight` | calendar | Deadlines render midnight–1AM not all-da | calendar-display | src/TIKR.Web/Components/Pages/Calendar.razor:81-87 | SfSchedule | All-day events without 12–1AM slot span. | RB-010 |
| `inv-prod-calendar-create-deadline-ux` | calendar | Create deadline appears enabled until te | button | src/TIKR.Web/Components/Pages/Calendar.razor:53-56 | Ollama | Button visibly disabled OR helper text before typing. | RB-009 |
| `inv-prod-calendar-recurring-roll` | calendar | Recurring deadlines do not roll forward | calendar-logic | src/TIKR.Infrastructure (recurrence seed/update) | API, EF Core | Next instance appears after due date passes. | RB-011 |
| `inv-prod-documents-row-click-select` | documents | Document grid row click selects / previe | grid-row | src/TIKR.Web/Components/Pages/Documents.razor:242-245 CheckboxOnly | SfGrid | Row click updates preview without checkbox-only. | RB-012 |
| `inv-prod-documents-upload-utc-display` | documents | Upload times shown as local not UTC | display-format | src/TIKR.Web/Components/Pages/Documents.razor (grid date column) | — | Displayed time matches local offset. | RB-013 |
| `inv-prod-a11y-overdue-contrast` | home | Dashboard Overdue pill contrast ≥4.5:1 | accessibility | src/TIKR.Web/Components/Pages/Home.razor:73; priority-overdue CSS | — | Contrast ratio ≥ 4.5:1 (prod walkthrough: 3.06:1). | RB-022 |
| `inv-prod-home-add-requirement-nav-only` | home | Add requirement navigates only (no creat | button | src/TIKR.Web/Components/Pages/Home.razor:87 | — | Create dialog on Requirements after click. | RB-024 |
| `inv-prod-home-upload-document-picker` | home | Dashboard Upload document opens file pic | button | src/TIKR.Web/Components/Pages/Home.razor:89 | — | File input focused or chooser opens. | RB-016 |
| `inv-prod-page-help-click` | home | Page help (i) click opens help (not hove | help | src/TIKR.Web/Components/Shared/PageHelp.razor:4-7 | SfTooltip | Dialog/drawer on click; not tooltip-only. | RB-015 |
| `inv-prod-user-guide-reopen` | home | User guide opens on repeated clicks | button | src/TIKR.Web/Services/ClerkUserGuideService.cs:10; ClerkUserGuideDialog.razor:49 | — | Dialog visible after second Open user guide click. | RB-014 |
| `inv-layout-clerktoasthost-undo-L10` | layout | Undo | button | src/TIKR.Web/Components/Shared/ClerkToastHost.razor:10 | — | Control responds without unhandled error. | — |
| `inv-layout-clerkuserguidedialog-text-field-L8` | layout | Text field | input | src/TIKR.Web/Components/Shared/ClerkUserGuideDialog.razor:8 | — | Value binds and persists on save. | — |
| `inv-layout-confirmdeletedialog-cancel-L10` | layout | Cancel | button | src/TIKR.Web/Components/Shared/ConfirmDeleteDialog.razor:10 | — | Control responds without unhandled error. | — |
| `inv-layout-confirmdeletedialog-delete-L11` | layout | Delete | button | src/TIKR.Web/Components/Shared/ConfirmDeleteDialog.razor:11 | — | Record removed or confirm dialog shown. | — |
| `inv-layout-documentworkspacedialog-close-workspace-L52` | layout | Close workspace | button | src/TIKR.Web/Components/Shared/DocumentWorkspaceDialog.razor:52 | — | Control responds without unhandled error. | — |
| `inv-layout-documentworkspacedialog-delete-L70` | layout | Delete | button | src/TIKR.Web/Components/Shared/DocumentWorkspaceDialog.razor:70 | — | Record removed or confirm dialog shown. | — |
| `inv-layout-documentworkspacedialog-open-in-document-library-L185` | layout | Open in Document Library | button | src/TIKR.Web/Components/Shared/DocumentWorkspaceDialog.razor:185 | — | URL changes or target page loads. | — |
| `inv-layout-mainlayout-account-account-L57` | layout | Account | link-nav | src/TIKR.Web/Components/Layout/MainLayout.razor:57 | — | Target route loads with expected heading. | — |
| `inv-layout-mainlayout-ai-assistant-assistant-L38` | layout | AI Assistant | link-nav | src/TIKR.Web/Components/Layout/MainLayout.razor:38 | — | Target route loads with expected heading. | — |
| `inv-layout-mainlayout-calendar-calendar-L35` | layout | Calendar | link-nav | src/TIKR.Web/Components/Layout/MainLayout.razor:35 | — | Target route loads with expected heading. | — |
| `inv-layout-mainlayout-dashboard-control-L34` | layout | Dashboard | link-nav | src/TIKR.Web/Components/Layout/MainLayout.razor:34 | — | Target route loads with expected heading. | — |
| `inv-layout-mainlayout-documents-documents-L37` | layout | Documents | link-nav | src/TIKR.Web/Components/Layout/MainLayout.razor:37 | — | Target route loads with expected heading. | — |
| `inv-layout-mainlayout-hide-menu-L23` | layout | « Hide menu | button | src/TIKR.Web/Components/Layout/MainLayout.razor:23 | — | Control responds without unhandled error. | — |
| `inv-layout-mainlayout-knowledge-vault-vault-L39` | layout | Knowledge Vault | link-nav | src/TIKR.Web/Components/Layout/MainLayout.razor:39 | — | Target route loads with expected heading. | — |
| `inv-layout-mainlayout-menu-L66` | layout | ☰ Menu | button | src/TIKR.Web/Components/Layout/MainLayout.razor:66 | — | Control responds without unhandled error. | — |
| `inv-layout-mainlayout-requirements-requirements-L36` | layout | Requirements | link-nav | src/TIKR.Web/Components/Layout/MainLayout.razor:36 | — | Target route loads with expected heading. | — |
| `inv-layout-mainlayout-settings-settings-L40` | layout | Settings | link-nav | src/TIKR.Web/Components/Layout/MainLayout.razor:40 | — | Target route loads with expected heading. | — |
| `inv-layout-mainlayout-sign-out-L56` | layout | Sign out | button | src/TIKR.Web/Components/Layout/MainLayout.razor:56 | — | Control responds without unhandled error. | — |
| `inv-layout-mainlayout-tools-tour-demo-clerk-tools-L41` | layout | Tools tour | link-nav | src/TIKR.Web/Components/Layout/MainLayout.razor:41 | — | Target route loads with expected heading. | — |
| `inv-layout-mainlayout-try-again-L92` | layout | Try again | button | src/TIKR.Web/Components/Layout/MainLayout.razor:92 | — | Control responds without unhandled error. | RB-004 |
| `inv-layout-mainlayout-users-settings-users-L45` | layout | Users | link-nav | src/TIKR.Web/Components/Layout/MainLayout.razor:45 | — | Target route loads with expected heading. | — |
| `inv-layout-pagehelp-help-for-this-page-L14` | layout | Help for this page | button | src/TIKR.Web/Components/Shared/PageHelp.razor:14 | — | Control responds without unhandled error. | — |
| `inv-layout-pagehelp-help-for-this-page-L5` | layout | Help for this page | button | src/TIKR.Web/Components/Shared/PageHelp.razor:5 | — | Control responds without unhandled error. | — |
| `inv-layout-pagetourbutton-tour-this-page-L4` | layout | Tour this page | button | src/TIKR.Web/Components/Shared/PageTourButton.razor:4 | — | Control responds without unhandled error. | — |
| `inv-layout-reconnectmodal-resume-L27` | layout | Resume | button | src/TIKR.Web/Components/Layout/ReconnectModal.razor:27 | — | Control responds without unhandled error. | — |
| `inv-layout-reconnectmodal-retry-L18` | layout | Retry | button | src/TIKR.Web/Components/Layout/ReconnectModal.razor:18 | — | Control responds without unhandled error. | — |
| `inv-prod-guide-ask-advanced-button` | layout | User guide cites nonexistent Ask Advance | help-content | src/TIKR.Web/wwwroot/help/clerk-user-guide.md:41; ClerkTourCatalog.cs:84 | — | No 'Ask Advanced' affordance promised in guide/tour. | RB-008 |
| `inv-prod-grid-filter-aria` | requirements | Grid filter inputs accessible name | accessibility | Syncfusion SfGrid filter bar | SfGrid | No aria-label _Filter; human-readable column names. | RB-023 |
| `inv-prod-req-print-council-packet` | requirements | Print council packet prints packet not e | button | src/TIKR.Web/Components/Pages/Requirements.razor (Print council packet) | — | Print job targets packet content, not full SPA chrome. | RB-018 |
| `inv-prod-settings-duplicate-tour` | settings | Two Settings tour entry points do not du | button | Settings.razor:28 PageTourButton; :311-312 tools vs replay | ClerkTourService | Page tour ≠ tools tour ≠ replay tour. | RB-019 |
| `inv-prod-settings-reindex-control` | settings | Settings embedding reindex control visib | button | src/TIKR.Web/Components/Pages/Settings.razor:425 | API, Ollama | Reindex embeddings button visible and enabled when connected | — |
| `inv-prod-settings-status-contradiction` | settings | Town helper status not contradictory | status-display | src/TIKR.Web/Components/Pages/Settings.razor status cards | Ollama, API | Cannot show Ready and Ollama offline together. | RB-021 |
| `inv-vault-add-howto` | vault | Add how-to / tribal entry (missing contr | button | src/TIKR.Web/Components/Pages/Vault.razor:— | API | New entry dialog or inline editor opens. | RB-005 |

## Regenerate

```bash
python3 scripts/generate-functions-inventory.py
```
