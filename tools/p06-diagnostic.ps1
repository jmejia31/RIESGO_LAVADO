$script:P06ScenarioIds = @('AUTH_01', 'AUTH_02', 'AUTH_03', 'AUTH_04', 'AUTH_05', 'AUTH_06A')
$script:P06ScenarioStatuses = @('PASS', 'FAIL', 'SKIPPED', 'NOT_RUN')
$script:P06FailureCategories = @('ASSERTION_FAILURE', 'HTTP_STATUS_MISMATCH', 'TIMEOUT', 'NAVIGATION_FAILURE', 'BROWSER_FAILURE', 'ENVIRONMENT_FAILURE', 'SETUP_FAILURE', 'UNKNOWN_FAILURE')
$script:P06ScenarioStages = @{
    AUTH_01 = 'login_profile_permissions'
    AUTH_02 = 'session_storage'
    AUTH_03 = 'csrf'
    AUTH_04 = 'refresh_rotation'
    AUTH_05 = 'logout_revocation'
    AUTH_06A = 'refresh_concurrency'
}

function Get-P06SafeDiagnostic {
    param([object[]]$Lines)

    $statuses = @{}
    $stages = @{}
    $categories = @{}
    $reportedFailed = $null
    $reportedCounts = @{}
    $reporter = $false
    $available = $false
    $reportedReal = $null
    foreach ($item in @($Lines)) {
        $line = [string]$item
        if ($line -match '^P06_DIAGNOSTIC_REPORTER=(PASS)$') { $reporter = $true; continue }
        if ($line -match '^P06_DIAGNOSTIC_STATUS=(AVAILABLE)$') { $available = $true; continue }
        if ($line -match '^P06_(AUTH_01|AUTH_02|AUTH_03|AUTH_04|AUTH_05|AUTH_06A)=(PASS|FAIL|SKIPPED|NOT_RUN)$') { $statuses[$Matches[1]] = $Matches[2]; continue }
        if ($line -match '^P06_(AUTH_01|AUTH_02|AUTH_03|AUTH_04|AUTH_05|AUTH_06A)_STAGE=(login_profile_permissions|session_storage|csrf|refresh_rotation|logout_revocation|refresh_concurrency)$') { $stages[$Matches[1]] = $Matches[2]; continue }
        if ($line -match '^P06_(AUTH_01|AUTH_02|AUTH_03|AUTH_04|AUTH_05|AUTH_06A)_FAILURE_CATEGORY=(ASSERTION_FAILURE|HTTP_STATUS_MISMATCH|TIMEOUT|NAVIGATION_FAILURE|BROWSER_FAILURE|ENVIRONMENT_FAILURE|SETUP_FAILURE|UNKNOWN_FAILURE)$') { $categories[$Matches[1]] = $Matches[2]; continue }
        if ($line -match '^P06_TESTS_(TOTAL|PASSED|FAILED|SKIPPED|NOT_RUN)=(\d+)$') { $reportedCounts[$Matches[1]] = [int]$Matches[2]; continue }
        if ($line -match '^P06_FAILED_SCENARIOS=(NONE|(?:AUTH_01|AUTH_02|AUTH_03|AUTH_04|AUTH_05|AUTH_06A)(?:,(?:AUTH_01|AUTH_02|AUTH_03|AUTH_04|AUTH_05|AUTH_06A))*)$') { $reportedFailed = $Matches[1]; continue }
        if ($line -match '^P06_FAILURE_CATEGORY=(NONE|ASSERTION_FAILURE|HTTP_STATUS_MISMATCH|TIMEOUT|NAVIGATION_FAILURE|BROWSER_FAILURE|ENVIRONMENT_FAILURE|SETUP_FAILURE|UNKNOWN_FAILURE)$') { $aggregateCategory = $Matches[1]; continue }
        if ($line -match '^P06_REAL_CERTIFICATION=(PASS|FAIL)$') { $reportedReal = $Matches[1]; continue }
        if ($line -like 'P06_*') { return $null }
    }

    if (-not $reporter -or -not $available -or $null -eq $reportedFailed -or $null -eq $reportedReal -or $null -eq $aggregateCategory) { return $null }
    foreach ($id in $script:P06ScenarioIds) {
        if (-not $statuses.ContainsKey($id) -or -not $stages.ContainsKey($id) -or $stages[$id] -ne $script:P06ScenarioStages[$id]) { return $null }
        if ($statuses[$id] -eq 'FAIL' -and -not $categories.ContainsKey($id)) { return $null }
        if ($statuses[$id] -ne 'FAIL' -and $categories.ContainsKey($id)) { return $null }
    }

    $passed = @($script:P06ScenarioIds | Where-Object { $statuses[$_] -eq 'PASS' }).Count
    $failed = @($script:P06ScenarioIds | Where-Object { $statuses[$_] -eq 'FAIL' }).Count
    $skipped = @($script:P06ScenarioIds | Where-Object { $statuses[$_] -eq 'SKIPPED' }).Count
    $notRun = @($script:P06ScenarioIds | Where-Object { $statuses[$_] -eq 'NOT_RUN' }).Count
    $failedIds = @($script:P06ScenarioIds | Where-Object { $statuses[$_] -eq 'FAIL' })
    $expectedFailed = if ($failedIds.Count -eq 0) { 'NONE' } else { $failedIds -join ',' }
    if ($reportedCounts['TOTAL'] -ne 6 -or $reportedCounts['PASSED'] -ne $passed -or $reportedCounts['FAILED'] -ne $failed -or $reportedCounts['SKIPPED'] -ne $skipped -or $reportedCounts['NOT_RUN'] -ne $notRun) { return $null }
    if ($reportedFailed -ne $expectedFailed) { return $null }
    $expectedReal = if ($passed -eq 6) { 'PASS' } else { 'FAIL' }
    if ($reportedReal -ne $expectedReal) { return $null }

    return [PSCustomObject]@{
        Statuses = $statuses
        Stages = $stages
        Categories = $categories
        Passed = $passed
        Failed = $failed
        Skipped = $skipped
        NotRun = $notRun
        FailedScenarios = $expectedFailed
        FailureCategory = $aggregateCategory
        RealCertification = $reportedReal
    }
}

function Write-P06SafeDiagnostic {
    param([Parameter(Mandatory)]$Diagnostic)

    foreach ($id in $script:P06ScenarioIds) {
        Write-Output "P06_$id=$($Diagnostic.Statuses[$id])"
        Write-Output "P06_${id}_STAGE=$($Diagnostic.Stages[$id])"
        if ($Diagnostic.Categories.ContainsKey($id)) { Write-Output "P06_${id}_FAILURE_CATEGORY=$($Diagnostic.Categories[$id])" }
    }
    Write-Output 'P06_TESTS_TOTAL=6'
    Write-Output "P06_TESTS_PASSED=$($Diagnostic.Passed)"
    Write-Output "P06_TESTS_FAILED=$($Diagnostic.Failed)"
    Write-Output "P06_TESTS_SKIPPED=$($Diagnostic.Skipped)"
    Write-Output "P06_TESTS_NOT_RUN=$($Diagnostic.NotRun)"
    Write-Output "P06_FAILED_SCENARIOS=$($Diagnostic.FailedScenarios)"
    Write-Output "P06_FAILURE_CATEGORY=$($Diagnostic.FailureCategory)"
    Write-Output "P06_REAL_CERTIFICATION=$($Diagnostic.RealCertification)"
}
