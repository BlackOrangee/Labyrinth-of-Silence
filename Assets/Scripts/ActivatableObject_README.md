# ActivatableObject - Руководство по использованию

## Описание
Скрипт для создания активируемых объектов (генераторы, рычаги, кнопки и т.д.), которые выдают игроку виртуальный ключ и показывают визуальную обратную связь.

## Виртуальные ключи
Виртуальные ключи (GeneratorPower, Electricity, WaterValve, SecurityAccess) находятся в инвентаре игрока технически, но **НЕ отображаются** в UI инвентаря. Они используются только для проверки условий (например, открытие двери).

## Настройка генератора с закрытой дверью

### Шаг 1: Настройка генератора
1. Создайте/выберите GameObject генератора
2. Добавьте компонент `ActivatableObject`
3. Настройте параметры:
   - **Object Name**: "Generator"
   - **Key To Give**: `GeneratorPower` (виртуальный ключ)
   - **One Time Activation**: ✓ (можно активировать только раз)

#### Visual Feedback (визуальная обратная связь):
- **Lights To Enable**: добавьте лампочки, которые загорятся
- **Objects To Enable**: GameObject'ы которые появятся (свечение, эффекты)
- **Target Renderer**: Renderer объекта для смены материала
- **Activated Material**: материал после активации
- **Emission Color**: цвет свечения (например, зеленый)
- **Enable Emission**: ✓

#### Audio:
- **Activation Sound**: звук включения генератора
- **Already Activated Sound**: звук если уже активирован
- **Locked Sound**: звук если заблокирован

#### Messages:
- **Activation Message**: "Generator activated! Power restored!"
- **Already Activated Message**: "Generator is already running"

### Шаг 2: Настройка двери
1. Выберите GameObject двери/триггера перехода
2. Добавьте компонент `SceneTrigger` (или используйте существующий)
3. Настройте параметры:
   - **Target Scene Name**: название следующей сцены
   - **Require Interaction**: ✓
   - **Require Keys**: ✓
   - **Required Keys**: добавьте `GeneratorPower`
   - **Prompt Text**: "Door locked. Power required."

### Результат
Когда игрок:
1. Подойдет к генератору - увидит: "Generator: Press [E] to activate"
2. Нажмет [E] - генератор активируется:
   - Загорятся лампочки
   - Сыграет звук
   - Игрок получит виртуальный ключ `GeneratorPower` (невидимый в инвентаре)
3. Подойдет к двери - увидит: "Press [E] to proceed"
4. Нажмет [E] - дверь откроется и произойдет переход на следующую локацию

## Дополнительные возможности

### Требование ключа для активации
Если нужно, чтобы генератор требовал обычный ключ для активации:
```
Require Key: ✓
Required Key: Green (или любой другой)
```

### Несколько условий для двери
Можно добавить несколько виртуальных ключей:
```csharp
Required Keys:
  - GeneratorPower
  - WaterValve
  - SecurityAccess
```
Игрок должен будет активировать все объекты.

### Анимация
Если у объекта есть Animator:
1. Добавьте Trigger параметр в аниматоре (например, "Activate")
2. В скрипте укажите:
   - **Animator**: ссылка на Animator компонент
   - **Activation Trigger**: "Activate"

### Ручная активация
Можно вызвать из другого скрипта:
```csharp
ActivatableObject generator = generatorObj.GetComponent<ActivatableObject>();
generator.ActivateManually();
```

## Доступные виртуальные ключи
В `KeyColorType` добавлены:
- `GeneratorPower` = 100 (питание от генератора)
- `Electricity` = 101 (электричество)
- `WaterValve` = 102 (водяной вентиль)
- `SecurityAccess` = 103 (доступ к охране)

Можно добавить свои, начиная со значения 104+.

## Проверка состояния
```csharp
bool isActive = activatableObject.IsActivated();
```

## Отладка
В Scene View активированные объекты отображаются зеленым Gizmo, неактивированные - желтым.

## Интеграция с DoorController
`DoorController` также можно использовать, но он анимирует физическое открытие двери.
`SceneTrigger` лучше подходит для перехода между локациями.

Можно комбинировать:
1. Активируем генератор → получаем `GeneratorPower`
2. DoorController проверяет `GeneratorPower` → дверь открывается
3. Игрок проходит через SceneTrigger → переход на локацию
