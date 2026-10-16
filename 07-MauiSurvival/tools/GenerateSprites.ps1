# Генерує піксельні картинки для гри на виживання у Resources/Images.
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

    # Для сцен дій (рубання дерева тощо) зберігаємо ще й велику версію цього ж малюнка
    $bigScale = 0
    if ($name -like 'res_*') { $bigScale = 11 } elseif ($name -like 'hero*') { $bigScale = 14 } elseif ($name -eq 'loc_forest') { $bigScale = 12 } elseif ($name -like 'tool_*') { $bigScale = 8 }
    if ($bigScale -gt 0) {
        $bigName = if ($name -like 'hero*') { "${name}_big" } else { "big_$name" }
        Save-Scaled ($bmp.Clone()) $bigName $bigScale
    }

    Save-Scaled $bmp $name $scale
}

# ===== Ресурси 12x12 -> 48x48 =====
New-Sprite @(
    '............', '..kkkkkkkkk.', '.kyyyknnnnnk', 'kyykyyknnnnk', 'kyyyyyknnnnk', '.kyyykknnnnk', '..kkkkkkkkk.') 'res_wood' 12 4

New-Sprite @(
    '............', '....kkkk....', '..kkssssk...', '.ksswsssskk.', 'kssssssssssk', 'ksstsssstssk', '.kttttttttk.', '..kkkkkkkk..') 'res_stone' 12 4

New-Sprite @(
    '.....g.g....', '......gg....', '...kkk.kkk..', '..krrrkrrrk.', '..krwrkrwrk.', '..krrrkrrrk.', '...kkk.kkk..') 'res_berry' 12 4

New-Sprite @(
    '....kkkk....', '..kkrrwrkk..', '.krrrrrrrrk.', 'krwwrrrrwwrk', 'krwwrrrrwwrk', 'krrrrrrrrrrk', '.kkkkwwkkkk.', '....kwwk....', '....kwwk....', '...kwwwwk...', '...kkkkkk...') 'res_mushroom' 12 4

New-Sprite @(
    '.....kk.....', '....kggk....', '..kk.kggk.kk', '.kggkkggkggk', '.kggggggkggk', '..kkkkggkkk.', '.....kggk...', '.....kggk...', '....kkkkkk..') 'res_cactus' 12 4

New-Sprite @(
    '............', '....kkkk....', '..kkbbbbkk.k', '.kbwkbbbbbkbk', 'kbbbbbbbbbbbk', '.kbbbbbbbbkbk', '..kkbbbbkk.k', '....kkkk....') 'res_fish' 12 4

New-Sprite @(
    '............', '...kkkkkk...', '..kcwcccck..', '.kcwccccbck.', 'kkkkkkkkkkkk', '.kccccccbck.', '..kcccccbk..', '...kccbck...', '....kbck....', '.....kk.....') 'res_ice' 12 4

New-Sprite @(
    '............', '....kkkk....', '..kkssssk...', '.ksysssyskk.', 'kssssssssssk', 'ksstsyssstsk', '.kttttttttk.', '..kkkkkkkk..') 'res_ore' 12 4

New-Sprite @(
    '............', '...kkkkkk...', '..kmwmmmmk..', '.kmwmmmmmpk.', 'kkkkkkkkkkkk', '.kmmmmmmmpk.', '..kmmmmmpk..', '...kmmmpk...', '....kmpk....', '.....kk.....') 'res_crystal' 12 4

# ===== Локації 12x12 -> 60x60 =====
New-Sprite @(
    '....kkkk....', '..kkggggkk..', '.kggggdggk..', 'kggdggggggk.', 'kggggggdggk.', '.kggggggggk.', '..kkggggkk..', '....knnk....', '....knnk....', '....knnk....', '...kkkkkk...') 'loc_forest' 12 5

New-Sprite @(
    '.....yy.....', '..y..yy..y..', '...yyyyyy...', 'yy.yyyyyy.yy', 'yy.yyyyyy.yy', '...yyyyyy...', '..y..yy..y..', '.....yy.....') 'loc_desert' 12 5

New-Sprite @(
    '.....cc.....', '..c..cc..c..', '...c.cc.c...', '....cccc....', 'cccccccccccc', '....cccc....', '...c.cc.c...', '..c..cc..c..', '.....cc.....') 'loc_tundra' 12 5

New-Sprite @(
    '.....kk.....', '....kttk....', '...kttttk...', '..kttttttk..', '.kttkkkkttk.', 'kttkkkkkkttk', 'kttkkkkkkttk', 'kkkkkkkkkkkk') 'loc_cave' 12 5

# ===== Характеристики 12x12 -> 36x36 =====
New-Sprite @(
    '............', '.kkkk..kkkk.', 'krwrrkkrrrrk', 'krrrrrrrrrrk', 'krrrrrrrrrrk', '.krrrrrrrrk.', '..krrrrrrk..', '...krrrrk...', '....krrk....', '.....kk.....') 'stat_health' 12 3

New-Sprite @(
    '..kkkk......', '.knnnnk.....', 'knnnnnnk....', 'knnnnnnk....', '.knnnnkk....', '..kkkkwk....', '.....kwwk...', '......kwwk..', '.......kk...') 'stat_food' 12 3

New-Sprite @(
    '.......kkk..', '......kyyk..', '.....kyyk...', '....kyyk....', '...kyyyyyk..', '..kkkkyyk...', '.....kyk....', '....kyk.....', '...kyk......', '...kk.......') 'stat_energy' 12 3

# ===== Інструменти 12x12 -> 48x48 =====
New-Sprite @(
    '...kkkk.....', '..kssssk....', '.kssssskk...', '.ksssskkn...', '..kkkk.kn...', '......kn....', '.....kn.....', '....kn......', '...kn.......', '...kk.......') 'tool_axe' 12 4

New-Sprite @(
    '..kkkkkkkk..', '.ksssssssssk', 'kssskkkkksssk', 'kssk..kn.kssk', 'kkk...kn..kkk', '......kn....', '......kn....', '......kn....', '......kk....') 'tool_pickaxe' 12 4

New-Sprite @(
    '.........kk.', '........kn..', '.......kn...', '......kn....', '.....kn.....', '....kn.kb...', '...kn..kb...', '..kn...kbk..', '.kn....kkk..') 'tool_rod' 12 4

# ===== Інше =====
# Багаття-логотип 16x16 -> 128x128
New-Sprite @(
    '................', '.......oo.......', '......oyyo......', '.....oyyyyo.....', '....oyywwyyo....', '....oyywwyyo....', '.....oyyyyo.....',
    '......oyyo......', '..nn..nnnn..nn..', '.nnnn.nnnn.nnnn.', 'nnnnnnnnnnnnnnnn', '.kkkkkkkkkkkkkk.') 'logo_fire' 16 8

# Персонаж-виживший 12x12 -> 96x96
New-Sprite @(
    '....kkkk....', '...kyyyyk...', '..kyyyyyyk..', '..kwkwwkwk..', '..kwwwwwwk..', '...kkrrkk...', '..kkggggkk..', '.kkgggggkkk.', '.kwkggggkwk.', '...kggggk...', '...kk..kk...', '..kkk..kkk..') 'hero' 12 8

# Місяць для сну 12x12 -> 48x48
New-Sprite @(
    '....kkkk....', '..kkyyyyk...', '.kyyyykk....', '.kyyyk......', 'kyyyyk......', 'kyyyyk......', 'kyyyyk......', '.kyyyyk.....', '.kyyyyykk...', '..kkyyyyykk.', '....kkkkkk..') 'icon_sleep' 12 4

# Іконки меню 16x16 -> 32x32
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

# Вода, м'ясо, спрага, нова зброя (спис і меч)
New-Sprite @(
    '.....kk.....', '....kbbk....', '....kbbk....', '...kbwbbk...', '..kbwbbbbk..', '.kbwbbbbbbk.', '.kbbbbbbbbk.', '.kbbbbbbbek.', '..kbbbbbek..', '...kkeeekk..', '....kkkk....') 'res_water' 12 4

New-Sprite @(
    '...kkkkkk...', '..knnnnnnk..', '.knnrrnnnnk.', '.knnrrnnnnk.', '.knnnnnnnnk.', '..knnnnnnk..', '...kkwwkk...', '.....kwwk...', '......kwwk..', '.......kk...') 'res_meat' 12 4

New-Sprite @(
    '.....kk.....', '....kbbk....', '....kbbk....', '...kbwbbk...', '..kbwbbbbk..', '.kbwbbbbbbk.', '.kbbbbbbbbk.', '.kbbbbbbbek.', '..kbbbbbek..', '...kkeeekk..', '....kkkk....') 'stat_thirst' 12 3

New-Sprite @(
    '.........kk.', '........kssk', '.......kssk.', '......kssk..', '.....kssk...', '....kssk....', '...knk......', '..knk.......', '.knk........', 'knk.........', 'kk..........') 'tool_spear' 12 4

New-Sprite @(
    '..........kk', '.........ksk', '........ksk.', '.......ksk..', '......ksk...', '.k...ksk....', '.kk.ksk.....', '..kkyk......', '..knyk......', '.knk.kk.....', 'knk.........') 'tool_sword' 12 4

# Герой: кадри анімації (моргання і робота руками вгору), 12x12 -> 96x96
New-Sprite @(
    '....kkkk....', '...kyyyyk...', '..kyyyyyyk..', '..kwwwwwwk..', '..kwwwwwwk..', '...kkrrkk...', '..kkggggkk..', '.kkgggggkkk.', '.kwkggggkwk.', '...kggggk...', '...kk..kk...', '..kkk..kkk..') 'hero_blink' 12 8

New-Sprite @(
    '....kkkk....', '...kyyyyk...', '.w.kyyyyk.w.', '.wkwkwwkwkw.', '..kwwwwwwk..', '...kkrrkk...', '..kkggggkk..', '..kgggggkk..', '...kggggk...', '...kggggk...', '...kk..kk...', '..kkk..kkk..') 'hero_work' 12 8

# Хмаринка для живого фону, 20x20 (малюнок займає верх) -> 160x160
New-Sprite @(
    '.......kkkk.........', '.....kkssssskk......', '...kkssssssssskkk...', '..ksssssssssssssk...', '.kssssssssssssssssk.', 'kssssssssssssssssssk', '.kkkkkkkkkkkkkkkkkk.') 'cloud' 20 8

# --- Фон: нічне небо з лісом (160x90 -> 1280x720) ---
$bg = New-Object System.Drawing.Bitmap 160, 90
$rnd = New-Object System.Random 31
$bands = @('#1a1c2c', '#1f2140', '#262b56', '#2d3470')
for ($y = 0; $y -lt 90; $y++) { for ($x = 0; $x -lt 160; $x++) {
    $bg.SetPixel($x, $y, [System.Drawing.ColorTranslator]::FromHtml($bands[[Math]::Min(3, [int]($y / 20))]))
} }
for ($i = 0; $i -lt 70; $i++) {
    $bg.SetPixel($rnd.Next(0, 160), $rnd.Next(0, 55), [System.Drawing.ColorTranslator]::FromHtml($(if ($rnd.Next(0, 4) -eq 0) { '#ffcd75' } else { '#94b0c2' })))
}
for ($x = 0; $x -lt 160; $x++) {
    $h1 = 72 + [int](4 * [Math]::Sin($x / 8.0)) + [int](2 * [Math]::Sin($x / 3.0))
    for ($y = $h1; $y -lt 90; $y++) { $bg.SetPixel($x, $y, [System.Drawing.ColorTranslator]::FromHtml('#1b3a3f')) }
}
# ялинки на горизонті
foreach ($tx in 6, 19, 33, 47, 70, 98, 113, 127, 141, 154) {
    $base = 76 + ($tx % 5)
    for ($row = 0; $row -lt 10; $row++) {
        $half = [int]($row / 2) + 1
        for ($dx = -$half; $dx -le $half; $dx++) {
            $px = $tx + $dx; $py = $base - 10 + $row
            if ($px -ge 0 -and $px -lt 160) { $bg.SetPixel($px, $py, [System.Drawing.ColorTranslator]::FromHtml('#14302f')) }
        }
    }
}
for ($x = 0; $x -lt 160; $x++) {
    $h2 = 83 + [int](2 * [Math]::Sin($x / 5.0 + 5))
    for ($y = $h2; $y -lt 90; $y++) { $bg.SetPixel($x, $y, [System.Drawing.ColorTranslator]::FromHtml($(if ((($x + $y) % 4) -eq 0) { '#2f8f57' } else { '#38b764' }))) }
}
Save-Scaled $bg 'bg_night' 8
Write-Host 'Готово: картинки записано в' (Resolve-Path $out)

# --- Два прозорі шари зірок для мерехтіння: вони плавно змінюють один одного (160x90 -> 1280x720) ---
foreach ($layer in 'a', 'b') {
    $stars = New-Object System.Drawing.Bitmap 160, 90
    $rnd2 = New-Object System.Random ($(if ($layer -eq 'a') { 101 } else { 202 }))
    for ($i = 0; $i -lt 46; $i++) {
        $sx = $rnd2.Next(0, 160); $sy = $rnd2.Next(0, 56)
        $col = if ($rnd2.Next(0, 4) -eq 0) { '#ffcd75' } else { '#f4f4f4' }
        $stars.SetPixel($sx, $sy, [System.Drawing.ColorTranslator]::FromHtml($col))
        if ($rnd2.Next(0, 5) -eq 0 -and $sx -lt 159) { $stars.SetPixel($sx + 1, $sy, [System.Drawing.ColorTranslator]::FromHtml($col)) }
    }
    Save-Scaled $stars "stars_$layer" 8
}
