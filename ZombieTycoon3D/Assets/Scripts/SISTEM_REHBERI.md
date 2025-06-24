# 🚗 ZombieTycoon3D - Modüler Upgrade Sistemi Rehberi

## 📁 Sistem Genel Bakış

### Yeni Akış:
1. **MainMenu Scene** → GameManager oluşur (DontDestroyOnLoad)
2. **Play** → Game Scene'e geç, seçili araç spawn olur
3. **Zombi Öldürme** → Para kazanılır 
4. **Ölüm** → Game Over panel'i, kazanılan para gösterilir
5. **Main Menu** → Market ve Garage panelleri
6. **Market** → Araç/upgrade satın al → **Inventory'ye gider**
7. **Garage** → Inventory'den araç/upgrade "use" et → **Equipped olur**
8. **Play** → Game scene'e dön

### Key Difference (Önemli Fark):
- **Market'ten alım** = **Inventory'ye** eklenir (henüz kullanılmıyor)
- **Garage'da "Use"** = **Equipped** hale gelir (game'de aktif)

### 🎯 **Basit GameManager Yaklaşımı:**
- GameManager ana menüde prefab olarak oluşturulur
- Inspector'da catalog'lar ayarlanır
- DontDestroyOnLoad ile sahne geçişlerinde korunur
- Resources klasörü gereksinimi YOK!

---

## 🎮 SAHNELER ve GEREKLI SCRIPTLER

### 1. **MainMenu Scene** (Ana menü - GameManager başlangıç noktası)
```
Gerekli Script'ler:
├── GameManager (Ana menüde prefab olarak)
├── InventoryManager (GameManager tarafından otomatik oluşturulur)
├── MainMenuManager
├── ModernMarketManager
├── ModernGarageManager
└── UI Prefab'ları (aşağıda detaylı)
```

### 2. **Gameplay Scene** (Zombie öldürme sahnesi)
```
Gerekli Script'ler:
├── GameplayUIManager 
├── Player
├── Enemy'ler
├── SpawnerManager
└── ScoreManager

NOT: GameManager ve InventoryManager zaten DontDestroyOnLoad 
     ile geldiği için bu sahnede ayrıca oluşturulmaz!
```

**KRITIK:** Oyun artık **MainMenu → Gameplay** akışı ile çalışır. 
Gameplay sahnesinden direkt başlatma artık desteklenmiyor (ve gerek de yok).

---

## 🏗️ UNITY INSPECTOR KURULUMU (ADIM ADIM)

## 🎯 **0. GameManager Prefab Kurulumu (İLK ADIM!)**

### **ADIM 0.1: GameManager Prefab Oluştur**
```
1. Hierarchy'de sağ tık → Create Empty
2. Name: "GameManager"
3. Add Component: GameManager script
4. Inspector'da catalog'ları doldur:
   ├── Car Catalog (Size: 10) ← CarDefinition asset'lerini sürükle
   └── Upgrade Catalog (Size: 15) ← CarUpgrade asset'lerini sürükle
5. GameManager'ı Prefab yap:
   - Assets/Prefabs/ klasörü oluştur
   - GameManager'ı Prefabs klasörüne sürükle
   - Prefab name: "GameManager.prefab"
```

### **ADIM 0.2: MainMenu Sahnesine GameManager Ekle**
```
1. MainMenu sahnesini aç
2. GameManager.prefab'ını MainMenu sahnesine sürükle
3. Hierarchy'de GameManager'ın olduğunu doğrula
4. Test: Play butonuna bas → Console'da "GameManager initialized!" görmeli
```

**⚠️ UYARI:** Bu adım yapılmadan diğer adımlar çalışmaz! GameManager olmadan 
Market/Garage sistemleri hata verir.

---

## 📱 **1. MainMenu Scene Setup**

### **ADIM 1: Ana Canvas Oluştur**
```
1. Sağ tık → UI → Canvas
2. Canvas name: "MainMenuCanvas"
3. Canvas Scaler: Scale With Screen Size
4. Reference Resolution: 1920x1080
```

### **ADIM 2: MainMenuManager GameObject - DETAYLI AÇIKLAMA**
```
1. MainMenuCanvas'ta sağ tık → Create Empty
2. Name: "MainMenuManager" 
3. Add Component: MainMenuManager script
4. ⚠️ Inspector'da alanları DOLDUR:

[UI References] - Script'te tanımlı alanlar:
├── Coins Text: CoinsText TextMeshPro'yu sürükle
├── Play Button: PlayButton'u sürükle  
├── Garage Button: GarageButton'u sürükle
├── Market Button: MarketButton'u sürükle

[Panels] - Script'te tanımlı panel alanları:
├── Main Menu Panel: mainMenuPanel GameObject'ini sürükle (ana butonların olduğu panel)
├── Garage Panel: GaragePanel'i sürükle
├── Market Panel: MarketPanel'i sürükle
└── Fade Background: FadeBackground CanvasGroup'unu sürükle

[Animations] - Script'te tanımlı animasyon ayarları:
├── Animation Duration: 0.4 (saniye)
└── Animation Ease: OutCubic (açılır listeden seç)
```

### **ADIM 3: Ana UI Elemanlarını Oluştur - DETAYLI AÇIKLAMA**

#### **A) Coins Display (Para göstergesi) - DETAYLI AÇIKLAMA**
```
1. MainMenuCanvas'ta sağ tık → UI → Text - TextMeshPro
2. Name: "CoinsText"
3. RectTransform ayarları:
   - Anchor Presets: "top-left"
   - Position X: 100, Y: -50 (sol üst köşe)
   - Width: 200, Height: 50
4. TextMeshPro ayarları:
   - Text: "💰 0"
   - Font Size: 36
   - Font Style: Bold
   - Alignment: Center
   - Color: Altın sarısı (255,215,0,255)
```

#### **B) Ana Butonlar - DETAYLI AÇIKLAMA**
```
🎮 PlayButton:
1. MainMenuCanvas'ta sağ tık → UI → Button - TextMeshPro
2. Name: "PlayButton"
3. RectTransform:
   - Anchor: "bottom-center"
   - Position X: 0, Y: 150
   - Width: 200, Height: 60
4. Button ayarları:
   - Normal Color: Yeşil (0,200,0,255)
5. Text child ayarları:
   - Text: "🎮 PLAY"
   - Font Size: 24, Font Style: Bold

🏠 GarageButton:
1. MainMenuCanvas'ta sağ tık → UI → Button - TextMeshPro
2. Name: "GarageButton"
3. RectTransform:
   - Anchor: "bottom-center"
   - Position X: -120, Y: 70 (PlayButton'un altında, solda)
   - Width: 180, Height: 50
4. Button ayarları:
   - Normal Color: Mavi (0,150,255,255)
5. Text: "🏠 GARAGE"

🛒 MarketButton:
1. MainMenuCanvas'ta sağ tık → UI → Button - TextMeshPro
2. Name: "MarketButton"
3. RectTransform:
   - Anchor: "bottom-center"
   - Position X: 120, Y: 70 (PlayButton'un altında, sağda)
   - Width: 180, Height: 50
4. Button ayarları:
   - Normal Color: Turuncu (255,165,0,255)
5. Text: "🛒 MARKET"
```

#### **C) Fade Background - DETAYLI AÇIKLAMA**
```
1. MainMenuCanvas'ta sağ tık → UI → Image
2. Name: "FadeBackground"
3. RectTransform ayarları:
   - Anchor Presets: Alt+Shift basılı tut → "stretch" seç (tam ekran)
   - Left, Right, Top, Bottom: 0 (tam ekran doldur)
4. Image component ayarları:
   - Source Image: None (düz renk)
   - Color: Siyah (0,0,0,128) - yarı saydam
5. Add Component: CanvasGroup
6. CanvasGroup ayarları:
   - Alpha: 0 (başlangıçta görünmez)
   - Interactable: ✗ (tıklanamaz)
   - Blocks Raycasts: ✓ (arka planı kapatır)
7. ⚠️ Inspector'da FadeBackground checkbox'ını KAPAT (başlangıçta gizli)
```

#### **D) Main Menu Panel Container - DETAYLI AÇIKLAMA**
```
1. MainMenuCanvas'ta sağ tık → Create Empty
2. Name: "MainMenuPanel"
3. RectTransform: Stretch (tam ekran)
4. Bu panel'in içine PlayButton, GarageButton, MarketButton'u TAŞI:
   - PlayButton'u MainMenuPanel'in child'ı yap
   - GarageButton'u MainMenuPanel'in child'ı yap  
   - MarketButton'u MainMenuPanel'in child'ı yap
   - (Hierarchy'de sürükleyerek taşı)
```

---

## 🛒 **2. Market Panel Detaylı Setup**

### **ADIM 1: Market Panel Container**
```
1. Sağ tık MainMenuCanvas → Create Empty
2. Name: "MarketPanel"
3. Add Component: CanvasGroup
4. RectTransform: Stretch to fill screen
5. Add Component: ModernMarketManager script
```

### **ADIM 2: Tab System (Sekme Sistemi)**

#### **A) Tab Header Area - TAM DETAYLI AÇIKLAMA**
```
1. MarketPanel'de sağ tık → UI → Image
2. Name: "TabHeader"
3. RectTransform ayarları (Inspector'da):
   - Anchor Presets: "top-stretch" (üst tarafa yapışık, genişliği tam ekran)
   - Left: 0, Right: 0 (yan kenarları tam doldursun)
   - Top: 0 (en üst kenar)
   - Height: 80 (sabit yükseklik 80 pixel)
4. Image component ayarları:
   - Source Image: None (düz renk)
   - Color: Koyu gri (60,60,60,255) - koyu arka plan
```

#### **B) Cars Tab Button - TAM DETAYLI AÇIKLAMA**
```
1. TabHeader'da sağ tık → UI → Button - TextMeshPro
2. Name: "CarsTabButton"
3. RectTransform ayarları (Inspector'da):
   - Anchor Presets: "top-left"
   - Position X: 0, Y: 0 (sol üst köşe)
   - Width: 480 (ekranın yarısı - 960'ın yarısı)
   - Height: 80 (TabHeader ile aynı yükseklik)
4. Button component ayarları:
   - Normal Color: Açık gri (200,200,200,255)
   - Highlighted Color: Beyaz (255,255,255,255)
5. Text child ayarları:
   - Text: "🚗 CARS"
   - Font Size: 20
   - Font Style: Bold
   - Alignment: Center
```

#### **C) Upgrades Tab Button - TAM DETAYLI AÇIKLAMA**
```
1. TabHeader'da sağ tık → UI → Button - TextMeshPro
2. Name: "UpgradesTabButton"
3. RectTransform ayarları (Inspector'da):
   - Anchor Presets: "top-right"
   - Position X: 0, Y: 0 (sağ üst köşe)
   - Width: 480 (ekranın yarısı)
   - Height: 80 (TabHeader ile aynı yükseklik)
4. Button component ayarları:
   - Normal Color: Açık gri (200,200,200,255)
   - Highlighted Color: Beyaz (255,255,255,255)
5. Text child ayarları:
   - Text: "🔧 UPGRADES"
   - Font Size: 20
   - Font Style: Bold
   - Alignment: Center
```

#### **D) Tab Indicators (Aktif sekme göstergesi) - TAM DETAYLI AÇIKLAMA**
```
🚗 Cars Tab Indicator:
1. CarsTabButton'da sağ tık → UI → Image
2. Name: "CarsTabIndicator"
3. RectTransform ayarları (Inspector'da):
   - Anchor Presets: "bottom-center"
   - Position X: 0, Y: 0 (butonun alt ortası)
   - Width: 100 (ince çizgi genişliği)
   - Height: 5 (ince çizgi yüksekliği)
4. Image component ayarları:
   - Source Image: None (düz renk)
   - Color: Parlak mavi (0,150,255,255) - aktif gösterge

🔧 Upgrades Tab Indicator:
1. UpgradesTabButton'da sağ tık → UI → Image
2. Name: "UpgradesTabIndicator"
3. RectTransform ayarları: CarsTabIndicator ile AYNI
4. Image component ayarları: CarsTabIndicator ile AYNI
5. ⚠️ ÖNEMLİ: Inspector'da checkbox'ı KAPAT (başlangıçta gizli)
```

### **ADIM 3: Content Areas (İçerik alanları)**

#### **A) Cars Content Area - DETAYLI AÇIKLAMA**
```
1. MarketPanel'de sağ tık → UI → Scroll View
2. Yeni oluşan Scroll View'in adını "CarsContent" yap
3. ⚠️ "Tab'ların altı, tüm ekran" açıklaması:
   - CarsContent'in RectTransform'unu aç
   - Anchor Presets: Alt+Shift basılı tut → "stretch" seç (tam ekran doldurur)
   - Top: 80 (TabHeader'ın yüksekliği kadar boşluk bırak)
   - Left, Right, Bottom: 0 (kenarları tam doldur)
   
4. CarsContent → Inspector → Scroll Rect component:
   - Movement Type: Elastic (kaydırırken esnek davransın)
   - Scrollbar Visibility: Auto Hide (gerekmedikçe scrollbar gizli)
   
5. CarsContent'in child'ı olan "Content" objesini seç:
   - Name: "CarShopParent" olarak değiştir
   - Add Component: Vertical Layout Group
   - Vertical Layout Group ayarları (Inspector'da):
     ✅ Control Child Size → Width: ✓ Height: ✓
     ✅ Use Child Scale → Width: ✓ Height: ✓  
     ✅ Child Force Expand → Width: ✓ Height: ✗
     - Spacing: 10 (elemanlar arası 10 pixel boşluk)
```

#### **B) Upgrades Content Area - DETAYLI AÇIKLAMA**
```
1. MarketPanel'de sağ tık → UI → Scroll View
2. Name: "UpgradesContent"
3. RectTransform ayarları AYNI (CarsContent ile aynı):
   - Anchor: stretch, Top: 80, Left/Right/Bottom: 0
4. Scroll Rect ayarları AYNI (CarsContent ile aynı)
5. Content child'ının adı: "UpgradeShopParent" 
6. Layout ayarları AYNI (CarsContent ile aynı)
7. ⚠️ ÖNEMLİ: Inspector'da UpgradesContent objesini seç
   - En üstteki checkbox'ı KAPAT (başlangıçta görünmez)
```

### **ADIM 4: ModernMarketManager Inspector Setup - NET AÇIKLAMA**
```
1. MarketPanel objesini seç
2. Inspector'da ModernMarketManager component'ini bul
3. Aşağıdaki alanları SÜRÜKLEYEREK doldur:

[Tab System] - Bu alanlar script'te tanımlı:
├── Cars Tab Button: Hierarchy'den CarsTabButton'u sürükle
├── Upgrades Tab Button: Hierarchy'den UpgradesTabButton'u sürükle  
├── Cars Content: Hierarchy'den CarsContent'i sürükle
├── Upgrades Content: Hierarchy'den UpgradesContent'i sürükle
├── Cars Tab Indicator: Hierarchy'den CarsTabIndicator'u sürükle
└── Upgrades Tab Indicator: Hierarchy'den UpgradesTabIndicator'u sürükle

[Car Shop] - Bu alanlar script'te tanımlı:
├── Car Shop Parent: Hierarchy'den CarShopParent'i sürükle 
└── Car Item Prefab: Assets'den ModernCarShopItem prefab'ını sürükle

[Upgrade Shop] - Bu alanlar script'te tanımlı:
├── Upgrade Shop Parent: Hierarchy'den UpgradeShopParent'i sürükle
└── Upgrade Item Prefab: Assets'den ModernUpgradeShopItem prefab'ını sürükle

[Colors] - Bu alanlar script'te tanımlı:
├── Active Tab Color: Beyaz (255,255,255,255) renk seç
└── Inactive Tab Color: Gri (128,128,128,255) renk seç
```

---

## 🏠 **3. Garage Panel Detaylı Setup**

### **ADIM 1: Garage Panel Container - DETAYLI AÇIKLAMA**
```
1. MainMenuCanvas'ta sağ tık → Create Empty
2. Name: "GaragePanel"  
3. Add Component: CanvasGroup (animasyonlar için gerekli)
4. Add Component: ModernGarageManager script
5. RectTransform ayarları:
   - Anchor Presets: Alt+Shift basılı tut → "stretch" seç
   - Left, Right, Top, Bottom: 0 (tam ekran doldur)
6. ⚠️ ÖNEMLİ: Inspector'da GaragePanel objesini seç
   - En üstteki checkbox'ı KAPAT (başlangıçta görünmez)
```

### **ADIM 2: Tab System - DETAYLI AÇIKLAMA (Market ile AYNI)**
```
1. GaragePanel'de sağ tık → UI → Image
   - Name: "TabHeader"
   - RectTransform: Anchor "top-stretch", Height: 80
   - Color: Koyu arka plan rengi

2. TabHeader'da sağ tık → UI → Button - TextMeshPro
   - Name: "CarsTabButton"
   - Text: "🚗 CARS"
   - RectTransform: Anchor "left", Width: ekranın yarısı
   
3. TabHeader'da sağ tık → UI → Button - TextMeshPro
   - Name: "UpgradesTabButton"  
   - Text: "🔧 UPGRADES"
   - RectTransform: Anchor "right", Width: ekranın yarısı

4. CarsTabButton'da sağ tık → UI → Image
   - Name: "CarsTabIndicator"
   - Size: 50x5 (ince çizgi), Position: butonun alt kenarı
   
5. UpgradesTabButton'da sağ tık → UI → Image
   - Name: "UpgradesTabIndicator"
   - Size: 50x5, Position: butonun alt kenarı
```

### **ADIM 3: Cars Content Area - DETAYLI AÇIKLAMA**

#### **A) Cars Content Container**
```
1. GaragePanel'de sağ tık → Create Empty
2. Name: "CarsContent"
3. RectTransform ayarları:
   - Anchor Presets: Alt+Shift basılı tut → "stretch" seç
   - Top: 80 (TabHeader yüksekliği), Left/Right/Bottom: 0
4. Add Component: Vertical Layout Group
5. Vertical Layout Group ayarları (Inspector'da):
   - Spacing: 20 (elemanlar arası boşluk)
   - Child Alignment: Upper Center (üstte ortalı)
   - Control Child Size: Width ✓, Height ✗ (genişliği kontrol et, yüksekliği serbest)
   - Use Child Scale: Width ✓, Height ✓
   - Child Force Expand: Width ✓, Height ✗ (genişliği zorla doldur)
```

#### **B) Selected Car Display - DETAYLI AÇIKLAMA**
```
1. CarsContent'te sağ tık → UI → Text - TextMeshPro
2. Name: "SelectedCarText"
3. Text: "Selected: None"
4. ⚠️ ÖNEMLİ: RectTransform manuel ayarlanmaz! 
   CarsContent'te Vertical Layout Group var, otomatik boyutlandırır
5. Layout Element ekle (opsiyonel boyut kontrolü için):
   - Add Component: Layout Element
   - Preferred Height: 40 (istediğin yükseklik)
6. TextMeshPro ayarları:
   - Font Size: 24
   - Alignment: Center
   - Font Style: Bold
```

#### **C) Inventory Cars Section - DETAYLI AÇIKLAMA**
```
1. CarsContent'te sağ tık → UI → Text - TextMeshPro
   - Name: "InventoryLabel" 
   - Text: "📦 INVENTORY"
   - Font Size: 18, Style: Bold
   - Layout Element: Preferred Height: 30

2. CarsContent'te sağ tık → UI → Scroll View
   - Name: "InventoryCarsScrollView"
   - Layout Element ekle: Preferred Height: 200 (sabit yükseklik istiyorsan)
   
3. Scroll View'in "Content" child'ını seç:
   - Name: "InventoryCarsParent" olarak değiştir
   - Add Component: Vertical Layout Group
   - Layout ayarları: Spacing: 5, Child Force Expand: Width ✓
```

#### **D) Garage Cars Section - DETAYLI AÇIKLAMA**
```
1. CarsContent'te sağ tık → UI → Text - TextMeshPro  
   - Name: "GarageLabel"
   - Text: "🏠 GARAGE"
   - Font Size: 18, Style: Bold
   - Layout Element: Preferred Height: 30

2. CarsContent'te sağ tık → UI → Scroll View
   - Name: "GarageCarsScrollView"  
   - Layout Element ekle: Preferred Height: 200
   
3. Scroll View'in "Content" child'ını seç:
   - Name: "GarageCarsParent" olarak değiştir
   - Layout ayarları AYNI (InventoryCarsParent ile aynı)
```

### **ADIM 4: Upgrades Content Area - DETAYLI AÇIKLAMA**

#### **A) Upgrades Content Container**
```
1. GaragePanel'de sağ tık → Create Empty
2. Name: "UpgradesContent"
3. RectTransform ayarları: CarsContent ile AYNI
4. ⚠️ ÖNEMLİ: Inspector'da checkbox'ı KAPAT (başlangıçta gizli)
```

#### **B) Inventory Upgrades Section**
```
1. UpgradesContent'te sağ tık → UI → Text - TextMeshPro
   - Text: "📦 INVENTORY UPGRADES"
2. UpgradesContent'te sağ tık → UI → Scroll View
   - Content name: "InventoryUpgradesParent"
   - Layout: Vertical Layout Group
```

#### **C) Equipped Upgrades Section**  
```
1. UpgradesContent'te sağ tık → UI → Text - TextMeshPro
   - Text: "⚙️ EQUIPPED UPGRADES"
2. UpgradesContent'te sağ tık → UI → Scroll View
   - Content name: "EquippedUpgradesParent"
   - Layout: Vertical Layout Group
```

### **ADIM 5: ModernGarageManager Inspector Setup - NET AÇIKLAMA**
```
1. GaragePanel objesini seç
2. Inspector'da ModernGarageManager component'ini bul
3. Aşağıdaki alanları SÜRÜKLEYEREK doldur (Market'teki gibi):

[Tab System]:
├── Cars Tab Button: CarsTabButton'u sürükle
├── Upgrades Tab Button: UpgradesTabButton'u sürükle
├── Cars Content: CarsContent'i sürükle  
├── Upgrades Content: UpgradesContent'i sürükle
├── Cars Tab Indicator: CarsTabIndicator'u sürükle
└── Upgrades Tab Indicator: UpgradesTabIndicator'u sürükle

[Cars Section]:
├── Inventory Cars Parent: InventoryCarsParent'i sürükle
├── Garage Cars Parent: GarageCarsParent'i sürükle
├── Car Item Prefab: Assets'den GarageCarItem prefab'ını sürükle
└── Selected Car Text: SelectedCarText'i sürükle

[Upgrades Section]:
├── Inventory Upgrades Parent: InventoryUpgradesParent'i sürükle
├── Equipped Upgrades Parent: EquippedUpgradesParent'i sürükle
└── Upgrade Item Prefab: Assets'den GarageUpgradeItem prefab'ını sürükle
```

---

## 🎨 **4. UI PREFAB'LARINI OLUŞTURMA**

### **ModernCarShopItem Prefab (Market'te araç satın alma) - DETAYLI AÇIKLAMA**

#### **ADIM 1: Base Prefab Oluşturma**
```
1. Hierarchy'de sağ tık → UI → Image
2. Name: "ModernCarShopItem"
3. RectTransform ayarları:
   - Width: 300, Height: 120
   - Anchor: "middle-center" 
4. Image component ayarları:
   - Color: Açık gri (200,200,200,255)
   - Image Type: Sliced (kenarlar düzgün görünsün)
5. Add Component: ModernCarShopItem script
```

#### **ADIM 2: Child UI Elements - DETAYLI AÇIKLAMA**
```
🖼️ CarIcon (Image):
1. ModernCarShopItem'da sağ tık → UI → Image
2. Name: "CarIcon"
3. RectTransform:
   - Anchor: "middle-left"
   - Position X: 50, Y: 0 (sol tarafta ortalı)
   - Width: 80, Height: 80
4. Image component:
   - Preserve Aspect: ✓ (orantıyı korur)

📝 CarNameText (TextMeshPro):
1. ModernCarShopItem'da sağ tık → UI → Text - TextMeshPro
2. Name: "CarNameText"
3. RectTransform:
   - Anchor: "top-stretch" 
   - Left: 100 (icon'dan sonra), Right: 150 (price için yer bırak)
   - Top: 10, Height: 25
4. TextMeshPro ayarları:
   - Text: "Car Name"
   - Font Size: 18
   - Font Style: Bold
   - Alignment: Left

📊 CarStatsText (TextMeshPro):  
1. ModernCarShopItem'da sağ tık → UI → Text - TextMeshPro
2. Name: "CarStatsText"
3. RectTransform:
   - Anchor: "top-stretch"
   - Left: 100, Right: 150 (CarNameText ile aynı)
   - Top: 40, Height: 60
4. TextMeshPro ayarları:
   - Text: "Speed: 100\nHealth: 200"
   - Font Size: 12
   - Alignment: Left

💰 PriceText (TextMeshPro):
1. ModernCarShopItem'da sağ tık → UI → Text - TextMeshPro
2. Name: "PriceText"
3. RectTransform:
   - Anchor: "top-right"
   - Position X: -75, Y: -20
   - Width: 120, Height: 30
4. TextMeshPro ayarları:
   - Text: "💰 1000"
   - Font Size: 16
   - Font Style: Bold
   - Alignment: Center

🟢 BuyButton (Button):
1. ModernCarShopItem'da sağ tık → UI → Button - TextMeshPro
2. Name: "BuyButton"
3. RectTransform:
   - Anchor: "bottom-right"
   - Position X: -45, Y: 20
   - Width: 80, Height: 30
4. Button ayarları:
   - Normal Color: Yeşil (0,200,0,255)
   - Text: "BUY"

📺 WatchAdButton (Button):
1. ModernCarShopItem'da sağ tık → UI → Button - TextMeshPro
2. Name: "WatchAdButton"
3. RectTransform:
   - Anchor: "bottom-right"
   - Position X: -130, Y: 20 (BuyButton'un yanında)
   - Width: 60, Height: 30
4. Button ayarları:
   - Normal Color: Mavi (0,100,200,255)
   - Text: "📺 AD"

✅ OwnedIndicator (Image):
1. ModernCarShopItem'da sağ tık → UI → Image
2. Name: "OwnedIndicator"
3. RectTransform:
   - Anchor: "top-left"
   - Position X: 10, Y: -10
   - Width: 30, Height: 30
4. Image ayarları:
   - Color: Yeşil (0,255,0,255)
   - Sprite: Checkmark icon (Unity'nin default'u)
5. ⚠️ Inspector'da checkbox'ı KAPAT (başlangıçta gizli)

🔒 LockedIndicator (Image):
1. ModernCarShopItem'da sağ tık → UI → Image
2. Name: "LockedIndicator"
3. RectTransform:
   - Anchor: "top-right"
   - Position X: -10, Y: -10
   - Width: 30, Height: 30
4. Image ayarları:
   - Color: Kırmızı (255,0,0,255)
   - Sprite: Lock icon
5. ⚠️ Inspector'da checkbox'ı KAPAT (başlangıçta gizli)
```

#### **ADIM 3: ModernCarShopItem Script Inspector - DETAYLI AÇIKLAMA**
```
1. ModernCarShopItem objesini seç
2. Inspector'da ModernCarShopItem component'ini bul
3. Aşağıdaki alanları SÜRÜKLEYEREK doldur:

[UI References] - Script'te tanımlı alanlar:
├── Car Icon: CarIcon Image'ını sürükle
├── Car Name Text: CarNameText'i sürükle
├── Car Stats Text: CarStatsText'i sürükle
├── Price Text: PriceText'i sürükle
├── Buy Button: BuyButton'u sürükle
├── Watch Ad Button: WatchAdButton'u sürükle
├── Owned Indicator: OwnedIndicator GameObject'ini sürükle
└── Locked Indicator: LockedIndicator GameObject'ini sürükle

[Button States] - Script'te tanımlı renk alanları:
├── Buy Button Normal Color: Yeşil (0,200,0,255)
├── Buy Button Disabled Color: Gri (128,128,128,255)
└── Owned Button Color: Mavi (0,150,255,255)

4. ⚠️ ÖNEMLİ: Prefab oluştur:
   - Assets/Prefabs/ klasörü oluştur (yoksa)
   - ModernCarShopItem'ı Prefabs klasörüne sürükle
   - Hierarchy'deki orijinali sil
```

### **ModernUpgradeShopItem Prefab (Market'te upgrade satın alma)**

#### **Benzer yapı, ek olarak:**
```
QuantityText (TextMeshPro):
├── Position: Sol alt köşe
├── Text: "Owned: 0"
└── SetActive: false (başlangıçta)
```

### **GarageCarItem Prefab (Garage'da araç gösterimi)**

#### **Daha basit yapı:**
```
CarIcon + CarNameText + QuantityText + ActionButton + SelectedIndicator

ActionButton metni:
├── Inventory Mode: "USE"  
└── Garage Mode: "SELECT" veya "SELECTED"
```

### **GarageUpgradeItem Prefab (Garage'da upgrade gösterimi)**

#### **Upgrade bilgileri + action:**
```
UpgradeIcon + UpgradeNameText + UpgradeStatsText + QuantityText + ActionButton + SlotIndicator

ActionButton metni:
├── Inventory Mode: "EQUIP"
└── Equipped Mode: "UNEQUIP"

SlotIndicator (Text):
├── Content: "FrontBumper", "Roof", vs.
└── Style: Küçük, badge şeklinde
```

---

## 📝 **5. CARDEFINITION SETUP**

### **Inspector'da Doldurulacaklar:**
```
Car Definition Asset:
├── carId: "BasicCar" (unique string)
├── displayName: "Basic Car" (UI'da gösterilecek)
├── carIcon: Sprite asset (64x64 önerilen)
├── price: 0 (başlangıç aracı ücretsiz)
├── carPrefab: 3D araç GameObject
├── baseMaxSpeed: 100
├── baseAcceleration: 50
└── baseHealth: 100
```

### **10 Araç Önerisi + Fiyatları:**
```
1. BasicCar - 0 coin (ücretsiz başlangıç)
2. SportsCar - 500 coin  
3. TruckMonster - 1000 coin
4. RaceCar - 1500 coin (🚨 reklam gerekli)
5. ArmoredVan - 2000 coin
6. Buggy - 2500 coin (🚨 reklam gerekli)
7. Limousine - 3000 coin
8. TankCar - 4000 coin (🚨 reklam gerekli)  
9. SuperCar - 5000 coin
10. ApocalypseTruck - 7500 coin (🚨 reklam gerekli)
```

---

## 🔧 **6. CARUPGRADE SETUP**

### **Slot Types:**
```
- FrontBumper (ön tampon)
- RearBumper (arka tampon)  
- LeftDoor (sol kapı)
- RightDoor (sağ kapı)
- Roof (tavan)
```

### **Örnek Upgrade Assets:**
```
SpikedRam (FrontBumper):
├── upgradeName: "Spiked Ram"
├── upgradeSlot: FrontBumper
├── upgradePrefab: 3D spike model
├── maxSpeedModifier: +5
├── accelerationModifier: 0
└── healthModifier: +20

ArmoredDoors (LeftDoor/RightDoor):
├── upgradeName: "Armored Doors"  
├── upgradeSlot: LeftDoor (ayrı asset RightDoor için)
├── maxSpeedModifier: -2
├── accelerationModifier: -1
└── healthModifier: +50

TurboEngine (RearBumper):
├── upgradeName: "Turbo Engine"
├── upgradeSlot: RearBumper
├── maxSpeedModifier: +15
├── accelerationModifier: +10
└── healthModifier: 0
```

---

## 🎮 **7. OYUN TEST AKIŞI**

### **İlk Kurulum Test:**
```
1. ✅ GameManager prefab'ı oluştur ve catalog'ları doldur
2. ✅ MainMenu sahnesine GameManager prefab'ını ekle
3. ✅ MainMenu sahnesinde tüm UI'ları kur  
4. ✅ Prefab'ları oluştur ve referansları bağla
5. ✅ Car Definition asset'leri oluştur
6. ✅ CarUpgrade asset'leri oluştur
```

### **Test Sırası (MUTLAKA BU SIRADA!):**
```
🔸 ADIM 1: MainMenu Sahnesi Test
1. ▶️ MainMenu sahnesini aç ve Play
2. ✅ Console: "GameManager initialized!" görmeli
3. ✅ Coins UI: "💰 0" görmeli (başlangıç parası)
4. ✅ Market butonuna tıkla → Market panel açılmalı
5. ✅ Garage butonuna tıkla → Garage panel açılmalı

🔸 ADIM 2: Market Test
1. 🛒 Market panel → Cars tab'ına tıkla
2. ✅ BasicCar görünmeli (owned = true, çünkü başlangıç hediyesi)
3. ✅ Diğer araçlar görünmeli (buy button aktif)
4. 🛒 Market panel → Upgrades tab'ına tıkla  
5. ✅ Upgrade'ler görünmeli (buy button aktif)

🔸 ADIM 3: Garage Test
1. 🏠 Garage panel → Cars tab'ına tıkla
2. ✅ "Selected: BasicCar" yazısı görünmeli
3. ✅ Garage Cars altında BasicCar görünmeli
4. ✅ Inventory Cars boş olmalı (başlangıçta)

🔸 ADIM 4: Gameplay Geçiş Test
1. 🎮 Play butonuna tıkla
2. ✅ Gameplay sahnesine geçmeli
3. ✅ BasicCar spawn olmalı
4. ✅ UI'da coins gösterilmeli
```

### **Tam Oyun Akışı Test:**
```
🎯 BAŞLANGIÇ: MainMenu sahnesinden başla

1. ▶️ MainMenu Play → Gameplay sahne
2. 🧟 Zombi öldür → Para kazan (örn: 100 coin)
3. 💀 Öl → GameOver panel → "Return to Menu"
4. 🏠 MainMenu → Coins: "💰 100" görmeli

5. 🛒 Market → SportsCar satın al (500 coin - yeterli değil)
6. 🎮 Play → Tekrar gameplay → Daha fazla para kazan
7. 🏠 Return → Market → SportsCar satın al (artık yeterli)
8. ✅ Console: "Market: SportsCar purchased for 500 coins"

9. 🏠 Garage → Inventory Cars → SportsCar "USE" et
10. ✅ Console: "Garage: SportsCar moved from inventory and selected"
11. ✅ Selected Car text: "Selected: SportsCar"

12. 🎮 Play → SportsCar ile gameplay başlamalı
13. 🛒 Market → SpikedRam upgrade satın al
14. 🏠 Garage → Upgrades tab → SpikedRam "EQUIP" et
15. ✅ Console: "Garage: SpikedRam equipped to SportsCar"

16. 🎮 Play → SportsCar + SpikedRam ile oyna
```

### **Debug Console Mesajları (Başarılı Test):**
```
✅ "GameManager initialized!"
✅ "Market: SportsCar purchased for 500 coins"
✅ "Garage: SportsCar moved from inventory and selected"  
✅ "Market: SpikedRam purchased for 200 coins"
✅ "Garage: SpikedRam equipped to SportsCar"
✅ "Game saved successfully!"
```

### **❌ Hata Durumları:**
```
❌ "GameManager.Instance is null" → GameManager prefab'ı eksik
❌ "Car catalog is empty" → Inspector'da catalog'lar doldurulmamış
❌ "No car selected" → Garage'da araç seçilmemiş
❌ Market'te araçlar görünmüyor → CarDefinition asset'leri eksik
❌ UI referans hataları → Prefab'larda Inspector eksik
```

## ⚡ **Hızlı Sorun Giderme:**
```
🔧 Market boş görünüyor:
   → GameManager Inspector'da Car/Upgrade Catalog'ları kontrol et

🔧 "Play" butonu çalışmıyor:
   → Garage'da araç seçili olduğunu kontrol et
   → Console'da "No car selected" hatası varsa Garage'a git

🔧 Gameplay'de araç spawn olmuyor:
   → GameManager.selectedCarId boş olabilir
   → CarDefinition'da carPrefab eksik olabilir

🔧 Save sistemi çalışmıyor:
   → Unity Application.persistentDataPath yazma izni
   → JSON hataları için Console'a bak
```

---

## 🚨 **8. REKLAM SİSTEMİ TODO'LARI**

### **Code'da Reklam Entegrasyon Noktaları:**
```csharp
// ModernCarShopItem.cs - Line 165
private void OnWatchAdClicked()
{
    // TODO: GERÇEK REKLAM SİSTEMİ
    // AdManager.ShowRewardedAd(() => UnlockCar());
}

// ModernUpgradeShopItem.cs - Line 215  
private void OnWatchAdClicked()
{
    // TODO: GERÇEK REKLAM SİSTEMİ
    // AdManager.ShowRewardedAd(() => UnlockUpgrade());
}
```

---

## 🎉 **SİSTEM TAMAM!**

Bu rehberi takip ederek:
- ✅ **Tam functional market + garage sistemi**
- ✅ **Inventory/Equipped ayrımı** 
- ✅ **Modern animasyonlu UI**
- ✅ **Save system**
- ✅ **Reklam hazırlığı**

Artık Unity'de adım adım kurmaya başlayabilirsiniz! 🚀 

---

## 📋 **HIZLI BAŞLANGIÇ ÖZETI**

### **🎯 3 Ana Adım:**
```
1️⃣ GameManager Prefab Oluştur + Catalog'ları Doldur
2️⃣ MainMenu Sahnesine GameManager Prefab'ını Ekle  
3️⃣ UI Sistemlerini Kur (Market/Garage Panelleri)
```

### **⚠️ UNUTMA:**
- **Oyun akışı:** MainMenu → Gameplay → MainMenu
- **Gameplay sahnesinden direkt başlatma:** Artık desteklenmiyor
- **Test ederken:** Hep MainMenu sahnesinden başla
- **Catalog'lar:** Inspector'da manuel doldur, Resources'a gerek yok

### **🎉 Bu Sistemle Elde Ettiklerin:**
- ✅ **Inventory vs Equipped** ayrımı
- ✅ **Modern animasyonlu UI**
- ✅ **Otomatik save/load sistemi**
- ✅ **Reklam hazırlığı** (TODO'lar ile)
- ✅ **Modüler upgrade sistemi**
- ✅ **Kolay test edilebilir yapı**

**Artık tek rehberden takip ederek sistemin tamamını kurabilirsin!** 🚀 

### **⚠️ LAYOUT GROUP KULLANIMI - ÖNEMLİ NOTLAR**

```
🔄 Vertical Layout Group Kullanıldığında:
- ❌ RectTransform manuel ayarlanamaz (Position, Size)
- ✅ Layout Element ile boyut kontrolü yapılır
- ✅ Vertical Layout Group ayarları ile davranış belirlenir

📏 Layout Element Kullanımı:
- Add Component: Layout Element (isteğe bağlı)
- Preferred Width: İstenen genişlik (yoksa Layout Group belirler)
- Preferred Height: İstenen yükseklik (yoksa Content Size Fitter belirler)
- Flexible Width/Height: Esnek boyutlandırma (varsayılan 0)

🎯 Layout Group Ayarları:
- Spacing: Elemanlar arası boşluk
- Child Alignment: Elemanların hizalanması
- Control Child Size: Layout Group boyutu kontrol etsin mi?
- Child Force Expand: Elemanlar boş alanı doldursun mu?
```