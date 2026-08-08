# Scrap the Dead — App Store Metadata Pack

Status: ready to paste, except for the explicitly marked owner decisions and device screenshots.

## Identity

| Field | Value |
| --- | --- |
| App name | `Scrap the Dead` |
| Subtitle | `Zombie Cars: Crush & Upgrade` |
| Bundle ID | `com.pixicorp.scrapthedead` |
| Primary language | English (U.S.) |
| Price | Free |
| Primary category | Games |
| Game subcategory 1 | Action |
| Game subcategory 2 | Racing |
| Support URL | `https://sites.google.com/view/scrap-the-dead/support` |
| Privacy Policy URL | `https://sites.google.com/view/scrap-the-dead/privacy-policy` |
| Copyright | `2026 PixiCorp` |

Apple currently limits the app name and subtitle to 30 characters, promotional text to 170 characters, description to 4,000 characters, and keywords to 100 bytes. The values below fit those limits. Sources: [App information](https://developer.apple.com/help/app-store-connect/reference/app-information/app-information/) and [Platform version information](https://developer.apple.com/help/app-store-connect/reference/app-information/platform-version-information/).

## Promotional text

> Crush zombie hordes, collect scrap, unlock brutal vehicles, and build the ultimate survival machine—one fast run at a time.

## Description

> The dead own the road. Build a machine that can take it back.
>
> Scrap the Dead is a fast, one-touch action driving game about crushing zombie hordes, collecting scrap, and turning a battered vehicle into the ultimate survival ride.
>
> Pick a vehicle, enter the infected zone, and steer through the horde. Every zombie you crush adds to your score and scrap haul. Return to the safehouse to unlock new vehicles, buy brutal attachments, and tune your build for the next run.
>
> FEATURES
>
> • Simple one-touch steering designed for quick runs
> • Satisfying zombie-crushing action with stylized, blood-free visuals
> • Multiple vehicles with different strengths and handling
> • A garage full of plows, armor, blades, guards, and performance upgrades
> • Scrap-based progression: play, upgrade, and push farther
> • Mayhem chains and score multipliers for aggressive driving
> • Game Center leaderboard for lifetime zombie kills
> • Optional rewarded ads to double the scrap earned in a run
> • Optional Ad-Free Rewards purchase: claim rewarded bonuses instantly without watching ads
> • Cloud-backed progression linked to Game Center
>
> No forced ads. If a rewarded ad is unavailable, the game remains fully playable and the normal mission reward can still be collected.
>
> Choose your ride. Build your crusher. Scrap the dead.

## Keywords

Paste as one comma-separated string:

`zombie,car,crusher,driving,upgrade,garage,survival,arcade,action,racing,horde`

Do not repeat `Scrap`, `Dead`, or `PixiCorp`; Apple already indexes the app name and company name.

## Age rating answer sheet

Use the following truthful answers for the current content. Apple calculates the final regional rating.

| App Store Connect topic | Answer |
| --- | --- |
| Made for Kids | No |
| Parental controls / age assurance | None |
| Unrestricted web access | No; only fixed Privacy Policy and Support links open externally |
| User-generated content | No |
| Messaging or chat | No |
| Social media | No |
| Advertising | Yes |
| Profanity / crude humor | None |
| Horror or fear themes | Infrequent/Mild at most; stylized zombies only |
| Cartoon or fantasy violence | Frequent, because crushing zombies is the core loop |
| Realistic violence | None |
| Prolonged graphic or sadistic violence | None |
| Guns or other weapons | Frequent; vehicles can use blades, plows, spikes, and rams |
| Gambling / simulated gambling / loot boxes | None |
| Contests | None; the app has no time-limited Game Center Challenge, tournament, prize, or contest. The permanent leaderboard only compares lifetime scores. |

Apple's current definitions place frequent cartoon/fantasy violence and frequent weapons in the 13+ band on the newest rating system; regional/older-OS labels may differ. Source: [Age ratings values and definitions](https://developer.apple.com/help/app-store-connect/reference/app-information/age-ratings-values-and-definitions/).

## App Review notes

> Scrap the Dead is a free single-player action driving game. It does not require a custom username or password. On iOS, Game Center authentication occurs through Apple's native flow and is used to submit lifetime zombie kills and identify the player's Unity Cloud Save account.
>
> Rewarded ad test path: Start a mission from the garage, complete or fail the run with a positive scrap reward, then tap “DOUBLE [amount] SCRAP” on the result screen. The bonus is granted only after the rewarded-ad completion callback. If an ad is unavailable or dismissed early, the base reward remains collectible and gameplay is not blocked.
>
> In-app purchase test path: Open the gear icon in the garage, find “AD-FREE REWARDS,” and tap the displayed price. Product ID: com.pixicorp.scrapthedead.iap.adfreerewards. This non-consumable does not remove reward choices; it lets the owner claim those optional bonuses instantly without watching an ad. “RESTORE” is next to the purchase button.
>
> Game Center test path: Open Settings and tap “LEADERBOARD.” The leaderboard records all-time lifetime zombie kills. Leaderboard ID: com.pixicorp.scrapthedead.leaderboard.lifetimekills.
>
> Account/data deletion path: Open Settings, choose “DELETE DATA,” read the permanent-deletion explanation, then confirm “DELETE PERMANENTLY.” This removes the Unity Authentication account, Unity Cloud Save progression, and the local progression cache. Game Center leaderboard records and App Store purchases remain managed by Apple and are not refunded by this action.
>
> Privacy Policy and Support links are also available in Settings. No CrazyGames branding, links, or SDK behavior is enabled in the iOS build.

## IAP review submission

| Field | Value |
| --- | --- |
| Reference name | `Ad-Free Rewards` |
| Product ID | `com.pixicorp.scrapthedead.iap.adfreerewards` |
| Type | Non-Consumable |
| Display name | `Ad-Free Rewards` |
| Description | `Claim rewarded bonuses without watching ads.` |
| Review screenshot | Landscape device screenshot with Settings open and the `AD-FREE REWARDS` row plus price button visible |

The screenshot must come from the final device UI; do not mock or composite it.

## Screenshot capture list

Capture the same five story beats on both required device families after the final UI pass:

1. Garage hero shot with a selected vehicle and visible scrap balance.
2. Vehicle/attachment upgrade screen with a meaningful purchase choice.
3. Gameplay shot showing a vehicle crushing a dense zombie group.
4. High-Mayhem gameplay shot with HUD, chain, and multiplier readable.
5. Mission result showing earned scrap and the explicit Double Scrap button.

Keep all shots in landscape, use actual gameplay, and ensure CrazyGames branding is absent. Do not show test-ad labels, debug overlays, personal Game Center names, or purchase confirmation sheets.

## Export compliance recommendation

The project uses HTTPS networking through AdMob and Unity Gaming Services and does not implement proprietary cryptography. Do not hard-code an exemption until the archive's frameworks are inspected and App Store Connect's live questionnaire is answered. Apple says apps limited to encryption within Apple's operating system need no documentation, while apps incorporating non-OS standard algorithms can require additional declarations. Sources: [Overview of export compliance](https://developer.apple.com/help/app-store-connect/manage-app-information/overview-of-export-compliance/) and [Export compliance documentation](https://developer.apple.com/help/app-store-connect/reference/app-information/export-compliance-documentation-for-encryption/).

## Still requires the project owner

- Choose the DSA trader/non-trader status and provide any identity details Apple requests.
- Paste metadata into App Store Connect.
- Capture final screenshots on the approved build.
- Upload the IAP review screenshot and submit the final App Review notes.
