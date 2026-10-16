# Генерує піксельні картинки для AI-чату у Resources/Images (аватари, іконки, фон).
# Кожен малюнок - "картинка з букв", яку збільшуємо методом найближчого сусіда, тому пікселі лишаються чіткими.
# Запуск: powershell -File tools\GenerateSprites.ps1
Add-Type -AssemblyName System.Drawing

$out = Join-Path $PSScriptRoot '..\Resources\Images'

# Палітра Sweetie-16 (кожна буква = колір; регістр важливий)
$pal = New-Object System.Collections.Hashtable ([StringComparer]::Ordinal)
@{
    'k' = '#1a1c2c'; 'w' = '#f4f4f4'; 'y' = '#ffcd75'; 'o' = '#ef7d57'; 'r' = '#b13e53'
    'g' = '#38b764'; 'd' = '#257179'; 'b' = '#41a6f6'; 'e' = '#3b5dc9'; 's' = '#94b0c2'
    't' = '#566c86'; 'n' = '#a05b3b'; 'p' = '#5d275d'; 'm' = '#b55088'; 'c' = '#73eff7'; 'l' = '#a7f070'
}.GetEnumerator() | ForEach-Object { $pal[$_.Key] = $_.Value }

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

function New-Sprite($rows, $name, $size, $scale) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    for ($y = 0; $y -lt $size; $y++) {
        $line = if ($y -lt $rows.Count) { $rows[$y].PadRight($size, '.') } else { '.' * $size }
        for ($x = 0; $x -lt $size; $x++) { if ($line[$x] -ne '.') { $bmp.SetPixel($x, $y, (Color ([string]$line[$x]))) } }
    }
    Save-Scaled $bmp $name $scale
}

# --- Мікрофон (кнопка запису), 16x16 -> 128x128 ---
New-Sprite @(
    '......ssss......', '.....swwwws.....', '.....swwwws.....', '.....swwwws.....', '.....swwwws.....',
    '..s..swswws..s..', '..s..ssssss..s..', '..ss.ssssss.ss..', '...ssssssssss...', '.....ssssss.....',
    '.......ss.......', '.......ss.......', '.....ssssss.....') 'mic' 16 8

# --- Стоп (кнопка під час запису): біла плитка ---
New-Sprite @(
    '................', '..kkkkkkkkkkkk..', '..kwwwwwwwwwwk..', '..kwwwwwwwwwwk..', '..kwwwwwwwwwwk..',
    '..kwwwwwwwwwwk..', '..kwwwwwwwwwwk..', '..kwwwwwwwwwwk..', '..kwwwwwwwwwwk..', '..kwwwwwwwwwwk..',
    '..kwwwwwwwwwwk..', '..kkkkkkkkkkkk..') 'stop' 16 8

# --- Малі іконки 16x16 -> 32x32 ---
# відтворити: трикутник
$play = New-Object System.Drawing.Bitmap 16, 16
for ($y = 1; $y -le 14; $y++) {
    $half = 7 - [Math]::Abs($y - 7.5)
    $xEnd = [int](2 + $half * 1.9)
    for ($x = 3; $x -le $xEnd; $x++) {
        $edge = ($x -eq 3 -or $x -ge $xEnd - 1 -or $y -eq 1 -or $y -eq 14)
        $play.SetPixel($x, $y, (Color $(if ($edge) { 'k' } else { 'w' })))
    }
}
Save-Scaled $play 'icon_play' 2

New-Sprite @(
    '................', '..kkkkkkkkkkkk..', '..kwwwwwwwwwwk..', '..kwwwwwwwwwwk..', '..kwwwwwwwwwwk..',
    '..kwwwwwwwwwwk..', '..kwwwwwwwwwwk..', '..kwwwwwwwwwwk..', '..kkkkkkkkkkkk..') 'icon_stop' 16 2

# ШІ: іскри
New-Sprite @(
    '.......kk.......', '......kyyk......', '......kyyk......', '.kkkkkkyykkkkkk.', '.kyyyyyyyyyyyyk.',
    '..kkkkkyykkkkk..', '.....kyyyyk.....', '....kyykkyyk....', '...kyyk..kyyk...', '...kyk....kyk...',
    '...kk......kk...') 'icon_ai' 16 2

# смітник
New-Sprite @(
    '.....kkkkk......', '..kkkkkkkkkkk...', '..kkkkkkkkkkk...', '...kskskskskk...', '...kskskskskk...',
    '...kskskskskk...', '...kskskskskk...', '...kskskskskk...', '...ksssssssk....', '....kkkkkkk.....') 'icon_trash' 16 2

# мікросхема (вибір моделі)
New-Sprite @(
    '..k..k..k..k....', '.kkkkkkkkkkkkk..', '..kbbbbbbbbbk...', 'kkkbwwwwwwwbkkk.', '..kbwkkkkkwbk...',
    'kkkbwkbbbkwbkkk.', '..kbwkkkkkwbk...', 'kkkbwwwwwwwbkkk.', '..kbbbbbbbbbk...', '.kkkkkkkkkkkkk..', '..k..k..k..k....') 'icon_chip' 16 3

# --- Декор: привид-слухач 12x12 -> 60x60 ---
New-Sprite @(
    '...kkkkkk...', '..kwwwwwwk..', '.kwwwwwwwwk.', '.kwkkwwkkwk.', '.kwkkwwkkwk.', '.kwwwwwwwwk.',
    '.kwwwkkwwwk.', '.kwwwwwwwwk.', '.kwwwwwwwwk.', '.kwkwwwwkwk.', '.kk.kwwk.kk.') 'deco_ghost' 12 5

# --- Фон: нічне небо (160x90 -> 1280x720) ---
$bg = New-Object System.Drawing.Bitmap 160, 90
$rnd = New-Object System.Random 11
$bands = @('#1a1c2c', '#1f2140', '#262b56', '#2d3470')
for ($y = 0; $y -lt 90; $y++) { for ($x = 0; $x -lt 160; $x++) {
    $bg.SetPixel($x, $y, [System.Drawing.ColorTranslator]::FromHtml($bands[[Math]::Min(3, [int]($y / 20))]))
} }
for ($i = 0; $i -lt 70; $i++) {
    $bg.SetPixel($rnd.Next(0, 160), $rnd.Next(0, 55), [System.Drawing.ColorTranslator]::FromHtml($(if ($rnd.Next(0, 4) -eq 0) { '#ffcd75' } else { '#94b0c2' })))
}
for ($x = 0; $x -lt 160; $x++) {
    $h1 = 72 + [int](4 * [Math]::Sin($x / 8.0 + 1)) + [int](2 * [Math]::Sin($x / 3.0))
    for ($y = $h1; $y -lt 90; $y++) { $bg.SetPixel($x, $y, [System.Drawing.ColorTranslator]::FromHtml('#1b3a3f')) }
    $h2 = 82 + [int](3 * [Math]::Sin($x / 6.0 + 3))
    for ($y = $h2; $y -lt 90; $y++) { $bg.SetPixel($x, $y, [System.Drawing.ColorTranslator]::FromHtml($(if ((($x + $y) % 4) -eq 0) { '#2f8f57' } else { '#38b764' }))) }
}
Save-Scaled $bg 'bg_night' 8
Write-Host 'Готово: картинки записано в' (Resolve-Path $out)
