[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$allDirs = Get-ChildItem 'C:\' -Directory -ErrorAction SilentlyContinue
$base = ($allDirs | Where-Object { $_.Name -match '[^\x00-\x7F]' } | Select-Object -First 1).FullName
Write-Output "Base: $base"
$subdirs = Get-ChildItem $base -Directory -ErrorAction SilentlyContinue
foreach ($sd in $subdirs) {
    $files = Get-ChildItem $sd.FullName -File -ErrorAction SilentlyContinue
    Write-Output ""
    Write-Output "=== $($sd.Name) === ($($files.Count) files)"
    foreach ($f in $files) {
        $sizeKB = [math]::Round($f.Length / 1KB)
        Write-Output "  $($f.Name) ($sizeKB KB)"
    }
}
