$xmlPath = "window_dump.xml"

adb shell uiautomator dump
adb shell cat /sdcard/window_dump.xml > $xmlPath
[xml]$xmlData = Get-Content -Path $xmlPath
$nodes = $xmlData.SelectNodes("//node")
foreach ($node in $nodes) {
    Write-Output $node."resource-id"
}