# Генерує піксельні картинки для менеджера паролів у Resources/Images (іконки категорій, кнопки, фон).
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

function New-Bitmap($rows, $size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    for ($y = 0; $y -lt $size; $y++) {
        $line = if ($y -lt $rows.Count) { $rows[$y].PadRight($size, '.') } else { '.' * $size }
        for ($x = 0; $x -lt $size; $x++) { if ($line[$x] -ne '.') { $bmp.SetPixel($x, $y, (Color ([string]$line[$x]))) } }
    }
    return $bmp
}

function New-Sprite($rows, $name, $size, $scale) { Save-Scaled (New-Bitmap $rows $size) $name $scale }

# --- Іконки категорій 12x12 -> 48x48 ---
New-Sprite @(
    '.kkkkkkkkkk.', 'kbbbbbbbbbbk', 'kbwwbwwbwwbk', 'kbbbbbbbbbbk', 'kbbbbbbbbbbk', '.kkkkbbkkkk.', '....kbk.....', '.....k......') 'cat_social' 12 4

New-Sprite @(
    '............', '..kkkkkkkk..', '.kttttttttk.', 'kttwtttttrtk', 'ktwwwtttrtrk', 'kttwtttttrtk', '.kttttttttk.', '..kk....kk..') 'cat_games' 12 4

New-Sprite @(
    '...kkkk.....', '...k..k.....', '.kkkkkkkkkk.', 'knnnnnnnnnnk', 'knnnnyynnnnk', 'knnnnnnnnnnk', 'knnnnnnnnnnk', '.kkkkkkkkkk.') 'cat_work' 12 4

New-Sprite @(
    '............', 'kkkkkkkkkkkk', 'kwkwwwwwwkwk', 'kwwkwwwwkwwk', 'kwwwkkkkwwwk', 'kwwwwwwwwwwk', 'kkkkkkkkkkkk') 'cat_mail' 12 4

New-Sprite @(
    '...kkkkkk...', '..kyyyyyyk..', '.kyyyooyyyk.', 'kyyyooyyyyyk', 'kyyooyyyyyyk', 'kyyyyooyyyyk', 'kyyyyyooyyyk', '.kyyyyyyyyk.', '..kyyyyyyk..', '...kkkkkk...') 'cat_bank' 12 4

New-Sprite @(
    '.....kk.....', '....kyyk....', '....kyyk....', 'kkkkkyyykkkk', 'kyyyyyyyyyyk', '.kyyyyyyyyk.', '..kyyyyyyk..', '..kyyyyyyk..', '.kyyykkyyyk.', '.kyykk.kkyyk', '.kkk....kkk.') 'cat_other' 12 4

New-Sprite @(
    '.kkkkkkkkkk.', 'kwwwwwwwwwwk', 'kwkkkkkkkkwk', 'kwkwwwwwwkwk', 'kwkkkkkkkkwk', 'kwwwwwwwwwwk', '.kkkkkkkkkk.') 'cat_all' 12 4

# --- Кнопки та іконки 16x16 -> 32x32 ---
$eye = @(
    '................', '................', '................', '................', '.kkwwwwwwwwwwkk.', 'kwwwwwkkkkwwwwwk',
    'kwwwwkbbbbkwwwwk', 'kwwwwkbkkbkwwwwk', 'kwwwwkbbbbkwwwwk', 'kwwwwwkkkkwwwwwk', '.kkwwwwwwwwwwkk.', '...kkkkkkkkkk...')
Save-Scaled (New-Bitmap $eye 16) 'icon_eye' 2

# Заплющене око: те саме, але перекреслене червоною лінією
$off = New-Bitmap $eye 16
for ($i = 0; $i -lt 12; $i++) { $off.SetPixel(2 + $i, 12 - $i, (Color 'r')); $off.SetPixel(3 + $i, 12 - $i, (Color 'r')) }
Save-Scaled $off 'icon_eye_off' 2

New-Sprite @(
    '....kkkkkkk.....', '....kwwwwwk.....', '..kkkkkkwwk.....', '..kwwwwkwwk.....', '..kwkkwkwwk.....', '..kwwwwkkkk.....',
    '..kwkkwwwk......', '..kwwwwwwk......', '..kkkkkkkk......') 'icon_copy' 16 2

New-Sprite @(
    '................', '.......kk.......', '......kwwk......', '.....kwwwwk.....', '..kkkkkwwkkkkk..', '..kwwwwwwwwwwk..',
    '..kwwwwwwwwwwk..', '..kkkkkwwkkkkk..', '.....kwwwwk.....', '......kwwk......', '.......kk.......') 'icon_plus' 16 2

New-Sprite @(
    '..........kkk...', '.........kyyyk..', '........kyyykk..', '.......kyyyk....', '......kyyyk.....', '.....kyyyk......',
    '....kyyyk.......', '...kyyyk........', '..kkyyk.........', '..kkkk..........') 'icon_edit' 16 2

New-Sprite @(
    '.....kkkkk......', '..kkkkkkkkkkk...', '..kkkkkkkkkkk...', '...kskskskskk...', '...kskskskskk...',
    '...kskskskskk...', '...kskskskskk...', '...kskskskskk...', '...ksssssssk....', '....kkkkkkk.....') 'icon_trash' 16 2

New-Sprite @(
    '..kkkk..........', '.kyyyyk.........', 'kyykkyykkkkkkkk.', 'kyk..kyyyyyyyyk.', 'kyykkyykkkkykyk.', '.kyyyyk...kkkk..') 'icon_key' 16 2

# Великий ключ-логотип 16x16 -> 96x96
New-Sprite @(
    '................', '..kkkk..........', '.kyyyyk.........', 'kyykkyykkkkkkkk.', 'kyk..kyyyyyyyyk.', 'kyykkyykkkkykyk.',
    '.kyyyyk...kkkk..', '..kkkk...kyk....', '..........kk....') 'logo_key' 16 6

# Кубик-«обробити» для генератора (кістка)
New-Sprite @(
    '.kkkkkkkkkkkk...', 'kwwwwwwwwwwwwk..', 'kwkkwwwwwwkkwk..', 'kwkkwwwwwwkkwk..', 'kwwwwwkkwwwwwk..', 'kwwwwwkkwwwwwk..',
    'kwkkwwwwwwkkwk..', 'kwkkwwwwwwkkwk..', 'kwwwwwwwwwwwwk..', '.kkkkkkkkkkkk...') 'icon_dice' 16 2

# --- Фон: нічне небо (160x90 -> 1280x720) ---
$bg = New-Object System.Drawing.Bitmap 160, 90
$rnd = New-Object System.Random 23
$bands = @('#1a1c2c', '#1f2140', '#262b56', '#2d3470')
for ($y = 0; $y -lt 90; $y++) { for ($x = 0; $x -lt 160; $x++) {
    $bg.SetPixel($x, $y, [System.Drawing.ColorTranslator]::FromHtml($bands[[Math]::Min(3, [int]($y / 20))]))
} }
for ($i = 0; $i -lt 70; $i++) {
    $bg.SetPixel($rnd.Next(0, 160), $rnd.Next(0, 55), [System.Drawing.ColorTranslator]::FromHtml($(if ($rnd.Next(0, 4) -eq 0) { '#ffcd75' } else { '#94b0c2' })))
}
for ($x = 0; $x -lt 160; $x++) {
    $h1 = 74 + [int](3 * [Math]::Sin($x / 7.0 + 2)) + [int](2 * [Math]::Sin($x / 3.0))
    for ($y = $h1; $y -lt 90; $y++) { $bg.SetPixel($x, $y, [System.Drawing.ColorTranslator]::FromHtml('#1b3a3f')) }
    $h2 = 83 + [int](2 * [Math]::Sin($x / 5.0 + 5))
    for ($y = $h2; $y -lt 90; $y++) { $bg.SetPixel($x, $y, [System.Drawing.ColorTranslator]::FromHtml($(if ((($x + $y) % 4) -eq 0) { '#2f8f57' } else { '#38b764' }))) }
}
Save-Scaled $bg 'bg_night' 8
Write-Host 'Готово: картинки записано в' (Resolve-Path $out)
