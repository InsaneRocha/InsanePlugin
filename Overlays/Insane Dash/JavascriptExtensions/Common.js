
function isGameIRacing()
{
    return $prop('DataCorePlugin.CurrentGame') == 'IRacing';
}

function isReplayPlaying()
{
    if (isGameIRacing())
    {
        // There's a short moment when loading into a session when isReplayPlaying is false but position is -1
        const isReplayPlaying = $prop('DataCorePlugin.GameRawData.Telemetry.IsReplayPlaying');
        const position = $prop('DataCorePlugin.GameRawData.Telemetry.PlayerCarPosition');
        const trackSurface = $prop('DataCorePlugin.GameRawData.Telemetry.PlayerTrackSurface');
        return isReplayPlaying || position < 0 || trackSurface < 0;
    }
    return false;
}

function isInPitLane()
{
    return $prop('DataCorePlugin.GameData.IsInPitLane');
}

function isGameRunning()
{
    return $prop('DataCorePlugin.GameRunning');
}

function isDriving()
{
    return isGameRunning() && !isReplayPlaying();
}

function isTowing()
{
    return $prop('DataCorePlugin.GameRawData.Telemetry.PlayerCarTowTime') > 0;
}

function isRace()
{
    var sessionTypeName = $prop('DataCorePlugin.GameData.SessionTypeName');
    return String(sessionTypeName).indexOf('Race') != -1;   
}

function isQual()
{
    var sessionTypeName = $prop('DataCorePlugin.GameData.SessionTypeName');
    return String(sessionTypeName).indexOf('Qual') != -1;
}

function isPractice()
{
    var sessionTypeName = $prop('DataCorePlugin.GameData.SessionTypeName');
    return (String(sessionTypeName).indexOf('Practice') != -1) ||
           (String(sessionTypeName).indexOf('Warmup') != -1) ||
           (String(sessionTypeName).indexOf('Testing') != -1);
}

function isOffline()
{
    var sessionTypeName = $prop('DataCorePlugin.GameData.SessionTypeName');
    return String(sessionTypeName).indexOf('Offline') != -1;
}

// Returns True if the specified flag is out.
// Supported colors: 'Black', 'Blue', 'Checkered', 'Green', 'Orange', 'White', 'Yellow'.
function isFlagOut(color)
{
    var flagOut = $prop('DataCorePlugin.GameData.Flag_' + color);
    return flagOut != 0;
}

function shouldPitThisLap()
{
    var fuelRemainingLaps = $prop('DataCorePlugin.Computed.Fuel_RemainingLaps');
    var trackPercentRemaining = 1 - $prop('DataCorePlugin.GameData.TrackPositionPercent');
    var fuelAlertLaps = $prop('DataCorePlugin.GameData.CarSettings_FuelAlertLaps')
    return (fuelRemainingLaps - trackPercentRemaining) < fuelAlertLaps;
}
