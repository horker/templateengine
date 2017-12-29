
task Build {
  XmlDoc2CmdletDoc.0.2.9\tools\XmlDoc2CmdletDoc.exe csproj\bin\Release\HorkerTemplateEngine.dll

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
  $null = mkdir "$HOME\Documents\WindowsPowerShell\Modules\HorkerTemplateEngine" -Force
  Get-Item "$PSScriptRoot\HorkerTemplateEngine\*" | foreach {
    $_.FullName
    try {
      Copy-Item $_.FullName "$HOME\Documents\WindowsPowerShell\Modules\HorkerTemplateEngine"
    }
    catch {
      Write-Error $_ -EA Continue
    }
  }
}
