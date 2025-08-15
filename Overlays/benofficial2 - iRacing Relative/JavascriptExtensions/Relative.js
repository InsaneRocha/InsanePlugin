/*
    benofficial2's Official Overlays
    Copyright (C) 2023-2025 benofficial2

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

const g_UseSimHubProp = false;

function getRelativeProp(index, prop)
{
    if (index > 0)
    {
        return $prop('benofficial2.Relative.Behind' + format(index - 1, '00') + '.' + prop);
    }
    return $prop('benofficial2.Relative.Ahead' + format(Math.abs(index + 1), '00') + '.' + prop);
}

function getStandingsClassProp(classIdx, prop)
{
    return $prop('benofficial2.Standings.Class' + format(classIdx, '00') 
        + '.' + prop);
}

function getRelativeRowVisible(index)
{
    if (index == 0 || g_UseSimHubProp)
    {
        const position = getopponentleaderboardposition_aheadbehind(index);
        return position >= 0;
    }
    else
    {
        return getRelativeProp(index, 'RowVisible');
    }
}

function getRelativePositionInClass(index)
{
    let posInClass = 0;
    if (g_UseSimHubProp)
    {
        const position = getopponentleaderboardposition_aheadbehind(index);
        posInClass = driverclassposition(position);
    }
    else if (index == 0)
    {
        posInClass = isnull($prop('benofficial2.Player.LivePositionInClass'), 0);
    }
    else
    {
        posInClass = getRelativeProp(index, 'LivePositionInClass');
    }
    return posInClass > 0 ? posInClass : '';
}

function getRelativeNumber(index)
{
    let number = '';
    if (index == 0 || g_UseSimHubProp)
    {
        const position = getopponentleaderboardposition_aheadbehind(index);
        number = drivercarnumber(position);
    }
    else
    {
        number = getRelativeProp(index, 'Number');
    }
    return '#' + number;
}

function getRelativeClassColor(index)
{
    if (index == 0 || g_UseSimHubProp)
    {
        const position = getopponentleaderboardposition_aheadbehind(index);
        return drivercarclasscolor(position);
    }
    else
    {
        return getRelativeProp(index, 'ClassColor');
    }
}

function getRelativeIsOutLap(index)
{
    if (g_UseSimHubProp)
    {
        const position = getopponentleaderboardposition_aheadbehind(index);
        return driverisoutlap(position);
    }
    else if (index == 0)
    {
        return isnull($prop('benofficial2.Player.OutLap'), false);
    }
    else
    {
        return getRelativeProp(index, 'OutLap');
    }
}

function getRelativeSessionFlags(index)
{
    if (index == 0)
    {
        return isnull($prop('DataCorePlugin.GameRawData.Telemetry.SessionFlags'), 0);
    }
    else
    {
        return getRelativeProp(index, 'SessionFlags');
    }
}

function getRelativeName(index)
{
    if (index == 0 || g_UseSimHubProp)
    {
        const position = getopponentleaderboardposition_aheadbehind(index);
        return drivername(position);
    }
    else
    {
        return getRelativeProp(index, 'Name');
    }
}

function getRelativeCarBrand(index)
{
    if (index == 0)
    {
        return $prop('benofficial2.Player.CarBrand');
    }
    else
    {
        return getRelativeProp(index, 'CarBrand');
    }
}

function getRelativeCountryCode(index)
{
    if (index == 0)
    {
        return $prop('benofficial2.Player.CountryCode');
    }
    else
    {
        return getRelativeProp(index, 'CountryCode');
    }
}

function getRelativeIRating(index)
{
    let iRating;
    if (index == 0 || g_UseSimHubProp)
    {
        const position = getopponentleaderboardposition_aheadbehind(index);
        iRating = driveriracingirating(position);
    }
    else
    {
        iRating = getRelativeProp(index, 'iRating');
    }
    return formatIRating(iRating);
}

function getRelativeIRatingChange(index)
{
    if (index == 0)
    {
        return isnull($prop('benofficial2.Player.iRatingChange'), 0)
    }
    else
    {
        return isnull(getRelativeProp(index, 'iRatingChange'), 0)
    }
}

function getRelativeSafetyRating(index)
{
    let iRating;
    if (index == 0 || g_UseSimHubProp)
    {
        const position = getopponentleaderboardposition_aheadbehind(index);
        return Number(String(driverlicencestring(position)).slice(2, 6));
    }
    else
    {
        return getRelativeProp(index, 'SafetyRating');
    }
}

function getRelativeLicense(index)
{
    if (index == 0 || g_UseSimHubProp)
    {
        const position = getopponentleaderboardposition_aheadbehind(index);
        return String(driverlicencestring(position)).slice(0, -5);
    }
    else
    {
        return getRelativeProp(index, 'License');
    }
}

function getRelativeLicenseColor(index)
{
    const license = getRelativeLicense(index);
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
    return '#808085'
}

function getRelativeLicenseTextColor(index)
{
    const license = getRelativeLicense(index);
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
    return '#AEAEB0'
}

function getRelativeLicenseBackColor(index)
{
    const license = getRelativeLicense(index);
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
    return '#37373F'
}

function getRelativeGapToPlayer(index)
{
    let gap;
    if (index == 0)
    {
        return '';
    }
    else if (g_UseSimHubProp)
    {
        const position = getopponentleaderboardposition_aheadbehind(index);
        gap = driverrelativegaptoplayer(position);
    }
    else
    {
        gap = getRelativeProp(index, 'GapToPlayer');
    }
    return Math.min(999.9, Math.abs(gap).toFixed(1));
}

function getRelativeTextColor(index)
{
    if (!isRace()) return 'White';

    if (index == 0)
    {
        const highlight = isnull($prop('benofficial2.Relative.HighlightPlayerRow'), false)
        if (highlight) return 'White'
        return '#FFEBAE00'
    }

    let lap;
    if (g_UseSimHubProp)
    {
        const position = getopponentleaderboardposition_aheadbehind(index);
        lap = drivercurrentlaphighprecision(position);
    }
    else
    {
        lap = getRelativeProp(index, 'CurrentLapHighPrecision');
    }

    const playerPosition = getopponentleaderboardposition_aheadbehind(0);
    const playerLap = drivercurrentlaphighprecision(playerPosition);

    if (playerLap <= 0 || lap <= 0) return 'White';

    const red = '#FFFF6345';
    const blue = '#43B7EA';
    const white = 'White';

    let deltaLap = playerLap - lap;

    if (index < 0)
    {
        if (deltaLap < -0.75) return red;
        if (deltaLap > 0.25) return blue;
    }
    else if (index > 0)
    {
        if (deltaLap < -0.25) return red;
        if (deltaLap > 0.75) return blue;
    }
    return white;
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
