[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
Add-Type -AssemblyName System.Drawing

$allDirs = Get-ChildItem 'C:\' -Directory -ErrorAction SilentlyContinue
$base = ($allDirs | Where-Object { $_.Name -match '[^\x00-\x7F]' } | Select-Object -First 1).FullName
$subdirs = Get-ChildItem $base -Directory -ErrorAction SilentlyContinue
$dir = ($subdirs | Select-Object -First 1).FullName
$file = Get-ChildItem $dir -File | Select-Object -First 1

Write-Output "File: $($file.FullName)"
Write-Output "Size: $([math]::Round($file.Length / 1KB)) KB"
Write-Output ""

try {
    $img = [System.Drawing.Image]::FromFile($file.FullName)
    Write-Output "Image: $($img.Width)x$($img.Height)"
    Write-Output "Format: $($img.RawFormat)"
    Write-Output ""
    Write-Output "=== EXIF Properties ==="
    foreach ($prop in $img.PropertyItems) {
        $id = "0x{0:X4}" -f $prop.Id
        $val = ""
        if ($prop.Type -eq 2) {
            $val = [System.Text.Encoding]::UTF8.GetString($prop.Value).TrimEnd([char]0)
        } elseif ($prop.Type -eq 1) {
            $val = "(bytes: $($prop.Value.Length))"
        } else {
            $val = [BitConverter]::ToString($prop.Value[0..([Math]::Min(19, $prop.Value.Length-1))])
        }
        Write-Output "  $id (type=$($prop.Type)): $val"
    }
    $img.Dispose()
} catch {
    Write-Output "System.Drawing failed: $($_.Exception.Message)"
    Write-Output ""
    Write-Output "=== Raw bytes scan for text chunks ==="
    $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
    $text = [System.Text.Encoding]::ASCII.GetString($bytes)
    # Look for common metadata markers
    foreach ($marker in @('tEXt','iTXt','Description','Comment','prompt','UserComment','XMP')) {
        $idx = $text.IndexOf($marker)
        if ($idx -ge 0) {
            $snippet = $text.Substring($idx, [Math]::Min(200, $text.Length - $idx)) -replace '[^\x20-\x7E\xC0-\xFF]','.'
            Write-Output "  Found '$marker' at byte $idx : $snippet"
        }
    }
}
