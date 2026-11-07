# Internal adapter policy for typed game_run_start; no public launcher surface.
function Get-RimWorldHoldDecision {
    param([bool]$UserPlaytest, [bool]$Expired, [bool]$ProcessExited, [bool]$CompletionRequested)
    if ($CompletionRequested) { return 'Complete' }
    if ($ProcessExited) {
        if ($UserPlaytest) { return 'Complete' }
        return 'UnexpectedExit'
    }
    if ($Expired -and -not $UserPlaytest) { return 'Complete' }
    return 'Continue'
}
Export-ModuleMember -Function Get-RimWorldHoldDecision
