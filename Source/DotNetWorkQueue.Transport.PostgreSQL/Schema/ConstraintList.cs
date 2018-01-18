using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DotNetWorkQueue.Exceptions;

namespace DotNetWorkQueue.Transport.PostgreSQL.Schema
{
    /// <inheritdoc />
    [SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable", Justification = "Not supported by children")]
    public class ConstraintList : Dictionary<string, Constraint>
    {
        /// <summary>
        /// Adds the specified constraint
        /// </summary>
        /// <param name="constraint">The constraint.</param>
        public void Add(Constraint constraint)
        {
            if (!ContainsKey(constraint.Name))
            {
                Add(constraint.Name, constraint);
            }
            else
            {
                throw new DotNetWorkQueueException($"Duplicate constraint name {constraint.Name}");
            }
        }

        /// <summary>
        /// Removes the specified constraint
        /// </summary>
        /// <param name="constraint">The constraint.</param>
        public void Remove(Constraint constraint)
        {
            if (ContainsKey(constraint.Name))
            {
                Remove(constraint.Name);
            }
        }
    }
}
