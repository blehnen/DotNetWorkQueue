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
namespace DotNetWorkQueue
{
    /// <summary>
    /// Defines a way to set and obtain extra configuration settings for transports
    /// </summary>
    public interface IConfiguration
    {
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
        /// <remarks>Expected usage is via type safe extension methods</remarks>
        /// <returns>true if the setting was found</returns>
        bool TryGetSetting(string name, out object value);
    }
}
