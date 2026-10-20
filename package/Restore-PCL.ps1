param(
    [Parameter(Position=0)][string]$Path,
    [switch]$Restore,
    [string]$Backup
)
$ErrorActionPreference='Stop'
$supportedVersion=[Version]'2.99.0.0'
try {
    if (!$Path) {
        Add-Type -AssemblyName System.Windows.Forms
        $dialog=New-Object System.Windows.Forms.OpenFileDialog
        $dialog.Title='选择要修改的 PCL 2.99.0.0 启动器'
        $dialog.Filter='PCL 启动器 (*.exe)|*.exe'
        if($dialog.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK){ return }
        $Path=$dialog.FileName
        $dialog.Dispose()
    }
    $target=(Resolve-Path -LiteralPath $Path).ProviderPath
    if([IO.Path]::GetExtension($target) -ine '.exe'){throw '请选择 EXE 文件。'}
    $current=(Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
    # Acquire and release an exclusive handle first: refuse a running or read-only EXE.
    $handle=[IO.File]::Open($target,[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
    $handle.Dispose()
    if($Restore){
        if(!$Backup){
            $choices=@(Get-ChildItem -LiteralPath ([IO.Path]::GetDirectoryName($target)) -File | Where-Object {$_.Name.StartsWith([IO.Path]::GetFileName($target)+'.pcl-backup-',[StringComparison]::OrdinalIgnoreCase)})
            if($choices.Count -ne 1){throw '备份不是唯一一个，请用 -Backup 指定原始备份文件。'}
            $Backup=$choices[0].FullName
        }
        $backupPath=(Resolve-Path -LiteralPath $Backup).ProviderPath
        if([Reflection.AssemblyName]::GetAssemblyName($backupPath).Version -ne $supportedVersion){throw '备份不是 2.99.0.0，拒绝还原。'}
        $temp=$target+'.restore-'+[Guid]::NewGuid().ToString('N')+'.tmp'
        [IO.File]::Copy($backupPath,$temp,$false)
        $undo=$target+'.before-restore-'+[DateTime]::Now.ToString('yyyyMMdd-HHmmss')+'-'+[Guid]::NewGuid().ToString('N')+'.bak'
        [IO.File]::Replace($temp,$target,$undo)
        Write-Host ('已还原原始启动器；还原前的文件保存在：'+$undo)
        return
    }
    if([Reflection.AssemblyName]::GetAssemblyName($target).Version -ne $supportedVersion){throw '目前仅支持 2.99.0.0，文件未修改。'}
    $helper=Join-Path $PSScriptRoot 'PclRestore.exe'
    $payload=Join-Path $PSScriptRoot 'restore-methods.dat'
    if(!(Test-Path -LiteralPath $helper) -or !(Test-Path -LiteralPath $payload)){throw '请完整解压补丁包，不要单独复制 PS1。'}
    $temp=$target+'.patch-'+[Guid]::NewGuid().ToString('N')+'.tmp'
    & $helper $target $payload $temp
    if($LASTEXITCODE -ne 0){throw '补丁生成或验证失败。原始 EXE 未替换。'}
    # Check again before replacing in case the target was changed during generation.
    if((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $current){throw '生成期间原文件发生变化，拒绝替换。'}
    $backupPath=$target+'.pcl-backup-'+[DateTime]::Now.ToString('yyyyMMdd-HHmmss')+'-'+[Guid]::NewGuid().ToString('N')+'.bak'
    [IO.File]::Replace($temp,$target,$backupPath)
    Write-Host ('下载功能补丁已应用：'+$target)
    Write-Host ('原始备份：'+$backupPath)
    Write-Host '直接运行原来的 EXE。官方更新可能覆盖补丁；补丁没有关闭自动更新。'
} catch {
    Write-Error $_
    exit 1
}
