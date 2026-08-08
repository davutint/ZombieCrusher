# Scrap the Dead — App Privacy Response Draft

This is a conservative, implementation-based answer sheet for App Store Connect. It is not a substitute for the privacy report generated from the final Xcode archive. Reconcile this draft with that report immediately before publishing the answers.

Apple requires disclosures to include the behavior of third-party SDKs. Google likewise states that the Mobile Ads SDK may collect IP-derived coarse location, crash logs, performance data, device identifiers, advertising data, and product interactions. Sources: [Apple — Manage app privacy](https://developer.apple.com/help/app-store-connect/manage-app-information/manage-app-privacy) and [Google Mobile Ads — App Store data disclosure](https://developers.google.com/admob/ios/privacy/data-disclosure).

## Top-level answer

**Do you or your third-party partners collect data from this app?** Yes.

## Data types to select

| App Privacy data type | Collected by / reason | Linked to user | Used for tracking | Purposes |
| --- | --- | --- | --- | --- |
| Coarse Location | AdMob and UGS can derive general location from network/IP data | Yes for the conservative UGS declaration | No under the no-ATT release policy | App Functionality; Third-Party Advertising; Analytics |
| User ID | Unity Authentication player ID and linked Apple Game Center identity | Yes | No | App Functionality; Analytics where declared by Unity |
| Gameplay Content | Cloud Save progression: scrap, ownership, loadouts, selected vehicle, lifetime kills | Yes | No | App Functionality |
| Device ID | Google Mobile Ads device/app identifiers | Yes in the SDK's conservative native manifest | No under the no-ATT release policy; re-check before submission | Third-Party Advertising; Analytics |
| Product Interaction | Ad impressions/video views and related ad interactions | Yes in the SDK's conservative native manifest | No under the no-ATT release policy | App Functionality; Third-Party Advertising; Analytics |
| Advertising Data | Ads shown and advertising performance data | Yes in the SDK's conservative native manifest | No under the no-ATT release policy | Third-Party Advertising; Analytics |
| Crash Data | AdMob and Unity Authentication SDK diagnostics | No in the supplied Unity manifest; verify archive | No | App Functionality; Analytics |
| Performance Data | Google Mobile Ads and UMP performance diagnostics | No in the installed native SDK manifests | No | App Functionality; Third-Party Advertising; Analytics |
| Other Diagnostic Data | Google Mobile Ads technical diagnostics | No in the installed native SDK manifest | No | Third-Party Advertising; Analytics |
| Other Usage Data | Unity Authentication service usage data | Yes | No | App Functionality; Analytics |

The game's own progression format does not create or upload an additional hardware-derived device identifier. Cloud concurrency uses Unity Cloud Save write locks plus save timestamps; the `Device ID` row above comes from the Google Mobile Ads SDK disclosure and must still be reconciled with the final archive.

## Package privacy-manifest evidence in this project

The installed package manifests currently report:

- Unity Authentication `3.7.3`: tracking `false`; User ID, Coarse Location, Other Usage Data, and Crash Data, plus broader optional contact/support fields in the package manifest.
- Unity Cloud Save `3.4.1`: tracking `false`; User ID and Coarse Location.
- Unity Services Core: tracking `false`; UserDefaults required-reason API `CA92.1`.
- Unity IAP `5.4.2`: tracking `false`; no collected data types; File Timestamp reason `C617.1` and Disk Space reason `E174.1`.
- Google Mobile Ads `11.3.0`: the resolved iOS framework manifest is present in the development Xcode export and declares Required Reason API usage plus coarse location, diagnostics, performance, advertising data, product interaction, and device ID. The manifest conservatively marks its Device ID capability as tracking-capable.
- Google UMP: the resolved framework manifest declares UserDefaults plus coarse location, performance data, and product interaction for app functionality; tracking is not declared.

The Authentication package manifest lists Email Address, Other User Contact Info, and Customer Support because the package supports flows that can collect them. Scrap the Dead's code uses Game Center/anonymous authentication only and does not ask for an email, phone number, address, or custom profile. Check the final Xcode privacy report before deciding whether App Store Connect still surfaces those package-wide entries.

## Tracking / ATT release decision

The first release uses **no ATT prompt and no cross-app tracking**. Before initializing Google Mobile Ads, the code disables publisher first-party ID and selects Google's non-personalized publisher privacy treatment. `NSUserTrackingUsageDescription` remains empty, and the iOS release guard rejects a configuration that silently adds it. UMP still updates consent information, shows required consent/privacy-options forms, and gates ad requests.

The Google Mobile Ads framework's static privacy manifest describes the SDK's full tracking-capable Device ID behavior even though the release configuration disables IDFA-dependent/personalized treatment. Immediately before completing App Store Privacy, reconcile the form with the final SDK version, AdMob account configuration, runtime test, and Xcode privacy report. If any component performs cross-app tracking, stop and add a complete ATT flow instead of changing only the form answer. Sources: [Google Mobile Ads — Privacy strategies](https://developers.google.com/admob/ios/privacy/strategies) and [Google Mobile Ads — App Store data disclosure](https://developers.google.com/admob/ios/privacy/data-disclosure).

## Privacy URLs

- Privacy Policy: `https://sites.google.com/view/scrap-the-dead/privacy-policy`
- Support: `https://sites.google.com/view/scrap-the-dead/support`
- User Privacy Choices URL: leave blank unless a public web-based choices page is added. The in-app Ad Privacy button already opens Google's required UMP privacy-options form when available.

## Account deletion disclosure

The iOS Settings screen includes a permanent deletion flow. After a second confirmation it deletes:

- the player's Unity Cloud Save progression key;
- the Unity Authentication account;
- the local progression cache and older local save keys.

It does not claim to delete Apple-managed Game Center leaderboard records or App Store purchase history. Apple requires in-app deletion even for automatically generated accounts; Unity states that `DeleteAccountAsync()` removes only the Authentication account, so the Cloud Save record is explicitly deleted first. Sources: [Apple — Offering account deletion](https://developer.apple.com/support/offering-account-deletion-in-your-app/) and [Unity — Delete accounts](https://docs.unity.com/en-us/authentication/delete-accounts).

## Final archive checks

- Generate and inspect Xcode's privacy report for every embedded SDK.
- Confirm all expected `PrivacyInfo.xcprivacy` files are included in the archive.
- Confirm Required Reason API declarations have no unresolved warnings.
- Re-check Google Mobile Ads disclosure documentation because Google updates it as SDK behavior changes.
- Confirm the no-ATT policy still matches UMP, the AdMob serving mode, App Privacy answers, and the public Privacy Policy.
