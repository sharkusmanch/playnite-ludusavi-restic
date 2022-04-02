using Playnite.SDK;
using System.Collections.Generic;
using System;
using System.ComponentModel;

namespace LudusaviRestic
{
    public enum ExecutionMode
    {
        [Description("LOCLuduRestBackupExcludeMode")]
        Exclude,
        [Description("LOCLuduRestBackupIncludeMode")]
        Include
    }
}