# Scrap the Dead — App Store Connect Formları

Bu belge App Store Connect'teki **gerçek sekmelere göre ayrılmıştır**. Bir sekmenin altında başka sekmeye ait alan yoktur.

Başlangıç yolu:

`App Store Connect > Apps > Scrap the Dead`

## Ekran sırası

| Sıra | Sol menüde açılacak sekme | Bu sekmede yapılacak iş |
| --- | --- | --- |
| 1 | `General > App Information` | İsim, kategori, içerik hakları ve yaş derecelendirmesi |
| 2 | `General > App Privacy` | Privacy URL ve veri toplama beyanları |
| 3 | `Monetization > Pricing and Availability` | Ücretsiz fiyat ve dağıtım ülkeleri |
| 4 | `Monetization > In-App Purchases` | Ad-Free Rewards ürünü |
| 5 | `Services > Game Center` | Lifetime Zombie Kills leaderboard |
| 6 | `iOS App > 1.0 Prepare for Submission` | Açıklama, Support URL, keywords, screenshots ve App Review bilgileri |

Her sekmeyi bitirince sağ üstteki `Save` düğmesine bas. Şimdilik build yükleme, build seçme veya `Submit for Review` yapma.

---

# SEKME 1 — General > App Information

Menü yolu:

`Apps > Scrap the Dead > General > App Information`

Bu sekmede yalnızca aşağıdaki alanları doldur.

## Localizable Information — English (U.S.)

| Ekrandaki alan | Girilecek değer |
| --- | --- |
| Name | `Scrap the Dead` |
| Subtitle | `Zombie Cars: Crush & Upgrade` |

## General Information

| Ekrandaki alan | Girilecek/seçilecek değer |
| --- | --- |
| Bundle ID | `com.pixicorp.scrapthedead` — sadece doğrula, değiştirme |
| SKU | Mevcut değer neyse bırak |
| Primary Language | `English (U.S.)` |
| Primary Category | `Games` |
| Subcategory 1 | `Action` |
| Subcategory 2 | `Racing` |
| Secondary Category | `None` / boş |

## Content Rights

`Content Rights > Set Up` veya `Edit` düğmesine bas.

| Soru | Cevap |
| --- | --- |
| Does your app contain, show, or access third-party content? | `Yes` |
| Do you have the necessary rights? | `Yes` / onay kutusunu işaretle |

Oyun satın alınmış/lisanslı asset'ler ve reklam içeriği kullandığı için cevap `Yes` olmalı.

## License Agreement

- `Apple's Standard End User License Agreement` seçili kalsın.
- `Custom EULA` oluşturma.

## Made for Kids

- `Made for Kids` → `No`.
- Kids yaş aralığı seçme.

## Age Ratings

`Age Ratings > Set Up Age Ratings` düğmesine bas.

### In-App Controls

| Soru | Seçim |
| --- | --- |
| Parental Controls | `No` / `None` |
| Age Assurance | `No` / `None` |

### Capabilities

| Soru | Seçim |
| --- | --- |
| Unrestricted Web Access | `No` |
| User-Generated Content | `No` |
| Social Media | `No` |
| Social Media Disabled for Users Under 13 | `No` / uygulanamaz |
| Messaging and Chat | `No` |
| Advertising | `Yes` |

### Mature Themes

| Soru | Seçim |
| --- | --- |
| Profanity or Crude Humor | `None` |
| Horror or Fear Themes | `Infrequent/Mild` |
| Alcohol, Tobacco, or Drug Use or References | `None` |

### Medical or Wellness

| Soru | Seçim |
| --- | --- |
| Medical or Treatment Information | `None` |
| Health or Wellness Topics | `None` |

### Sexuality or Nudity

Bu bölümdeki bütün seçimler `None`:

- Mature or Suggestive Themes → `None`
- Sexual Content or Nudity → `None`
- Graphic Sexual Content and Nudity → `None`

### Violence

| Soru | Seçim |
| --- | --- |
| Cartoon or Fantasy Violence | `Frequent` |
| Realistic Violence | `None` |
| Prolonged Graphic or Sadistic Realistic Violence | `None` |
| Guns or Other Weapons | `Frequent` |

### Chance-Based Activities

| Soru | Seçim |
| --- | --- |
| Gambling | `None` |
| Simulated Gambling | `None` |
| Contests | `None` |
| Loot Boxes | `None` |

Leaderboard bulunuyor ancak süreli yarışma, Game Center Challenge, turnuva veya ödüllü contest bulunmuyor.

### Age Rating son ekranı

- `Override to Higher Age Rating` işaretleme.
- Apple'ın hesapladığı sonucu kabul et.
- `Done` seç.

## App Store Regulations and Permits

Bu bölüm aynı `App Information` sayfasının alt tarafındadır.

### Digital Services Act

- Mevcut uygulama durumu zaten `Trader` veya tamamlanmış görünüyorsa **dokunma**.
- Yalnızca bu uygulama için `Missing Compliance` uyarısı varsa → `Trader` / `This app is distributed by a trader` seç.
- `Labels and Markings URL` → boş.

## App Store Server Notifications

- Production Server URL → boş.
- Sandbox Server URL → boş.

Özel IAP sunucusu kullanılmıyor.

## Bu sekmenin sonu

Sağ üstten `Save` seç. Sonra sol menüden `App Privacy` sekmesine geç.

---

# SEKME 2 — General > App Privacy

Menü yolu:

`Apps > Scrap the Dead > General > App Privacy`

Bu sekmede yalnızca Privacy Policy ve veri toplama beyanları bulunur.

## Privacy Policy

`Privacy Policy > Edit` düğmesine bas.

| Ekrandaki alan | Girilecek değer |
| --- | --- |
| Privacy Policy URL | `https://sites.google.com/view/scrap-the-dead/privacy-policy` |
| User Privacy Choices URL | Boş |

`Save` seç.

## Data Collection

1. `Get Started` düğmesine bas.
2. `Yes, we collect data from this app` seç.
3. `Next` seç.

## İşaretlenecek Data Types

Yalnızca aşağıdakileri işaretle:

- `Location > Coarse Location`
- `User Content > Gameplay Content`
- `Identifiers > User ID`
- `Identifiers > Device ID`
- `Usage Data > Product Interaction`
- `Usage Data > Advertising Data`
- `Usage Data > Other Usage Data`
- `Diagnostics > Crash Data`
- `Diagnostics > Performance Data`
- `Diagnostics > Other Diagnostic Data`

Bunların dışındaki veri türlerini işaretleme.

## Her Data Type içinde verilecek cevaplar

Her veri türüne ayrı ayrı tıkla ve aşağıdaki satırı aynen uygula.

| Data Type | Purposes — işaretlenecekler | Linked to User | Used for Tracking |
| --- | --- | --- | --- |
| Coarse Location | `App Functionality`, `Third-Party Advertising`, `Analytics` | `Yes` | `No` |
| Gameplay Content | `App Functionality` | `Yes` | `No` |
| User ID | `App Functionality`, `Analytics` | `Yes` | `No` |
| Device ID | `Third-Party Advertising`, `Analytics` | `Yes` | `No` |
| Product Interaction | `App Functionality`, `Third-Party Advertising`, `Analytics` | `Yes` | `No` |
| Advertising Data | `Third-Party Advertising`, `Analytics` | `Yes` | `No` |
| Other Usage Data | `App Functionality`, `Analytics` | `Yes` | `No` |
| Crash Data | `App Functionality`, `Analytics` | `No` | `No` |
| Performance Data | `App Functionality`, `Third-Party Advertising`, `Analytics` | `No` | `No` |
| Other Diagnostic Data | `Third-Party Advertising`, `Analytics` | `No` | `No` |

Her veri türünde `Save` seç.

## App Privacy sonu

1. Bütün veri türlerinin `Complete` olduğunu kontrol et.
2. Sağ üstte `Publish` seç.
3. Onay penceresinde tekrar `Publish` seç.

Bu düğme uygulamayı incelemeye göndermez; yalnızca privacy cevaplarını yayınlar.

Sonra sol menüden `Pricing and Availability` sekmesine geç.

---

# SEKME 3 — Monetization > Pricing and Availability

Menü yolu:

`Apps > Scrap the Dead > Monetization > Pricing and Availability`

Bu sekmede yalnızca uygulamanın fiyatı, ülkeleri ve dağıtım yöntemi ayarlanır.

## App Price

| Ekrandaki alan | Seçim |
| --- | --- |
| Price | `Free` / `0.00` |
| Price Schedule | Ek zamanlama oluşturma |
| Make Available for Pre-Order | Kapalı |

## Tax Category

- `App Store Software` veya mevcut varsayılan yazılım kategorisini seç.

## Availability

Burada yapacağın işlem yalnızca şu:

1. `All Countries or Regions` seç.
2. `China mainland` seçimini kaldır.
3. `Vietnam` seçimini kaldır.
4. `South Korea` dahil diğer bütün ülkeler açık kalsın.

Çin ve Vietnam oyun yayın lisansı istediği için şu anda kapatılıyor. Güney Kore için bu oyunda ek işlem gerekmiyor.

## Distribution Method

| Seçenek | Yapılacak işlem |
| --- | --- |
| Public — Available on the App Store | Seç |
| Private / Custom App | Seçme |
| Unlisted App | Seçme |
| Reduced price for educational institutions | Kapalı |

## Mac ve Vision Pro

- `Make this app available on Mac` → kapalı.
- `Make this app available on Apple Vision Pro` → kapalı.

## Bu sekmenin sonu

`Save` seç. Sonra sol menüden `In-App Purchases` sekmesine geç.

---

# SEKME 4 — Monetization > In-App Purchases

Menü yolu:

`Apps > Scrap the Dead > Monetization > In-App Purchases > Ad-Free Rewards`

Bu sekmede yalnızca `Ad-Free Rewards` satın alımı düzenlenir.

## Product Information

| Ekrandaki alan | Değer |
| --- | --- |
| Type | `Non-Consumable` |
| Reference Name | `Ad-Free Rewards` |
| Product ID | `com.pixicorp.scrapthedead.iap.adfreerewards` |

Product ID ve Type değiştirilemez; yalnızca doğrula.

## Price and Availability

| Ekrandaki alan | Seçim |
| --- | --- |
| Base Price | `4.99 USD` |
| Availability | China mainland ve Vietnam hariç diğer mağazalar |
| Start Date | Hemen / mevcut tarih |
| End Date | Boş |
| Tax Category | `Same as Parent App` / varsayılan |
| Family Sharing | Kapalı |
| Content Hosting | Kapalı |

## App Store Localization

`Add Localization` seç.

| Ekrandaki alan | Değer |
| --- | --- |
| Language | `English (U.S.)` |
| Display Name | `Ad-Free Rewards` |
| Description | `Claim rewarded bonuses without watching ads.` |

## Image

- Promotional Image → boş bırak.

## Review Information

### Review Screenshot

Şimdilik boş bırak. Final testten sonra Settings ekranında `AD-FREE REWARDS` satırı ve fiyat düğmesi görünen gerçek landscape screenshot yüklenecek.

### Review Notes

Şunu yapıştır:

> This non-consumable unlocks instant claiming of optional rewarded bonuses. It does not remove the reward choices: when the owner taps the Double Scrap reward, the bonus is granted immediately without showing an ad. To test, open the gear icon in the garage, find “AD-FREE REWARDS,” and tap the displayed price. The RESTORE button is next to the purchase button. Product ID: com.pixicorp.scrapthedead.iap.adfreerewards.

## Bu sekmenin sonu

`Save` seç. Şimdilik `Add for Review` veya `Submit for Review` seçme. Sonra `Game Center` sekmesine geç.

---

# SEKME 5 — Services > Game Center

Menü yolu:

`Apps > Scrap the Dead > Services > Game Center > Leaderboards > Lifetime Zombie Kills`

Bu sekmede yalnızca Game Center leaderboard ayarlanır.

## Leaderboard Information

| Ekrandaki alan | Değer |
| --- | --- |
| Type | `Classic` |
| Reference Name | `Lifetime Zombie Kills` |
| Leaderboard ID | `com.pixicorp.scrapthedead.leaderboard.lifetimekills` |
| Score Format | `Integer` |
| Score Submission Type | `Best Score` |
| Sort Order | `High to Low` |
| Score Range | Boş |

## Leaderboard Localization

| Ekrandaki alan | Değer |
| --- | --- |
| Language | `English (U.S.)` |
| Display Name | `Lifetime Zombie Kills` |
| Description | `Most zombies crushed across all runs.` |
| Singular Suffix | `zombie` |
| Plural Suffix | `zombies` |
| Image | Boş |

## Default Leaderboard

- Game Center ana sayfasında `Default Leaderboard` seçimi görünürse `Lifetime Zombie Kills` seç.

## Bu sekmenin sonu

`Save` seç. Şimdilik `Add for Review` veya `Submit for Review` seçme. Sonra `iOS App 1.0` sekmesine geç.

---

# SEKME 6 — iOS App > 1.0 Prepare for Submission

Menü yolu:

`Apps > Scrap the Dead > iOS App > 1.0 Prepare for Submission`

Bu sekmede mağazada müşterinin göreceği açıklamalar, Support URL, screenshot'lar ve Apple reviewer bilgileri bulunur.

Sağ üstteki dil `English (U.S.)` olmalı.

## App Previews and Screenshots

- App Preview video → boş.
- iPhone Screenshots → şimdilik boş.
- iPad Screenshots → şimdilik boş.

Finalde beş iPhone ve beş iPad landscape screenshot yüklenecek.

## Promotional Text

Şunu yapıştır:

> Crush zombie hordes, collect scrap, unlock brutal vehicles, and build the ultimate survival machine—one fast run at a time.

## Description

Şunu yapıştır:

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

Tek satır olarak yapıştır:

`zombie,car,crusher,driving,upgrade,garage,survival,arcade,action,racing,horde`

## URLs ve Version Information

| Ekrandaki alan | Değer |
| --- | --- |
| Support URL | `https://sites.google.com/view/scrap-the-dead/support` |
| Marketing URL | Boş |
| Version | `1.0.0` |
| Copyright | `2026 PixiCorp` |
| Routing App Coverage File | Boş |
| What's New in This Version | İlk sürümde boş/görünmez |

## Build

- Şimdilik build seçme.
- Final archive yüklendikten sonra build `1` seçilecek.

## Game Center

- `Game Center` kutusunu işaretle.

## App Review Information — Contact Information

| Ekrandaki alan | Değer |
| --- | --- |
| First Name | `[GERÇEK AD]` |
| Last Name | `[GERÇEK SOYAD]` |
| Phone Number | `[+90... GERÇEK TELEFON]` |
| Email | `davutinat@gmail.com` |

## App Review Information — Sign-in

- `Sign-in required` → işaretleme.
- Username → boş.
- Password → boş.

## App Review Information — Notes

Şunu yapıştır:

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

## App Review Information — Attachment

- Attachment → boş.

## Version Release

- `Manually release this version` → seç.
- Automatic release → seçme.
- Automatic, no earlier than → seçme.

## Bu sekmenin sonu

`Save` seç. Build ve screenshot eksik olduğu için şimdilik `Add for Review` yapma.

---

# FORM İŞİ BİTTİKTEN SONRA DUR

Yukarıdaki sekmeler tamamlandığında henüz App Review'a gönderme. Kalan işler:

1. Final UI ve oyun işlev testi.
2. Açık onayından sonra final Xcode build/archive.
3. iPhone ve iPad App Store screenshot'ları.
4. IAP Review Screenshot.
5. Final build'in App Store Connect'te seçilmesi.
6. App version, IAP ve leaderboard'un aynı review draft'ına eklenmesi.
7. Son kontrolden sonra `Submit for Review`.

# Export Compliance — yalnızca build yüklendikten sonra soru çıkarsa

| App Store Connect sorusu | Cevap |
| --- | --- |
| Does your app use encryption? | `Yes` — HTTPS/Apple OS şifrelemesi |
| Apple OS veya yalnızca HTTPS muafiyetlerinden biri geçerli mi? | `Yes` |
| Proprietary/non-standard encryption? | `No` |
| Standard encryption implemented in addition to Apple OS? | `No` |
| Uses non-exempt encryption? | `No` |

Final build `ITSAppUsesNonExemptEncryption = NO` üretecek. App Store Connect ayrıca belge istememelidir. Belge isterse gönderimi durdur.

# Resmî kaynaklar

- [Apple — App Information](https://developer.apple.com/help/app-store-connect/reference/app-information/app-information/)
- [Apple — Age Ratings](https://developer.apple.com/help/app-store-connect/manage-app-information/set-an-app-age-rating/)
- [Apple — App Privacy](https://developer.apple.com/help/app-store-connect/manage-app-information/manage-app-privacy)
- [Apple — Pricing and Availability](https://developer.apple.com/help/app-store-connect/reference/pricing-and-availability/app-pricing-and-availability)
- [Apple — In-App Purchases](https://developer.apple.com/help/app-store-connect/manage-submissions-to-app-review/submit-an-in-app-purchase/)
- [Apple — Game Center](https://developer.apple.com/help/app-store-connect/reference/game-center/leaderboards/)
- [Apple — DSA](https://developer.apple.com/help/app-store-connect/manage-compliance-information/manage-european-union-digital-services-act-trader-requirements/)
