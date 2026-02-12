[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$apiKey = "AIzaSyBUC61LDhLONI-O0QOFl3y2C4uWMapbbI0"
$projectId = "always-hair-salon"
$firestoreBase = "https://firestore.googleapis.com/v1/projects/$projectId/databases/(default)/documents"
$response = Invoke-RestMethod -Uri "$firestoreBase/styles?key=$apiKey&pageSize=300" -Method Get
foreach ($doc in $response.documents) {
    $title = $doc.fields.title.stringValue
    if ($title -match 'magic|Magic' -or $title -match 'Iron|iron') {
        Write-Output $title
    }
}
