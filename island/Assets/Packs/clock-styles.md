# Стили цифр часов под паки (`clock-styles.md`)

Часы сейчас — `MainWindow.xaml` → `ClockText` (`Segoe UI Variable`, 12px,
`SemiBold`, `#F5F5F7`). Ниже — стиль цифр под каждый пак: что поменять
в `ClockText` (или рантайм-аналог в `ApplyClockStyle`), короткий XAML-пример
и файл примера. Везде добавить `Typography.NumeralAlignment="Tabular"`,
иначе ширина цифр плавает и пилюля дышит.

## light (светлая пилюля)

Тёмные полужирные цифры, спокойный grotesque.
- `FontFamily="Segoe UI Variable Display"`, `FontSize="12"`, `FontWeight="SemiBold"`,
  `Foreground="#1C1C1E"`, `Typography.NumeralAlignment="Tabular"`

```xml
<TextBlock x:Name="ClockText" FontFamily="Segoe UI Variable Display" FontSize="12"
           FontWeight="SemiBold" Foreground="#1C1C1E"
           Typography.NumeralAlignment="Tabular" Text="12:45" />
```

## dark (чёрная пилюля `#0A0A0A`, дефолт 007)

Текущий вид, только семейство уточнено до Display и добавлены табличные цифры.
- `FontFamily="Segoe UI Variable Display"`, `FontSize="12"`, `FontWeight="SemiBold"`,
  `Foreground="#F5F5F7"`, `Typography.NumeralAlignment="Tabular"`

```xml
<TextBlock x:Name="ClockText" FontFamily="Segoe UI Variable Display" FontSize="12"
           FontWeight="SemiBold" Foreground="#F5F5F7"
           Typography.NumeralAlignment="Tabular" Text="12:45" />
```

## colorful (акцентный)

Белые полужирные цифры + янтарное двоеточие под keyline `#FF9F0A`.
Двоеточие — отдельный `Run`, чтобы мигало вместе с существующим blink (`_colonOn`).
- Цифры: `Segoe UI Variable Display`, `Bold`, `#F5F5F7`, Tabular.
- Двоеточие: `Foreground="#FF9F0A"`.

```xml
<TextBlock FontFamily="Segoe UI Variable Display" FontSize="12" FontWeight="Bold"
           Foreground="#F5F5F7" Typography.NumeralAlignment="Tabular">
    <Run Text="12" /><Run Text=":" Foreground="#FF9F0A" /><Run Text="45" />
</TextBlock>
```

## mono (терминальный)

Моноширинный шрифт вместо табличных цифр, разреженный трекинг.
- `FontFamily="Cascadia Mono,Consolas"`, `FontSize="12"`, `FontWeight="Normal"`,
  `Foreground="#F5F5F7"`, `CharacterSpacing="200"` (1/1000 em; WinUI-единицы).

```xml
<TextBlock x:Name="ClockText" FontFamily="Cascadia Mono,Consolas" FontSize="12"
           FontWeight="Normal" Foreground="#F5F5F7" CharacterSpacing="200"
           Text="12:45" />
```

## neon (свечение)

Жирные почти белые цифры + циановое свечение через `ThemeShadow`.
Тень работает, когда у текста задан `Translation` (z поднимает над приёмником).
- `Segoe UI Variable Display`, `Bold`, `#EAFDFF`, Tabular,
  `Shadow="{ThemeResource ClockNeonShadow}"`, `Translation="0,0,8"`.

```xml
<Grid>
    <Grid.Resources>
        <ThemeShadow x:Name="ClockNeonShadow" />
    </Grid.Resources>
    <TextBlock x:Name="ClockText" FontFamily="Segoe UI Variable Display" FontSize="12"
               FontWeight="Bold" Foreground="#EAFDFF"
               Typography.NumeralAlignment="Tabular"
               Shadow="{ThemeResource ClockNeonShadow}" Translation="0,0,8"
               Text="12:45" />
</Grid>
```

Цвет тени задаётся кодом (`ClockNeonShadow.Receivers` + `DropShadow` с `#00F0FF`);
без кода — мягкая белая тень по умолчанию, тоже ок.

## pastel (мягкий)

Лёгкое начертание, тёплые кремовые цифры.
- `FontFamily="Segoe UI Variable Text"`, `FontSize="12"`, `FontWeight="SemiLight"`,
  `Foreground="#F2EAD9"`, `Typography.NumeralAlignment="Tabular"`

```xml
<TextBlock x:Name="ClockText" FontFamily="Segoe UI Variable Text" FontSize="12"
           FontWeight="SemiLight" Foreground="#F2EAD9"
           Typography.NumeralAlignment="Tabular" Text="12:45" />
```

## Пример

`clock-preview.png` — рендер всех шести стилей (шрифт-замена DejaVu,
метрики приблизительные; в WinUI смотреть с шрифтами из таблицы).
Подключение пака часов — та же строка `Content` в csproj не нужна:
стили применяются кодом/XAML, assets часов не требуют.
