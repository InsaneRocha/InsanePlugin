function isGameRunning()
{
    return isnull($prop('InsanePlugin.iRacingRunning'), false);
}

function isReplayPlaying()
{
    return isnull($prop('InsanePlugin.Session.ReplayPlaying'), false);
}

function isDriving()
{
    return isGameRunning() && !isReplayPlaying();
}

function isInPitLane()
{
    return $prop('DataCorePlugin.GameData.IsInPitLane');
}

function isRace()
{
    return isnull($prop('InsanePlugin.Session.Race'), false);
}

function isRaceStarted()
{
    return isnull($prop('InsanePlugin.Session.RaceStarted'), false);
}

function isRaceFinished()
{
    return isnull($prop('InsanePlugin.Session.RaceFinished'), false);
}

function isRaceInProgress()
{
    return isRaceStarted() && !isRaceFinished();
}

function isQual()
{
    return isnull($prop('InsanePlugin.Session.Qual'), false);
}

function isPractice()
{
    return isnull($prop('InsanePlugin.Session.Practice'), false);
}

function isOffline()
{
    return isnull($prop('InsanePlugin.Session.Offline'), false);
}

function formatSecondsToTimecode(totalSeconds) 
{
    if (totalSeconds < 0 || totalSeconds > 172800)
    {
        return '';
    }

    totalSeconds = Math.floor(totalSeconds); // Ensure it's an integer
    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = totalSeconds % 60;
    
    if (hours > 0) 
    {
        return `${hours}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
    } 
    else 
    {
        return `${minutes}:${seconds.toString().padStart(2, '0')}`;
    }
}
