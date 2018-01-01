Set-StrictMode -Version 3

task Build {
  lib\XmlDoc2CmdletDoc.0.2.9\tools\XmlDoc2CmdletDoc.exe csproj\bin\Release\HorkerTemplateEngine.dll

  $null = mkdir "$PSScriptRoot\HorkerTemplateEngine\" -Force

  Copy-Item "$PSScriptRoot\scripts\HorkerTemplateEngine.psd1" "$PSScriptRoot\HorkerTemplateEngine"
  Copy-Item "$PSScriptRoot\csproj\bin\Release\HorkerTemplateEngine.dll-Help.xml" "$PSScriptRoot\HorkerTemplateEngine"
  Copy-Item "$PSScriptRoot\csproj\bin\Release\HorkerTemplateEngine.dll" "$PSScriptRoot\HorkerTemplateEngine"
}

task Test {
  # To avoid dll locking, start separate powershell instance
  powershell -c "Invoke-Pester $PSScriptRoot\tests"
}

task Install {
  $psd = Invoke-Expression ((Get-Content "$PSScriptRoot\scripts\HorkerTemplateEngine.psd1") -join "`r`n")
  $version = $psd.ModuleVersion
  $installPath = "$HOME\Documents\WindowsPowerShell\Modules\HorkerTemplateEngine\$version"

  $null = mkdir $installPath -Force
  Write-Host "INSTALL PATH: $installPath"

  Get-Item "$PSScriptRoot\HorkerTemplateEngine\*" | foreach {
    Write-Host "FILE: $_.FullName"
    try {
      Copy-Item $_.FullName $installPath
    }
    catch {
      Write-Error $_ -EA Continue
    }
  }
}
