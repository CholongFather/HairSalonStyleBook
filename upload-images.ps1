[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$bucket = "always-hair-salon.firebasestorage.app"
$apiKey = "AIzaSyBUC61LDhLONI-O0QOFl3y2C4uWMapbbI0"
$projectId = "always-hair-salon"
$firestoreBase = "https://firestore.googleapis.com/v1/projects/$projectId/databases/(default)/documents"

# Find the image directory
$allDirs = Get-ChildItem 'C:\' -Directory -ErrorAction SilentlyContinue
$base = ($allDirs | Where-Object { $_.Name -match '[^\x00-\x7F]' } | Select-Object -First 1).FullName

# Style name to Firestore document ID mapping - fetch from Firestore
Write-Output "=== Fetching styles from Firestore ==="
$stylesUrl = "$firestoreBase/styles?key=$apiKey&pageSize=300"
$response = Invoke-RestMethod -Uri $stylesUrl -Method Get
$styleMap = @{}
foreach ($doc in $response.documents) {
    $docId = $doc.name.Split('/')[-1]
    $title = $doc.fields.title.stringValue
    $existingUrls = @()
    if ($doc.fields.imageUrls -and $doc.fields.imageUrls.arrayValue -and $doc.fields.imageUrls.arrayValue.values) {
        $existingUrls = $doc.fields.imageUrls.arrayValue.values | ForEach-Object { $_.stringValue }
    }
    $isPublished = $false
    if ($doc.fields.isPublished -and $doc.fields.isPublished.booleanValue) {
        $isPublished = $true
    }
    $styleMap[$title] = @{ Id = $docId; ExistingUrls = $existingUrls; IsPublished = $isPublished }
}
Write-Output "Loaded $($styleMap.Count) styles"

# Track uploaded style IDs for publishing
$uploadedDocIds = @()

# Process each subfolder (angle)
$subdirs = Get-ChildItem $base -Directory -ErrorAction SilentlyContinue
foreach ($sd in $subdirs) {
    $angle = $sd.Name
    Write-Output ""
    Write-Output "=== Processing: $angle ==="

    $files = Get-ChildItem $sd.FullName -File -Filter "*.webp" -ErrorAction SilentlyContinue
    foreach ($f in $files) {
        # Parse filename: {num}_{category}_{styleName}_{angle}.webp
        $nameParts = $f.BaseName -split '_', 4
        if ($nameParts.Count -lt 4) {
            Write-Output "  SKIP (bad name): $($f.Name)"
            continue
        }
        $styleTitle = $nameParts[2]

        # Find matching style in Firestore
        $styleInfo = $styleMap[$styleTitle]
        if (-not $styleInfo) {
            Write-Output "  SKIP (not found): $styleTitle"
            continue
        }

        # Check if this angle already uploaded (URL-decode before comparison)
        $angleTag = "_$($nameParts[3])"
        $alreadyHasAngle = $styleInfo.ExistingUrls | Where-Object {
            $decoded = [Uri]::UnescapeDataString($_)
            $decoded -match [regex]::Escape($angleTag)
        }
        if ($alreadyHasAngle) {
            Write-Output "  SKIP (already has $angle): $($f.Name)"
            continue
        }

        $docId = $styleInfo.Id
        Write-Output "  Uploading: $($f.Name) -> style/$docId"

        # 1. Upload to Firebase Storage
        $timestamp = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
        $storageName = "styles/$($timestamp)_$($f.BaseName).webp"
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

        # 2. Update Firestore document - add URL to imageUrls
        $allUrls = @($styleInfo.ExistingUrls) + @($downloadUrl)
        $urlValues = $allUrls | ForEach-Object { @{ stringValue = $_ } }

        $patchBody = @{
            fields = @{
                imageUrls = @{
                    arrayValue = @{
                        values = $urlValues
                    }
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
}

# 3. Publish all uploaded styles (isPublished = true)
if ($uploadedDocIds.Count -gt 0) {
    Write-Output ""
    Write-Output "=== Publishing $($uploadedDocIds.Count) styles ==="
    foreach ($docId in $uploadedDocIds) {
        $publishBody = @{
            fields = @{
                isPublished = @{
                    booleanValue = $true
                }
            }
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
