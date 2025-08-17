function getStandingsProp(classIdx, rowIdx, prop)
{
    return $prop('InsanePlugin.Standings.Class' + format(classIdx, '00') 
        + '.Row' + format(rowIdx, '00')
        + '.' + prop);
}

function getStandingsClassProp(classIdx, prop)
{
    return $prop('InsanePlugin.Standings.Class' + format(classIdx, '00') 
        + '.' + prop);
}

function isDividerVisible(classIdx)
{
    return isnull($prop('InsanePlugin.Standings.Class' + format(classIdx, '00') + '.LeadFocusedDividerVisible'), false);
}

function isLeadFocusedRow(classIdx, rowIdx)
{
    const leadFocusedRows = $prop('InsanePlugin.Standings.LeadFocusedRows');
    return isDividerVisible(classIdx) && (rowIdx < leadFocusedRows);
}

function getTireCompoundVisible(classIdx, rowIdx)
{
    const visible = isnull(getStandingsProp(classIdx, rowIdx, 'TireCompoundVisible'), false);
    if (!visible) 
        return false;

    // Hide before the race start
    if (isRace()) 
        return isRaceStarted();

    const connected = getStandingsProp(classIdx, rowIdx, 'Connected');
    return connected;
}

function getLapTimingVisible(classIdx, rowIdx)
{
    if (isRace())
    {
        if (!isRaceStarted())
            return false;

        // Wait until driver completed 1 lap before showing lap timing (best / last lap)
        const lap = isnull(getStandingsProp(classIdx, rowIdx, 'CurrentLap'), 0);
        if(lap == 1) 
            return false;
    }
    return true;
}

function getLicenseColor(classIdx, rowIdx)
{
    const license = isnull(getStandingsProp(classIdx, rowIdx, 'License'), 'R');
    if (license == 'A')
    {
        return '#006EFF'
    }
    else if (license == 'B')
    {
        return '#33CC00'
    }
    else if (license == 'C')
    {
        return '#FFCC00'
    }
    else if (license == 'D')
    {
        return '#FF6600'
    }
    else if (license == 'R')
    {
        return '#E1251B'
    }
    return 'Black'
}

function getLicenseTextColor(classIdx, rowIdx)
{
    const license = isnull(getStandingsProp(classIdx, rowIdx, 'License'), 'R');
    if (license == 'A')
    {
        return '#66A8FF'
    }
    else if (license == 'B')
    {
        return '#85E066'
    }
    else if (license == 'C')
    {
        return '#FFE066'
    }
    else if (license == 'D')
    {
        return '#FFA366'
    }
    else if (license == 'R')
    {
        return '#ED7C66'
    }
    return 'White'
}

function getLicenseBackColor(classIdx, rowIdx)
{
    const license = isnull(getStandingsProp(classIdx, rowIdx, 'License'), 'R');
    if (license == 'A')
    {
        return '#032F6F'
    }
    else if (license == 'B')
    {
        return '#175509'
    }
    else if (license == 'C')
    {
        return '#50410A'
    }
    else if (license == 'D')
    {
        return '#692C09'
    }
    else if (license == 'R')
    {
        return '#5D1214'
    }
    return 'Black'
}

function getClassColor(classIdx)
{
    return isnull(getStandingsClassProp(classIdx, 'Color'), 'White');
}

function getClassTextColor(classIdx)
{
    return isnull(getStandingsClassProp(classIdx, 'TextColor'), 'Black');
}

function getClassSof(classIdx)
{
    return isnull(getStandingsClassProp(classIdx, 'Sof'), 0);
}

function truncateToDecimal(num, decimals) 
{
  const factor = Math.pow(10, decimals)
  return (Math.floor(num * factor) / factor).toFixed(decimals)
}

function formatIRating(iRating)
{
    return truncateToDecimal(Number(iRating) / 1000, 1) + 'k';
}