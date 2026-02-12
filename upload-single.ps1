param(
    [Parameter(Mandatory=$true)][string]$StyleName,
    [Parameter(Mandatory=$true)][string]$Image,
    [string]$Angle
)

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$bucket = "always-hair-salon.firebasestorage.app"
$apiKey = "AIzaSyBUC61LDhLONI-O0QOFl3y2C4uWMapbbI0"
$projectId = "always-hair-salon"
$firestoreBase = "https://firestore.googleapis.com/v1/projects/$projectId/databases/(default)/documents"

# 이미지 파일 확인
if (-not (Test-Path $Image)) {
    Write-Output "ERROR: 파일을 찾을 수 없습니다: $Image"
    return
}

# Firestore에서 스타일 검색
Write-Output "=== '$StyleName' 검색 중... ==="
$response = Invoke-RestMethod -Uri "$firestoreBase/styles?key=$apiKey&pageSize=300" -Method Get
$found = $null
foreach ($doc in $response.documents) {
    $title = $doc.fields.title.stringValue
    if ($title -eq $StyleName -or $title.Trim() -eq $StyleName.Trim()) {
        $docId = $doc.name.Split('/')[-1]
        $existingUrls = @()
        if ($doc.fields.imageUrls -and $doc.fields.imageUrls.arrayValue -and $doc.fields.imageUrls.arrayValue.values) {
            $existingUrls = $doc.fields.imageUrls.arrayValue.values | ForEach-Object { $_.stringValue }
        }
        $found = @{ Id = $docId; Title = $title; ExistingUrls = $existingUrls }
        break
    }
}

# 유사 매칭
if (-not $found) {
    foreach ($doc in $response.documents) {
        $title = $doc.fields.title.stringValue
        $cleanTitle = ($title -replace '\s*\(.*?\)\s*', ' ').Trim()
        $cleanInput = ($StyleName -replace '\s*\(.*?\)\s*', ' ').Trim()
        if ($cleanTitle -eq $cleanInput -or $title -replace '[()]', '' -replace '\s+', ' ' -eq ($StyleName -replace '\s+', ' ')) {
            $docId = $doc.name.Split('/')[-1]
            $existingUrls = @()
            if ($doc.fields.imageUrls -and $doc.fields.imageUrls.arrayValue -and $doc.fields.imageUrls.arrayValue.values) {
                $existingUrls = $doc.fields.imageUrls.arrayValue.values | ForEach-Object { $_.stringValue }
            }
            $found = @{ Id = $docId; Title = $title; ExistingUrls = $existingUrls }
            Write-Output "  (유사 매칭: $StyleName -> $title)"
            break
        }
    }
}

if (-not $found) {
    Write-Output "ERROR: Firestore에서 '$StyleName' 스타일을 찾을 수 없습니다."
    return
}

Write-Output "  스타일: $($found.Title) ($($found.Id))"
Write-Output "  기존 이미지: $($found.ExistingUrls.Count)장"

# Firebase Storage 업로드
$f = Get-Item $Image
$timestamp = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
$safeName = ($found.Title -replace '[^a-zA-Z0-9가-힣_\-]', '_') -replace '_+', '_'
$anglePart = if ($Angle) { "_$Angle" } else { "" }
$ext = $f.Extension.ToLower()
$storageName = "styles/$($timestamp)_$($safeName)$($anglePart)$ext"
$encodedName = [Uri]::EscapeDataString($storageName)
$contentType = switch -Regex ($ext) {
    '\.webp' { "image/webp" }
    '\.png'  { "image/png" }
    default  { "image/jpeg" }
}
$uploadUrl = "https://firebasestorage.googleapis.com/v0/b/$bucket/o?uploadType=media&name=$encodedName"

Write-Output "  업로드 중..."
try {
    $fileBytes = [System.IO.File]::ReadAllBytes($f.FullName)
    $uploadResponse = Invoke-RestMethod -Uri $uploadUrl -Method Post -Body $fileBytes -ContentType $contentType
    $downloadToken = $uploadResponse.downloadTokens
    $downloadUrl = "https://firebasestorage.googleapis.com/v0/b/$bucket/o/$($encodedName)?alt=media&token=$downloadToken"
    Write-Output "  업로드 OK"
} catch {
    Write-Output "  UPLOAD FAILED: $($_.Exception.Message)"
    return
}

# Firestore imageUrls 업데이트
$allUrls = @($found.ExistingUrls) + @($downloadUrl)
$urlValues = $allUrls | ForEach-Object { @{ stringValue = $_ } }
$patchBody = @{
    fields = @{
        imageUrls = @{
            arrayValue = @{ values = $urlValues }
        }
    }
} | ConvertTo-Json -Depth 10

$patchUrl = "$firestoreBase/styles/$($found.Id)?key=$apiKey&updateMask.fieldPaths=imageUrls"
try {
    Invoke-RestMethod -Uri $patchUrl -Method Patch -Body ([System.Text.Encoding]::UTF8.GetBytes($patchBody)) -ContentType "application/json; charset=utf-8" | Out-Null
    Write-Output "  Firestore 업데이트 (총 $($allUrls.Count)장)"
} catch {
    Write-Output "  FIRESTORE FAILED: $($_.Exception.Message)"
    return
}

# 게시
$publishBody = @{
    fields = @{ isPublished = @{ booleanValue = $true } }
} | ConvertTo-Json -Depth 5
$publishUrl = "$firestoreBase/styles/$($found.Id)?key=$apiKey&updateMask.fieldPaths=isPublished"
try {
    Invoke-RestMethod -Uri $publishUrl -Method Patch -Body ([System.Text.Encoding]::UTF8.GetBytes($publishBody)) -ContentType "application/json; charset=utf-8" | Out-Null
    Write-Output "  게시 완료"
} catch {
    Write-Output "  게시 실패: $($_.Exception.Message)"
}

Write-Output ""
Write-Output "=== 완료: $($found.Title) ($($anglePart -replace '_','')) ==="
