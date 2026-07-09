# TIKR clerk user guide

Quick reference for town clerks. Everything runs on your Synology NAS unless you enable optional cloud AI (Grok).

## Dashboard

The **Dashboard** shows what needs attention today: statutory deadlines and open requirements, sorted by urgency. Open cards to see due dates and reasons. Use the sidebar to jump to Calendar, Requirements, or Documents. Press **?** for keyboard shortcuts (`g d` goes to Dashboard, `g r` to Requirements, and so on).

## Requirements and deadlines

**Requirements Manager** holds Colorado seed deadlines plus town-specific items.

- **Add requirement** — create a custom deadline or recurring task for your municipality.
- **Edit** — select a row, then edit title, due date, priority, or completion.
- **Show completed** — toggle to include finished work in the grid.
- **Export CSV** — spreadsheet for council packets or your own tracking.
- **Print / PDF exports** — council packet, agenda, compliance Excel, and meeting minutes when Document SDK is licensed (see Settings).
- **AI Scan file upload** — upload a PDF or Word file; TIKR extracts candidate requirements (review before saving).

Statutory dates also appear on the **Calendar** (read-only). Change due dates on Requirements, then refresh Calendar.

## Calendar

**Deadline Calendar** is a read-only schedule of due dates from the database. Switch **Month** or **Agenda** view. Use **Today** to jump to the current period. To change a deadline, go to **Requirements** and edit the item there.

## Document library

**Documents** stores ordinances, minutes, forms, and correspondence on the NAS.

- **Folder tree** — browse by category; AI may suggest folders on upload.
- **Upload** — drag files or use Browse; supported types include PDF and Office formats.
- **Full-text / Semantic search** — keyword search vs meaning-based search (needs embeddings).
- **Row checkbox** — select a document to preview in the right pane.
- **Context menu** — tag with AI, download, or delete (with confirmation).

## AI Assistant

**AI Assistant** answers questions in plain English using **local Ollama** on the NAS by default — your document text is not sent to the cloud for ordinary chat.

- Type a question and press **Send**.
- **Ask Advanced AI** uses the API path (Ollama first, optional **Grok** when enabled in `docker/.env`) for harder reasoning after you have sent at least one message.

AI can summarize, explain procedures, and help draft language; always verify statutory citations against official sources.

## Knowledge Vault

**Knowledge Vault** captures *how things really work* — passwords locations (not the passwords themselves), vendor contacts, and “hit by a bus” procedures. Use **Copy for New Clerk** to export a handoff document for succession planning.

## Settings and deployment

**Settings** shows clerk preferences (display theme) and read-only health: Ollama, Grok, Syncfusion licenses, and NAS storage.

Town name, AI hosts, and API keys are set in **`docker/.env`** on the Synology, not inside this screen. After changing `.env`, restart the API and Web containers.

Use **Show me around TIKR** to replay the visual walkthrough. Open this guide anytime from Settings or the Dashboard.

## Display theme

Choose **Light**, **Dark**, or **High contrast** in the sidebar or under Clerk preferences. The choice is saved in this browser only.

## Getting help

- **Page help** (circle icon on each page) — short tooltip for that screen.
- **User guide** (Settings) — this document, searchable.
- **Walkthrough** — highlights controls step by step.
