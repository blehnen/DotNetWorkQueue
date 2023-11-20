// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------
namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Contains the script used to create a queue, if the target transport supports scripting.
    /// </summary>
    public class QueueScript
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueScript" /> class.
        /// </summary>
        /// <param name="script">The creation script</param>
        /// <param name="supported">If true, this transport supports scripting.  False otherwise</param>
        public QueueScript(string script, bool supported)
        {
            Script = script;
            Supported = supported;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueScript" /> class.
        /// </summary>
        /// <param name="script">The creation script</param>
        /// <param name="errors">The validation errors</param>
        /// <param name="supported">If true, this transport supports scripting.  False otherwise</param>
        public QueueScript(string script, string errors, bool supported)
        {
            Script = script;
            Errors = errors;
            Supported = supported;
        }

        /// <summary>
        /// The script that will create the queue
        /// </summary>
        /// <remarks>Can be null if the transport does not support scripting</remarks>
        public string Script { get; }
        /// <summary>
        /// Validation errors when generating the script
        /// </summary>
        public string Errors { get; }
        /// <summary>
        /// If true, this instance contains a creation script.
        /// </summary>
        public bool HasScript => !string.IsNullOrEmpty(Script);
        /// <summary>
        /// If true, this transport supports scripting.  False otherwise
        /// </summary>
        public bool Supported { get; }
    }
}
