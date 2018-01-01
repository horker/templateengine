Import-Module pester

Import-Module "$PSScriptRoot\..\HorkerTemplateEngine"

function Invoke-Generator {
  param(
    [string]$Template,
    [switch]$Join
  )
  $engine = New-Object Horker.TemplateEngine.ScriptGenerator
  $script = $engine.GenerateScript($Template)
#  Write-Host $script
  $result = Invoke-Expression $script
  if ($Join) {
    $result = ($result | foreach { $_.ToString() }) -join ''
  }
  $result
}

Describe "script generator" {
  It "generates the same output as input in case of a simple string" {
    $template = "hello`r`nworld"
    $result = Invoke-Generator $template -Join
    $result | Should -Be $template
  }

  It "accepts a code block" {
    $result = Invoke-Generator -join "<% 123 %>"
    $result | Should -Be "123"
  }

  It "accepts a variable" {
    $variable = 999
    $result = Invoke-Generator -Join "<% $variable %>"
    $result | Should -Be "999"
  }

  It "accepts a code structure" {
    $result = Invoke-Generator -Join '<% foreach ($i in 1..3) {%>hello <% } %>'
    $result | Should -Be "hello hello hello "
  }

  It "accepts outputs from a code structure" {
    $result = Invoke-Generator -Join '<% foreach ($i in 1..3) { $i } %>'
    $result | Should -Be "123"
  }

  It "throws a TemplateSyntaxErrorException when a code block is not closed" {
    { Invoke-Generator -Join "<% abc" } | Should -Throw "premature end"
  }

  It "accepts a multiline template" {
    $result = Invoke-Generator -Join @'
<table><tr>
<% foreach ($i in ("foo", "bar", "baz")) { %>
<td>This is <% $i %></td>
<% } %>
</tr></table>
'@
    $result | Should -Be @'
<table><tr>

<td>This is foo</td>

<td>This is bar</td>

<td>This is baz</td>

</tr></table>
'@
  }

  It "discards a newline after -%>" {
    $result = Invoke-Generator -Join @'
<% 123 -%>
456
'@
    $result | Should -Be @'
123456
'@
  }

  It "recognizes a various types of newlines" {
    $result = Invoke-Generator -Join "<% 1 -%>`r<% 2 -%>`r`n<% 3 -%>`n<% 4 -%>`n`r"
    $result | Should -Be "1234`r"
  }

  It "treats incomplete code tags as normal characters" {
    $result = Invoke-Generator -Join '< % <% "%"; "-%"; ">" %>'
    $result | Should -Be "< % %-%>"
  }

  It "can process consecutive code blocks" {
    $result = Invoke-Generator -Join "<%1%><%2%><%3-%><%4%>"
    $result | Should -Be "1234"
  }

  It "can invoke a function defined in the current session" {
    function f () { "1234"; "abc" }
    $result = Invoke-Generator -Join "<% f %>"
    $result | Should -Be "1234abc"
  }
}

Describe "Invoke-TemplateEngine" {
  It "processes a template" {
    $temp = [IO.Path]::GetTempFileName()
    1..3 | Set-Content $temp
    $result = Get-Content $temp | Invoke-TemplateEngine
    $result | Should -Be "1`r`n2`r`n3`r`n"
  }

  It "accepts a variable" {
    $variable = 999
    $result = "<% $variable %>" | Invoke-TemplateEngine
    $result | Should -Be "999`r`n"
  }

  It "accepts a template from a pipeline including empty strings" {
    $template = @"
a

b

c
"@
    $result = $template | Invoke-TemplateEngine
    $result | Should -Be ($template + "`r`n") # PowerShell heredoc can't end with newline
  }

#  It "reports an error with the source line number" {
#    $result = "10`r`n20`r`n<% xxx %>`r`n<% yyy %>" | Invoke-TemplateEngine 2>&1
#  }
}

Describe "help topc" {
  It "shows a help topic when Get-Help is invoked" {
    Get-Help Invoke-TemplateEngine | Out-String | Should -Match "a document template"
  }
}

#Get-Help Invoke-TemplateEngine -Detailed
