Setup-TIKR.exe is not built on macOS.

1. Ensure publish/TIKR-Deploy exists (run from repo root):
     SKIP_TESTS=1 ./scripts/package-thumb-drive.sh

2. On a Windows PC with Inno Setup 6, from installer\:
     ISCC.exe /DMySourceDir="<full-path-to>\publish\TIKR-Deploy" /DMyAppVersion="1.0.1" .\tikr-setup.iss

3. This folder will then contain Setup-TIKR.exe.

See: ../README.md and ../../scripts/build-windows-setup-notes.sh
Clerk: ../../docs/clerk-windows-install.md
Smoke: ../../docs/clerk-windows-smoke.md
