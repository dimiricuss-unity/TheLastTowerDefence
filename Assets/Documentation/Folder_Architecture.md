# Архитектура папок (целевая)

Документ описывает целевую структуру `Assets` для The Last Tower Defence и правила, куда класть код и контент.

---

## Принципы

- Разделять runtime-код, editor-код, тесты и контент.
- Держать структуру "по доменам" (Combat, Inventory, Heroes), а не только "по типам файлов".
- Учитывать WebGL: минимальный стартовый контент, загрузка тяжёлых данных через Addressables.
- Подготовить основу под `asmdef`, чтобы ускорить компиляцию и изолировать модули.

---

## Целевая структура

```text
Assets/
  _Project/
    Runtime/
      Bootstrap/
      Common/
        Config/
        Extensions/
        Services/
        Utilities/
      CoreLoop/
        WaveFlow/
        Battle/
        Defeat/
      Heroes/
        Domain/
        Systems/
      Combat/
        Domain/
        Systems/
        Damage/
      Enemies/
        Domain/
        Systems/
        Spawning/
      Inventory/
        Domain/
        Systems/
        AutoSell/
      Loot/
        Domain/
        Systems/
      Progression/
        XP/
        Stats/
      Economy/
        Currency/
        Shop/
      Save/
        Domain/
        Systems/
      UI/
        Common/
        Battle/
        Oracle/
        Management/
        Meta/
      Audio/
      VFX/
      Integration/
        DOTween/
        Addressables/
        InputSystem/
        Extenject/

    Editor/
      Tools/
      Validation/
      Build/

    Tests/
      EditMode/
      PlayMode/

    Content/
      ScriptableObjects/
        Configs/
        Heroes/
        Enemies/
        Items/
        Waves/
      Addressables/
        Labels/
        Groups/

    Art/
      Sprites/
      UI/
      VFX/
      Fonts/
      Materials/

    Audio/
      Music/
      SFX/
      Mixers/

    Prefabs/
      Characters/
      Enemies/
      Environment/
      UI/

    Scenes/
      Bootstrap/
      Battle/
      Meta/
      Tests/

    ThirdParty/
      Demigiant/

  Documentation/
  Resources/
```

---

## Что где хранить

- `_Project/Runtime`: весь боевой код, который попадает в билд.
- `_Project/Editor`: скрипты только для Unity Editor (валидация, утилиты, build pipeline).
- `_Project/Tests`: тесты формул, волновой логики, инвентаря, автопродажи и сохранения.
- `_Project/Content/ScriptableObjects`: статические игровые данные (волны, враги, предметы, кривые прогрессии).
- `_Project/Content/Addressables`: группы/лейблы для подгрузки тяжёлого контента.
- `_Project/Integration`: адаптеры к сторонним решениям (Input System, DOTween, DI).
- `_Project/Scenes/Bootstrap`: минимальная стартовая сцена загрузки.

---

## Правила по `asmdef` (этап 2)

Рекомендуемый набор сборок:

- `TheLastTowerDefence.Runtime`
- `TheLastTowerDefence.UI`
- `TheLastTowerDefence.Integration`
- `TheLastTowerDefence.Editor` (Editor only)
- `TheLastTowerDefence.Tests.EditMode`
- `TheLastTowerDefence.Tests.PlayMode`

Базовая зависимость: `UI` и `Integration` зависят от `Runtime`, но не наоборот.

**Минимум уже в репозитории:** сборка `TheLastTowerDefence.Runtime` (`Assets/_Project/Runtime/TheLastTowerDefence.Runtime.asmdef`) и тестовая **Edit Mode** `TheLastTowerDefence.Tests` (`Assets/_Project/Tests/EditMode/TheLastTowerDefence.Tests.asmdef`, `testAssemblies: true`). Папка `Tests/PlayMode/` пока без своего `asmdef` — добавь `TheLastTowerDefence.Tests.PlayMode.asmdef`, когда появятся Play Mode тесты.

---

## Переезд текущего содержимого

1. Создать `_Project` и перечисленные корневые папки.
2. Перенести `Assets/Scenes/BaseScene.unity` в `_Project/Scenes/Battle/`.
3. Перенести DOTween в `_Project/ThirdParty/Demigiant/` или оставить в `Assets/Demigiant`, но зафиксировать единый стандарт.
4. Новый код создавать только в `_Project/Runtime`.
5. По мере появления данных переносить конфиги в `_Project/Content/ScriptableObjects`.

---

## Именование

- Папки: `PascalCase`, без пробелов.
- Скрипты: `PascalCase.cs`, один публичный тип на файл.
- Конфиги SO: `<Domain><Purpose>Config` (например `WaveCycleConfig`).
- Префабы: `<Domain>_<Name>` (например `Enemy_SkeletonMelee`).

