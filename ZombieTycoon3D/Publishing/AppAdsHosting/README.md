# Scrap the Dead app-ads.txt hosting package

Google Sites cannot expose a plain file at the hostname root, but AdMob's crawler requires `/app-ads.txt` at the root of the developer website. This ready-to-deploy Firebase Hosting package contains the correct file and redirects the hosting root to the public Scrap the Dead Google Site.

Nothing has been deployed and no Firebase project has been created.

## Owner steps when external account work begins

1. Create or choose a Firebase project used only for static Hosting. Firebase Analytics is not required.
2. Install/sign in to the Firebase CLI.
3. In this directory run `firebase use --add` and select that project.
4. Run `firebase deploy --only hosting`.
5. Verify `https://PROJECT_ID.web.app/app-ads.txt` displays exactly:

   `google.com, pub-6131087568871639, DIRECT, f08c47fec0942fa0`

6. Put `https://PROJECT_ID.web.app` in the App Store listing's Marketing URL / developer website field.
7. Wait at least 24 hours, then check the app-ads.txt status in AdMob.

Do not put the Google Sites URL in the Marketing URL field if app-ads.txt verification is required; the crawler would instead look for `https://sites.google.com/app-ads.txt`, which this project cannot control.

Official setup reference: https://developers.google.com/admob/ios/app-ads
