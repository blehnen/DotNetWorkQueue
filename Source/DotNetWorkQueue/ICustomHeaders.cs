// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
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

namespace DotNetWorkQueue
{
    /// <summary>
    /// Allows adding and getting custom headers
    /// </summary>
    public interface ICustomHeaders
    {
        /// <summary>
        /// Adds the specified header accessor to the collection
        /// </summary>
        /// <typeparam name="T">The type of the header data.</typeparam>
        /// <param name="name">The name.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns></returns>
        IMessageContextData<T> Add<T>(string name, T defaultValue)
           where T : class;

        /// <summary>
        /// Gets the specified header accessor from the collection
        /// </summary>
        /// <typeparam name="T">The type of the header data.</typeparam>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        IMessageContextData<T> Get<T>(string name)
           where T : class;
    }
}
