using System.Globalization;
﻿
﻿namespace Aiursoft.StatHub.SDK.Models;
﻿
﻿public static class DstatNumberProcessor
﻿{
﻿    public static long ParseDataSize(string number)
﻿    {
﻿        if (number.EndsWith("B"))
﻿        {
﻿            return (long)(double.Parse(number.Replace("B", ""), CultureInfo.InvariantCulture));
﻿        }
﻿        if (number.EndsWith("k"))
﻿        {
﻿            return (long)(double.Parse(number.Replace("k", ""), CultureInfo.InvariantCulture) * 1024);
﻿        }
﻿        if (number.EndsWith("M"))
﻿        {
﻿            return (long)(double.Parse(number.Replace("M", ""), CultureInfo.InvariantCulture) * 1024 * 1024);
﻿        }
﻿        if (number.EndsWith("G"))
﻿        {
﻿            return (long)(double.Parse(number.Replace("G", ""), CultureInfo.InvariantCulture) * 1024 * 1024 * 1024);
﻿        }
﻿        return long.Parse(number, CultureInfo.InvariantCulture);
﻿    }
﻿}
﻿