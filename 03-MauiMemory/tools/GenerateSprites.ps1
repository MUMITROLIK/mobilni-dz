# Генерує піксельні картки для Memory і фон у Resources/Images.
# Кожен символ - це 12x12 "картинка з букв", вона кладеться на картку 16x16 і збільшується без розмиття.
# 9 малюнків x 2 кольорові варіанти = 18 унікальних карток для найскладнішого рівня.
# Запуск: powershell -File tools\GenerateSprites.ps1
Add-Type -AssemblyName System.Drawing

$out = Join-Path $PSScriptRoot '..\Resources\Images'

# Палітра (кожна буква = колір; регістр важливий, тому беремо різні букви)
$pal = New-Object System.Collections.Hashtable ([StringComparer]::Ordinal)
@{
    'k' = '#1a1c2c'; 'w' = '#f4f4f4'; 'y' = '#ffcd75'; 'o' = '#ef7d57'; 'r' = '#b13e53'
    'g' = '#38b764'; 'd' = '#257179'; 'b' = '#41a6f6'; 'e' = '#3b5dc9'; 's' = '#94b0c2'
    't' = '#566c86'; 'n' = '#a05b3b'; 'p' = '#5d275d'; 'm' = '#b55088'; 'c' = '#73eff7'; 'l' = '#a7f070'
}.GetEnumerator() | ForEach-Object { $pal[$_.Key] = $_.Value }

# Другий кольоровий варіант: червоне <-> синє, жовте -> лайм, зелене <-> малинове, біле -> блакитне
$variant = @{ 'r' = 'b'; 'b' = 'r'; 'e' = 'p'; 'y' = 'l'; 'g' = 'm'; 'm' = 'g'; 'w' = 'c'; 'o' = 'c' }

$sprites = [ordered]@{
    heart = @(
        '............', '.kkkk..kkkk.', 'krwrrkkrrrrk', 'krrrrrrrrrrk', 'krrrrrrrrrrk', '.krrrrrrrrk.',
        '..krrrrrrk..', '...krrrrk...', '....krrk....', '.....kk.....')
    star = @(
        '.....kk.....', '....kyyk....', '....kyyk....', 'kkkkkyyykkkk', 'kyyyyyyyyyyk', '.kyyyyyyyyk.',
        '..kyyyyyyk..', '..kyyyyyyk..', '.kyyykkyyyk.', '.kyykk.kkyyk', '.kkk....kkk.')
    apple = @(
        '......nk....', '.....gk.....', '..kkkkkkkk..', '.krrrrrrrrk.', 'krwrrrrrrrrk', 'krwrrrrrrrrk',
        'krrrrrrrrrrk', 'krrrrrrrrrrk', '.krrrrrrrrk.', '.krrrrrrrrk.', '..kkrrrrkk..', '....kkkk....')
    mushroom = @(
        '....kkkk....', '..kkrrwrkk..', '.krrrrrrrrk.', 'krwwrrrrwwrk', 'krwwrrrrwwrk', 'krrrrrrrrrrk',
        '.kkkkwwkkkk.', '....kwwk....', '....kwwk....', '...kwwwwk...', '...kkkkkk...')
    gem = @(
        '............', '...kkkkkk...', '..kbwbbbek..', '.kbwbbbbbek.', 'kkkkkkkkkkkk', '.kbbbbbbbek.',
        '..kbbbbbek..', '...kbbbek...', '....kbek....', '.....kk.....')
    flower = @(
        '....kkkk....', '...kmmmmk...', '..kmmmmmmk..', '.kmmkyykmmk.', 'kmmmkyykmmmk', '.kmmkyykmmk.',
        '..kmmmmmmk..', '...kmmmmk...', '....kggk....', '...kgggk....', '..kggkk.....')
    moon = @(
        '....kkkk....', '..kkyyyyk...', '.kyyyykk....', '.kyyyk......', 'kyyyyk......', 'kyyyyk......',
        'kyyyyk......', '.kyyyyk.....', '.kyyyyykk...', '..kkyyyyykk.', '....kkkkkk..')
    ghost = @(
        '...kkkkkk...', '..kwwwwwwk..', '.kwwwwwwwwk.', '.kwkkwwkkwk.', '.kwkkwwkkwk.', '.kwwwwwwwwk.',
        '.kwwwkkwwwk.', '.kwwwwwwwwk.', '.kwwwwwwwwk.', '.kwkwwwwkwk.', '.kk.kwwk.kk.')
    drop = @(
        '.....kk.....', '.....kk.....', '....kbbk....', '....kbbk....', '...kbwbbk...', '..kbwbbbbk..',
        '.kbwbbbbbbk.', '.kbbbbbbbbk.', '.kbbbbbbbek.', '..kbbbbbek..', '...kkeeekk..', '....kkkk....')
}

function Color($ch) { [System.Drawing.ColorTranslator]::FromHtml($pal[[string]$ch]) }

function Save-Scaled($bmp, $name, $scale) {
    $w = $bmp.Width * $scale; $h = $bmp.Height * $scale
    $big = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($big)
    $g.InterpolationMode = 'NearestNeighbor'
    $g.PixelOffsetMode = 'Half'
    $g.DrawImage($bmp, 0, 0, $w, $h)
    $g.Dispose()
    $big.Save((Join-Path $out "$name.png"), [System.Drawing.Imaging.ImageFormat]::Png)
    $big.Dispose(); $bmp.Dispose()
}

# --- Лицьові боки карток: кремове тло з рамкою + малюнок 12x12 по центру ---
$index = 0
foreach ($v in 0, 1) {
    foreach ($name in $sprites.Keys) {
        $bmp = New-Object System.Drawing.Bitmap 16, 16
        for ($y = 0; $y -lt 16; $y++) { for ($x = 0; $x -lt 16; $x++) {
            $edge = ($x -eq 0 -or $y -eq 0 -or $x -eq 15 -or $y -eq 15)
            $corner = (($x -eq 0 -or $x -eq 15) -and ($y -eq 0 -or $y -eq 15))
            if ($corner) { continue }                                  # зрізані кути
            $bmp.SetPixel($x, $y, $(if ($edge) { Color 'k' } else { [System.Drawing.ColorTranslator]::FromHtml('#fff1d6') }))
        } }
        $rows = $sprites[$name]
        for ($y = 0; $y -lt 12; $y++) {
            $line = if ($y -lt $rows.Count) { $rows[$y].PadRight(12, '.') } else { '.' * 12 }
            for ($x = 0; $x -lt 12; $x++) {
                $ch = [string]$line[$x]
                if ($ch -eq '.') { continue }
                if ($v -eq 1 -and $variant.ContainsKey($ch)) { $ch = $variant[$ch] }
                $bmp.SetPixel(2 + $x, 2 + $y, (Color $ch))
            }
        }
        Save-Scaled $bmp ('face_{0:D2}' -f $index) 8
        $index++
    }
}

# --- Сорочка картки: синє тло з ромбовим візерунком ---
$back = New-Object System.Drawing.Bitmap 16, 16
for ($y = 0; $y -lt 16; $y++) { for ($x = 0; $x -lt 16; $x++) {
    $edge = ($x -eq 0 -or $y -eq 0 -or $x -eq 15 -or $y -eq 15)
    $corner = (($x -eq 0 -or $x -eq 15) -and ($y -eq 0 -or $y -eq 15))
    if ($corner) { continue }
    if ($edge) { $back.SetPixel($x, $y, (Color 'k')); continue }
    $inner = ($x -eq 1 -or $y -eq 1 -or $x -eq 14 -or $y -eq 14)
    $diamond = ([Math]::Abs($x - 7.5) + [Math]::Abs($y - 7.5)) -le 5
    $dot = (($x + $y) % 4 -eq 0)
    $c = if ($inner) { 'b' } elseif ($diamond -and $dot) { 'y' } elseif ($diamond) { 'e' } elseif ($dot) { 'b' } else { 'e' }
    $back.SetPixel($x, $y, (Color $c))
} }
Save-Scaled $back 'card_back' 8

# --- Фон: нічне небо із зірками і пагорбами (160x90 -> 1280x720) ---
$bg = New-Object System.Drawing.Bitmap 160, 90
$rnd = New-Object System.Random 7
$bands = @('#1a1c2c', '#1f2140', '#262b56', '#2d3470')
for ($y = 0; $y -lt 90; $y++) { for ($x = 0; $x -lt 160; $x++) {
    $bg.SetPixel($x, $y, [System.Drawing.ColorTranslator]::FromHtml($bands[[Math]::Min(3, [int]($y / 20))]))
} }
for ($i = 0; $i -lt 70; $i++) {
    $bg.SetPixel($rnd.Next(0, 160), $rnd.Next(0, 55), [System.Drawing.ColorTranslator]::FromHtml($(if ($rnd.Next(0, 4) -eq 0) { '#ffcd75' } else { '#94b0c2' })))
}
for ($x = 0; $x -lt 160; $x++) {
    $h1 = 70 + [int](4 * [Math]::Sin($x / 9.0 + 2)) + [int](2 * [Math]::Sin($x / 3.5))
    for ($y = $h1; $y -lt 90; $y++) { $bg.SetPixel($x, $y, [System.Drawing.ColorTranslator]::FromHtml('#1b3a3f')) }
    $h2 = 80 + [int](3 * [Math]::Sin($x / 6.0 + 4))
    for ($y = $h2; $y -lt 90; $y++) { $bg.SetPixel($x, $y, [System.Drawing.ColorTranslator]::FromHtml($(if ((($x + $y) % 4) -eq 0) { '#2f8f57' } else { '#38b764' }))) }
}
Save-Scaled $bg 'bg_night' 8
Write-Host "Готово: $index карток + сорочка + фон у" (Resolve-Path $out)

# --- Окремі малюнки без картки (для декору меню та вибору рівня), 12x12 -> 72x72 ---
foreach ($name in $sprites.Keys) {
    $bmp = New-Object System.Drawing.Bitmap 12, 12
    $rows = $sprites[$name]
    for ($y = 0; $y -lt 12; $y++) {
        $line = if ($y -lt $rows.Count) { $rows[$y].PadRight(12, '.') } else { '.' * 12 }
        for ($x = 0; $x -lt 12; $x++) { if ($line[$x] -ne '.') { $bmp.SetPixel($x, $y, (Color ([string]$line[$x]))) } }
    }
    Save-Scaled $bmp "deco_$name" 6
}

# --- Іконки для кнопок меню, 16x16 -> 48x48 ---
function New-Icon($rows, $name) {
    $bmp = New-Object System.Drawing.Bitmap 16, 16
    for ($y = 0; $y -lt 16; $y++) {
        $line = if ($y -lt $rows.Count) { $rows[$y].PadRight(16, '.') } else { '.' * 16 }
        for ($x = 0; $x -lt 16; $x++) { if ($line[$x] -ne '.') { $bmp.SetPixel($x, $y, (Color ([string]$line[$x]))) } }
    }
    Save-Scaled $bmp $name 3
}

# Кнопка "грати": зелений трикутник з обводкою (малюємо за формулою)
$play = New-Object System.Drawing.Bitmap 16, 16
for ($y = 1; $y -le 14; $y++) {
    $half = 7 - [Math]::Abs($y - 7.5)
    $xEnd = [int](2 + $half * 1.9)
    for ($x = 3; $x -le $xEnd; $x++) {
        $edge = ($x -eq 3 -or $x -ge $xEnd - 1 -or $y -eq 1 -or $y -eq 14)
        $play.SetPixel($x, $y, (Color $(if ($edge) { 'k' } else { 'w' })))
    }
}
Save-Scaled $play 'icon_play' 3

New-Icon @(
    '.kkkkkkkkkkkkkk.', 'kbbbbbbkbbbbbbk.', 'kbwwwwbkbwwwwbk.', 'kbbbbbbkbbbbbbk.', 'kbwwwwbkbwwwwbk.',
    'kbbbbbbkbbbbbbk.', 'kbwwwwbkbwwwwbk.', 'kbbbbbbkbbbbbbk.', '.kkkkkkkkkkkkkk.') 'icon_help'

New-Icon @(
    '..kkkkkkkk......', '..knnnnnnnk.....', '..knnnnnnnnk....', '..knnnnnnynk....', '..knnnnnnnnk....',
    '..knnnnnnnnk....', '..knnnnnnnnk....', '..knnnnnnnnk....', '..knnnnnnnnk....', '..kkkkkkkkkk....') 'icon_exit'
