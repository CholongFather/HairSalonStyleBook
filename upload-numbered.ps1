[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$bucket = "always-hair-salon.firebasestorage.app"
$apiKey = "AIzaSyBUC61LDhLONI-O0QOFl3y2C4uWMapbbI0"
$projectId = "always-hair-salon"
$firestoreBase = "https://firestore.googleapis.com/v1/projects/$projectId/databases/(default)/documents"
$angle = "_front"

# Load number-to-style mapping from JSON (UTF-8 safe)
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$mapping = Get-Content "$scriptDir\style-mapping.json" -Encoding UTF8 | ConvertFrom-Json

# Find image directory (non-ASCII named folder in C:\)
$allDirs = Get-ChildItem 'C:\' -Directory -ErrorAction SilentlyContinue
$base = ($allDirs | Where-Object { $_.Name -match '[^\x00-\x7F]' } | Select-Object -First 1).FullName
$subdirs = Get-ChildItem $base -Directory -ErrorAction SilentlyContinue
$frontDir = ($subdirs | Select-Object -First 1).FullName
Write-Output "Dir: $frontDir"

# Fetch styles from Firestore
Write-Output "=== Fetching styles ==="
$response = Invoke-RestMethod -Uri "$firestoreBase/styles?key=$apiKey&pageSize=300" -Method Get
$styleMap = @{}
foreach ($doc in $response.documents) {
    $docId = $doc.name.Split('/')[-1]
    $title = $doc.fields.title.stringValue
    $existingUrls = @()
    if ($doc.fields.imageUrls -and $doc.fields.imageUrls.arrayValue -and $doc.fields.imageUrls.arrayValue.values) {
        $existingUrls = $doc.fields.imageUrls.arrayValue.values | ForEach-Object { $_.stringValue }
    }
    $styleMap[$title] = @{ Id = $docId; ExistingUrls = $existingUrls }
}
Write-Output "Loaded $($styleMap.Count) styles"

# Find numbered files (just number as filename)
$numberedFiles = Get-ChildItem $frontDir -File | Where-Object { $_.BaseName -match '^\d+$' }
Write-Output "Found $($numberedFiles.Count) numbered files"

$uploadedDocIds = @()

foreach ($f in $numberedFiles) {
    $num = $f.BaseName
    $styleTitle = $mapping.$num
    if (-not $styleTitle) {
        Write-Output "  SKIP (no mapping): $($f.Name)"
        continue
    }

    $styleInfo = $styleMap[$styleTitle]
    if (-not $styleInfo) {
        Write-Output "  SKIP (not in Firestore): $num -> $styleTitle"
        continue
    }

    # Check duplicate (already has front angle)
    $hasFront = $styleInfo.ExistingUrls | Where-Object {
        $decoded = [Uri]::UnescapeDataString($_)
        $decoded -match '_front' -or $decoded -match '_정면'
    }
    if ($hasFront) {
        Write-Output "  SKIP (already has front): $num -> $styleTitle"
        continue
    }

    $docId = $styleInfo.Id
    Write-Output "  Uploading: $($f.Name) -> $styleTitle (style/$docId)"

    # Upload to Firebase Storage
    $timestamp = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
    $storageName = "styles/$($timestamp)_$($num)_front.webp"
    $encodedName = [Uri]::EscapeDataString($storageName)
    $uploadUrl = "https://firebasestorage.googleapis.com/v0/b/$bucket/o?uploadType=media&name=$encodedName"

    try {
        $fileBytes = [System.IO.File]::ReadAllBytes($f.FullName)
        $uploadResponse = Invoke-RestMethod -Uri $uploadUrl -Method Post -Body $fileBytes -ContentType "image/webp"
        $downloadToken = $uploadResponse.downloadTokens
        $downloadUrl = "https://firebasestorage.googleapis.com/v0/b/$bucket/o/$($encodedName)?alt=media&token=$downloadToken"
        Write-Output "    Uploaded OK"
    } catch {
        Write-Output "    UPLOAD FAILED: $($_.Exception.Message)"
        continue
    }

    # Update Firestore imageUrls
    $allUrls = @($styleInfo.ExistingUrls) + @($downloadUrl)
    $urlValues = $allUrls | ForEach-Object { @{ stringValue = $_ } }
    $patchBody = @{
        fields = @{
            imageUrls = @{
                arrayValue = @{ values = $urlValues }
            }
        }
    } | ConvertTo-Json -Depth 10

    $patchUrl = "$firestoreBase/styles/$($docId)?key=$apiKey&updateMask.fieldPaths=imageUrls"
    try {
        Invoke-RestMethod -Uri $patchUrl -Method Patch -Body ([System.Text.Encoding]::UTF8.GetBytes($patchBody)) -ContentType "application/json; charset=utf-8" | Out-Null
        Write-Output "    Firestore updated (total: $($allUrls.Count) images)"
        $styleInfo.ExistingUrls = $allUrls
        if ($uploadedDocIds -notcontains $docId) { $uploadedDocIds += $docId }
    } catch {
        Write-Output "    FIRESTORE FAILED: $($_.Exception.Message)"
    }

    Start-Sleep -Milliseconds 300
}

# Publish uploaded styles
if ($uploadedDocIds.Count -gt 0) {
    Write-Output ""
    Write-Output "=== Publishing $($uploadedDocIds.Count) styles ==="
    foreach ($docId in $uploadedDocIds) {
        $publishBody = @{
            fields = @{ isPublished = @{ booleanValue = $true } }
        } | ConvertTo-Json -Depth 5
        $publishUrl = "$firestoreBase/styles/$($docId)?key=$apiKey&updateMask.fieldPaths=isPublished"
        try {
            Invoke-RestMethod -Uri $publishUrl -Method Patch -Body ([System.Text.Encoding]::UTF8.GetBytes($publishBody)) -ContentType "application/json; charset=utf-8" | Out-Null
            Write-Output "  Published: $docId"
        } catch {
            Write-Output "  PUBLISH FAILED ($docId): $($_.Exception.Message)"
        }
        Start-Sleep -Milliseconds 200
    }
}

Write-Output ""
Write-Output "=== DONE (uploaded: $($uploadedDocIds.Count) styles) ==="
