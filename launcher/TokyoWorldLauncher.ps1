# Tokyo World Launcher v0.9 — Windows PowerShell + WinForms
# Prism manages Microsoft OAuth and game files. We do not store passwords/tokens.
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[System.Windows.Forms.Application]::EnableVisualStyles()
$ErrorActionPreference = 'Stop'
$script:repo = Split-Path -Parent $PSScriptRoot
$script:instance = 'Tokyo_World_Forge_1.20.1'
$script:prismRoot = Join-Path $env:APPDATA 'PrismLauncher'
$script:gameDir = Join-Path $script:prismRoot ('instances\' + $script:instance + '\minecraft')
$script:localDir = Join-Path $env:LOCALAPPDATA 'TokyoWorldLauncher'
$script:settingsPath = Join-Path $script:localDir 'settings.json'
$script:settings = $null
$script:prismExe = ''
try {
    if (Test-Path $script:settingsPath) {
        $script:settings = Get-Content $script:settingsPath -Raw -Encoding UTF8 | ConvertFrom-Json
        $script:prismExe = [string]$script:settings.prismExe
    }
} catch {}

$colors = @{
    bg = [Drawing.Color]::FromArgb(12,13,20)
    side = [Drawing.Color]::FromArgb(22,24,34)
    field = [Drawing.Color]::FromArgb(31,34,46)
    red = [Drawing.Color]::FromArgb(225,55,76)
    pale = [Drawing.Color]::FromArgb(240,240,246)
    gray = [Drawing.Color]::FromArgb(155,158,171)
}
function Label($parent, [string]$content, [int]$x,[int]$y,[int]$w,[int]$h,[int]$font,[bool]$strong,$fg) {
    $l=New-Object Windows.Forms.Label
    $l.Text=$content
    $l.Location=New-Object Drawing.Point($x,$y)
    $l.Size=New-Object Drawing.Size($w,$h)
    $weight=if($strong){[Drawing.FontStyle]::Bold}else{[Drawing.FontStyle]::Regular}
    $l.Font=New-Object Drawing.Font('Segoe UI',$font,$weight)
    $l.ForeColor=$fg
    $l.BackColor=[Drawing.Color]::Transparent
    $parent.Controls.Add($l)
    return $l
}
function Button($parent,[string]$content,[int]$x,[int]$y,[int]$w,[int]$h,$bg) {
    $b=New-Object Windows.Forms.Button
    $b.Text=$content
    $b.Location=New-Object Drawing.Point($x,$y)
    $b.Size=New-Object Drawing.Size($w,$h)
    $b.BackColor=$bg
    $b.ForeColor=$colors.pale
    $b.Font=New-Object Drawing.Font('Segoe UI',10,[Drawing.FontStyle]::Bold)
    $b.FlatStyle=[Windows.Forms.FlatStyle]::Flat
    $b.FlatAppearance.BorderSize=0
    $b.Cursor=[Windows.Forms.Cursors]::Hand
    $parent.Controls.Add($b)
    return $b
}
function ErrorBox([string]$message) {
    [Windows.Forms.MessageBox]::Show($message,'Tokyo World','OK','Error') | Out-Null
}
function Save-Preferences {
    try {
        if(!(Test-Path $script:localDir)){New-Item -ItemType Directory -Path $script:localDir -Force|Out-Null}
        $mode=if($script:offline -and $script:offline.Checked){'offline'}else{'premium'}
        $nick=if($script:nick){[string]$script:nick.Text}else{''}
        $account=if($script:accounts){[string]$script:accounts.Text}else{''}
        $data=[ordered]@{prismExe=$script:prismExe;mode=$mode;nick=$nick;account=$account}
        $json=ConvertTo-Json -InputObject $data
        [IO.File]::WriteAllText($script:settingsPath,$json,(New-Object Text.UTF8Encoding($false)))
    } catch {}
}
function Find-Prism {
    if($script:prismExe -and (Test-Path -LiteralPath $script:prismExe -PathType Leaf)){return $true}
    $paths=New-Object Collections.Generic.List[string]
    $command=Get-Command prismlauncher.exe -ErrorAction SilentlyContinue
    if($command){$paths.Add($command.Source)}
    if($env:LOCALAPPDATA){
        $paths.Add((Join-Path $env:LOCALAPPDATA 'Programs\PrismLauncher\prismlauncher.exe'))
        $paths.Add((Join-Path $env:LOCALAPPDATA 'PrismLauncher\prismlauncher.exe'))
    }
    if($env:ProgramFiles){$paths.Add((Join-Path $env:ProgramFiles 'PrismLauncher\prismlauncher.exe'))}
    $pf86=[Environment]::GetFolderPath('ProgramFilesX86')
    if($pf86){$paths.Add((Join-Path $pf86 'PrismLauncher\prismlauncher.exe'))}
    $paths.Add((Join-Path $script:prismRoot 'prismlauncher.exe'))
    foreach($candidate in $paths){
        if(Test-Path -LiteralPath $candidate -PathType Leaf){
            $script:prismExe=$candidate
            Save-Preferences
            return $true
        }
    }
    $fileDialog=New-Object Windows.Forms.OpenFileDialog
    $fileDialog.Title='Wybierz plik prismlauncher.exe'
    $fileDialog.Filter='Prism Launcher|prismlauncher.exe|Pliki EXE|*.exe'
    if($fileDialog.ShowDialog() -eq [Windows.Forms.DialogResult]::OK){
        $script:prismExe=$fileDialog.FileName
        Save-Preferences
        return $true
    }
    return $false
}
function Reload-MicrosoftAccounts {
    $current=$script:accounts.Text
    $script:accounts.Items.Clear()
    $file=Join-Path $script:prismRoot 'accounts.json'
    if(Test-Path -LiteralPath $file -PathType Leaf){
        try {
            $json=Get-Content -LiteralPath $file -Raw -Encoding UTF8 | ConvertFrom-Json
            foreach($account in @($json.accounts)){
                if([string]$account.type -ne 'MSA'){continue}
                $name=[string]$account.minecraftProfile.name
                if(-not $name -and $account.profile){$name=[string]$account.profile.name}
                if($name -match '^[A-Za-z0-9_]{3,16}$' -and -not $script:accounts.Items.Contains($name)){
                    [void]$script:accounts.Items.Add($name)
                }
            }
        } catch {}
    }
    if($script:accounts.Items.Count -gt 0){$script:accounts.SelectedIndex=0}
    if($current){$script:accounts.Text=$current}
}
function Set-Mode {
    $off=$script:offline.Checked
    $script:nick.Visible=$off
    $script:accounts.Visible=(-not $off)
    $script:caption.Text=if($off){'Nick gracza (3–16 znaków)'}else{'Konto Minecraft Premium'}
    $script:hint.Text=if($off){
        'Offline: świat lokalny lub serwer bez weryfikacji premium.'+[Environment]::NewLine+
        'Prism może wymagać wcześniejszego legalnego konta Microsoft.'
    }else{
        'Dodaj konto przez KONTA MICROSOFT w oficjalnym Prism.'+[Environment]::NewLine+
        'Launcher Tokyo World nigdy nie pobiera hasła ani tokenu.'
    }
    Save-Preferences
}

$form=New-Object Windows.Forms.Form
$form.Text='Tokyo World Launcher'
$form.ClientSize=New-Object Drawing.Size(880,535)
$form.StartPosition=[Windows.Forms.FormStartPosition]::CenterScreen
$form.FormBorderStyle=[Windows.Forms.FormBorderStyle]::FixedSingle
$form.MaximizeBox=$false
$form.BackColor=$colors.bg
$form.ForeColor=$colors.pale

$side=New-Object Windows.Forms.Panel
$side.Location=New-Object Drawing.Point(0,0)
$side.Size=New-Object Drawing.Size(320,535)
$side.BackColor=$colors.side
$form.Controls.Add($side)
$accent=New-Object Windows.Forms.Panel
$accent.Location=New-Object Drawing.Point(0,0)
$accent.Size=New-Object Drawing.Size(320,5)
$accent.BackColor=$colors.red
$side.Controls.Add($accent)
[void](Label $side 'TOKYO' 26 79 265 59 34 $true $colors.pale)
[void](Label $side 'WORLD' 26 130 265 59 34 $true $colors.red)
[void](Label $side 'EXPERT FUN // TECH // ADVENTURE' 28 197 285 23 9 $false $colors.gray)
[void](Label $side 'Minecraft 1.20.1' 28 286 280 30 15 $true $colors.pale)
[void](Label $side 'Forge 47.4.28' 28 318 280 25 11 $false $colors.gray)
[void](Label $side 'TOKYO WORLD v0.9' 28 461 280 22 11 $true $colors.pale)
[void](Label $side 'Silnik gry: Prism Launcher' 28 489 280 22 9 $false $colors.gray)
[void](Label $form 'URUCHOM GRĘ' 355 29 500 50 23 $true $colors.pale)
[void](Label $form 'Wybierz tryb logowania i rozpocznij przygodę.' 355 80 500 25 10 $false $colors.gray)

$script:premium=New-Object Windows.Forms.RadioButton
$script:premium.Text='Premium / Microsoft'
$script:premium.Location=New-Object Drawing.Point(355,125)
$script:premium.Size=New-Object Drawing.Size(245,30)
$script:premium.Font=New-Object Drawing.Font('Segoe UI',11,[Drawing.FontStyle]::Bold)
$script:premium.ForeColor=$colors.pale
$form.Controls.Add($script:premium)
$script:offline=New-Object Windows.Forms.RadioButton
$script:offline.Text='Offline / Nick'
$script:offline.Location=New-Object Drawing.Point(616,125)
$script:offline.Size=New-Object Drawing.Size(200,30)
$script:offline.Font=New-Object Drawing.Font('Segoe UI',11,[Drawing.FontStyle]::Bold)
$script:offline.ForeColor=$colors.pale
$form.Controls.Add($script:offline)
$script:caption=Label $form 'Konto Premium' 355 178 472 22 10 $false $colors.gray

$script:accounts=New-Object Windows.Forms.ComboBox
$script:accounts.Location=New-Object Drawing.Point(355,203)
$script:accounts.Size=New-Object Drawing.Size(467,30)
$script:accounts.DropDownStyle=[Windows.Forms.ComboBoxStyle]::DropDown
$script:accounts.Font=New-Object Drawing.Font('Segoe UI',12)
$script:accounts.BackColor=$colors.field
$script:accounts.ForeColor=$colors.pale
$form.Controls.Add($script:accounts)
$script:nick=New-Object Windows.Forms.TextBox
$script:nick.Location=New-Object Drawing.Point(355,203)
$script:nick.Size=New-Object Drawing.Size(467,31)
$script:nick.Font=New-Object Drawing.Font('Segoe UI',12)
$script:nick.BackColor=$colors.field
$script:nick.ForeColor=$colors.pale
$script:nick.BorderStyle=[Windows.Forms.BorderStyle]::FixedSingle
$form.Controls.Add($script:nick)
$script:hint=Label $form '' 355 243 471 51 9 $false $colors.gray

$play=Button $form 'GRAJ W TOKYO WORLD' 355 312 467 55 $colors.red
$login=Button $form 'KONTA MICROSOFT' 355 383 224 39 $colors.field
$refresh=Button $form 'ODŚWIEŻ KONTA' 596 383 226 39 $colors.field
$update=Button $form 'AKTUALIZUJ' 355 444 145 40 $colors.field
$mods=Button $form 'NOWE MODY' 513 444 145 40 $colors.field
$folder=Button $form 'FOLDER GRY' 671 444 151 40 $colors.field
$script:status=Label $form 'Gotowy do uruchomienia.' 357 501 460 23 9 $false $colors.gray

$script:premium.Add_CheckedChanged({Set-Mode})
$script:offline.Add_CheckedChanged({Set-Mode})
$script:nick.Add_TextChanged({Save-Preferences})
$script:accounts.Add_TextChanged({Save-Preferences})

$play.Add_Click({
    try{
        if(-not (Find-Prism)){return}
        if(-not (Test-Path -LiteralPath $script:gameDir -PathType Container)){
            ErrorBox 'Brak instancji Tokyo_World_Forge_1.20.1 w Prism Launcher.'
            return
        }
        $name=if($script:offline.Checked){$script:nick.Text.Trim()}else{$script:accounts.Text.Trim()}
        if($name -cnotmatch '^[A-Za-z0-9_]{3,16}$'){
            ErrorBox 'Nick musi mieć 3–16 znaków: litery A-Z, cyfry i podkreślenie.'
            return
        }
        $argline=if($script:offline.Checked){
            @('--launch',$script:instance,'--offline',$name)
        }else{
            @('--launch',$script:instance,'--profile',$name)
        }
        Save-Preferences
        Start-Process -FilePath $script:prismExe -ArgumentList $argline -WorkingDirectory (Split-Path $script:prismExe -Parent) | Out-Null
        $script:status.Text="Przekazano uruchomienie do Prism: $name"
    }catch{ErrorBox ("Błąd uruchamiania: "+$_.Exception.Message)}
})
$login.Add_Click({
    try{
        if(-not (Find-Prism)){return}
        Start-Process -FilePath $script:prismExe -WorkingDirectory (Split-Path $script:prismExe -Parent)|Out-Null
        [Windows.Forms.MessageBox]::Show('W Prism otwórz Konta > Zarządzaj kontami > Dodaj Microsoft. Po logowaniu kliknij ODŚWIEŻ KONTA.','Tokyo World','OK','Information')|Out-Null
    }catch{ErrorBox $_.Exception.Message}
})
$refresh.Add_Click({Reload-MicrosoftAccounts;$script:status.Text='Odświeżono listę kont Prism.'})
$update.Add_Click({
    $path=Join-Path $script:repo 'tools\Update-TokyoWorld.ps1'
    if(!(Test-Path $path)){ErrorBox 'Brak skryptu aktualizacji.';return}
    $arguments='-NoExit -NoProfile -ExecutionPolicy Bypass -File "'+$path+'" -GameDir "'+$script:gameDir+'"'
    Start-Process -FilePath 'powershell.exe' -ArgumentList $arguments|Out-Null
    $script:status.Text='Aktualizacja została otwarta w osobnym oknie.'
})
$mods.Add_Click({
    $path=Join-Path $script:repo 'INSTALUJ_TOKYO_WAVE.cmd'
    if(!(Test-Path $path)){ErrorBox 'Brak instalatora WAVE. Zaktualizuj GitHub.';return}
    Start-Process -FilePath 'cmd.exe' -ArgumentList ('/c ""'+$path+'""') | Out-Null
})
$folder.Add_Click({
    if(Test-Path -LiteralPath $script:gameDir){
        Start-Process -FilePath 'explorer.exe' -ArgumentList ('"'+$script:gameDir+'"')|Out-Null
    }else{ErrorBox 'Nie znaleziono folderu Minecraft.'}
})
$script:premium.Checked=$true
if($script:settings -and $script:settings.mode -eq 'offline'){$script:offline.Checked=$true}
if($script:settings -and $script:settings.nick){$script:nick.Text=[string]$script:settings.nick}
Reload-MicrosoftAccounts
if($script:settings -and $script:settings.account){$script:accounts.Text=[string]$script:settings.account}
Set-Mode
[void]$form.ShowDialog()
$form.Dispose()
