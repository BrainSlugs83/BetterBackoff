using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterBackoff
{
    public enum RetryAction
    {
        /// <summary>
        /// Immediately Aborts the Retry Action, without re-throwing the Exception.
        /// </summary>
        Stop,

        /// <summary>
        /// Immediately Aborts the Retry Action, re-throwing the Exception.
        /// </summary>
        Throw,

        /// <summary>
        /// Continues the Retry Action.
        /// </summary>
        Continue
    }
}