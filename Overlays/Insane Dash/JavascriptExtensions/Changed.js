
function changed(delay, value, name) 
{
    const previousValueName = 'previousValue' + name;
    const changedTimeName = 'changedTime' + name;

    if (root[previousValueName] != null && root[previousValueName] != value)
    {
        root[changedTimeName] = Date.now();
    }

    root[previousValueName] = value;

    if (root[changedTimeName] != null)
    {
        const timeSinceChanged = Date.now() - root[changedTimeName];
        if (timeSinceChanged <= delay)
        {
            return true;
        }
    }

    return false;
}

// Returns true for the specified delay in miliseconds if the value increased
// since the last time the function was called. Must specificy a unique name
// so the function can be called multiple times for different variables.
// Ex: 
// return isincreasing(500, $prop('DataCorePlugin.GameData.CurrentLap'), 'lap')
function isincreasing(delay, value, name) 
{
    const previousValueName = 'previousValue' + name;
    const changedTimeName = 'changedTime' + name;

    if (root[previousValueName] != null && root[previousValueName] < value)
    {
        root[changedTimeName] = Date.now();
    }

    root[previousValueName] = value;

    if (root[changedTimeName] != null)
    {
        const timeSinceChanged = Date.now() - root[changedTimeName];
        if (timeSinceChanged <= delay)
        {
            return true;
        }
    }

    return false;
}

// Returns true for the specified delay in miliseconds if the value decreased
// since the last time the function was called. Must specificy a unique name
// so the function can be called multiple times for different variables.
// Ex: 
// return isdecreasing(500, $prop('DataCorePlugin.GameData.CurrentLap'), 'lap')
function isdecreasing(delay, value, name) 
{
    const previousValueName = 'previousValue' + name;
    const changedTimeName = 'changedTime' + name;

    if (root[previousValueName] != null && root[previousValueName] > value)
    {
        root[changedTimeName] = Date.now();
    }

    root[previousValueName] = value;

    if (root[changedTimeName] != null)
    {
        const timeSinceChanged = Date.now() - root[changedTimeName];
        if (timeSinceChanged <= delay)
        {
            return true;
        }
    }

    return false;
}