# Генерує піксельні спрайти для кліkера у Resources/Images.
# Кожен спрайт - це 16x16 "картинка з букв", яку ми збільшуємо методом найближчого сусіда (без розмиття).
# Запуск: powershell -File tools\GenerateSprites.ps1
Add-Type -AssemblyName System.Drawing

$out = Join-Path $PSScriptRoot '..\Resources\Images'

# Палітра (кожна буква = колір)
$pal = @{
    'k' = '#1a1c2c'; 'w' = '#f4f4f4'; 'y' = '#ffcd75'; 'o' = '#ef7d57'; 'r' = '#b13e53'
    'g' = '#38b764'; 'd' = '#257179'; 'b' = '#41a6f6'; 'e' = '#3b5dc9'; 's' = '#94b0c2'
    't' = '#566c86'; 'n' = '#a05b3b'; 'p' = '#5d275d'
}

function New-Sprite($rows, $name, $scale) {
    $bmp = New-Object System.Drawing.Bitmap 16, 16
    for ($y = 0; $y -lt 16; $y++) {
        $line = if ($y -lt $rows.Count) { $rows[$y].PadRight(16, '.') } else { '.' * 16 }
        for ($x = 0; $x -lt 16; $x++) {
            $c = $line[$x]
            if ($c -ne '.' -and $pal.ContainsKey([string]$c)) {
                $bmp.SetPixel($x, $y, [System.Drawing.ColorTranslator]::FromHtml($pal[[string]$c]))
            }
        }
    }
    Save-Scaled $bmp $name $scale
}

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

# --- Монета: коло, обведення, світлий відблиск і знак $ ---
$coin = New-Object System.Drawing.Bitmap 16, 16
$col = @{}
foreach ($k in $pal.Keys) { $col[$k] = [System.Drawing.ColorTranslator]::FromHtml($pal[$k]) }
for ($y = 0; $y -lt 16; $y++) { for ($x = 0; $x -lt 16; $x++) {
    $d = [Math]::Sqrt([Math]::Pow($x - 7.5, 2) + [Math]::Pow($y - 7.5, 2))
    if ($d -le 7.6) {
        if ($d -gt 6.6) { $coin.SetPixel($x, $y, $col['k']) }
        elseif ($d -gt 5.2) { $coin.SetPixel($x, $y, $col['o']) }
        else { $coin.SetPixel($x, $y, $col['y']) }
    }
} }
$dollar = @('..o..', '.ooo.', 'o.o..', '.ooo.', '..o.o', '.ooo.', '..o..')
for ($j = 0; $j -lt $dollar.Count; $j++) { for ($i = 0; $i -lt 5; $i++) {
    if ($dollar[$j][$i] -eq 'o') { $coin.SetPixel(5 + $i, 4 + $j, $col['o']) }
} }
foreach ($p in @(@(3,5),@(3,4),@(4,3),@(5,3))) { $coin.SetPixel($p[0], $p[1], $col['w']) }  # відблиск
Save-Scaled $coin 'coin' 10      # 160x160

# --- Іконки 16x16 -> 48x48 ---
New-Sprite @(
    '................', '.........kkkk...', '........kyyyk...', '.......kyyyk....', '......kyyyk.....',
    '.....kyyyk......', '....kyyyykkkk...', '...kyyyyyyyyk...', '...kkkkkyyyk....', '......kyyyk.....',
    '.....kyyyk......', '....kyyyk.......', '....kyyk........', '...kyyk.........', '...kyk..........', '...kk...........'
) 'icon_power' 3

New-Sprite @(
    '......kk........', '.....kwwk.......', '.....kwwk.......', '.....kwwk.......', '.....kwwkkk.....',
    '.....kwwkwwkkk..', '..kk.kwwkwwkwwk.', '.kwwkkwwwwwwwwwk', '.kwwwkwwwwwwwwwk', '..kwwwwwwwwwwwwk',
    '...kwwwwwwwwwwk.', '...kwwwwwwwwwwk.', '....kwwwwwwwwk..', '....kwwwwwwwwk..', '.....kkkkkkkk...'
) 'icon_finger' 3

New-Sprite @(
    '.....kkkkkk.....', '...kkwwwwwwkk...', '..kwwwwwwwwwwk..', '.kwwwwwwkwwwwwk.', '.kwwwwwwkwwwwwk.',
    'kwwwwwwwkwwwwwwk', 'kwwwwwwwkwwwwwwk', 'kwwwwwwwkkkkwwwk', 'kwwwwwwwwwwwwwwk', 'kwwwwwwwwwwwwwwk',
    '.kwwwwwwwwwwwwk.', '.kwwwwwwwwwwwwk.', '..kwwwwwwwwwwk..', '...kkwwwwwwkk...', '.....kkkkkk.....'
) 'icon_auto' 3

New-Sprite @(
    '.......kk.......', '.....kkrrkk.....', '...kkrrrrrrkk...', '.kkrrrrrrrrrrkk.', 'kkrrrrrrrrrrrrkk',
    'krrrrrrrrrrrrrrk', 'krrrrrkkkkrrrrrk', 'krrrrkwwwwkrrrrk', 'krrrrkwkkwkrrrrk', 'krrrrkwwwwkrrrrk',
    'krrrrkkwwkkrrrrk', 'krrrrkkwwkkrrrrk', 'krrrrkkwwkkrrrrk', 'kkkkkkkkkkkkkkkk'
) 'icon_farm' 3

New-Sprite @(
    '...kk...........', '..kssk..........', '..kssk..........', '..kssk....kk....', '..kssk...kssk...',
    '.kkssk...kssk...', '.kssskkkkksskkk.', '.ksssssssssssssk', '.ksbbsssbbsssbbk', '.ksbbsssbbsssbbk',
    '.ksssssssssssssk', '.ksssssssssssssk', '.kkkkkkkkkkkkkkk'
) 'icon_factory' 3

New-Sprite @(
    '....kkkkkkkk....', '..kkssssssssskk.', '.ksttkkkkkkttsk.', '.ktk.....kn.ktk.', '.k.......kn...k.',
    '.........kn.....', '.........kn.....', '.........kn.....', '.........kn.....', '.........kn.....',
    '.........kn.....', '.........kn.....', '.........kn.....', '.........kk.....'
) 'icon_mine' 3

New-Sprite @(
    'kkkkkkkkkkkkkkk.', 'keekwwwwwwkeeek.', 'keekwwwwwwkeeek.', 'keekwwwwwwkeeek.', 'keeeeeeeeeeeeek.',
    'keeeeeeeeeeeeek.', 'kekkkkkkkkkkeek.', 'kekwwwwwwwwkeek.', 'kekwwwwwwwwkeek.', 'kekwwwwwwwwkeek.',
    'kekwwwwwwwwkeek.', 'kkkkkkkkkkkkkkk.'
) 'icon_save' 3

New-Sprite @(
    '................', '.kkkkk..........', 'kyyyyykkkkkkkkk.', 'kyyyyyyyyyyyyyk.', 'kykkkkkkkkkkkkk.',
    'kyoooooooooooook', 'kyoooooooooooook', 'kyoooooooooooook', 'kyoooooooooooook', 'kyoooooooooooook',
    'kkkkkkkkkkkkkkkk'
) 'icon_load' 3

New-Sprite @(
    'kk..........kk..', 'krk........krk..', '.krk......krk...', '..krk....krk....', '...krk..krk.....',
    '....krkkrk......', '.....krrk.......', '....krkkrk......', '...krk..krk.....', '..krk....krk....',
    '.krk......krk...', 'krk........krk..', 'kk..........kk..'
) 'icon_reset' 3

# --- Фон: нічне небо з зірками, пагорбами і смугою трави (160x90 -> 1280x720) ---
$bg = New-Object System.Drawing.Bitmap 160, 90
$rnd = New-Object System.Random 42
$bands = @('#1a1c2c', '#1f2140', '#262b56', '#2d3470')
for ($y = 0; $y -lt 90; $y++) { for ($x = 0; $x -lt 160; $x++) {
    $band = [Math]::Min(3, [int]($y / 20))
    $bg.SetPixel($x, $y, [System.Drawing.ColorTranslator]::FromHtml($bands[$band]))
} }
for ($i = 0; $i -lt 70; $i++) {
    $sx = $rnd.Next(0, 160); $sy = $rnd.Next(0, 55)
    $bg.SetPixel($sx, $sy, [System.Drawing.ColorTranslator]::FromHtml($(if ($rnd.Next(0, 4) -eq 0) { '#ffcd75' } else { '#94b0c2' })))
}
for ($x = 0; $x -lt 160; $x++) {   # далекі пагорби і трава
    $h1 = 70 + [int](4 * [Math]::Sin($x / 9.0)) + [int](2 * [Math]::Sin($x / 3.5))
    for ($y = $h1; $y -lt 90; $y++) { $bg.SetPixel($x, $y, [System.Drawing.ColorTranslator]::FromHtml('#1b3a3f')) }
    $h2 = 80 + [int](3 * [Math]::Sin($x / 6.0 + 1))
    for ($y = $h2; $y -lt 90; $y++) { $bg.SetPixel($x, $y, [System.Drawing.ColorTranslator]::FromHtml($(if ((($x + $y) % 4) -eq 0) { '#2f8f57' } else { '#38b764' }))) }
}
Save-Scaled $bg 'bg_night' 8
Write-Host 'Готово: спрайти записано в' (Resolve-Path $out)
