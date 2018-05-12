﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Zetester.Data.Models
{
    public class UserTasks
    {
        public int UserId { get; set; }

        public ApplicationUser User { get; set; }

        public int TaskId { get; set; }

        public Task Task { get; set; }

        public AccessRights UserRights { get; set; }

        public DateTime Deadline { get; set; }
    }
}
