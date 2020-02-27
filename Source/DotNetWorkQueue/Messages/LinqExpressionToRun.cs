// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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
using System.Collections.Generic;
using System.Linq;

namespace DotNetWorkQueue.Messages
{
    /// <summary>
    /// Defines a method to be executed; the linq expression will be compiled when consumed.
    /// </summary>
    /// <remarks>The caller must explicitly specify DLL references and using statements.</remarks>
    public class LinqExpressionToRun
    {
        private readonly int _hashCode;

        /// <summary>
        /// Initializes a new instance of the <see cref="LinqExpressionToRun"/> class.
        /// </summary>
        protected LinqExpressionToRun()
        {
            References = new List<string>();
            Usings = new List<string>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LinqExpressionToRun" /> class.
        /// </summary>
        /// <param name="linq">The linq.</param>
        /// <param name="references">The references. Add any DLL references needed by your code here</param>
        /// <param name="usingStatements">The using statements. Add any using statements needed by your code here</param>
        /// <param name="unique">if set to <c>true</c> this expression contains data that makes it likely to unique. The compiler may choose to not cache the output if this flag is true.</param>
        /// <example>
        /// The compiler needs to know about all of your references and any using statements that are required.
        /// new LinqExpressionToRun(
        /// "(message, workerNotification) =&gt; new TestClass().RunMe((IWorkerNotification)workerNotification, \"dynamic\", 2, new SomeInput(DateTime.UtcNow.ToString()))",
        /// new List{string} {"ProducerMethodTestingClasses.dll"}, //additional references
        /// new List{string} {"ProducerMethodTestingClasses.TestClasses"})); //additional using statements
        /// </example>
        public LinqExpressionToRun(string linq, IReadOnlyList<string> references = null, IReadOnlyList<string> usingStatements = null, bool unique = false) : this()
        {
            Linq = linq;
            Unique = unique;
            if(references != null)
                References = references;
            if(usingStatements != null)
                Usings = usingStatements;

            _hashCode = CalculateHashCode();
        }

        /// <summary>
        /// Gets the linq statement.
        /// </summary>
        /// <value>
        /// The linq statement.
        /// </value>
        /// <example>"(message, workerNotification) => new TestClass().RunMe((IWorkerNotification)workerNotification, \"more input\", 2, new SomeInput(DateTime.UtcNow.ToString()))"</example>
        public string Linq { get;  }

        /// <summary>
        /// Add references needed by <seealso cref="Linq"/>
        /// </summary>
        /// <value>
        /// The references.
        /// </value>
        public IReadOnlyList<string> References { get;  }
        /// <summary>
        /// Add using statements needed by <seealso cref="Linq"/>
        /// </summary>
        /// <value>
        /// The using statements
        /// </value>
        public IReadOnlyList<string> Usings { get;  }

        /// <summary>
        /// If true, this expression contains data that makes it likely to unique. The compiler may choose to not cache the output if this flag is true.
        /// </summary>
        /// <value>
        ///   <c>true</c> if unique; otherwise, <c>false</c>.
        /// </value>
        public bool Unique { get; }
        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            // Check for null values and compare run-time types.
            if (obj == null || GetType() != obj.GetType())
                return false;

            var input = (LinqExpressionToRun)obj;
            if (input.Linq != Linq) return false;

            //NOTE - elements must be in the exact same order to be a match
            //we don't actually care if they are or not, but this isn't worth changing right now
            return input.References.SequenceEqual(References) && input.Usings.SequenceEqual(Usings);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return _hashCode;
        }

        /// <summary>
        /// Calculates the hash code.
        /// </summary>
        /// <returns></returns>
        protected int CalculateHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                var hash = (int)2166136261;
                hash = (hash * 16777619) ^ Linq.GetHashCode();
                hash = (hash*16777619) ^ Unique.GetHashCode();
                hash = References.Aggregate(hash, (current, field) => (current*16777619) ^ field.GetHashCode());
                return Usings.Aggregate(hash, (current, field) => (current*16777619) ^ field.GetHashCode());
            }
        }
    }
}
