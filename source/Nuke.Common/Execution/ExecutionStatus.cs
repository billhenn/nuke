﻿// Copyright 2019 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Linq;

namespace Nuke.Common.Execution
{
    public enum ExecutionStatus
    {
        NotRun,
        Skipped,
        Succeeded,
        Failed,
        Executing,
        Aborted,
        Collective
    }
}
