// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
namespace DotNetWorkQueue
{
    /// <summary>
    /// Defines additional data that can be attached to a user message
    /// </summary>
    public interface IAdditionalMessageData
    {
        /// <summary>
        /// Gets or sets the correlation identifier. Used to optionally track a message through a system.
        /// </summary>
        /// <value>
        /// The correlation identifier.
        /// </value>
        ICorrelationId CorrelationId { get; set; }

        /// <summary>
        /// Gets the additional meta data defined by the user.
        /// </summary>
        /// <value>
        /// The additional meta data.
        /// </value>
        List<IAdditionalMetaData> AdditionalMetaData { get; }

         /// <summary>
        /// Gets the headers.
        /// </summary>
        /// <value>
        /// The headers.
        /// </value>
        IReadOnlyDictionary<string, object> Headers { get; }

        /// <summary>
        /// Returns data set by <see cref="SetHeader{THeader}"/> 
        /// </summary>
        /// <typeparam name="THeader">data type</typeparam>
        /// <param name="property">The property.</param>
        /// <returns></returns>
        THeader GetHeader<THeader>(IMessageContextData<THeader> property)
            where THeader : class;

        /// <summary>
        /// Allows additional information to be attached to a message, that is not part of the message body.
        /// </summary>
        /// <typeparam name="THeader">data type</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        void SetHeader<THeader>(IMessageContextData<THeader> property, THeader value)
            where THeader : class;

        /// <summary>
        /// Sets a setting.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        /// <remarks>Expected usage is via type safe extension methods</remarks>
        void SetSetting(string name, object value);

        /// <summary>
        /// Tries to get a setting
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        /// <returns>true if the setting was found</returns>
        /// <remarks>Expected usage is via type safe extension methods</remarks>
        bool TryGetSetting(string name, out object value);
    }
}
