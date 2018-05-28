﻿using System;

namespace Lightest.Data.Models
{
    [Flags]
    public enum AccessRights : int
    {
        Owner = 1,
        Read = 2,
        Write = 4,
        AssignAdmin = 8
    }
}