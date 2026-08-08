# ZombieTycoon3D — iOS / App Store Aksiyon Planı

> Oluşturulma tarihi: 7 Ağustos 2026
>
> Belge durumu: Aktif uygulama ve doğrulama takip dokümanı
>
> Kapsam: CrazyGames sürümünü koruyarak bağımsız bir iPhone/iPad sürümü hazırlamak
>
> Önemli: SDK/paket seçimi veya kurulumu, hesap bağlantısı, kayıt/kimlik mimarisi, Apple capability yaklaşımı, mağaza kimlikleri ve ürün davranışı gibi büyük kararlar proje sahibinin açık onayı olmadan uygulanmayacaktır.

## 1. Hedef

Mevcut CrazyGames/WebGL sürümünü güncellenebilir halde tutarken aynı oyun içeriğinden bağımsız bir iOS sürümü çıkarmak.

iOS sürümü:

- Ücretsiz olacak.
- iPhone ve iPad'i destekleyecek.
- Yatay çalışacak.
- İngilizce ve dünya genelinde yayınlanacak.
- Yalnızca ödüllü AdMob reklamı kullanacak.
- Oyuncu, el sonunda reklam izleyerek o elde kazandığı scrap'i ikiye katlayabilecek.
- Tek seferlik bir oyun içi satın alımla reklamsız/instant ödülleri açabilecek.
- Game Center'da ömür boyu öldürülen toplam zombi sayısına göre leaderboard kullanacak.
- iPhone ve iPad arasında ilerlemeyi bulut üzerinden geri getirecek.
- CrazyGames logosu, bağlantıları, SDK davranışları ve WebGL'e özel arayüz öğeleri içermeyecek.
- Mevcut oyunun içeriğini koruyacak; mobil joystick ve mobil okunabilirlik düzenlemeleri dışında oynanış değişmeyecek.

### 1.1 Çalışma biçimi ve sorumluluk ayrımı

`[CODEX]` görevleri proje kodu, Unity içi proje ayarları, sahne/profile düzeni, entegrasyon kodu, build guard, kaynak incelemesi ve derleme kontrolüdür.

`[KULLANICI]` görevleri Apple/Google/Unity hesaplarında oturum açmayı gerektiren panel işlemleri, SDK'nın üretici talimatına göre kurulması, sertifika/provisioning, mağaza sözleşmeleri, gerçek kimliklerin oluşturulması, yasal metinlerin yayınlanması ve fiziksel cihaz/TestFlight kontrolleridir. Sırası geldiğinde kullanıcıya tıklanacak ekran ve girilecek değer adım adım tarif edilecektir.

`[ORTAK TEST]` görevlerinde Codex build/kod tarafını hazırlar; kullanıcı imzalama, cihazdaki izin/giriş ekranı ve fiziksel davranışı doğrular.

Çalışma kuralları:

- Kullanıcı kurulumuna ait bir iş, özel native köprü veya geçici alternatifle atlanmayacak.
- Apple Game Center için yalnızca kullanıcının kurduğu resmî `Apple.Core` ve `Apple.GameKit` paketleri kullanılacak.
- Özel Objective-C köprü, `DllImport("__Internal")` Game Center katmanı veya özel PBX capability post-process eklenmeyecek.
- Game Center capability ve framework işlemleri resmî Apple Build Profile içindeki `Apple.GameKit` build step'ine bırakılacak.
- UGS'de `ForceLink` kullanılmayacak ve mevcut cloud hesabı sessizce başka hesaba taşınmayacak.
- Unity/Xcode build veya archive yalnızca proje sahibinin o build için verdiği açık onaydan sonra başlatılacak.
- Her büyük karar önce öneri olarak açıklanacak ve açık onaydan sonra uygulanacak.

### 1.2 İşaretlerin anlamı

- `[x]`: Kod/asset/ayar canlı projede uygulanmış ve kaynak düzeyinde doğrulanmış.
- `[ ]`: Henüz yapılmamış veya gerekli dış panel/gerçek cihaz/build doğrulaması tamamlanmamış.
- Bir kod maddesinin `[x]` olması App Store/gerçek cihaz testinin geçtiği anlamına gelmez; test maddeleri ayrıca kapatılır.

## 2. Kesinleşen ürün kararları

| Konu | Karar |
|---|---|
| CrazyGames | Korunacak ve gelecekte güncelleme alabilecek. |
| Platform ilişkisi | CrazyGames ve iOS birbirinden bağımsız olacak. Platformlar arası kayıt aktarımı olmayacak. |
| iOS fiyatı | Ücretsiz. |
| Reklam ağı | AdMob. |
| Reklam türü | Yalnızca ödüllü reklam. Banner ve interstitial yok. |
| iOS reklam noktası | El/görev sonunda `DOUBLE SCRAP`: o elde kazanılan scrap kadar ek scrap. |
| Garaj ödülü | Mevcut sabit `SALVAGE DROP +100` iOS sürümünde kullanılmayacak. CrazyGames davranışına bu kapsamda dokunulmayacak. |
| Reklam satın alımı | Satın alım sahibi aynı düğmeye bastığında reklam gösterilmeden 2x ödül alacak. |
| İlk IAP | Tek seferlik, non-consumable reklamsız/instant ödül hakkı. |
| Gelecekteki IAP | Araç satın alımları daha sonra değerlendirilecek; ilk sürüm kapsamında değil. |
| Game Center | Ömür boyu öldürülen toplam zombi sayısına göre all-time leaderboard. |
| Achievement | İlk sürümde yok. |
| Kayıt | iOS cihazları arasında geri yüklenebilir bulut kayıt isteniyor. |
| Kontrol | Parmağın ilk değdiği yerde beliren sade dinamik joystick. Joystick yönü kameraya göre hedef sürüş yönünü, uzaklığı ileri hız miktarını belirler. Mobilde geri vites ve ayrı gaz/fren düğmesi yok. |
| Cihazlar | iPhone + iPad. |
| Yön | Landscape Left ve Landscape Right. Portre yok. |
| Dil / ülke | İngilizce, tüm ülkeler. |
| Hedef kitle | Çocuklara yönelik değil; genel kitle. Stilize/çizgi film zombi şiddeti, kan/gore hedeflenmiyor. |
| Analytics | İlk sürümde Firebase veya ayrı analytics SDK'sı yok. |
| Web sitesi | Gizlilik politikası ve destek sayfası Google Sites ile hazırlanacak. |
| Uygulama adı | `Scrap the Dead`; 7 Ağustos 2026 tarihinde proje sahibi tarafından onaylandı. |
| App Store subtitle | `Zombie Cars: Crush & Upgrade`; 28/30 karakter. |
| App Store ikonu | Yalnızca oyunda gerçekten seçilip kullanılan araçlardan birini gösterecek; jenerik veya oyunda bulunmayan araç kullanılmayacak. Seçilen aday, oyundaki mavi-beyaz şeritli zırhlı `Muscle Car` modelidir; Ambulance kullanılmayacak. |
| Bundle ID | `com.pixicorp.scrapthedead`; 7 Ağustos 2026 tarihinde proje sahibi tarafından onaylandı ve Unity iOS Player Settings'e işlendi. |
| Minimum iOS | `15.6`; resmî Apple Unity plug-in destek tabanına göre proje sahibi tarafından onaylandı. |
| Yayın zamanı | Hazır olur olmaz. |

## 3. Canlı projede doğrulanan mevcut durum

### 3.1 Unity ve yerel araç zinciri

- Canlı proje Unity sürümü: `6000.3.10f1`.
- Xcode: 26.0 kurulu; güncel App Store yükleme araç zinciri için uygun başlangıç noktası.
- CocoaPods çalıştırılabilir dosyası sistemde mevcut; gerçek pod kurulumu sırasında sürüm ve repo çözümlemesi ayrıca doğrulanacak.
- Unity `6000.3.10f1` kurulumunda iOS Build Support modülü kurulu. Unity Hub ekranı ve canlı iOS build target ile doğrulandı.
- Unity Editor'daki etkin platform inceleme anında iOS olarak görünüyordu.
- Proje çalışma ağacı zaten değiştirilmiş dosyalar içeriyor. Bu değişiklikler proje sahibine ait kabul edilecek; iOS işi sırasında geri alınmayacak veya üzerine yazılmayacak.

### 3.2 Mevcut CrazyGames sahne akışı

Mevcut Build Settings akışı:

1. `Assets/Scenes/CrazyGamesBootstrap.unity`
2. `Assets/_ASSETS/Ash Assets/Arcade Vehicle Physics/Demo Scene/Demo.unity`

`CrazyGamesBootstrap`, CrazySDK hazırlandıktan sonra ikinci sahneyi açıyor. Mevcut CrazyGames yayınının bu akışı korunmalı.

### 3.3 Platform bağımlılığı

Eski `Assets/Scripts/Platform/CrazyGamesPlatformService.cs` ayrıştırıldı. Ortak garaj, kayıt ve reklam akışı artık `IGamePlatformAdapter` / `GamePlatformService` sınırını kullanıyor; CrazyGames ve iOS davranışları ayrı adaptörlerde tutuluyor.

Sonuç: Yalnızca sahne kopyasının yeterli olmadığı doğrulandı ve ortak oyun kodu platformdan bağımsız servis sözleşmesine taşındı. iOS development export'u ile CrazySDK dışlaması, WebGL regression build'i ile WebGL derleme sınırı doğrulandı. CrazyGames'in canlı SDK davranışı ayrıca tarayıcı smoke testinde doğrulanacak.

İlk iOS development export incelemesinde CrazySDK'nin WebGL `.jslib` dosyası iOS'a girmediği halde CrazySDK C# runtime/demo tiplerinin IL2CPP çıktısına girdiği görüldü. SDK kaynakları değiştirilmeden üç assembly sınırı eklendi: `CrazyGames.Runtime` ve `CrazyGames.Demo` yalnızca Editor/WebGL, `CrazyGames.Editor` yalnızca Editor/WebGL için derleniyor. Sonraki iOS export'unda CrazySDK vendor runtime tiplerinin IL2CPP çıktısında bulunmadığı doğrulandı; WebGL regression build'i de 0 hata ile tamamlandı.

### 3.4 Mevcut kayıt sistemi

Ana dosya:

- `Assets/Scripts/Garage/GarageEconomyController.cs`

Mevcut kayıt:

- Sürüm: 3
- Anahtar: `zt3d_garage_progression_v3`
- İçerik: scrap, seçili araç, sahip olunan araçlar, sahip olunan attachment'lar, araç loadout'ları ve ömür boyu toplam zombi öldürme sayısı.
- CrazyGames hazır olduğunda CrazySDK Data kullanıyor.
- CrazyGames hazır değilse PlayerPrefs'e JSON yazıyor ve `PlayerPrefs.Save()` çağırıyor.

PlayerPrefs iOS'ta offline/local kaynak olarak korunuyor. Aynı JSON Unity Cloud Save'e zaman damgalı envelope ile yazılıyor. Reklamsız ödül hakkı Unity IAP satın alma/restore akışından alınır; PlayerPrefs yalnızca yerel cache'tir. İki cihaz ve temiz kurulum geri yükleme davranışı henüz gerçek cihazda doğrulanmadı.

Cloud Save yazma kuyruğu, başarısız ağ isteğinde pending veriyi artık silmiyor. Bir yazma sürerken daha yeni kayıt oluşursa başarılı olan eski snapshot kaldırılıyor, daha yeni kayıt kuyrukta tutuluyor. Unity projesi `Scrap the Dead` adlı Cloud Project'e, `davut177` organizasyonu altında bağlandı; Project ID `68f7978b-6391-4492-b1ab-61ae32e2927c` proje ayarına işlendi.

### 3.5 Mevcut reklam akışı

Ana dosya:

- `Assets/Scripts/Garage/GarageFlowController.cs`

Mevcut oyunda:

- El sonunda `DOUBLE SCRAP` akışı zaten bulunuyor.
- Garajda ayrıca sabit `SALVAGE DROP` ödülü bulunuyor.

iOS'ta yalnızca el sonu `DOUBLE SCRAP` kullanılacak. Bonus, reklamın ödül callback'i başarılı olduğunda ve yalnızca bir kez verilecek.

### 3.6 Mevcut skor verisi

`ScoreManager` içindeki kill değeri görev başında sıfırlanan el bazlı değerdir. Görev sonunda bu değer Save V3 içindeki 64-bit `lifetimeZombieKills` toplamına ekleniyor, kaydediliyor ve Game Center submit kuyruğuna bildiriliyor.

### 3.7 Mevcut araç kontrolü

`Assets/_ASSETS/Ash Assets/Arcade Vehicle Physics/Scripts/InputManager_ArcadeVP.cs` şu anda:

- `Horizontal` eksenini direksiyona,
- `Vertical` eksenini ileri/geri ivmeye,
- `Jump` eksenini ek girişe

aktarır.

iOS dinamik joystick vektörü kameraya göre dünya üzerindeki hedef sürüş yönüne çevrilecek. Araç mevcut rotasyonundan bu hedefe direksiyon kıracak ve yalnızca ileri yönde hareket edecek; joystick uzaklığı ileri ivme miktarını belirleyecek. Mobilde geri vites olmayacak, `Jump` sıfır kalacak ve ayrı gaz/fren düğmesi eklenmeyecek. CrazyGames klavye kontrolü değişmeyecek.

### 3.8 Mevcut UI ve iOS ayarları

- iOS UI Toolkit paneli `Scale With Screen Size`, `852×393` referans çözünürlüğü ve genişlik eşlemesi kullanıyor. Böylece düzen cihaz DPI değerine göre rastgele büyümüyor; farklı yatay oranlarda aynı mantıksal genişlik korunuyor.
- CrazyGames/WebGL Panel Settings asset'i değiştirilmedi; iOS ölçekleme ve responsive kuralları ayrı `GaragePanelSettings_iOS` ve `.platform-ios` kapsamı altında tutuluyor.
- iOS sahnesinde `IosSafeAreaController` var; UI köküne safe-area ve platform sınıfı uyguluyor.
- Tarayıcıya özel fullscreen seçeneği iOS'ta görünmemeli.
- iPhone+iPad hedefi mevcut.
- Portre yönleri kapalı, iki landscape yönü açık.
- Minimum iOS değeri Unity Player Settings ve Apple Build Profile içinde `15.6` olarak sabitlendi.
- iOS bundle identifier, App Store ikonu ve Launch Screen tamamlandı; imzalama takımı henüz tamamlanmadı.
- iPhone için varsayılan kalite seviyesi `Very Low`; iPhone 7 testinde görsel kalite ve performans birlikte doğrulanmalı.
- AdMob `11.3.0`, Unity IAP `5.4.2`, Unity Authentication `3.7.3`, Unity Cloud Save `3.4.1`, Apple.Core `3.2.0` ve Apple.GameKit `4.0.1` projede kurulu.
- Settings panelinde gerçek Privacy Policy/Support bağlantıları, UMP privacy options, Game Center leaderboard, IAP satın alma/restore ve kalıcı oyuncu hesabı/verisi silme akışları bulunuyor. Panel son responsive düzeltmeden sonra 2532×1170 yatay iPhone profilinde kaydırılabilir ve taşmasız olarak yeniden doğrulandı.
- Unity Authentication DSA service notifications girişten sonra sorgulanıyor; okunmamış kısıtlama bildirimi varsa oyuncuya engelleyici olmayan bir panelle gösterilip okundu zamanı yerelde saklanıyor.

### 3.9 Apple Game Center entegrasyonunun güncel durumu

- Apple.Core ve Apple.GameKit paketlerini proje sahibi üreticinin GitHub deposundan kurdu.
- `Assets/Apple Plug-In Support/Editor/DefaultAppleBuildProfile.asset` içinde resmî `Apple.GameKit` build step'i açık.
- Resmî step, iOS export sırasında `GameKit.framework` ve `com.apple.developer.game-center` entitlement'ını yönetir.
- Oyun kodu `GKLocalPlayer`, `GKLeaderboard` ve `GKGameCenterViewController` kullanır; eski Unity `Social` Game Center çağrıları kaldırıldı.
- Unity Authentication, taze Game Center identity signature ile `SignInWithAppleGameCenterAsync` kullanır. Daha önce offline/anonim fallback oturumu oluşmuşsa normal `LinkWithAppleGameCenterAsync` ile taşınabilir hale getirir; `ForceLink` kullanmaz. Kimlik başka bir UGS oyuncusuna zaten bağlıysa canonical Game Center hesabına giriş yapar.
- iOS IL2CPP stripping sırasında reflection ile oluşturulan Apple.Core `NSData(IntPtr)` ve Apple.GameKit `GKLeaderboard(IntPtr)` kurucularının kaldırılmasını önlemek için proje `Assets/link.xml` içinde iki tipi de korur. Bu koruma, Game Center identity signature ve leaderboard yükleme akışlarında görülen `Default constructor not found` hatalarını önler.
- Game Center veya UGS kullanılamazsa oyun engellenmez; local kayıt korunur ve mümkünse anonim UGS oturumuna düşülür.
- Önceki izinsiz özel native/PBX yaklaşımından kalan Objective-C dosyası, `DllImport` köprüsü veya özel capability post-process bulunmuyor.
- App Store Connect classic leaderboard'u oluşturuldu ve Unity ayarına bağlandı. Unity Dashboard Apple Game Center provider'ı `com.pixicorp.scrapthedead` Bundle ID ile etkinleştirildi.

### 3.10 İlk iOS development export kanıtı

- Development Xcode export'u `/tmp/ZombieTycoon3D-iOS-GameKit` yoluna başarıyla üretildi; build sonucu 0 error / 100 warning idi.
- Çıktıda `GameKit.framework`, resmî GameKit wrapper ve `com.apple.developer.game-center = true` doğrulandı.
- Minimum iOS `15.6`, yalnızca landscape yönleri ve Development AdMob test App ID doğrulandı.
- Bu export imzalanmış archive veya fiziksel cihaz testi değildir.
- Export, Apple Security build step'inin iOS entitlement dosyasına `com.apple.security.app-sandbox = true` eklediğini de gösterdi. Proje sahibi onayıyla Apple Security step'i ve App Sandbox entitlement ayarı kapatıldı. Kaynak ayarı tamamlandı; entitlement'ın sonraki onaylı export'ta yokluğu ayrıca doğrulanacak.
- Exporttan sonra CrazySDK assembly sınırı ve diğer runtime düzeltmeleri yapıldığı için sonraki doğrulama build'i tekrar onay gerektirir.

### 3.11 Editor Play Mode engeli

- İlk iOS sahnesi Editor Play Mode denemesinde ProjectDawn Navigation'ın `WriteAgentRectTransformSystem` ve `ReadAgentRectTransformSystem` sistemleri `UnityEngine.RectTransform` tipinin Entities `TypeManager` içinde kayıtlı olmadığını bildiriyordu.
- Projedeki Agents Navigation `4.1.1` paketi `com.unity.entities 1.3.5` isterken çözümlenmiş Entities sürümü `1.4.2`. ProjectDawn hybrid kodu `RectTransform` sorguluyor fakat bu UnityEngine tipini kendisi kaydetmiyor.
- Proje sahibi onayıyla vendor paketini değiştirmeden `Assets/Scripts/Compatibility/EntitiesUnityEngineComponentRegistration.cs` içinde assembly attribute ile `RectTransform` tipi Entities'e kaydedildi.
- Unity compile 0 hata ile tamamlandı. Console temizlendikten sonra iOS sahnesi yeniden Play Mode'a alındı; ProjectDawn'a ait iki `RectTransform` hatası tekrar oluşmadı.
- Unity 6.3'te vHierarchy'nin domain reload sırasında `IEnumerable<int>` sonucunu doğrudan `List<int>` sanmasından kaynaklanan `InvalidCastException`, sonuç güvenli biçimde `ToList()` ile materyalize edilerek giderildi. Sonraki importta aynı exception tekrarlanmadı.

### 3.12 Build öncesi kaynak denetimi — 8 Ağustos 2026, mobil kontrol sonrası yenilendi

- Aktif sahnenin `Assets/Scenes/iOS/Demo_iOS.unity`, aktif platformun iOS ve etkin tek build sahnesinin iOS duplicate sahnesi olduğu Unity üzerinden doğrulandı.
- Sahne doğrulaması 0 eksik script, 0 kırık prefab ve 0 sahne sorunu ile geçti. iOS sahnesinde `IosSafeAreaController` ve `MobileVehicleInputController` bulundu; CrazyGames bootstrap/adaptör bileşeni bulunmadı.
- Proje sahibinin onayıyla iOS joystick'i kamera-relative hedef sürüş yönüne çevrildi. Joystick uzaklığı yalnızca pozitif ileri ivme üretiyor; mobil geri vites kaldırıldı ve araç hedef yöne dönerek burnu önde ilerliyor. CrazyGames klavye girişi ile vendor araç fiziği değiştirilmedi.
- CrazySDK runtime ve demo assembly tanımları yalnızca Editor/WebGL için tutuluyor. Ortak platform servisi iOS'ta yalnızca `IosPlatformAdapter` oluşturuyor; CrazyGames kaynakları değiştirilmeden platform davranışı ayrılmış durumda.
- Unity yeniden açıldıktan sonraki görsel denetimde ana garajın gerçekten üst üste bindiği görüldü. Kök nedenler iOS Panel Settings'in `Constant Physical Size` kullanması, CrazyGames için ayrılan 360 px topbar alanının iOS'a sızması ve önceki iOS büyütme kurallarının dar yatay içerik yüksekliğine uymamasıydı.
- iOS Panel Settings `Scale With Screen Size / 852×393 / Match Width` olacak şekilde düzeltildi. CrazyGames asset'i değiştirilmedi; garaj topbarı, beş araç istatistik kartı, showroom açıklaması, seçim okları, araç/parts bağlam kartı ve alt aksiyon alanı yalnızca `.platform-ios` altında kompakt-responsive hale getirildi.
- İlk responsive düzeltme taşmayı giderse de proje sahibi garaj ve gameplay HUD oranlarını gereğinden büyük ve görsel olarak dengesiz buldu; bu ilk çıktı final kabul edilmedi.
- İkinci revizyonda garaj topbarı `48`, footer `52`, seçim okları `44`, istatistik alanı `200×170` civarı ve sağ bağlam kartı `230×68–76` mantıksal birime indirildi. Gameplay üst şeridi `430×46`, pause düğmesi `44×44`, sol alt telemetri yaklaşık `245×100` olacak şekilde küçültüldü. 2532×1170 Play Mode çıktısında taşma/çakışma görülmedi ve proje sahibi ikinci revizyonun görsel oranlarını onayladı.
- Settings'te Game Center, Ad-Free Rewards, Restore, Privacy, Support ve Delete Data satırları kullanılabilir; WebGL fullscreen satırı iOS'ta gizli. Uzun Settings içeriği ekran içinde kaydırılıyor ve iOS'ta dekoratif scrollbar gizleniyor.
- Editör dışı iPad render denemesi görünür içerik üretmediği için iPad düzeni geçmiş sayılmadı. Gerçek iPad veya Device Simulator kontrolü Aşama 10'da açık kalıyor.
- Rewarded Double Scrap akışında bonusun yalnızca AdMob reward callback'inden sonra ve el başına bir kez verildiği; reklam iptal/yükleme hatasında taban ödülün korunduğu kaynak üzerinden doğrulandı. Ad-Free Rewards entitlement'ı aynı ödül callback yolunu reklam göstermeden çalıştırıyor.
- IAP ürünü non-consumable olarak yapılandırılmış; satın alma, pending/cancel/fail, confirmed entitlement, StoreKit kaynaklı restore ve yerel entitlement cache akışları bağlı.
- Game Center authentication, all-time lifetime zombie kills submit/retry, Game Center kimliğiyle UGS Authentication, Cloud Save write-lock uzlaştırması ve hesap/veri silme sırası kaynak üzerinden doğrulandı.
- Production AdMob App ID, rewarded ad unit ID, IAP product ID, leaderboard ID, Privacy Policy/Support URL'leri ve bağlı Unity Cloud Project release guard tarafından doğrulandı. `ZombieTycoon3D/iOS/Validate App Store Configuration` menüsü `iOS App Store configuration is complete.` sonucu verdi.
- Privacy Policy ve Support URL'leri dışarıdan HTTP 200 ile açıldı. Apple GameKit build step açık; Apple Security/App Sandbox step kapalı; iOS release Build Profile development/debug/profiler seçenekleri kapalı.
- Proje Play Mode testi geçti; mobil kontrolün araç, ana kamera ve UI referansları çalışma anında çözüldü ve proje sahibi yeni sürüş hissini Play Mode'da onayladı. İlgili iOS/platform/garage scriptleri Unity içinde derleniyor; yenilenen kaynak denetiminde yeni proje compile hatası oluşmadı.
- Odaklı statik denetimde iOS/CrazyGames Build Profile sahne sınırları, CrazySDK assembly platformları, iOS sahnesinin CrazyGames bileşeni içermemesi, ileri-only kamera-relative kontrol, resmî GameKit kullanımı, Apple Security/App Sandbox kapalı durumu, production kimlikleri, HTTPS bağlantıları, Player Settings, boş privacy usage-description alanları, ikon/launch kaynakları ve app-ads.txt eşleşmesi yeniden doğrulandı.
- Son UI doğrulamasında yeni proje compile hatası görülmedi. Görsel yakalama sırasında Unity'nin memoryless depth surface için ürettiği iki render bildirimi görüldü; bunlar UI veya oyun kodu compile hatası değildir.
- Bu denetimde build/export/archive alınmadı.

### 3.13 Tek `main` branch ile Windows/macOS çalışma sözleşmesi

- Windows, CrazyGames ve ana oyun geliştirme makinesidir; macOS yalnızca iOS geliştirme, Xcode export ve cihaz testi için kullanılır. Kaynak kod iki makinede de aynı `main` branch üzerinden taşınır.
- İki makinede de proje tam `Unity 6000.3.10f1` ile açılacak. Pull işleminden önce Unity kapatılacak; pull sonrasında paket importu ve derleme tamamen bitmeden sahne veya ayar kaydedilmeyecek.
- Windows'ta `Assets/Settings/Build Profiles/CrazyGames WebGL.asset`, Mac'te `Assets/Settings/Build Profiles/iOS App Store.asset` etkinleştirilecek. Global Build Settings sahne listesi platform ayrımı için elle değiştirilmemeli.
- Apple.Core `3.2.0`, Apple.GameKit `4.0.1`, Google Mobile Ads `11.3.0` ve External Dependency Manager `1.2.187` tam kullanılan sürümleriyle `Packages/` altında gömülüdür. Windows pull sonrasında GitHub/OpenUPM veya Mac'e özel mutlak `file:` yolu gerekmez.
- CrazySDK assembly'leri WebGL; Apple/GameKit runtime çağrıları iOS sembolleriyle sınırlandırılmıştır. Bir platformun modülü diğer platformun oyuncu derlemesine girmemelidir.
- `Library`, `Temp`, build/Xcode/CocoaPods çıktıları, Apple Play Mode generated support bundle'ları, crash recovery ve performans test run kayıtları commit kapsamı dışındadır.
- Commit öncesi Mac'te iOS compile/configuration guard ve WebGL regression build sonucu kaydedilecek. Commit yalnızca proje sahibinin açık onayından sonra oluşturulacak; push ayrıca açık onay gerektirir.
- Native Apple/Google plug-in dosyaları ve mevcut proje medyaları Git LFS kurallarına tabidir. İki makinede de Git LFS kurulu olmalı; normal pull sonrasında Unity açılmadan önce `git lfs pull` tamamlanmalı.
- Windows'taki ilk pull sonrası kontrol sırası: Unity kapalıyken pull → `git lfs pull` → `6000.3.10f1` ile aç → paket importunu bekle → `CrazyGames WebGL` profilini etkinleştir → Console compile hatalarını kontrol et → kısa CrazyGames gameplay/rewarded smoke testi yap.

## 4. Hedef mimari

### 4.1 Sahne ve Build Profile ayrımı

Önerilen yapı:

- CrazyGames/WebGL Build Profile
  - `CrazyGamesBootstrap.unity`
  - Mevcut `Demo.unity`
- iOS Build Profile
  - iOS için kopyalanmış, proje sahipliğindeki sahne
  - CrazyGames bootstrap içermez

Önerilen iOS sahne yolu:

`Assets/Scenes/iOS/Demo_iOS.unity`

Sahne kopyası iOS'a özel UI, servis bootstrap'ı ve kontrol bileşimi için kullanılacak. Araçlar, zombiler, ortak prefab'lar ve temel oynanış scriptleri mümkün olduğunca paylaşılacak. Aynı oynanış kodunun iki kopyası oluşturulmayacak.

### 4.2 Platform servis katmanı

Ortak oyun kodu doğrudan CrazySDK veya iOS SDK'larını çağırmayacak. Platformdan bağımsız bir facade/sözleşme kullanılacak.

Sorumluluklar:

- Kayıt yükleme ve kaydetme
- Ödüllü reklam isteme
- Reklamsız ödül hakkını sorgulama
- Satın alma ve restore işlemleri
- Game Center kimlik doğrulama
- Leaderboard skor gönderme/açma
- Platforma özel gameplay-start/stop olayları

Adaptörler:

- CrazyGames adaptörü: CrazySDK Data, rewarded ad ve CrazyGames gameplay olayları.
- iOS adaptörü: AdMob, Unity IAP, Game Center ve Unity Cloud Save.
- Editor/test adaptörü: SDK olmadan kontrollü test davranışı.

Platforma özgü kod assembly definition ve/veya doğru derleme sembolleriyle ayrılacak. Amaç:

- CrazySDK iOS oyuncu build'ine girmesin.
- iOS framework'leri WebGL build'ine girmesin.
- Shared gameplay iki platformda da aynı kalsın.

### 4.3 CrazyGames koruma kuralları

- Mevcut `CrazyGamesBootstrap.unity` silinmeyecek veya iOS için dönüştürülmeyecek.
- Mevcut CrazyGames yayın sahnesine mobil servis objeleri eklenmeyecek.
- `Assets/CrazySDK` klasörü sırf iOS için projeden silinmeyecek.
- Aynı Unity sürümünde kurulu WebGL ve iOS Build Support modülleri korunacak.
- Her platform servis refactor'ından sonra CrazyGames/WebGL regression build'i alınacak.
- CrazyGames reklam, kayıt, oyun başlangıç/bitiş ve fullscreen davranışları regression kontrolünden geçecek.
- Vendor/sample dosyaları zorunlu olmadıkça düzenlenmeyecek.

### 4.4 App Store isim / ASO araştırması — 7 Ağustos 2026

Araştırma, canlı proje dosyalarındaki gerçek oyun döngüsü ile ABD App Store'daki güncel yakın rakipleri birlikte değerlendirdi. Oyun 120 saniyelik görevlerde araçla 100 zombi öldürme hedefi, kill ve görev bonusundan scrap kazanma, el sonu 2x ödül, altı araç satın alma ve plow/blade/armor gibi araç attachment'larıyla ilerleme üzerine kurulu. Oyun bir yarış, shooter, runner veya roguelite değildir; isim bu türleri vaat etmemelidir.

App Store rating sayıları indirme sayısı veya Apple Search Ads arama hacmi değildir. Aşağıdaki değerler, mağaza ilgisi ve yerleşmiş marka gücü için herkese açık yaklaşık sinyal olarak kullanıldı; zamanla ve ülkeye göre değişebilir.

| Yakın / öğretici rakip | ABD App Store sinyali | İsimden çıkan ders |
|---|---:|---|
| Into the Dead 2 | 114K rating | Kısa, ayırt edici marka ifadesi; doğrudan zombi çağrışımı. |
| Dead Ahead: Zombie Warfare | 59K rating | Marka adı + açıklayıcı iki nokta sonrası yapı. |
| Zombie Catchers | 33K rating | `Zombie + özgün eylem/rol` kalıbı. |
| Zombie Raft | 26K rating | `Zombie + ayırt edici araç/nesne` kalıbı. |
| Downhill Smash | 22K rating | Kısa eylem sözcüğü; mağaza metninde zombie-smashing machine ve upgrade vaadi. |
| Zombie Tsunami | 13K rating | İki kelimelik, kolay okunan ve görsel çağrışımı güçlü marka. |
| Earn to Die Rogue | 11K rating | Ana marka korunuyor; subtitle doğrudan “Drive cars and smash zombies” diyor. |
| Zombie Highway 2 | 8.5K rating | Zombi + sürüş ortamını doğrudan anlatan literal isim. |
| Earn to Die 2 | 5.6K rating | Araç açma/yükseltme ve zombi ezme döngüsüne en yakın yerleşmiş rakip. |

Araştırmadan çıkan isim ilkeleri:

- Apple adı en fazla 30, subtitle en fazla 30 karakterdir. Ad basit, hatırlanabilir, kolay yazılır ve mevcut uygulamalardan ayırt edilebilir olmalıdır.
- Güçlü rakipler çoğunlukla kısa bir marka adını, ne oynandığını anlatan subtitle ile tamamlıyor. Başlığı jenerik kelimelerle doldurmak tek başına avantaj değil.
- Bu oyun için yüksek niyetli anlam kümesi `zombie`, `car/drive`, `crush/smash`, `upgrade/garage`; `scrap` ise oyuna özgü ekonomi ve marka farkıdır.
- `Zombie Crusher`, `Zombie Car Racing`, `Zombie Derby`, `Zombie Highway`, `Earn to Die`, `Dead Ahead` gibi mevcut/jenerik başlıklar kullanılmamalı.
- `Scrapocalypse` adı birden fazla mevcut bağımsız oyunla çakıştığı için elendi.
- Store ve açık web taraması yalnızca ön çakışma kontrolüdür; marka tescili açısından hukuki uygunluk garantisi değildir. Son isim kilitlenmeden önce resmî marka veritabanı kontrolü yapılmalıdır.

Adaylar; gerçek oyun uyumu, hatırlanabilirlik, ASO açıklığı, ön çakışma/saturasyon riski ve uluslararası okunabilirlik üzerinden 100 puanla değerlendirildi:

| Sıra | App Store adı | Önerilen subtitle | Puan | Değerlendirme |
|---:|---|---|---:|---|
| 1 | `Scrap the Dead` | `Zombie Cars: Crush & Upgrade` | 92 | En dengeli seçim. Scrap ekonomisini ve zombi ezmeyi tek kısa kelime oyununda birleştiriyor; exact App Store/açık web taramasında aynı adlı oyun görülmedi. Subtitle eksik olan araba ve upgrade açıklığını tamamlıyor. |
| 2 | `Zombie Wreckshop` | `Car Smash & Garage Upgrades` | 88 | `workshop + wreck` kelime oyunu, zombi ve garaj döngüsünü güçlü anlatıyor. Çok ayırt edici; ancak `Wreckshop` sözcüğünün oyun dışı müzik kullanımından dolayı resmî marka taraması önemli. |
| 3 | `Horde Harvester` | `Zombie Cars: Scrap & Smash` | 85 | Oyundaki Prison Bus attachment'ına ve kalabalık biçme fantezisine tam oturuyor. Ana başlıkta zombie/car olmadığı için subtitle'a daha bağımlı. |
| 4 | `Wreck the Dead` | `Crush Zombies, Upgrade Cars` | 83 | Kısa, güçlü ve eylem odaklı. Scrap ekonomisi ve garaj kimliği ilk bakışta görünmüyor. |
| 5 | `Zombie Motorworks` | `Crush, Collect, Upgrade` | 83 | Zombie anahtar sözcüğü ile araç atölyesi ilerlemesini açık anlatıyor; aksiyon enerjisi ilk üç adaydan düşük. |
| 6 | `Zombie Cars: Scrap & Smash` | `Build Your Apocalypse Ride` | 78 | Oynanışı en hızlı açıklayan literal seçenek. Jenerik ve kolay taklit edilir olduğu için uzun vadeli marka değeri daha zayıf. |

Net ASO önerisi:

- App Store adı: `Scrap the Dead` — 14/30 karakter
- Subtitle: `Zombie Cars: Crush & Upgrade` — 28/30 karakter
- Konumlandırma cümlesi: “Choose a ride, crush zombie hordes, collect scrap, and build a stronger apocalypse machine.”

`Scrap the Dead` tek başına en yüksek arama hacimli ifade değildir; avantajı özgün marka olmasıdır. `zombie`, `cars`, `crush` ve `upgrade` arama niyeti subtitle üzerinden kapsanır. Proje sahibi 7 Ağustos 2026 tarihinde adı ve önerilen subtitle'ı onayladı. Bundle ID daha sonra `com.pixicorp.scrapthedead` olarak ayrıca onaylandı ve Unity iOS Player Settings'e işlendi. Apple Developer App ID, App Store Connect uygulama kaydı, Game Center leaderboard, AdMob uygulama/rewarded birimi ve non-consumable IAP dış panel kayıtları aynı Bundle ID ailesiyle oluşturuldu.

## 5. Uygulama aşamaları

### Aşama 0 — Güvenli başlangıç ve baseline

- [x] Proje sahibinden uygulama aşamasına başlama izni al.
- [x] Mevcut kirli çalışma ağacını tekrar kaydet; kullanıcı değişikliklerini ayır.
- [x] Mevcut CrazyGames build scene listesini bu belgenin 3.2 bölümünde doğrulanmış baseline notu olarak kaydet.
- [ ] Mevcut CrazyGames kayıt, rewarded ad ve el sonu akışını test et.
- [x] `[KULLANICI ONAYI + CODEX]` ProjectDawn/Entities `RectTransform` uyumluluk kaydını proje-sahipli assembly attribute ile ekle ve Play Mode'da hatanın giderildiğini doğrula.
- [x] Mevcut WebGL profilinden regression build raporu al. 8 Ağustos 2026 build'i 0 hata, 174 warning ve 111.94 MB sonuçla tamamlandı.
- [x] Unity Hub üzerinden tam `6000.3.10f1` sürümünde iOS Build Support kurulumunu doğrula.
- [x] WebGL Build Support'un aynı sürümde kurulu kaldığını Unity Hub'da doğrula.
- [x] Entegrasyonlu iOS development Xcode export'u alarak paket ve native uyumluluk engellerini incele. Bu işlem tekrar edilmeden önce açık build onayı al.

Çıkış ölçütü: CrazyGames baseline doğrulanmış, iOS modülü kurulu ve entegrasyonlar eklenmeden boş iOS Xcode projesi üretilebiliyor.

### Aşama 1 — Platform ve sahne ayrımı

- [x] Mevcut `Demo.unity` sahnesini `Assets/Scenes/iOS/Demo_iOS.unity` olarak duplicate et.
- [x] Duplicate sahnenin ayrı `.meta` kimliğini doğrula.
- [x] CrazyGames ve iOS için ayrı Unity Build Profile oluştur.
- [x] CrazyGames profilinin sahne sırasını değiştirme.
- [x] iOS profiline yalnızca iOS sahnesini ekle.
- [x] Platform servis sözleşmesini çıkar.
- [x] Mevcut CrazyGames çağrılarını CrazyGames adaptörüne taşı.
- [x] Ortak gameplay kodunu servis sözleşmesine bağla.
- [x] Editor/test adaptörü ekle.
- [x] Apple.GameKit referanslarını iOS derleme sembolleriyle WebGL'den ayır.
- [x] CrazySDK kaynaklarını değiştirmeden runtime/demo/editor assembly sınırlarını ekle; iOS'ta WebGL SDK C# kodunu dışla.
- [ ] `[ORTAK TEST]` iOS build'de CrazySDK tiplerinin IL2CPP çıktısında olmadığını ve WebGL regression build'in 0 hata tamamlandığını doğruladık; CrazyGames SDK'nın canlı tarayıcı davranışını smoke test ile tamamla.

Çıkış ölçütü: İki profile aynı ortak gameplay'i kullanıyor; iOS sahnesinde CrazyGames bootstrap yok ve WebGL akışı değişmeden çalışıyor.

### Aşama 2 — Mobil kontrol ve arayüz

- [x] iOS sahnesine sade dinamik/floating joystick ekle.
- [x] Joystick yalnızca gameplay dokunma alanında ilk temas noktasında belirsin.
- [x] Joystick parmak hareketini sınırlı yarıçap içinde normalize etsin.
- [x] Küçük bir dead zone uygula.
- [x] Joystick yönünü kameraya göre hedef dünya yönüne dönüştür ve steering'i araç ile hedef arasındaki açıdan hesapla.
- [x] Joystick uzaklığını yalnızca ileri acceleration'a bağla; mobil geri vitesi kaldır.
- [x] Parmak kalktığında giriş sıfıra dönsün ve joystick gizlensin/solsun.
- [x] UI butonlarına yapılan dokunuşların joystick'i başlatmasını engelle.
- [x] Çoklu dokunmada joystick parmağının pointer kimliğini koru.
- [x] WebGL klavye girişini değiştirme.
- [x] iOS UI'ına safe-area controller ekle.
- [x] iOS sınıfında ana aksiyon, mission, pause ve settings dokunma hedeflerini mobil kullanım için okunabilir boyuta getir.
- [x] iOS sınıfında topbar, sekme, HUD, timer, garaj istatistikleri, sonuç ekranı ve settings panellerini yatay telefon yüksekliğine sığan responsive düzene getir.
- [x] iOS Panel Settings'i `Scale With Screen Size / 852×393 / Match Width` olarak ayır; CrazyGames Panel Settings'i değiştirme.
- [x] `[KULLANICI]` İkinci responsive garaj ve gameplay HUD revizyonunu 2532×1170 yatay iPhone profilinde görsel olarak onayla.
- [x] Intro, pause, görev sonucu, Settings ve Delete Data ekranlarının ikinci revizyon sonrası final oran kontrolünü 2532×1170 profilde kapat.
- [x] iOS garaj PreviewCamera durumunu ve uzun `BUY · 650 SCRAP` metnini 2532×1170 profilde doğrula.
- [x] Hem 16:9 hem notch/Dynamic Island hem iPad oranlarını Play Mode'da kontrol et.
- [x] iOS'ta browser fullscreen butonunu gizle.
- [x] Settings'e `Restore Purchases`, `Privacy Options` ve `Leaderboard` girişlerini ekle.
- [x] `[KULLANICI + CODEX]` Privacy Policy ve Support URL'lerini yayınla ve oyun içi bağlantıları gerçek HTTPS URL'lerine bağla.
- [x] Landscape Left ve Landscape Right yönlerinde görsel safe-area davranışını Play Mode'da doğrula.
- [ ] `[FİZİKSEL CİHAZ]` Landscape Left/Right geçişinde aktif joystick parmağını, yeniden doğuşunu ve gerçek `Screen.safeArea` değerlerini doğrula.

Çıkış ölçütü: Oyun tek parmakla tam oynanabiliyor; ayrı gaz/fren gerekmiyor; UI telefon ve tablette taşmıyor.

### Aşama 3 — Ödüllü AdMob reklamı

- [x] `[KULLANICI]` AdMob hesabında `Scrap the Dead` iOS uygulamasını oluştur.
- [x] AdMob uygulamasını yayınlanmamış iOS uygulaması olarak ekle.
- [x] iOS AdMob App ID oluştur: `ca-app-pub-6131087568871639~4823144896`.
- [x] Tek rewarded ad unit oluştur: `End of Run - Double Scrap` / `ca-app-pub-6131087568871639/7191509125`.
- [x] Google Mobile Ads Unity eklentisini projeye ekle (`11.3.0`).
- [x] iOS App ID ve rewarded ad unit ID'yi `IosPlatformSettings` asset'ine bağla.
- [x] SDK'yı uygulama başlangıcında bir kez initialize et.
- [x] Development build için resmî Google test App ID/rewarded ID koruması ekle.
- [x] Production kimlikleri girilmiş olsa bile Development build'i zorunlu Google test App ID/rewarded ID ile çalıştır.
- [x] Rewarded reklamı önceden yükle ve gösterimden sonra yenisini hazırla.
- [x] Sonuç ekranında taban scrap miktarını değişmez `MissionResult` snapshot'ında sakla.
- [x] Düğmede verilecek ödülü açık yaz: örneğin `DOUBLE 240 SCRAP`.
- [x] Reklam gösterimini yalnızca oyuncunun açık düğme basışıyla başlat.
- [x] Bonus scrap'i sadece reward callback'i geldiğinde ver.
- [x] Aynı el için bonusun iki kez alınmasını engelle.
- [x] Reklam yüklenmediyse ödül verme ve sonuç ekranında hata göster.
- [x] Reklam kapanır/başarısız olursa taban scrap'i değiştirme.
- [x] Sıfır scrap kazanılan elde reklam düğmesini gösterme.
- [x] AdMob fail/close olayları birlikte gelirse sonuç callback'inin iki kez çalışmasını engelle.
- [x] iOS'ta `SALVAGE DROP +100` özelliğini gizle; CrazyGames davranışını değiştirme.

Çıkış ölçütü: Test reklamı izlenince yalnızca bir kez tam taban scrap kadar bonus veriliyor; iptal ve hata yollarında ekonomi bozulmuyor.

### Aşama 4 — IAP: reklamsız/instant ödüller

Ürün davranışı:

- App Store tipi: non-consumable.
- Satın alım bir kez yapılır ve aynı Apple hesabında restore edilebilir.
- Oyuncu sonuç ekranındaki 2x düğmesine yine kendisi basar.
- Hak aktifse reklam gösterilmez; bonus anında ve yalnızca bir kez verilir.
- Hak, otomatik ve her el sonunda kendiliğinden bonus vermemelidir.

Görevler:

- [x] Unity IAP paketini ekle (`5.4.2`).
- [x] `[KULLANICI]` App Store Connect'te `Ad-Free Rewards` non-consumable ürününü oluştur.
- [x] Ürün kimliğini `com.pixicorp.scrapthedead.iap.adfreerewards` olarak kesinleştir.
- [x] Referans/görünen adı `Ad-Free Rewards`, İngilizce açıklamayı `Claim rewarded bonuses without watching ads.` olarak onayla.
- [x] Başlangıç fiyatını ABD için `4.99 USD`, diğer bölgeler için Apple'ın otomatik eşdeğer fiyatlandırması olarak onayla.
- [x] Unity IAP servisini uygulama açılışında başlat.
- [x] Satın alma tamamlanmadan entitlement verme.
- [x] Pending, cancel ve failed durumlarını ayrı yönet.
- [x] Entitlement için StoreKit/Unity IAP confirmed purchase geçmişini kaynak gerçek kabul et.
- [x] PlayerPrefs içindeki entitlement alanını yalnızca cache kabul et.
- [x] `Restore Purchases` düğmesini ekle.
- [x] Restore tamamlandı/başarısız mesajlarını göster.
- [x] Restore başarı mesajını `FetchPurchases` tamamlanıp entitlement uygulanmadan gösterme.
- [x] Satın alım sonrası AdMob çağrısını bypass edip aynı reward grant fonksiyonunu çalıştır.
- [x] Sonuç ekranındaki ikon/metni entitlement aktifken video reklam çağrıştırmayacak şekilde değiştir.
- [ ] Yeni Sandbox Apple Account oluşturmadan önce Xcode StoreKit Testing ile satın alma/iptal/restore senaryolarını, ardından TestFlight'ta mevcut Apple hesabıyla App Store Connect ürün akışını doğrula. TestFlight satın alımları sandbox ortamında ücret çıkarmaz; özel sandbox geçmişi/senaryo kontrolleri kullanılmayacak.
- [x] IAP reviewer notes açıklamasını ve çekilecek gerçek cihaz görüntüsünün brief'ini hazırla.
- [ ] Final cihaz UI'ından IAP inceleme ekran görüntüsünü çek.

Adlandırma kararı: Oyunda zorunlu reklam bulunmadığı için yanıltıcı `Remove Ads` ifadesi kullanılmayacak. Kullanıcıya ve mağazada gösterilen son ürün adı `Ad-Free Rewards`, İngilizce açıklaması `Claim rewarded bonuses without watching ads.`, başlangıç fiyatı ABD için `4.99 USD` olarak onaylandı.

Çıkış ölçütü: Satın alan kullanıcı reklam görmeden isteğe bağlı 2x ödül alıyor; temiz kurulumdan sonra Restore Purchases hakkı geri getiriyor.

### Aşama 5 — Game Center leaderboard

- [x] `[KULLANICI]` Resmî Apple.Core `3.2.0` ve Apple.GameKit `4.0.1` paketlerini Apple GitHub deposundan kurdu.
- [x] `[CODEX]` Resmî Apple Build Profile içindeki `Apple.GameKit` step'inin açık ve kalıcı olduğunu doğruladı.
- [x] `[CODEX]` Eski Unity `Social` Game Center çağrılarını resmî `GKLocalPlayer`, `GKLeaderboard` ve `GKGameCenterViewController` API'leriyle değiştirdi.
- [x] `[CODEX]` Özel native Game Center köprüsü ve özel PBX capability kodu bulunmadığını doğruladı.
- [x] Development Xcode export'unda GameKit framework ve Game Center entitlement bulunduğunu doğrula.
- [x] `[KULLANICI]` App Store Connect'te `com.pixicorp.scrapthedead.leaderboard.lifetimekills` kimliğiyle classic, all-time leaderboard oluşturduğunu bildirdi.
- [x] `[KULLANICI]` Sıralamayı high-to-low, score submission türünü best score olarak ayarladı.
- [x] `[KULLANICI]` Skor formatını integer; İngilizce suffix değerlerini `zombie` / `zombies` olarak ayarladı.
- [x] `[CODEX]` Gerçek leaderboard ID'yi `Assets/Resources/IosPlatformSettings.asset` içine bağladı.
- [x] Kalıcı kayıt modeline 64-bit `lifetimeZombieKills` ekle.
- [x] Save sürümünü 2'den 3'e çıkar.
- [x] V2 kaydını V3'e migrate et; mevcut ilerlemeyi koru ve lifetime kill'i 0'dan başlat.
- [x] `Enemy.isDead` koruması ve tek `OnZombieKilled` olayıyla her zombinin yalnızca bir kez toplam değere eklenmesini güvenceye al.
- [x] Görev sonu, ekonomi işlemleri, loadout değişimi, pause ve quit noktalarında ilerlemeyi kaydet.
- [x] Game Center kimlik doğrulaması ve cloud başlangıcı tamamlandıktan sonra en güncel değeri submit et.
- [x] Yeni toplam değeri submit kuyruğuna al.
- [x] Submit başarısızlığında skoru bellekte pending tut ve sonraki skor bildiriminde tekrar dene.
- [x] Settings içinden native Game Center leaderboard'unu açabilen düğme ekle.
- [x] Game Center'a giriş yapılmamışsa oyunu engelleme; local oyun ve kayıt çalışmaya devam etsin.
- [ ] `[ORTAK TEST]` Gerçek leaderboard ID ile iPhone 7 üzerinde giriş, submit, retry ve native UI akışını doğrula.

Çıkış ölçütü: Aynı Game Center hesabı toplam kill değerini doğru görür; tekrar gönderimler toplamı yanlış artırmaz; sıralama en yüksek toplam değeri üstte gösterir.

### Aşama 6 — iPhone/iPad bulut kaydı

Onaylanan ilk sürüm çözümü: Unity Cloud Save + Unity Authentication; iOS oturumunu doğrudan Apple Game Center kimliğiyle açma.

Neden:

- Platform yalnızca iOS olduğu için Game Center doğal kullanıcı kimliğidir.
- Aynı kimlikle iPhone/iPad ve yeniden kurulum sonrası restore sağlanabilir.
- Mevcut tek ve küçük JSON kayıt modeli taşınabilir.
- Resmî Apple.GameKit paketi UGS'nin istediği identity signature, salt, public-key URL, timestamp ve team player ID değerlerini sağlar.
- Game Center ile UGS hesabına doğrudan giriş yapılır. Daha önce bağlantı yokken oluşturulmuş anonim fallback oturumu varsa normal link ile Game Center'a bağlanır; `ForceLink` kullanılmaz.
- Game Center veya UGS erişilemezse oyun local kayıtla açılır; mümkün olduğunda anonim UGS oturumu yalnızca fallback olarak kullanılır.

Görevler:

- [x] `[KULLANICI]` Unity Dashboard'da `Scrap the Dead` Cloud Project'ini oluşturup `davut177` organizasyonu altında Unity projesine bağladı; COPPA `No` seçildi.
- [x] Unity Authentication `3.7.3` ve Cloud Save `3.4.1` paketlerini ekle.
- [x] `[KULLANICI]` Unity Dashboard'da Apple Game Center identity provider'ını `com.pixicorp.scrapthedead` Bundle ID ile etkinleştir.
- [x] Game Center kimlik doğrulamasından taze identity verification değerlerini al.
- [x] Unity Authentication hesabını `SignInWithAppleGameCenterAsync` ile doğrudan aç.
- [x] Önceden oluşmuş anonim fallback oturumunu normal Game Center link ile taşınabilir hale getir; başka hesaba bağlı kimliği force-link etme.
- [x] iOS IL2CPP stripping'in Apple.GameKit identity callback'inde gereken `Apple.Core.Runtime.NSData(IntPtr)` kurucusunu kaldırmasını `Assets/link.xml` ile engelle; gerçek stripped assembly ve IL2CPP C++ çıktısında kurucunun üretildiğini doğrula.
- [x] iOS IL2CPP stripping'in leaderboard yüklemede gereken `Apple.GameKit.Leaderboards.GKLeaderboard(IntPtr)` kurucusunu kaldırmasını `Assets/link.xml` ile engelle; gerçek stripped assembly ve IL2CPP C++ çıktısında kurucunun üretildiğini doğrula.
- [x] Mevcut save JSON'unu version 3 modele taşı.
- [x] Local PlayerPrefs kaydını offline fallback/cache olarak koru.
- [x] Cloud kayıt bulunmazsa mevcut local kaydı ilk kez upload et.
- [x] Cloud kayıt varsa gameplay başlangıcında indir ve uygula.
- [x] Başarısız Cloud Save yazısında pending snapshot'ı silme; sonraki yazma denemesi için koru.
- [x] Progression `version`, cloud `modifiedAt` ve Cloud Save write-lock revision bilgisini tut; gereksiz kalıcı cihaz kimliği üretmeyerek veri toplamayı genişletme.
- [x] İki geçerli snapshot uzlaştırılırken owned vehicle/attachment listelerini birleşimle koru.
- [x] `lifetimeZombieKills` için iki geçerli snapshot içindeki daha yüksek değeri koru.
- [x] IAP entitlement'ını cloud değeriyle değil Unity IAP/StoreKit akışıyla doğrula.
- [x] Scrap snapshot çakışmasında bakiyeleri toplama; timestamp'e göre tercih edilen geçerli snapshot'ın bakiyesini kullan.
- [x] Eşzamanlı cihaz yazma çakışmasını Cloud Save write-lock ile yakala; çakışmada cloud değerini sessizce ezme, local pending snapshot'ı koru ve sonraki uygulama başlangıcında tekrar uzlaştır.
- [x] Eşzamanlı gerçek write-lock çakışmasında iki değerden birini sessizce ezme; local pending snapshot'ı koruyup sonraki açılışta sahiplik/lifetime-kill güvenli uzlaştırmasını yeniden çalıştır. İlk sürümde ayrıca cloud/device seçim ekranına gerek bırakma.
- [x] Ağ yokken local ilerlemeye izin ver; başarısız cloud yazısını pending tutup sonraki güvenli yazı/uygulama başlangıcında yeniden dene.
- [x] Pause/quit dışında araç/attachment satın alma, loadout değişimi, scrap ödülü ve görev sonunda açık save checkpoint'leri kullan.
- [x] Cloud save başarısızlığının oyunu açmayı engellememesini sağla.
- [ ] `[ORTAK TEST]` Aynı Game Center hesabıyla ikinci cihaz/temiz kurulum restore testini yap.
- [x] Unity Authentication DSA notification API'sini giriş ve authentication exception yollarına bağla; okunmamış bildirimi Settings üzerinde göster ve acknowledgement zamanını sakla.

Çıkış ölçütü: Bir cihazdaki ilerleme aynı Game Center hesabıyla ikinci cihazda ve temiz kurulumdan sonra geri geliyor; offline oyun veri kaybına veya scrap çoğaltmaya yol açmıyor.

### Aşama 7 — Gizlilik, UMP ve ATT

- [x] Google Sites üzerinde İngilizce Privacy Policy sayfası oluştur ve herkese açık yayınla; mevcut `PixiCorp Privacy Policy` sayfasının yapısı referans alındı ve Scrap the Dead'in gerçek SDK/veri davranışlarına uyarlandı.
- [x] Aynı Google Sites içinde kısa İngilizce Support/Contact sayfası oluştur ve herkese açık yayınla; public iletişim adresi `davutinat@gmail.com` olarak eklendi.
- [ ] Privacy Policy URL'ini App Store Connect'e ekle.
- [x] Privacy Policy ve Support bağlantılarını gerçek HTTPS URL'leriyle oyun Settings ekranına ekle.
- [x] Settings içine iki adımlı Unity Authentication/Cloud Save/local progression silme akışı ekle; Apple tarafından yönetilen Game Center skorları ve App Store satın alımlarının silinmediğini açıkça belirt.
- [x] `[KULLANICI]` AdMob Privacy & Messaging bölümünde Avrupa düzenlemeleri mesajını EEA/UK/İsviçre için ve ABD eyalet düzenlemeleri mesajını mevcut/gelecekte desteklenen eyaletler için yayınla.
- [x] Google UMP SDK'sını uygulama başlangıcında çalıştır.
- [x] Her açılışta consent bilgilerini güncelle.
- [x] UMP `CanRequestAds` olumlu olmadan reklam isteme.
- [x] UMP gerekli diyorsa Settings içinde görünür `Privacy Options` girişi göster.
- [x] Oyuncu consent vermese veya reklam kişiselleştirilmese de oyunun temel oynanışını engelleme.
- [x] İlk sürümde cross-app tracking kullanma; AdMob `PublisherFirstPartyIdEnabled = false` ve `PublisherPrivacyPersonalizationState.Disabled` ayarlarını SDK initialize edilmeden önce uygula.
- [x] İlk sürüm için ATT isteme: IDFA erişimi/personalized publisher treatment kullanılmıyor; `NSUserTrackingUsageDescription` boş ve release guard bunu koruyor.
- [x] Google Mobile Ads `11.3.0`, UMP, UnityFramework, Authentication, Cloud Save, Services Core ve IAP privacy manifestlerini paketlerde ve mevcut development Xcode export'unda doğrula.
- [x] Mevcut development Xcode export'undaki birleşik UnityFramework manifestinde Required Reason API kategorilerini doğrula; final archive raporunda tekrar kontrol et.
- [ ] App Store Privacy formunda kendi kodumuz ve SDK'ların topladığı verileri doğru beyan et.

Privacy Policy asgari içeriği:

- Uygulama ve yayıncı kimliği
- AdMob ve Google UMP kullanımı
- Reklam tanımlayıcıları/cihaz bilgileri ve amaçları
- Game Center kullanıcı kimliği ve leaderboard skoru
- Unity Authentication ve Cloud Save içindeki gameplay/progression verisi
- IAP/StoreKit satın alma durumu
- Verinin paylaşılabileceği hizmet sağlayıcılar
- Saklama ve silme/iletişim yöntemi
- Çocuklara yönelik olmadığı bilgisi
- Kullanıcı hakları ve iletişim adresi

Çıkış ölçütü: Consent, privacy options ve App Privacy yanıtları kullanılan gerçek SDK davranışıyla uyuşuyor; uygulama içinde çalışan URL'ler var.

### Aşama 8 — iOS Player Settings ve Xcode

- [x] Uygulama adı için ASO/market araştırmasını ve puanlı kısa listeyi hazırla.
- [x] Proje sahibinin seçimiyle son uygulama adını `Scrap the Dead` olarak kesinleştir.
- [x] Bundle ID'yi `com.pixicorp.scrapthedead` olarak seç, Unity iOS Player Settings'e işle ve kilitle.
- [x] `[KULLANICI]` Apple Developer portalında `com.pixicorp.scrapthedead` explicit App ID'sini oluşturduğunu bildirdi.
- [x] Unity Company Name/Product Name alanlarını `PixiCorp` / `Scrap the Dead` olarak doğrula.
- [x] İlk yayın sürümünü `1.0.0`, ilk iOS build numarasını `1` olarak ayarla; sonraki her upload'da build numarasını artır.
- [x] Target Device: iPhone + iPad.
- [x] Orientation: yalnızca Landscape Left + Landscape Right.
- [x] Minimum iOS: `15.6`; Unity Player Settings, iOS build guard ve resmî Apple Build Profile eşitlendi.
- [x] Development Xcode export'ta Scripting Backend: IL2CPP.
- [x] Development Xcode export'ta iOS cihaz mimarisi: ARM64.
- [ ] Graphics API/Metal davranışını gerçek cihazda doğrula.
- [ ] Automatic Signing veya manuel provisioning yaklaşımını belirle.
- [ ] Apple Developer Team ID'yi ayarla.
- [x] Development Xcode export'ta resmî Apple.GameKit step'inin Game Center entitlement/framework eklediğini doğrula. Bu sürüm UGS Cloud Save kullandığı için iCloud container veya Sign in with Apple capability ekleme.
- [x] `[KULLANICI ONAYI + CODEX]` iOS export'a eklenen macOS App Sandbox entitlement'ını kaldırmak için Apple Security build step'ini ve App Sandbox ayarını kapat.
- [ ] `[ORTAK TEST]` Açık onaylı sonraki Xcode export'ta `com.apple.security.app-sandbox` entitlement'ının artık üretilmediğini doğrula.
- [x] Google Mobile Ads `11.3.0` paketindeki resmî `PListProcessor` ve `GoogleMobileAdsSKAdNetworkItems.xml` kaynaklarının SKAdNetwork listesini iOS Info.plist'e eklemek üzere kurulu olduğunu doğrula.
- [ ] Açık onaylı final Xcode export'unda üretilen Info.plist içindeki SKAdNetwork ID'lerini doğrula.
- [x] Onaylanan `Muscle Car` görselinden alfa kanalsız 1024×1024 App Store ikonunu hazırla ve iPhone/iPad icon setinin tüm yuvalarına bağla.
- [x] Koyu arka plan üzerinde onaylı `SCRAP THE DEAD` / `PIXICORP` logosunu kullanan iPhone+iPad Launch Screen'i hazırla ve bağla.
- [x] Unity 6 Personal lisansında Made with Unity splash ekranının isteğe bağlı olduğunu resmî Unity kaynaklarından doğrula.
- [x] iOS'ta Apple Launch Screen'den sonra Unity splash ekranını gösterme; CrazyGames Build Profile'daki mevcut splash ayarını koru.
- [x] Kamera, mikrofon ve konum usage description alanlarını boş bırak; oyun bunları kullanmıyor.
- [ ] Quality level ve URP asset seçimini iPhone 7 üzerinde doğrula.
- [ ] Managed stripping/IL2CPP'nin AdMob, IAP, GameKit ve UGS tiplerini kırmadığını test et.
- [ ] Xcode archive al, Validate App çalıştır ve tüm warning/error'ları sınıflandır.

Çıkış ölçütü: İmzalı archive üretiliyor, validation geçiyor ve fiziksel iPhone'da açılıyor.

### Aşama 9 — App Store Connect hazırlığı

- [x] ASO/market araştırmasını tamamla; aday adların 30 karakter sınırına uyduğunu doğrula.
- [x] Proje sahibinin onayıyla son uygulama adını `Scrap the Dead`, subtitle'ı `Zombie Cars: Crush & Upgrade` olarak seç.
- [x] `[KULLANICI]` App Store Connect uygulama kaydını `Scrap the Dead` / `com.pixicorp.scrapthedead` ile oluşturduğunu bildirdi.
- [x] Primary/secondary category önerisini `Games / Action / Racing` olarak metadata paketinde hazırla.
- [x] İngilizce subtitle, promotional text, description ve keywords metinlerini `Docs/IOS_APP_STORE_METADATA.md` içinde hazırla ve karakter sınırlarını doğrula.
- [ ] Support URL ve Privacy Policy URL gir.
- [ ] Copyright, yaş derecelendirmesi ve iletişim bilgilerini tamamla.
- [ ] Age Rating formunda stilize/cartoon/fantasy violence ve advertising varlığını dürüstçe işaretle.
- [ ] App Privacy formunu gerçek SDK veri envanterine göre doldur.
- [ ] Export Compliance sorularını kullanılan şifreleme/HTTPS SDK'larına göre cevapla.
- [ ] Game Center leaderboard'u uygulama sürümüyle ilişkilendir ve review'a ekle.
- [ ] Non-consumable IAP metadata, fiyat, localization ve review screenshot'unu tamamla.
- [ ] iPhone landscape ekran görüntülerini hazırla.
- [ ] Universal uygulama olduğu için iPad landscape ekran görüntülerini hazırla.
- [ ] Ekran görüntülerinde yalnızca gerçek gameplay/UI göster; CrazyGames logosu bulunmasın.
- [x] App Review Notes içinde ödüllü reklam, reklamsız ödül ve hesap silme davranışını açıkla.
- [x] İncelemecinin IAP ve Game Center özelliklerine nasıl ulaşacağını yaz.
- [ ] Reklam yüklenmediğinde uygulamanın yine oynanabilir olduğunu doğrula.

Çıkış ölçütü: App Store Connect'te eksik metadata, privacy, IAP veya Game Center uyarısı kalmıyor.

### Aşama 10 — TestFlight ve yayın

- [ ] Development build'i iPhone 7'de test et.
- [ ] En az bir güncel notch/Dynamic Island iPhone'da test et veya dış TestFlight tester kullan.
- [ ] En az bir gerçek iPad'de test et veya dış TestFlight tester kullan.
- [ ] Gerekli iPhone/iPad Simulator düzen testlerini yap; reklam/IAP performansı için simulator'u tek kanıt sayma.
- [ ] Internal TestFlight build'i dağıt.
- [ ] AdMob test ID'lerinin release build'de kaldırıldığını doğrula.
- [ ] Production reklamını yalnızca kontrollü TestFlight testinde ve kendi reklamına tıklamadan doğrula.
- [ ] IAP sandbox purchase/restore testlerini tamamla.
- [ ] Game Center sandbox leaderboard testini tamamla.
- [ ] Cloud Save iki cihaz/temiz kurulum testini tamamla.
- [ ] Crash, memory, thermal ve uzun oturum davranışını kontrol et.
- [ ] Final commit/sürümden CrazyGames WebGL regression build'i al. Commit öncesi Mac doğrulama build'i 0 hata, 174 warning ve 111.94 MB tamamlandı; embedded EDM4U değişikliği sonrasında WebGL script recompile da geçti. Final kanıt Windows temiz pull sonrası build/smoke testidir.
- [ ] App Store archive'ını upload et.
- [ ] Build processing tamamlandıktan sonra privacy manifest ve export compliance uyarılarını tekrar kontrol et.
- [ ] Sürüm ve IAP'yi review'a birlikte gönder.
- [ ] Review sorusu/rejection gelirse cevabı ve düzeltmeyi bu belgenin karar kaydına ekle.
- [ ] Onaydan sonra manuel veya otomatik release seçimine göre yayınla.

Çıkış ölçütü: iPhone/iPad sürümü App Store'da yayında; CrazyGames sürümünün build ve çalışma akışı korunmuş.

## 6. Zorunlu test matrisi

| Alan | Testler |
|---|---|
| CrazyGames regression | Bootstrap, SDK init, local/cloud save, rewarded ad, gameplay start/stop, fullscreen, el sonu. |
| Mobil kontrol | İlk dokunuşta joystick, kamera-relative hedef yön, yalnızca ileri sürüş, 180° dönüş, bırakınca sıfır, çoklu dokunma, UI üzerinde başlamama. |
| Ekran | Landscape Left/Right, iPhone 7, notch/Dynamic Island, iPad, safe area, büyük/küçük oranlar. |
| Ödül | Başarılı reklam, iptal, load fail, bağlantı kesilmesi, arka plana atma, aynı elde çift basma, sıfır scrap. |
| IAP | Satın alma, cancel, pending, fail, entitlement cache, temiz kurulum, restore, farklı Apple hesabı. |
| Game Center | Giriş başarılı/başarısız, offline, skor submit retry, aynı skorun tekrar gönderimi, native leaderboard UI. |
| Cloud Save | İlk upload, ikinci cihaz download, offline ilerleme, çakışma, bozuk/eski save migration, temiz kurulum restore. |
| Privacy | EEA/UK consent, privacy options required/not required, ATT akışı varsa allow/deny, linkler, reklamsız kullanım. |
| Performans | iPhone 7 FPS, memory, thermal, uzun oturum, zombi yoğunluğu, reklam dönüşü sonrası stabilite. |
| Store build | Release IL2CPP, ARM64, archive, validation, production IDs, privacy manifests, entitlements, receipt. |

Önerilen düşük cihaz kabul hedefi: iPhone 7'de ana oynanış sırasında kararlı en az 30 FPS, crash/OOM olmaması ve kontrol gecikmesinin oynanışı bozmaması. Bu hedef uygulama öncesi proje sahibi tarafından onaylanmalıdır.

## 7. Dış panellerde oluşturulacak kimlikler

Onaylı Bundle ID ile sırayla oluşturulacak dış kayıtlar:

- Apple explicit App ID / Bundle ID
- App Store Connect app record
- Game Center leaderboard ID
- App Store IAP product ID
- AdMob iOS uygulama bağlantısı ve production ad unit eşlemesi
- Unity Authentication Apple Game Center provider yapılandırması

Sonradan belgeye yazılacak değerler:

| Değer | Durum |
|---|---|
| App Store adı | `Scrap the Dead` — proje sahibi tarafından onaylandı |
| App Store subtitle | `Zombie Cars: Crush & Upgrade` — proje sahibi tarafından onaylandı |
| Bundle ID | `com.pixicorp.scrapthedead` — proje sahibi tarafından onaylandı ve Unity iOS Player Settings'e işlendi |
| Apple Explicit App ID | `com.pixicorp.scrapthedead` — proje sahibi tarafından portalda oluşturulduğu bildirildi |
| Apple Team ID | Hesaptan alınacak |
| App Store SKU | `pixicorp-scrapthedead-ios` — önerilen değerle uygulama kaydı oluşturulduğu kullanıcı tarafından bildirildi |
| Game Center leaderboard ID | `com.pixicorp.scrapthedead.leaderboard.lifetimekills` — App Store Connect'te oluşturulduğu bildirildi ve Unity ayarına bağlandı |
| AdMob App ID | `ca-app-pub-6131087568871639~4823144896` — Unity ayarına bağlandı |
| AdMob rewarded ad unit ID | `ca-app-pub-6131087568871639/7191509125` — Unity ayarına bağlandı |
| IAP product ID | `com.pixicorp.scrapthedead.iap.adfreerewards` — App Store Connect'te oluşturuldu ve Unity ayarına bağlandı |
| Unity Cloud Project ID | `68f7978b-6391-4492-b1ab-61ae32e2927c` |
| Privacy Policy URL | `https://sites.google.com/view/scrap-the-dead/privacy-policy` — herkese açık erişim doğrulandı |
| Support URL | `https://sites.google.com/view/scrap-the-dead/support` — herkese açık erişim doğrulandı |

Kimlikler kaynağa dağınık biçimde hard-code edilmemeli. Platforma göre seçilen tek bir yapılandırma varlığı/ayar katmanı kullanılmalı; production ve test reklam kimlikleri karıştırılmamalıdır.

## 8. Hızlı yayın için kritik sıra

Güncel bağımlılık sırası:

1. `[x]` ProjectDawn/Entities `RectTransform` uyumluluk kaydını ekle; Apple Security/App Sandbox ayarını kapat; build almadan compile ve Play Mode açılışını doğrula.
2. `[x]` Unity Editor projesini `Scrap the Dead` Cloud Project/`davut177` Organization'a bağla ve COPPA `No` seç.
3. `[x]` Proje sahibi App Store adını `Scrap the Dead`, subtitle'ı `Zombie Cars: Crush & Upgrade` olarak onayladı.
4. `[x]` Proje sahibi Bundle ID'yi `com.pixicorp.scrapthedead` olarak onayladı; Codex Unity iOS Player Settings'e işledi.
5. `[x]` Proje sahibi Apple Developer portalında `com.pixicorp.scrapthedead` explicit App ID'sini oluşturduğunu bildirdi.
6. `[x]` Proje sahibi App Store Connect uygulama kaydını oluşturduğunu bildirdi.
7. `[x]` Proje sahibi App Store Connect'te `com.pixicorp.scrapthedead.leaderboard.lifetimekills` classic leaderboard'unu oluşturdu; Codex Unity ayarına bağladı.
8. `[x]` Proje sahibi Unity Dashboard Authentication bölümünde Apple Game Center identity provider'ını `com.pixicorp.scrapthedead` Bundle ID ile etkinleştirdi.
9. `[x]` AdMob'da yayımlanmamış iOS uygulaması ve yalnızca `End of Run - Double Scrap` rewarded ad unit'i oluşturuldu; production App ID ve ad unit ID Unity ayarına bağlandı.
10. `[x]` App Store Connect'te `Ad-Free Rewards` non-consumable IAP ürünü `com.pixicorp.scrapthedead.iap.adfreerewards` kimliğiyle oluşturuldu.
11. `[x]` Gerçek IAP product ID `IosPlatformSettings` içine bağlandı; build almadan App Store configuration guard doğrulaması geçti.
12. `[x]` Proje sahibi App Store Connect Paid Apps Agreement durumunun etkin olduğunu doğruladı.
13. `[x]` Proje sahibi yeni Sandbox Apple Account oluşturmayacak. IAP testleri Xcode StoreKit Testing ve TestFlight'ın sandbox ortamında mevcut Apple hesabıyla yürütülecek; sandbox tester'a özel reset/edge-case kontrolleri kapsam dışı.
14. `[CODEX TAMAMLANDI / KULLANICI PANELİ BEKLİYOR]` Privacy/Support sitesi herkese açık yayınlandı; iki gerçek URL oyun Settings ekranına bağlandı; UMP, hesap silme ve DSA notification kodu hazırlandı. URL'leri ve hazırlanan App Privacy cevaplarını App Store Connect'e kullanıcı girecek.
15. `[x]` Store metadata, yaş derecelendirmesi, review notes, screenshot çekim listesi ve app-ads.txt paketi hazırlandı. Proje sahibinin onayladığı App Store ikonu ve Launch Screen görselleri iOS Player Settings'e bağlandı.
16. `[x]` Build öncesi kaynak denetimini mobil kontrol ve responsive UI değişikliklerinden sonra yenile; iOS sahnesini, servis bağlantılarını, CrazySDK platform sınırlarını, privacy manifestlerini, kamera-relative ileri-only kontrolü, production kimliklerini ve ana iOS ekranlarını doğrula. Google Mobile Ads ana ayarı production App ID ile eşleşiyor; development build koruması test kimliklerini otomatik kullanmaya devam ediyor.
17. `[x]` iOS garajının üst çubuğuna fiyatı App Store'dan gelen `AD-FREE` satın alma butonunu ekle; ürün yüklenirken görünür/pasif tut, satın alım sırasında durum göster ve mevcut Settings içindeki `RESTORE` akışını koru. Görev sonunda pozitif scrap varsa rewarded video henüz yüklenmemiş olsa bile reklam sonrası alınacak toplam ödemeyi (`▶ GET [2x toplam] SCRAP`) görünür tut; hazır olana kadar pasif ve `REWARD VIDEO LOADING...` açıklamalı göster.
18. `[ORTAK TEST — EN SON]` Tüm build öncesi işler bittikten ve proje sahibi her build için açık onay verdikten sonra development build al; iPhone 7, Game Center, Cloud Save, AdMob test reklamı ve IAP StoreKit akışını doğrula.
19. `[ORTAK TEST / YAYIN]` TestFlight matrisini, App Store ekran görüntülerini, archive validation/upload/review işlerini ve açık onaylı CrazyGames WebGL regression build'ini tamamla.

Bundle ID'ye bağlı dış panel kayıtları `com.pixicorp.scrapthedead` ile oluşturulacak; farklı bir kimlik kullanılmayacak.

## 9. Yayın engelleyici kontrol listesi

Aşağıdakiler tamamlanmadan App Review'a gönderme:

- [x] Unity iOS Build Support kurulu.
- [ ] İmzalı iPhone/iPad archive doğrulandı.
- [ ] CrazySDK iOS build'inde çalışmıyor ve CrazyGames build'i bozulmamış.
- [ ] Joystick ile oyun tamamen oynanabiliyor.
- [ ] Portre kapalı, iki landscape yönü çalışıyor.
- [ ] Double Scrap sadece bir kez ve doğru tutarda veriliyor.
- [ ] IAP satın alma ve Restore Purchases çalışıyor.
- [ ] Game Center toplam kill leaderboard'u çalışıyor.
- [ ] Cloud Save temiz kurulum ve ikinci cihaz testini geçti.
- [ ] UMP/ATT kararı gerçek SDK davranışıyla uyumlu.
- [ ] Privacy Policy ve Support URL uygulama içinde açılıyor.
- [ ] App Privacy, age rating ve export compliance cevapları tamam.
- [ ] iPhone ve iPad ekran görüntüleri hazır.
- [ ] Production build'de test reklam ID'si veya debug menüsü yok.
- [ ] App Review Notes, IAP ve rewarded reward akışını açıklıyor.
- [ ] Son WebGL regression build'i geçti.

## 10. Hâlâ açık olan gelecek kararları

Bu plan aşağıdaki gelecek konularını bilinçli olarak kesinleştirmez:

- iPhone 7 performans kabul hedefinin kesin değeri
- Unity Cloud Save çakışmasında ileride kullanıcıya seçim ekranı eklenip eklenmeyeceği
- Game Center/UGS hesap migration veya force-link gerektiren gelecekteki herhangi bir davranış

Artık açık olmayan kararlar: IAP adı `Ad-Free Rewards`, ABD başlangıç fiyatı `4.99 USD`; App Store adı `Scrap the Dead`; subtitle `Zombie Cars: Crush & Upgrade`; kategori yapısı `Games / Action / Racing`; keywords paketi `Docs/IOS_APP_STORE_METADATA.md` içinde hazır. Kesinleşen teknik karar: minimum iOS `15.6`; resmî Apple.GameKit; Game Center kimliğiyle doğrudan UGS sign-in; Game Center yoksa local/anonim fallback; özel native/PBX Game Center entegrasyonu yok.

## 11. Güncel kontrol noktası

Kaynak düzeyinde tamamlanan ana parçalar:

1. Ayrı iOS sahnesi ve CrazyGames/iOS Build Profile'ları.
2. Platform adaptör sınırı, iOS safe-area altyapısı ve kamera-relative hedef yön kullanan ileri-only mobil kontrol; CrazyGames klavye kontrolü korunuyor.
3. AdMob rewarded ve UMP servis kodu; Development build test kimliği koruması.
4. Unity IAP non-consumable instant reward ve restore akışı.
5. Save V3, lifetime zombie kills ve Unity Cloud Save cache/sync akışı.
6. Kullanıcının kurduğu resmî Apple.Core/GameKit ile Game Center authentication, leaderboard submit ve native UI kodu.
7. Minimum iOS `15.6`, iPhone+iPad ve landscape ayarları.
8. Önceki özel Game Center native/PBX yöntemine ait kalıntı bulunmadığının taranması.
9. Cloud Save yazma kuyruğu, UGS anonim fallback yükseltmesi, IAP restore kaynak-gerçek akışı ve AdMob çift-callback/test-ID korumaları.
10. CrazySDK runtime/demo/editor assembly sınırları ve iOS sahnesinde UI üstü joystick başlangıç engeli.
11. Development Xcode export'unda GameKit framework/entitlement, IL2CPP, ARM64, iOS 15.6 ve landscape çıktısının doğrulanması.
12. Privacy Policy/Support linkleri, iki aşamalı UGS hesap ve progression silme akışı, DSA service-notification ekranı.
13. App Store metadata, age-rating cevapları, review notes, screenshot planı ve App Privacy cevap taslağı.
14. AdMob app-ads.txt içeriği ve Firebase Static Hosting için deploy edilmeye hazır paket.
15. Cloud Save yazılarında write-lock çakışma koruması; çakışmada local pending snapshot korunuyor.
16. iOS Settings panelinin 2003×1127 landscape Play Mode görsel doğrulaması ve build guard'ın eksiksiz konfigürasyon sonucu.
17. Onaylı App Store ikonu ile koyu arka planlı `SCRAP THE DEAD` / `PIXICORP` Launch Screen görselleri ve bunları her iOS build öncesi koruyan Editor yapılandırması.
18. Mobil kontrol değişikliğinden sonra yenilenen kaynak denetimi; production AdMob App ID ana ayarla eşleşiyor, release guard eksiksiz konfigürasyon sonucu veriyor, platform/sahne/assembly sınırları geçiyor ve iOS sahnesi Play Mode'da yeni proje-sahipli compile hatası üretmiyor.
19. iOS UI ölçeklemesi `Scale With Screen Size / 852×393 / Match Width` olarak düzeltildi ve CrazyGames Panel Settings korundu. İlk responsive oranlar proje sahibi tarafından reddedildi; ikinci kompakt garaj/HUD revizyonu kaynakta taşmasız çalıştı ve proje sahibi tarafından görsel olarak onaylandı.
20. Intro, pause, görev sonucu, Settings ve Delete Data overlay ekranları 2532×1170 yatay iPhone profilinde tekrar denetlendi. iOS'ta klavye pause ipucu ve sonuç açıklaması gizlendi; Settings'in dekoratif scrollbar'ı koddan kapatılırken dokunarak kaydırma korundu.
21. Tam 2532×1170 iPhone-landscape UI matrisi tamamlandı: Vehicles, Parts, en uzun parça adı/fiyatı, gameplay HUD, aktif sanal joystick, intro, pause, başarılı/başarısız sonuç, Settings'in üst/orta/alt kaydırma durumları, Delete Data ve Service Notice. Parts görünümünde bilgi paneli ve istatistikler sıkıştırıldı; parça odak kamerası geri alınarak araç ile seçim noktalarının tamamı görünür tutuldu. CrazyGames UI kaynaklarına dokunulmadı.
22. Play Mode safe-area/oran matrisi 1920×1080 16:9, 2796×1290 Dynamic Island Landscape Left/Right ve 2732×2048 iPad 4:3 profillerinde Garage, gameplay HUD, sonuç ve Settings için tamamlandı. Dynamic Island profilinde sonuç panelini gereksiz sıkıştıran C# taban padding değeri USS ile eşitlenerek `32 → 14` yapıldı; standart 2532×1170 sonuç ekranı ayrıca regresyon kontrolünden geçti. CrazyGames kaynakları ve profili değiştirilmedi.
23. Güncel iOS Development Xcode export'u açık proje sahibi onayıyla başarıyla alındı. Export `0` hata ile tamamlandı; Bundle ID `com.pixicorp.scrapthedead`, minimum iOS `15.6` ve Game Center entitlement doğrulandı. CrazySDK vendor runtime tipleri IL2CPP çıktısında bulunmadı.
24. `Ad-Free Rewards` IAP erişimi iOS garaj üst çubuğuna taşındı; buton ürün yüklenirken görünür/pasif, fiyat geldikten sonra etkin ve satın alım sırasında durum gösterir. Settings içindeki Restore korunur. Görev sonucu reklam butonu yükleme sırasında artık kaybolmaz; oyuncunun reklam sonrası alacağı toplam ödemeyi `▶ GET [2x toplam] SCRAP` olarak gösterir, hazır olana kadar pasif ve yükleme açıklamalı kalır. Garaj fiyat durumu ve rewarded-loading sonucu 2532×1170 Play Mode ekran görüntülerinde taşmasız doğrulandı. CrazyGames/WebGL koşulları değiştirilmedi.

Henüz tamamlanmış sayılmayan kanıtlar:

1. CrazyGames SDK davranışının Windows/tarayıcı smoke testi.
2. Fiziksel iPhone kontrol testindeki henüz doğrulanmamış senaryolar ve final TestFlight matrisi.
3. Gerçek provider/AdMob/IAP kimliklerinin TestFlight üzerinde çalışma kanıtı; tüm kimlikler kaynakta hazır.
4. Sandbox ve iki cihaz/temiz kurulum Cloud Save testleri.
5. App Store Connect Privacy Policy/Support URL, App Privacy, age rating ve export compliance alanlarının canlı panelde son kontrolü.
6. Fiziksel iPad veya dış tester üzerinde gerçek safe-area, iki landscape yönü ve final cihaz ekran görüntüleri.

Play Mode safe-area/oran matrisi tamamlandı. Sıradaki UI kanıtı, yalnızca proje sahibinin o build için vereceği açık onayla alınacak yeni iOS Xcode development export'undan sonra fiziksel iPhone 7 üzerinde gerçek `Screen.safeArea`, Landscape Left/Right geçişi ve dokunmatik joystick testidir.

### 11.1 Xcode build hata/çözüm kaydı

Bu bölüm sonraki Xcode exportlarında aynı sorunları yeniden araştırmamak için tutulur.

#### `Signing for "Unity-iPhone" requires a development team`

- Neden: Unity export'u `DEVELOPMENT_TEAM` seçimini taşımadan açıldı.
- Çözüm: Xcode'da `Unity-iPhone` target'ı → `Signing & Capabilities` → `Automatically manage signing` açık → geliştirici Team'i seç. Bu projede seçilen Team ID: `7JVZGHB5S5`.
- Durum: 2026-08-08 cihaz build'inde görüldü; Team seçildikten sonra signing aşaması geçildi.

#### `Sandbox: rsync deny file-write-create ... GoogleUserMessagingPlatform`

- Eşlik eden hata: `mkpathat: Operation not permitted` / `rsync ... exited with status 1`.
- Neden: CocoaPods `GoogleUserMessagingPlatform` XCFramework kopyalama scripti çalışırken `Pods` projesinde `ENABLE_USER_SCRIPT_SANDBOXING = YES` olması.
- Güncel Xcode çözümü: Workspace'te mavi `Pods` projesini seç → `PROJECT / Pods` → `Build Settings` → `All` → `User Script Sandboxing` → `No`. Ardından `Product → Clean Build Folder` ve yeniden `Run`.
- Gelecek export hazırlığı: Pod kurulumu tamamlandıktan sonra `Pods` project build configurations için `ENABLE_USER_SCRIPT_SANDBOXING=NO` uygulanmalı. Otomatik post-export düzeltmesi kaynakta uygulanmadığı için her append/export sonrasında bu ayar yeniden doğrulanmalı.
- Durum: 2026-08-08 cihaz build'inde gözlemlendi. `Pods / User Script Sandboxing = No` ve temiz build sonrasında Xcode build'i başarıyla tamamlandı; çözüm doğrulandı.
- Append doğrulaması: 2026-08-08'de mevcut Xcode export'u `AcceptExternalModificationsToPlayer` ile güncellendi. `DEVELOPMENT_TEAM = 7JVZGHB5S5` korundu; CocoaPods projesi yeniden üretildiği için dört Pods build configuration değeri tekrar `YES` oldu ve export sonrasında yeniden `NO` yapıldı. Sonraki her Unity append/export işleminden sonra bu değer yeniden doğrulanmalı.

#### `Unexpected duplicate tasks` — `GameAssembly / Run Script`

- Eşlik eden kayıt: Aynı `WriteAuxiliaryFile ... GameAssembly.build/Script-<ID>.sh` görevinin iki kez oluşturulması.
- Neden: Unity iOS append export'u sırasında `GameAssembly` target'ının `buildPhases` listesine aynı IL2CPP `PBXShellScriptBuildPhase` kimliği iki kez yazıldı.
- Çözüm: `Unity-iPhone.xcodeproj/project.pbxproj` içinde `GameAssembly` target'ının `buildPhases` listesindeki ikinci yinelenen `ShellScript` referansını kaldır; phase tanımının kendisini ve ilk referansı koru.
- Durum: 2026-08-08 rewarded-ad append export'undan sonraki Xcode Run işleminde görüldü. Yinelenen referans kaldırıldı ve aynı phase kimliğinin bir target referansı ile bir phase tanımı kaldığı doğrulandı. Sonraki append export'larında Xcode açılmadan önce bu liste kontrol edilmeli.

## 12. Resmî kaynaklar

- [Apple App Review Guidelines](https://developer.apple.com/app-store/review/guidelines/)
- [Apple — Creating your product page](https://developer.apple.com/app-store/product-page/)
- [Into the Dead 2 — App Store](https://apps.apple.com/us/app/into-the-dead-2/id1151220243)
- [Dead Ahead: Zombie Warfare — App Store](https://apps.apple.com/us/app/dead-ahead-zombie-warfare/id1017311881)
- [Zombie Raft — App Store](https://apps.apple.com/us/app/zombie-raft/id1608077775)
- [Downhill Smash — App Store](https://apps.apple.com/us/app/downhill-smash/id1586265901)
- [Earn to Die Rogue — App Store](https://apps.apple.com/us/app/earn-to-die-rogue/id1564024870)
- [Zombie Highway 2 — App Store](https://apps.apple.com/us/app/zombie-highway-2/id892092770)
- [Earn to Die 2 — App Store](https://apps.apple.com/us/app/earn-to-die-2/id891194610)
- [Apple upcoming submission requirements](https://developer.apple.com/news/upcoming-requirements/)
- [App Store Connect — App Privacy](https://developer.apple.com/help/app-store-connect/manage-app-information/manage-app-privacy)
- [Apple privacy details guidance](https://developer.apple.com/app-store/app-privacy-details/)
- [Required Reason APIs](https://developer.apple.com/documentation/bundleresources/describing-use-of-required-reason-api)
- [App Tracking Transparency](https://developer.apple.com/documentation/apptrackingtransparency)
- [App Store screenshot specifications](https://developer.apple.com/help/app-store-connect/reference/app-information/screenshot-specifications/)
- [App Store age rating definitions](https://developer.apple.com/help/app-store-connect/reference/app-information/age-ratings-values-and-definitions)
- [Export compliance overview](https://developer.apple.com/help/app-store-connect/manage-app-information/overview-of-export-compliance)
- [Apple GameKit](https://developer.apple.com/documentation/gamekit/)
- [Apple Game Center entitlement](https://developer.apple.com/documentation/bundleresources/entitlements/com.apple.developer.game-center)
- [Apple App Sandbox bilgisi — macOS](https://developer.apple.com/help/app-store-connect/reference/app-uploads/app-sandbox-information/)
- [App Store Connect leaderboards](https://developer.apple.com/help/app-store-connect/configure-game-center/manage-leaderboards)
- [Apple Unity GameKit plug-in documentation](https://github.com/apple/unityplugins/blob/main/plug-ins/Apple.GameKit/Apple.GameKit_Unity/Assets/Apple.GameKit/Documentation~/Apple.GameKit.md)
- [Apple game controls HIG](https://developer.apple.com/design/human-interface-guidelines/game-controls)
- [Google Mobile Ads Unity quick start](https://developers.google.com/admob/unity/quick-start)
- [Google UMP for Unity](https://developers.google.com/admob/unity/privacy)
- [Google rewarded ad policy](https://support.google.com/admob/answer/7313578)
- [Google Mobile Ads test ads](https://developers.google.com/admob/unity/test-ads)
- [Unity IAP](https://docs.unity.com/en-us/iap)
- [Unity IAP restore purchases](https://docs.unity.com/en-us/iap/restore-purchases)
- [Unity Cloud Save](https://docs.unity.com/en-us/cloud-save/get-started)
- [Unity Authentication with Apple Game Center](https://docs.unity.com/en-us/authentication/platform-signin/apple-game-center)
- [Unity Authentication account deletion](https://docs.unity.com/en-us/authentication/delete-accounts)
- [Unity Authentication DSA notifications](https://docs.unity.com/en-us/authentication/dsa-notifications)
- [Apple — Offering account deletion](https://developer.apple.com/support/offering-account-deletion-in-your-app/)
- [Google AdMob app-ads.txt](https://developers.google.com/admob/ios/app-ads)
- [Unity Build Profiles](https://docs.unity3d.com/Manual/build-profiles.html)
- [Unity iOS build process](https://docs.unity3d.com/Manual/iphone-BuildProcess.html)

## 13. Karar ve ilerleme kaydı

Bu bölüm güncel karar ve doğrulama kaydıdır. Kaynak uygulaması, dış panel bildirimi, Play Mode, gerçek cihaz ve build kanıtları birbirinin yerine geçmez; her satır yalnızca belirtilen kanıt kapsamını doğrular.

| Tarih | Karar / doğrulama | Kanıt | Durum |
|---|---|---|---|
| 2026-08-07 | iOS ücretsiz, AdMob rewarded ve non-consumable reklamsız ödül hakkı kullanılacak. | Proje sahibi kararı | Onaylandı |
| 2026-08-07 | iOS rewarded ödülü, el sonunda kazanılan scrap'i ikiye katlayacak. | Proje sahibi kararı | Onaylandı |
| 2026-08-07 | Leaderboard ömür boyu toplam öldürülen zombi sayısına göre olacak. | Proje sahibi kararı | Onaylandı |
| 2026-08-07 | CrazyGames sürümü korunacak; iOS sahnesi/profile'ı ayrılacak. | Proje sahibi kararı + canlı proje incelemesi | Onaylandı |
| 2026-08-07 | Canlı Unity sürümü ve MCP bağlantısı doğrulandı. | Unity MCP: `ZombieTycoon3D`, `6000.3.10f1`, ready | Doğrulandı |
| 2026-08-07 | Unity `6000.3.10f1` iOS Build Support kurulumu doğrulandı; önceki eksik tespiti düzeltildi. | Proje sahibi Unity Hub ekranı + canlı iOS build target | Doğrulandı |
| 2026-08-07 | Bu aksiyon dokümanının oluşturulmasına izin verildi. | Proje sahibi kararı | Tamamlandı |
| 2026-08-07 | Resmî Apple.Core ve Apple.GameKit paketlerini proje sahibi kuracak; özel native/PBX alternatif kullanılmayacak. | Proje sahibi kararı | Onaylandı |
| 2026-08-07 | Apple.Core `3.2.0` ve Apple.GameKit `4.0.1` kuruldu. | `Packages/manifest.json` + Unity compile | Doğrulandı |
| 2026-08-07 | Game Center capability/framework yönetimi resmî Apple.GameKit build step'ine bırakılacak. | Proje sahibi onayı + Apple Build Profile | Onaylandı / uygulandı |
| 2026-08-07 | Game Center kimliğiyle UGS'ye doğrudan giriş; başarısızlıkta local/anonim fallback kullanılacak. | Proje sahibi onayı + `IosPlatformAdapter` | Onaylandı / uygulandı |
| 2026-08-07 | Minimum iOS `15.6` olacak. | Proje sahibi onayı + Player Settings + Apple Build Profile | Onaylandı / uygulandı |
| 2026-08-07 | SDK, hesap, capability, kayıt mimarisi ve benzeri büyük kararlar açık onay olmadan uygulanmayacak. | Proje sahibi talimatı | Sürekli kural |
| 2026-08-07 | Unity/Xcode build veya archive her seferinde açık proje sahibi onayı gerektirir. | Proje sahibi talimatı | Sürekli kural |
| 2026-08-07 | iOS development Xcode export'u başarıyla üretildi; GameKit framework/entitlement, IL2CPP, ARM64, iOS 15.6 ve landscape doğrulandı. | `/tmp/ZombieTycoon3D-iOS-GameKit` | Doğrulandı; imzasız/cihaz testi değil |
| 2026-08-07 | İlk exportta CrazySDK C# tipleri iOS IL2CPP çıktısına girdi; SDK kaynağı değiştirilmeden platform assembly sınırları eklendi. | Generated C++ taraması + sonraki iOS export + WebGL regression build | Doğrulandı; sonraki iOS çıktısında CrazySDK runtime tipi yok ve WebGL build 0 hata |
| 2026-08-07 | Apple Security step'i iOS export'a macOS App Sandbox entitlement'ı ekledi; proje sahibi onayıyla step ve entitlement ayarı kapatıldı. | Export `.entitlements` + Apple macOS App Sandbox dokümanı + Apple Build Profile | Kaynakta uygulandı; sonraki export kanıtı bekliyor |
| 2026-08-07 | Unity projesi `Scrap the Dead` Cloud Project'e `davut177` organizasyonu altında bağlandı; COPPA `No`. | Unity Services ekranı + `ProjectSettings.asset` | Doğrulandı |
| 2026-08-07 | Editor Play Mode'daki ProjectDawn/Entities eksik `RectTransform` tip kaydı proje-sahipli assembly attribute ile düzeltildi. | Unity compile 0 hata + temiz Console sonrası Play Mode | Doğrulandı; iki ProjectDawn hatası giderildi |
| 2026-08-07 | App Store isim/ASO araştırması tamamlandı; birinci öneri `Scrap the Dead`, subtitle `Zombie Cars: Crush & Upgrade`. | Canlı oyun döngüsü + ABD App Store rakip/rating/isim kalıbı taraması + exact isim ön taraması | Araştırma tamamlandı; seçim bir sonraki satırda onaylandı |
| 2026-08-07 | App Store adı `Scrap the Dead`, subtitle `Zombie Cars: Crush & Upgrade` olarak seçildi. | Proje sahibi kararı | Onaylandı |
| 2026-08-07 | Bundle ID `com.pixicorp.scrapthedead` olarak seçildi ve Unity iOS Player Settings'e işlendi; Company Name `PixiCorp`, Product Name `Scrap the Dead`. | Proje sahibi kararı + canlı Unity Player Settings okuması | Onaylandı / uygulandı |
| 2026-08-07 | Apple Developer portalında `com.pixicorp.scrapthedead` explicit App ID oluşturuldu. | Proje sahibi bildirimi | Kullanıcı tarafından tamamlandı |
| 2026-08-08 | App Store Connect'te `Scrap the Dead` uygulama kaydı `com.pixicorp.scrapthedead` Bundle ID ile oluşturuldu. | Proje sahibi bildirimi | Kullanıcı tarafından tamamlandı |
| 2026-08-08 | Classic Game Center leaderboard `com.pixicorp.scrapthedead.leaderboard.lifetimekills` oluşturuldu ve Unity `IosPlatformSettings` ayarına bağlandı. | Proje sahibi bildirimi + canlı Unity asset değişikliği | Kullanıcı/Codex tarafından tamamlandı |
| 2026-08-08 | Unity Authentication Apple Game Center identity provider, `com.pixicorp.scrapthedead` Bundle ID ile etkinleştirildi. | Proje sahibi bildirimi | Kullanıcı tarafından tamamlandı |
| 2026-08-08 | AdMob'da yayımlanmamış `Scrap the Dead` iOS uygulaması ve `End of Run - Double Scrap` rewarded ad unit'i oluşturuldu; App ID ile rewarded ad unit ID Unity ayarına bağlandı. | Proje sahibi bildirimi + canlı Unity asset değişikliği | Kullanıcı/Codex tarafından tamamlandı |
| 2026-08-08 | Non-consumable IAP için `Ad-Free Rewards`, product ID `com.pixicorp.scrapthedead.iap.adfreerewards` ve `4.99 USD` başlangıç fiyatı onaylandı. | Proje sahibi kararı | Onaylandı |
| 2026-08-08 | `Ad-Free Rewards` non-consumable IAP ürünü App Store Connect'te oluşturuldu ve product ID Unity ayarına bağlandı; App Store configuration guard build alınmadan başarıyla doğrulandı. | Proje sahibi bildirimi + Unity asset/guard doğrulaması | Kullanıcı/Codex tarafından tamamlandı |
| 2026-08-08 | App Store Connect Paid Apps Agreement durumunun etkin olduğu doğrulandı. | Proje sahibi bildirimi | Kullanıcı tarafından tamamlandı |
| 2026-08-08 | Yeni Sandbox Apple Account oluşturulmayacak; IAP önce Xcode StoreKit Testing, sonra mevcut Apple hesabıyla TestFlight sandbox ortamında doğrulanacak. | Proje sahibi kararı + Apple resmî test seçenekleri | Onaylandı; sandbox tester'a özel reset/senaryo kontrolleri kapsam dışı |
| 2026-08-08 | Google Sites üzerinde `Scrap the Dead` ana sayfası ile İngilizce Privacy Policy ve Support sayfaları oluşturuldu; mevcut PixiCorp policy yapısı referans alındı, AdMob/UMP, Game Center, Unity Authentication/Cloud Save, IAP davranışları ve `davutinat@gmail.com` iletişim adresi işlendi. | Canlı Google Sites + herkese açık sayfa doğrulaması | `https://sites.google.com/view/scrap-the-dead` adresinde yayınlandı |
| 2026-08-08 | Privacy/Support URL'leri iOS Settings'e bağlandı; iki aşamalı UGS hesap/progression silme ve DSA notification acknowledgement akışı eklendi. | `IosPlatformAdapter`, `GarageUiController`, UXML/USS + Unity compile | Kaynakta uygulandı; gerçek cihaz testi bekliyor |
| 2026-08-08 | App Store metadata, yaş derecelendirmesi, review notes, screenshot planı ve App Privacy cevap taslağı hazırlandı. | `Docs/IOS_APP_STORE_METADATA.md`, `Docs/IOS_APP_PRIVACY_RESPONSES.md` | Codex tarafı tamamlandı; App Store Connect girişi bekliyor |
| 2026-08-08 | AdMob app-ads.txt doğru publisher kaydıyla hazırlandı; Google Sites root dosya sunamadığı için Firebase Static Hosting deploy paketi oluşturuldu. | `Docs/app-ads.txt`, `Publishing/AppAdsHosting` | Yerel paket hazır; hesap/deploy kullanıcı adımı |
| 2026-08-08 | Unity yeniden açıldıktan sonra görülen iOS garaj UI çakışmasının kök nedeni `Constant Physical Size`, CrazyGames'e ait topbar rezervasyonu ve aşırı büyük iOS kuralları olarak belirlendi. iOS paneli `Scale With Screen Size / 852×393 / Match Width` yapıldı. İlk responsive oranlar proje sahibi tarafından görsel olarak reddedildi; ikinci revizyonda garaj çerçeveleri ve gameplay HUD belirgin biçimde küçültüldü. | `GaragePanelSettings_iOS.asset`, `SafehouseGarage.uss`, 2532×1170 Play Mode ölçüm ve görsel denetimi + proje sahibi onayı | İkinci garaj/HUD revizyonu görsel olarak onaylandı; overlay, fiziksel iPhone/iPad ve iki landscape yönü testleri bekliyor |
| 2026-08-08 | Cloud Save yazıları write-lock kullanacak şekilde güçlendirildi; eşzamanlı yazma çakışmasında local pending snapshot silinmiyor veya cloud sessizce ezilmiyor. | `IosPlatformAdapter` + Cloud Save 3.4.1 API kaynak doğrulaması | Kaynakta uygulandı; iki cihaz testi bekliyor |
| 2026-08-08 | Play Mode kapanışında yeniden oluşturulan kalıcı `Game Platform Service` nesnesi engellendi. | Kapanış öncesi/sonrası Editor log hata sayısı `3 → 3`; yeni cleanup kaydı yok | Doğrulandı |
| 2026-08-08 | App Store ikonunda oyunda bulunmayan jenerik araç ve Ambulance kullanılmayacak; gerçek oynanabilir spor araç referansı zorunlu. Oyundaki `Muscle Car` prefabı doğrudan referans alınarak ikon üretildi. | Proje sahibi görsel onayı + `Assets/Branding/iOS/AppIcon_ScrapTheDead_1024.png` + iOS Player Settings | Onaylandı; alfa kanalsız 1024×1024 ikon iPhone/iPad icon setinin tüm yuvalarına bağlandı |
| 2026-08-08 | iOS Launch Screen koyu `#0B1018` arka plan üzerinde `SCRAP THE DEAD` / `PIXICORP` metal logosunu gösterecek. | Proje sahibi tasarım onayı + `Assets/Branding/iOS/LaunchScreenLogo_ScrapTheDead.png` + iPhone/iPad Player Settings | Uygulandı; gerçek cihaz görünümü sonraki açık onaylı build'de doğrulanacak |
| 2026-08-08 | İlk sürümde ATT istemi ve cross-app tracking kullanılmayacak; AdMob publisher first-party ID ile personalized publisher treatment koddan kapalı tutulacak, UMP consent/privacy-options akışı korunacak. | Google Mobile Ads `11.3.0` API + resmî Google privacy/targeting belgeleri + release guard | Kaynakta uygulandı; final archive privacy report ve TestFlight davranışı tekrar doğrulanacak |
| 2026-08-08 | İlk App Store sürümü `1.0.0`, ilk iOS build numarası `1` olacak; iOS'ta markalı Apple Launch Screen'den sonra Unity splash gösterilmeyecek. CrazyGames Build Profile'ın splash ayarı değiştirilmeyecek. | Proje sahibi onayı + iOS Player Settings/build guard | Onaylandı / uygulandı |
| 2026-08-08 | iOS mobil kontrol kamera-relative hedef yön ve yalnızca ileri sürüş olarak kesinleştirildi; mobil geri vites kaldırıldı. CrazyGames klavye girişi ve vendor araç fiziği değiştirilmedi. | Proje sahibi onayı + `MobileVehicleInputController` + Unity compile/runtime referans kontrolü + proje sahibinin Play Mode sürüş onayı | Onaylandı / kaynakta uygulandı; fiziksel cihaz testi bekliyor |
| 2026-08-08 | Build öncesi kaynak denetimi mobil kontrol değişikliğinden sonra yenilendi; iOS sahnesinde eksik script veya CrazyGames bileşeni bulunmadı, platform/build-profile/assembly sınırları, production kimlikleri, privacy alanları, Apple build step'leri, branding kaynakları ve release guard tekrar doğrulandı. | Unity App Store configuration guard + Play Mode + iOS export + WebGL regression build | Kaynak, iOS export ve WebGL derlemesi doğrulandı; archive/TestFlight ve Windows tarayıcı smoke testi bekliyor |
| 2026-08-08 | Kalan iOS overlay ekranlarının 2532×1170 final oran kontrolü tamamlandı. Pause klavye ipucu ve çakışan sonuç açıklaması iOS'ta gizlendi; Settings scrollbar'ı yalnızca iOS'ta görünmez yapıldı ve dokunmatik kaydırma korundu. | Intro, pause, rewarded result, Settings ve Delete Data Play Mode ekran görüntüleri + Unity compile | Kaynakta ve Play Mode'da doğrulandı; diğer ekran oranları ile fiziksel cihaz testi bekliyor |
| 2026-08-08 | Geçici ekran görüntüsü kontrolünden sonra sahnede pasif kalan `PreviewCamera` yeniden aktif edildi. iOS araç bilgi paneli genişletildi; `AMBULANCE` adı ile `BUY · 650 SCRAP` metni aynı anda kendi alanlarında doğrulandı. | `Demo_iOS.unity`, `SafehouseGarage.uss` + Play Mode ekran görüntüleri | Düzeltildi; CrazyGames düzeni değiştirilmedi |
| 2026-08-08 | iOS UI'ın tam 2532×1170 ekran matrisi denetlendi. Parts paneli ve odak kamerası düzeltildi; en uzun parça adı/fiyatı ile aktif sanal joystick dahil tüm ana ekran ve overlay durumlarında taşma/çakışma kontrolü yapıldı. | `Temp/CodexUiAudit` tam ekran görüntüleri + Unity Console 0 compile error | Kaynakta ve Play Mode'da doğrulandı; fiziksel cihaz/iPad ve ikinci landscape yönü bekliyor |
| 2026-08-08 | iOS Play Mode safe-area/oran matrisi 16:9, Dynamic Island Landscape Left/Right ve iPad 4:3 profillerinde Garage, HUD, sonuç ve Settings için tamamlandı. Sonuç panelinin C# taban padding değeri USS ile eşitlenerek `32 → 14` düzeltildi ve standart iPhone oranında regresyon kontrolü yapıldı. | `Temp/CodexSafeAreaAudit` tam ekran görüntüleri + Unity Console 0 compile error | Kaynakta ve Play Mode'da doğrulandı; gerçek cihaz safe-area/orientation/joystick dokunma testi bekliyor |
| 2026-08-08 | Güncel iOS Development Xcode export'u açık onayla üretildi. Bundle ID, iOS 15.6 deployment target, Game Center entitlement ve CrazySDK vendor runtime dışlaması doğrulandı. | `/tmp/ScrapTheDead-iOS-DeviceTest-1786176443408` + Unity Build Report | Build succeeded; 0 error. Xcode workspace açıldı, fiziksel cihaz signing/run bekliyor |
| 2026-08-08 | IAP erişimi garaj üst çubuğuna eklendi; App Store fiyatı yüklenmeden satın alma başlatılamıyor ve Restore Settings içinde kalıyor. Rewarded reklam yüklenirken görev sonu seçeneği artık saklanmıyor; reklam sonrası toplam ödemeyi `▶ GET [2x toplam] SCRAP` olarak gösteriyor. | `GarageUiController`, `SafehouseGarage.uxml/.uss` + 2532×1170 Play Mode görsel denetimi | Kaynakta ve Play Mode'da doğrulandı; gerçek Apple satın alma ve AdMob gösterimi sonraki açık onaylı cihaz build'inde test edilecek |
| 2026-08-08 | Game Center identity signature alınırken cihazda görülen `Default constructor not found for type Apple.Core.Runtime.NSData` anonim Cloud Save fallback hatasının IL2CPP stripping kaynaklı olduğu doğrulandı. `NSData` proje `link.xml` dosyasında korundu ve Game Center/UGS oturum sonucu logları eklendi. | `Assets/link.xml`, `IosPlatformAdapter`, güncel stripped `Apple.Core.dll`, üretilen `Apple.Core.cpp` ve fiziksel iPhone 7 cihaz logu | iOS append build 0 hata ile tamamlandı; `NSData(IntPtr)` kurucusu çıktıda mevcut. Fiziksel cihazda Game Center authentication ve Game Center kimliğiyle UGS Authentication başarıyla doğrulandı |
| 2026-08-08 | Fiziksel cihaz logunda leaderboard submit akışının `Default constructor not found for type Apple.GameKit.Leaderboards.GKLeaderboard` nedeniyle tekrar kuyruğa düştüğü görüldü. `GKLeaderboard` tipi `link.xml` ile korundu ve başarılı skor gönderimi için pozitif cihaz logu eklendi. | `Assets/link.xml`, `IosPlatformAdapter`, güncel stripped `Apple.GameKit.dll` ve üretilen `Apple.GameKit.cpp` | Unity append build 0 hata ile tamamlandı; `GKLeaderboard(IntPtr)` final iOS çıktısında doğrulandı. Son fiziksel cihaz submit testi bekliyor |
| 2026-08-08 | Windows ve macOS aynı `main` branch üzerinde çalışacak; platform ayrımı Build Profile, asmdef ve compile guard ile korunacak. Tüm makineler tam Unity `6000.3.10f1` kullanacak. | Proje sahibi kararı + `AGENTS.md` + iki committed Build Profile | Onaylandı / çalışma sözleşmesine işlendi |
| 2026-08-08 | Apple.Core `3.2.0`, Apple.GameKit `4.0.1`, Google Mobile Ads `11.3.0` ve EDM4U `1.2.187` paketleri makineye özel mutlak yol/OpenUPM bağımlılığı olmadan `Packages/` altında embedded tutulacak. | `Packages/manifest.json`, `packages-lock.json`, embedded package dizinleri + Unity package resolve/compile | Doğrulandı; Windows temiz pull/import testi bekliyor |
| 2026-08-08 | CrazyGames WebGL regression build'i `CrazyGames WebGL` profilinden tamamlandı. | Unity Build Report `build-b6f87e2660`, `/tmp/ScrapTheDead-WebGL-CommitAudit` | Başarılı: 0 hata, 174 warning, 111.94 MB; tarayıcı smoke testi Windows'ta yapılacak |
| 2026-08-08 | Unity 6.3 vHierarchy `IEnumerable<int>` cast exception'ı ve WebGL DestroyIt SpeedTree LOD keyword compile hatası dar uyumluluk yamalarıyla giderildi. | Unity reimport + WebGL build | Doğrulandı; WebGL build 0 hata |
| 2026-08-09 | Embedded paket taşınabilirliği sonrasında hedef tekrar iOS'a alındı; `iOS App Store` profili ve yalnızca `Demo_iOS.unity` sahnesi etkin. | Unity MCP project/profile/scene bilgisi + iOS Tundra script compile + release guard | Doğrulandı; `error CS` yok ve `iOS App Store configuration is complete.` |
