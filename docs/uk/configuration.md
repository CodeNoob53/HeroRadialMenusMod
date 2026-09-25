# Налаштування Hero Radial Menus / CFG reference

Файл: `BepInEx/config/com.heromod.valheim.radialmenus.cfg` у папці гри або
активного профілю менеджера модів. BepInEx створює його після першого запуску.
Файл можна редагувати просто під час гри: після збереження мод перечитує його
за мить (у лозі — «CFG перечитано з диска.»). Виняток — зміщення коліс
(`HealOffsetX/Y`, `ArrowRadial OffsetX/Y`): вони застосовуються лише при
завантаженні світу, тож після їх зміни перезайдіть у світ або перезапустіть гру.

Таблиці містять усі 33 параметри із Plugin.BindConfig(). Назви секцій та ключів
не перекладайте. Дроби пишіть із крапкою: 0.25, не 0,25.
Логічні значення: true / false. Кольори RGB та непрозорість: 0..1;
alpha 0 — прозоро, 1 — непрозоро. Розміри — одиниці HUD; масштаб інтерфейсу
гри може змінити фізичний розмір на екрані.

## [Radial]

Керування консумаблами та спільний вигляд обох коліс.

| Key | Default | Значення / effect |
|---|---|---|
| `RadialKey` | `Mouse3` | Утримувати для відкриття консумаблів. Unity KeyCode, наприклад G, Mouse3, Mouse4; None вимикає гарячу клавішу. |
| `IncludeAllConsumables` | `false` | false: їжа з health-показником та консумабли зі статус-ефектом (зілля, медовуха); true: всі Consumable. Це не фільтр «тільки лікування». |
| `Radius` | `320` | Базова радіальна відстань. Нижче 200 використовується 320. Внутрішній край: Radius − 86, зовнішній: Radius + 94, центр іконок: Radius + 4. |
| `SlotSize` | `68` | Ширина й висота іконки, мінімум 1. Не змінює сектори або текст кількості. |
| `DeadZone` | `100` | Радіус центральної зони скасування. Використовуйте додатне число, менше Radius; надто велике значення унеможливить вибір. |
| `HoverFadeSeconds` | `0.12` | Час появи/зникнення підсвітки та орнаменту. 0 або менше — миттєво. |
| `EmptySlotAlpha` | `0.35` | Непрозорість порожніх секторів, обмежена 0..1. |
| `HoverColorR` | `0.23` | Червона складова активного фону; потрібне TintBackground = true. |
| `HoverColorG` | `0.14` | Зелена складова активного фону; потрібне TintBackground = true. |
| `HoverColorB` | `0.03` | Синя складова активного фону; потрібне TintBackground = true. |
| `HoverAlpha` | `0.45` | Непрозорість активного фону; потрібне TintBackground = true. |
| `FrameColorR` | `1.0` | Червона складова контуру, орнаменту та вказівника. |
| `FrameColorG` | `0.72` | Зелена складова контуру, орнаменту та вказівника. |
| `FrameColorB` | `0.36` | Синя складова контуру, орнаменту та вказівника. |
| `FrameAlpha` | `1.0` | Непрозорість контуру, орнаменту та вказівника. |
| `FrameWidth` | `4` | Товщина контуру. Рекомендовано 1..8; код обмежує лише мінімум до 1. |
| `OrnamentOffset` | `115` | Відступ від Radius. При 100 зазор від зовнішнього краю — 6, при 109 — 15. Анімаційний зсув додається окремо. |
| `SlowMotion` | `false` | Експериментально: локальний час 0.25x лише для колеса консумаблів. Для одиночної гри, без серверної синхронізації. На закритті timeScale = 1; не поєднуйте з іншими модами часу. |
| `ShowFrame` | `true` | Контур активного сектора. Не вимикає орнамент або вказівник. |
| `TintBackground` | `false` | Застосовувати HoverColorR/G/B та HoverAlpha; false залишає темний фон. |
| `BlockMovement` | `false` | true блокує WASD. false дозволяє ходьбу; бойові дії, біг, стрибок, огляд камерою все одно заблоковані. Геймпадний рух під час відкриття блокується. |
| `ClickToUse` | `true` | Клік ClickKey по слоту одразу застосовує предмет; колесо лишається відкритим, тож за одне відкриття можна вжити кілька предметів. Коли стак закінчується, сектори перебудовуються. Якщо за відкриття був хоч один клік, відпускання RadialKey лише закриває колесо. false — старий режим «застосувати при відпусканні». |
| `ClickKey` | `Mouse0` | Клавіша для ClickToUse (Unity KeyCode). Має відрізнятися від RadialKey; None вимикає клік. |
| `ConsumeAnimation` | `true` | Ванільна анімація пиття/їжі при застосуванні з колеса. false — предмет вживається без анімації; звук/ефект вживання та ванільні обмеження (кулдаун зілля, ситість) лишаються. |

## [ArrowRadial]

| Key | Default | Значення / effect |
|---|---|---|
| `Key` | `R` | Основна клавіша; None вимикає її. Коротке R зберігає ванільне ховання зброї. |
| `KeySecondary` | `None` | Альтернатива, наприклад Mouse4. None — вимкнено. |
| `HoldDelay` | `0.25` | Секунди утримання до відкриття; 0 або додатне число. |
| `PickDelay` | `0.15` | Секунди підтвердження після вибору; 0 закриває на наступному оновленні. Під час підтвердження керування вже повертається гравцю. Використовуйте невід'ємне число. |
| `OffsetX` | `0` | Зміщення від центру HUD; додатне — праворуч, від'ємне — ліворуч. |
| `OffsetY` | `0` | Зміщення: додатне — вгору, від'ємне — вниз. |

## [Position]

| Key | Default | Значення / effect |
|---|---|---|
| `HealOffsetX` | `0` | Зміщення консумаблів по горизонталі; додатне — праворуч. |
| `HealOffsetY` | `0` | Зміщення консумаблів по вертикалі; додатне — вгору. |

## [Debug]

| Key | Default | Значення / effect |
|---|---|---|
| `EnableDebugLogs` | `false` | Єдиний перемикач детальної діагностики UI, профілів і вводу в BepInEx/LogOutput.log. Однакові повідомлення вводу — один раз за сеанс. Повідомлення запуску, попередження й помилки не залежать від перемикача. |

Старий ключ `[Debug] LogInput` більше не використовується. Якщо він залишився
у CFG попередньої версії, його можна видалити; для діагностики ввімкніть
`EnableDebugLogs = true`.

## Приклад / Example

Змініть значення в наявних секціях, не створюйте дублікати:

```ini
[Radial]
RadialKey = G
TintBackground = true
HoverColorR = 0.23
HoverColorG = 0.14
HoverColorB = 0.03
HoverAlpha = 0.45
BlockMovement = true

[ArrowRadial]
Key = R
KeySecondary = Mouse4
HoldDelay = 0.25
```

Не призначайте одну клавішу обом колесам. Враховуйте прив'язки гри та інших
модів: мод не скасовує всі можливі дії сторонніх клавіш.

## Скидання, профілі та оновлення

Закрийте гру й перейменуйте CFG (наприклад, додайте .bak) для створення нового
із замовчуваннями. Профілі в `BepInEx/config/HeroModUiProfiles` мають вищий
пріоритет для підтримуваних властивостей. Для повного скидання також приберіть
ці JSON-файли за межі папки профілів. [Контракт профілів](radial-surfaces.md).

Після релізного аудиту SlotSize, HoverFadeSeconds, EmptySlotAlpha,
HoverColorR/G/B, HoverAlpha, TintBackground та OrnamentOffset почали реально
застосовуватися. Вигляд може змінитися навіть зі старим CFG.
Для близького до попередньої dev-збірки вигляду: SlotSize = 80,
EmptySlotAlpha = 0.65, TintBackground = true, HoverColorR/G/B = 0.25/0.15/0.03,
HoverAlpha = 0.90, OrnamentOffset = 109. Раніше швидкості анімації секторів
та орнаменту були різні; тепер ними керує HoverFadeSeconds.

## English quick reference

Tables list every key and default. The file is re-read a moment after it is
saved, even in-game; wheel offsets apply on the next world load. Decimal values use a dot. Appearance affects both wheels;
SlowMotion and IncludeAllConsumables only affect consumables. SlotSize is the
item icon size. Radius below 200 falls back to 320. Keep DeadZone positive and
smaller than Radius. Hover colours require TintBackground. Frame colours also
tint the ornament/cursor. OrnamentOffset is measured from Radius, not the
outer edge. Positive offsets move right/up. Use Unity KeyCode names, or None
to disable a key. SlowMotion is experimental, client-local and intended for
single-player. Rename the CFG to reset; remove editor profiles separately.
