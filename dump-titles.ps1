[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$apiKey = "AIzaSyBUC61LDhLONI-O0QOFl3y2C4uWMapbbI0"
$projectId = "always-hair-salon"
$firestoreBase = "https://firestore.googleapis.com/v1/projects/$projectId/databases/(default)/documents"
$response = Invoke-RestMethod -Uri "$firestoreBase/styles?key=$apiKey&pageSize=300" -Method Get
$titles = @()
foreach ($doc in $response.documents) {
    $title = $doc.fields.title.stringValue
    $cat = $doc.fields.category.stringValue
    $titles += "$cat | $title"
}
$titles | Sort-Object | Out-File -FilePath "style-titles.txt" -Encoding UTF8
Write-Output "Saved $($titles.Count) titles to style-titles.txt"
