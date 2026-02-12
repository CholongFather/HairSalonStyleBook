[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$bucket = "always-hair-salon.firebasestorage.app"
$apiKey = "AIzaSyBUC61LDhLONI-O0QOFl3y2C4uWMapbbI0"
$projectId = "always-hair-salon"
$firestoreBase = "https://firestore.googleapis.com/v1/projects/$projectId/databases/(default)/documents"

# Find file
$allDirs = Get-ChildItem 'C:\' -Directory -ErrorAction SilentlyContinue
$base = ($allDirs | Where-Object { $_.Name -match '[^\x00-\x7F]' } | Select-Object -First 1).FullName
$subdirs = Get-ChildItem $base -Directory -ErrorAction SilentlyContinue
$frontDir = ($subdirs | Select-Object -First 1).FullName
$file = Get-ChildItem $frontDir -File -Filter "*14*" | Select-Object -First 1

if (-not $file) { Write-Output "File not found"; exit }
Write-Output "File: $($file.Name)"

# Fetch styles and find fuzzy match
$response = Invoke-RestMethod -Uri "$firestoreBase/styles?key=$apiKey&pageSize=300" -Method Get
$target = $null
foreach ($doc in $response.documents) {
    $title = $doc.fields.title.stringValue
    # Fuzzy: remove parentheses and match
    $cleanTitle = $title -replace '[()]','' -replace '\s+',' '
    if ($cleanTitle -like '*울프 컷 남성*' -or $title -like '*울프 컷*남성*') {
        $target = $doc
        Write-Output "Matched: $title (ID: $($doc.name.Split('/')[-1]))"
        break
    }
}
if (-not $target) { Write-Output "Style not found"; exit }

$docId = $target.name.Split('/')[-1]
$existingUrls = @()
if ($target.fields.imageUrls -and $target.fields.imageUrls.arrayValue -and $target.fields.imageUrls.arrayValue.values) {
    $existingUrls = $target.fields.imageUrls.arrayValue.values | ForEach-Object { $_.stringValue }
}

# Upload
$timestamp = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
$storageName = "styles/$($timestamp)_$($file.BaseName).webp"
$encodedName = [Uri]::EscapeDataString($storageName)
$uploadUrl = "https://firebasestorage.googleapis.com/v0/b/$bucket/o?uploadType=media&name=$encodedName"
$fileBytes = [System.IO.File]::ReadAllBytes($file.FullName)
$uploadResponse = Invoke-RestMethod -Uri $uploadUrl -Method Post -Body $fileBytes -ContentType "image/webp"
$downloadToken = $uploadResponse.downloadTokens
$downloadUrl = "https://firebasestorage.googleapis.com/v0/b/$bucket/o/$($encodedName)?alt=media&token=$downloadToken"
Write-Output "Uploaded OK"

# Update imageUrls
$allUrls = @($existingUrls) + @($downloadUrl)
$urlValues = $allUrls | ForEach-Object { @{ stringValue = $_ } }
$patchBody = @{ fields = @{ imageUrls = @{ arrayValue = @{ values = $urlValues } } } } | ConvertTo-Json -Depth 10
$patchUrl = "$firestoreBase/styles/$($docId)?key=$apiKey&updateMask.fieldPaths=imageUrls"
Invoke-RestMethod -Uri $patchUrl -Method Patch -Body ([System.Text.Encoding]::UTF8.GetBytes($patchBody)) -ContentType "application/json; charset=utf-8" | Out-Null
Write-Output "Firestore updated (total: $($allUrls.Count) images)"

# Publish
$publishBody = @{ fields = @{ isPublished = @{ booleanValue = $true } } } | ConvertTo-Json -Depth 5
$publishUrl = "$firestoreBase/styles/$($docId)?key=$apiKey&updateMask.fieldPaths=isPublished"
Invoke-RestMethod -Uri $publishUrl -Method Patch -Body ([System.Text.Encoding]::UTF8.GetBytes($publishBody)) -ContentType "application/json; charset=utf-8" | Out-Null
Write-Output "Published: $docId"
Write-Output "=== DONE ==="
