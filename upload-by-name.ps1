param(
    [string]$Folder,
    [switch]$ListOnly
)

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$bucket = "always-hair-salon.firebasestorage.app"
$apiKey = "AIzaSyBUC61LDhLONI-O0QOFl3y2C4uWMapbbI0"
$projectId = "always-hair-salon"
$firestoreBase = "https://firestore.googleapis.com/v1/projects/$projectId/databases/(default)/documents"

# Firestore에서 스타일 목록 가져오기
Write-Output "=== Fetching styles ==="
$response = Invoke-RestMethod -Uri "$firestoreBase/styles?key=$apiKey&pageSize=300" -Method Get
$styleMap = @{}
foreach ($doc in $response.documents) {
    $docId = $doc.name.Split('/')[-1]
    $title = $doc.fields.title.stringValue
    $category = if ($doc.fields.category) { $doc.fields.category.stringValue } else { "" }
    $imgCount = 0
    $existingUrls = @()
    if ($doc.fields.imageUrls -and $doc.fields.imageUrls.arrayValue -and $doc.fields.imageUrls.arrayValue.values) {
        $existingUrls = $doc.fields.imageUrls.arrayValue.values | ForEach-Object { $_.stringValue }
        $imgCount = $existingUrls.Count
    }
    $styleMap[$title] = @{ Id = $docId; ExistingUrls = $existingUrls; Category = $category; ImgCount = $imgCount }
}
Write-Output "Loaded $($styleMap.Count) styles"

# 가이드 모드: 이미지가 필요한 스타일 목록 출력
if ($ListOnly -or -not $Folder) {
    Write-Output ""
    Write-Output "=== 이미지가 없는 스타일 ==="
    Write-Output ""
    $needsImage = @()
    $hasImage = @()
    foreach ($title in ($styleMap.Keys | Sort-Object)) {
        $info = $styleMap[$title]
        if ($info.ImgCount -eq 0) {
            $needsImage += $title
            Write-Output "  [ ] $($info.Category) | $title"
        } else {
            $hasImage += $title
        }
    }
    Write-Output ""
    Write-Output "--- 이미 이미지가 있는 스타일 ---"
    foreach ($t in ($hasImage | Sort-Object)) {
        $info = $styleMap[$t]
        Write-Output "  [$($info.ImgCount)] $($info.Category) | $t"
    }
    Write-Output ""
    Write-Output "=== 총 $($needsImage.Count)개 스타일에 이미지 필요 ==="
    Write-Output ""
    Write-Output "파일명을 위 스타일 이름과 동일하게 저장하세요."
    Write-Output "  예: '내추럴 가르마 펌.webp', '쉐도우 펌.jpg'"
    Write-Output ""
    Write-Output "업로드 실행:"
    Write-Output "  .\upload-by-name.ps1 -Folder 'C:\이미지폴더경로'"
    return
}

# 업로드 모드
if (-not (Test-Path $Folder)) {
    Write-Output "ERROR: 폴더를 찾을 수 없습니다: $Folder"
    return
}

$files = Get-ChildItem $Folder -File | Where-Object { $_.Extension -match '\.(webp|jpg|jpeg|png)$' }
Write-Output ""
Write-Output "=== 폴더: $Folder ==="
Write-Output "이미지 파일 $($files.Count)개 발견"
Write-Output ""

$uploadedDocIds = @()
$skipCount = 0
$failCount = 0

foreach ($f in $files) {
    $styleTitle = $f.BaseName.Trim()
    $styleInfo = $styleMap[$styleTitle]

    # 정확 매칭 실패 시 유사 매칭 시도
    if (-not $styleInfo) {
        $matched = $styleMap.Keys | Where-Object { $_.Trim() -eq $styleTitle }
        if ($matched) {
            $styleTitle = if ($matched -is [array]) { $matched[0] } else { $matched }
            $styleInfo = $styleMap[$styleTitle]
        }
    }

    # 괄호 없는 버전으로도 시도 (예: "볼륨 매직 남성" → "볼륨 매직 (남성)")
    if (-not $styleInfo) {
        $matched = $styleMap.Keys | Where-Object {
            ($_ -replace '\s*\(.*?\)\s*', ' ').Trim() -eq $styleTitle -or
            $_ -replace '[()]', '' -replace '\s+', ' ' -eq ($styleTitle -replace '\s+', ' ')
        }
        if ($matched) {
            $styleTitle = if ($matched -is [array]) { $matched[0] } else { $matched }
            $styleInfo = $styleMap[$styleTitle]
            Write-Output "  (유사 매칭: $($f.BaseName) -> $styleTitle)"
        }
    }

    if (-not $styleInfo) {
        Write-Output "  SKIP (미등록): $($f.Name)"
        $skipCount++
        continue
    }

    $docId = $styleInfo.Id
    Write-Output "  업로드: $($f.Name) -> $styleTitle"

    # Firebase Storage 업로드
    $timestamp = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
    $safeName = ($styleTitle -replace '[^a-zA-Z0-9가-힣_\-]', '_') -replace '_+', '_'
    $ext = $f.Extension.ToLower()
    $storageName = "styles/$($timestamp)_$($safeName)$ext"
    $encodedName = [Uri]::EscapeDataString($storageName)
    $contentType = switch -Regex ($ext) {
        '\.webp' { "image/webp" }
        '\.png'  { "image/png" }
        default  { "image/jpeg" }
    }
    $uploadUrl = "https://firebasestorage.googleapis.com/v0/b/$bucket/o?uploadType=media&name=$encodedName"

    try {
        $fileBytes = [System.IO.File]::ReadAllBytes($f.FullName)
        $uploadResponse = Invoke-RestMethod -Uri $uploadUrl -Method Post -Body $fileBytes -ContentType $contentType
        $downloadToken = $uploadResponse.downloadTokens
        $downloadUrl = "https://firebasestorage.googleapis.com/v0/b/$bucket/o/$($encodedName)?alt=media&token=$downloadToken"
        Write-Output "    OK"
    } catch {
        Write-Output "    UPLOAD FAILED: $($_.Exception.Message)"
        $failCount++
        continue
    }

    # Firestore imageUrls 업데이트
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
        Write-Output "    Firestore 업데이트 (총 $($allUrls.Count)장)"
        $styleInfo.ExistingUrls = $allUrls
        if ($uploadedDocIds -notcontains $docId) { $uploadedDocIds += $docId }
    } catch {
        Write-Output "    FIRESTORE FAILED: $($_.Exception.Message)"
        $failCount++
    }

    Start-Sleep -Milliseconds 300
}

# 업로드된 스타일 게시
if ($uploadedDocIds.Count -gt 0) {
    Write-Output ""
    Write-Output "=== $($uploadedDocIds.Count)개 스타일 게시 ==="
    foreach ($docId in $uploadedDocIds) {
        $publishBody = @{
            fields = @{ isPublished = @{ booleanValue = $true } }
        } | ConvertTo-Json -Depth 5
        $publishUrl = "$firestoreBase/styles/$($docId)?key=$apiKey&updateMask.fieldPaths=isPublished"
        try {
            Invoke-RestMethod -Uri $publishUrl -Method Patch -Body ([System.Text.Encoding]::UTF8.GetBytes($publishBody)) -ContentType "application/json; charset=utf-8" | Out-Null
            Write-Output "  Published: $docId"
        } catch {
            Write-Output "  PUBLISH FAILED: $($_.Exception.Message)"
        }
        Start-Sleep -Milliseconds 200
    }
}

Write-Output ""
Write-Output "=== 완료 (업로드: $($uploadedDocIds.Count) / 스킵: $skipCount / 실패: $failCount) ==="
