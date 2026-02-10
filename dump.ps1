$currentDir = Get-Location
$outputFolder = Join-Path $currentDir "dump/"
$xmlPath = Join-Path $outputFolder "window_dump.xml"
$screenshotPath = Join-Path $outputFolder "screen.png"
$adbDevice = "192.168.1.63:5555"

Write-Host "Capturing xml..."
adb -s $adbDevice shell uiautomator dump
adb -s $adbDevice pull /sdcard/window_dump.xml $xmlPath

Write-Host "Capturing screenshot..."
adb -s $adbDevice shell screencap -p /sdcard/screen.png
adb -s $adbDevice pull /sdcard/screen.png $screenshotPath

# Add-Type -AssemblyName System.Drawing
# [xml]$xmlData = Get-Content -Path $xmlPath
# $gridNodes = $xmlData.SelectNodes("//node")
 
# $tempImg = [System.Drawing.Image]::FromFile((Resolve-Path $screenshotPath))
# $sourceImg = New-Object System.Drawing.Bitmap($tempImg)
# $tempImg.Dispose()

# $index = 0
# foreach ($node in $gridNodes) {
#     $index++
#     if ($node.bounds -match '\[(\d+),(\d+)\]\[(\d+),(\d+)\]') {
#         $x1, $y1, $x2, $y2 = [int]$matches[1], [int]$matches[2], [int]$matches[3], [int]$matches[4]
#         $width, $height = ($x2 - $x1), ($y2 - $y1)

#         if ($width -gt 0 -and $height -gt 0) {
#             $rect = New-Object System.Drawing.Rectangle($x1, $y1, $width, $height)
#             $bmp = New-Object System.Drawing.Bitmap($width, $height)
#             $graphics = [System.Drawing.Graphics]::FromImage($bmp)
            
#             $graphics.DrawImage($sourceImg, (New-Object System.Drawing.Rectangle(0, 0, $width, $height)), $rect, [System.Drawing.GraphicsUnit]::Pixel)
            
#             # Utilisation d'un chemin complet et propre pour le Save
#             $fileName = Join-Path $outputFolder "node_$($index).png"
            
#             try {
#                 $bmp.Save($fileName, [System.Drawing.Imaging.ImageFormat]::Png)
#                 Write-Host "Cadrage réussi : $fileName" -ForegroundColor Green
#             } catch {
#                 Write-Error "Erreur sur le node $index : $($_.Exception.Message)"
#             }
            
#             $graphics.Dispose()
#             $bmp.Dispose()
#         }
#     }
# }

# $sourceImg.Dispose()
# Write-Host "Traitement terminé !" -ForegroundColor Yellow
# Write-Host "Done!"